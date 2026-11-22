using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using AxHarness.Cli;
using AxHarness.Logging;
using AxHarness.Tools;
using Microsoft.Extensions.AI;
using OpenAI;

namespace AxHarness.Tests;

public class ImageMentionTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 5, 6];
    private static readonly byte[] Gif = [.. "GIF89a"u8, 1, 0, 1, 0];
    private static readonly byte[] Webp = [.. "RIFF"u8, 9, 0, 0, 0, .. "WEBP"u8, 7];

    private static (TempRoot, PathGuard) Make(params (string Name, byte[] Content)[] files)
    {
        var t = new TempRoot();
        foreach (var (n, c) in files)
        {
            var p = Path.Combine(t.Path, n);
            Directory.CreateDirectory(Path.GetDirectoryName(p)!);
            File.WriteAllBytes(p, c);
        }
        return (t, new PathGuard(t.Path));
    }

    private static byte[] Text(string s) => Encoding.UTF8.GetBytes(s);

    [Theory]
    [InlineData("a.png", "image/png")]
    [InlineData("a.jpg", "image/jpeg")]
    [InlineData("a.gif", "image/gif")]
    [InlineData("a.webp", "image/webp")]
    public void Detects_supported_formats_by_magic_bytes(string name, string mediaType)
    {
        var bytes = mediaType switch { "image/png" => Png, "image/jpeg" => Jpeg, "image/gif" => Gif, _ => Webp };
        var (t, g) = Make((name, bytes));
        using var _ = t;
        var r = FileMentions.Expand($"look at @{name}", g);

        Assert.True(r.Ok, string.Join(";", r.Errors));
        var a = r.Attachments.Single();
        Assert.Equal(AttachmentKind.Image, a.Kind);
        Assert.Equal(mediaType, a.MediaType);
        Assert.Equal(bytes, r.Images.Single().Data);
        Assert.Equal(name, r.Images.Single().Path);
        Assert.Contains($"<image path=\"{name}\" media_type=\"{mediaType}\"", r.Message);
        Assert.StartsWith("look at @" + name, r.Message);
    }

    [Fact]
    public void Chat_message_has_text_first_then_one_data_part_per_image()
    {
        var (t, g) = Make(("a.png", Png), ("b.jpg", Jpeg), ("n.txt", Text("hi\n")));
        using var _ = t;
        var msg = FileMentions.Expand("@a.png @n.txt @b.jpg", g).ToChatMessage();

        Assert.Equal(ChatRole.User, msg.Role);
        Assert.IsType<TextContent>(msg.Contents[0]);
        Assert.Contains("<file path=\"n.txt\">", msg.Text);
        var data = msg.Contents.OfType<DataContent>().ToList();
        Assert.Equal(["image/png", "image/jpeg"], data.Select(d => d.MediaType));
        Assert.Equal(Png, data[0].Data.ToArray());
    }

    [Fact]
    public void Plain_message_stays_a_single_text_part()
    {
        var (t, g) = Make(("n.txt", Text("hi\n")));
        using var _ = t;
        Assert.Single(FileMentions.Expand("@n.txt", g).ToChatMessage().Contents);
    }

    [Fact]
    public void Magic_bytes_win_over_the_extension_and_fake_images_are_rejected()
    {
        var (t, g) = Make(("pic.txt", Png), ("fake.png", Text("not an image")), ("old.bmp", [(byte)'B', (byte)'M', 0, 0]), ("a.svg", Text("<svg/>")));
        using var _ = t;

        Assert.Equal(AttachmentKind.Image, FileMentions.Expand("@pic.txt", g).Attachments.Single().Kind);

        var fake = FileMentions.Expand("@fake.png", g);
        Assert.False(fake.Ok);
        Assert.Contains("unsupported or corrupt image (supported: png, jpeg, gif, webp)", fake.Errors.Single());
        Assert.Contains("unsupported or corrupt image", FileMentions.Expand("@old.bmp", g).Errors.Single());

        Assert.Equal(AttachmentKind.Text, FileMentions.Expand("@a.svg", g).Attachments.Single().Kind); // svg is text
    }

    [Fact]
    public void Enforces_size_and_count_limits_with_clear_errors()
    {
        var big = new byte[FileMentions.MaxImageBytes + 1];
        Png.CopyTo(big, 0);
        var (t, g) = Make(("big.png", big), ("1.png", Png), ("2.png", Png), ("3.png", Png), ("4.png", Png), ("5.png", Png));
        using var _ = t;

        var r1 = FileMentions.Expand("@big.png", g);
        Assert.Equal("@big.png: image too large (5 MB; max 5 MB per image)", r1.Errors.Single());

        Assert.True(FileMentions.Expand("@1.png @2.png @3.png @4.png", g).Ok);
        var r2 = FileMentions.Expand("@1.png @2.png @3.png @4.png @5.png", g);
        Assert.False(r2.Ok);
        Assert.Contains("too many images attached (max 4)", r2.Errors.Single());
    }

    [Fact]
    public void Secret_files_are_still_refused_even_when_named_like_images()
    {
        var (t, g) = Make(("id_rsa.png", Png));
        using var _ = t;
        Assert.Contains("secrets", FileMentions.Expand("@id_rsa.png", g).Errors.Single());
    }

    private static (string Path, MentionResult Result, string Dir) LogTurn(TempRoot t, PathGuard g, string input, Action<string>? afterSave = null)
    {
        using var l = SessionLogger.Create(Path.Combine(t.Path, "logs"));
        var r = FileMentions.Expand(input, g);
        var saved = r.Attachments.Select(a => l.SaveAttachment(a.FullPath, a.Path)).ToList();
        for (int i = 0; i < saved.Count; i++)
        {
            var a = r.Attachments[i];
            l.Log("file_attached", ("path", a.Path), ("kind", a.Kind == AttachmentKind.Image ? "image" : "text"), ("mediaType", a.MediaType), ("saved", saved[i]));
        }
        l.Log("user", ("text", input));
        afterSave?.Invoke(l.Dir);
        return (l.Path, r, l.Dir);
    }

    [Fact]
    public void Resume_restores_images_from_the_saved_copies_and_the_log_has_no_image_bytes()
    {
        var (t, g) = Make(("a.png", Png), ("n.txt", Text("hi\n")));
        using var _ = t;
        var (path, r, dir) = LogTurn(t, g, "see @a.png and @n.txt");

        Assert.True(File.Exists(Path.Combine(dir, "attachments", "0001_a.png")));
        Assert.DoesNotContain(Convert.ToBase64String(Png), File.ReadAllText(path));

        var msg = SessionLoader.Load(path).Messages.Single();
        Assert.Equal(r.Message, msg.Text);
        Assert.Equal(Png, msg.Contents.OfType<DataContent>().Single().Data.ToArray());
    }

    [Fact]
    public void Resume_uses_a_placeholder_when_the_saved_image_is_gone_or_replaced()
    {
        var (t, g) = Make(("a.png", Png), ("b.png", Png));
        using var _ = t;
        var (path, _, _) = LogTurn(t, g, "@a.png @b.png", dir =>
        {
            File.Delete(Path.Combine(dir, "attachments", "0001_a.png"));
            File.WriteAllText(Path.Combine(dir, "attachments", "0001_b.png"), "now text");
        });

        var msg = SessionLoader.Load(path).Messages.Single();
        Assert.Empty(msg.Contents.OfType<DataContent>());
        Assert.Contains("<image path=\"a.png\">[attachment no longer available]</image>", msg.Text);
        Assert.Contains("<image path=\"b.png\">[attachment no longer available]</image>", msg.Text);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? Body;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Body = await request.Content!.ReadAsStringAsync(ct);
            const string json = """{"id":"x","object":"chat.completion","created":1,"model":"m","choices":[{"index":0,"finish_reason":"stop","message":{"role":"assistant","content":"ok"}}]}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }

    [Fact]
    public async Task Openai_client_sends_images_as_image_url_data_uris()
    {
        var (t, g) = Make(("a.png", Png));
        using var _ = t;
        var handler = new CaptureHandler();
        var client = new OpenAIClient(new ApiKeyCredential("k"), new OpenAIClientOptions
        {
            Endpoint = new Uri("http://localhost/v1"),
            Transport = new HttpClientPipelineTransport(new HttpClient(handler)),
        }).GetChatClient("m").AsIChatClient();

        await client.GetResponseAsync([FileMentions.Expand("what is @a.png", g).ToChatMessage()]);

        Assert.Contains("\"image_url\"", handler.Body);
        Assert.Contains("data:image/png;base64," + Convert.ToBase64String(Png), handler.Body);
    }
}
