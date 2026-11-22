using AxHarness.Cli;
using AxHarness.Logging;
using AxHarness.Tools;
using Microsoft.Extensions.AI;

namespace AxHarness.Tests;

public class FileMentionTests
{
    private static (TempRoot, PathGuard) Make(params (string Name, string Content)[] files)
    {
        var t = new TempRoot();
        foreach (var (n, c) in files)
        {
            var p = Path.Combine(t.Path, n);
            Directory.CreateDirectory(Path.GetDirectoryName(p)!);
            File.WriteAllText(p, c);
        }
        return (t, new PathGuard(t.Path));
    }

    [Fact]
    public void Expands_mention_with_numbered_lines_and_keeps_raw_text()
    {
        var (t, g) = Make(("src/a.cs", "one\ntwo\n"));
        using var _ = t;
        var r = FileMentions.Expand("fix @src/a.cs now", g);
        Assert.True(r.Ok);
        Assert.StartsWith("fix @src/a.cs now", r.Message);
        Assert.Contains("<file path=\"src/a.cs\">", r.Message);
        Assert.Contains("1\tone", r.Message);
        Assert.Contains("2\ttwo", r.Message);
        Assert.Equal(2, r.Attachments.Single().Lines);
    }

    [Fact]
    public void Ignores_emails_and_escaped_at()
    {
        var (t, g) = Make();
        using var _ = t;
        var r = FileMentions.Expand("mail me a@b.com or \\@Override", g);
        Assert.True(r.Ok);
        Assert.Empty(r.Attachments);
        Assert.Equal("mail me a@b.com or @Override", r.Message);
    }

    [Theory]
    [InlineData("see @a.txt.")]
    [InlineData("see (@a.txt)")]
    [InlineData("see @a.txt, ok")]
    public void Trims_trailing_punctuation(string input)
    {
        var (t, g) = Make(("a.txt", "x"));
        using var _ = t;
        Assert.Equal("a.txt", FileMentions.Expand(input, g).Attachments.Single().Path);
    }

    [Fact]
    public void Supports_quoted_paths_with_spaces()
    {
        var (t, g) = Make(("my file.txt", "x"));
        using var _ = t;
        Assert.Equal("my file.txt", FileMentions.Expand("read @\"my file.txt\" please", g).Attachments.Single().Path);
    }

    [Theory]
    [InlineData("@../x")]
    [InlineData("@/etc/passwd")]
    [InlineData("@missing.txt")]
    [InlineData("@dir")]
    [InlineData("@bin.dat")]
    [InlineData("@.env")]
    [InlineData("@sub/id_rsa")]
    public void Bad_mentions_abort(string input)
    {
        var (t, g) = Make((".env", "K=V"), ("sub/id_rsa", "k"));
        using var _ = t;
        Directory.CreateDirectory(Path.Combine(t.Path, "dir"));
        File.WriteAllBytes(Path.Combine(t.Path, "bin.dat"), [1, 0, 2]);
        var r = FileMentions.Expand("go " + input, g);
        Assert.False(r.Ok);
        Assert.Null(r.Context);
    }

    [Fact]
    public void Symlink_escape_is_rejected()
    {
        var (t, g) = Make();
        using var _ = t;
        using var outside = new TempRoot();
        File.WriteAllText(Path.Combine(outside.Path, "s.txt"), "secret");
        Directory.CreateSymbolicLink(Path.Combine(t.Path, "link"), outside.Path);
        Assert.False(FileMentions.Expand("@link/s.txt", g).Ok);
    }

    [Fact]
    public void Oversize_file_is_truncated_with_marker()
    {
        var (t, g) = Make(("big.txt", string.Join('\n', Enumerable.Repeat(new string('x', 100), 5000))));
        using var _ = t;
        var r = FileMentions.Expand("@big.txt", g);
        Assert.True(r.Ok);
        Assert.True(r.Attachments.Single().Truncated);
        Assert.Contains("[truncated", r.Message);
    }

    [Fact]
    public void Dedupes_and_caps_file_count()
    {
        var (t, g) = Make(Enumerable.Range(1, 6).Select(i => ($"f{i}.txt", "x")).ToArray());
        using var _ = t;
        Assert.Single(FileMentions.Expand("@f1.txt @f1.txt", g).Attachments);
        Assert.True(FileMentions.Expand("@f1.txt @f2.txt @f3.txt @f4.txt @f5.txt", g).Ok);
        Assert.False(FileMentions.Expand("@f1.txt @f2.txt @f3.txt @f4.txt @f5.txt @f6.txt", g).Ok);
    }

    private static (string Path, MentionResult Result) LogTurn(TempRoot t, PathGuard g, string input, bool deleteCopies = false)
    {
        string path;
        MentionResult r;
        using (var l = SessionLogger.Create(Path.Combine(t.Path, "logs")))
        {
            path = l.Path;
            r = FileMentions.Expand(input, g);
            var saved = r.Attachments.Select(a => l.SaveAttachment(a.FullPath, a.Path)).ToList();
            for (int i = 0; i < saved.Count; i++)
                l.Log("file_attached", ("path", r.Attachments[i].Path), ("saved", saved[i]));
            l.Log("user", ("text", input));
            if (deleteCopies)
                Directory.Delete(l.AttachmentsDir, true);
        }
        return (path, r);
    }

    [Fact]
    public void Resume_rebuilds_the_exact_message_from_saved_attachments()
    {
        var (t, g) = Make(("a.txt", "one\ntwo\n"), ("big.txt", string.Join("\n", Enumerable.Repeat("0123456789", 20000))));
        var (path, r) = LogTurn(t, g, "fix @a.txt and @big.txt");
        Assert.Contains(r.Attachments, a => a.Truncated);

        var msgs = SessionLoader.Load(path).Messages;
        Assert.Equal(r.Message, msgs.Single().Text);
        Assert.DoesNotContain("context", File.ReadAllText(path));
    }

    [Fact]
    public void Resume_uses_a_placeholder_when_the_saved_copy_is_gone()
    {
        var (t, g) = Make(("a.txt", "one\n"));
        var (path, _) = LogTurn(t, g, "see @a.txt", deleteCopies: true);

        var text = SessionLoader.Load(path).Messages.Single().Text;
        Assert.StartsWith("see @a.txt", text);
        Assert.Contains("<file path=\"a.txt\">", text);
        Assert.Contains("[attachment no longer available]", text);
    }

    [Fact]
    public void Resume_without_attachments_is_plain_text_and_ignores_unsafe_saved_paths()
    {
        using var t = new TempRoot();
        string path;
        using (var l = SessionLogger.Create(t.Path))
        {
            path = l.Path;
            l.Log("user", ("text", "plain"));
            l.Log("file_attached", ("path", "x"), ("saved", "../../../etc/passwd"));
            l.Log("user", ("text", "tricky"));
        }
        var msgs = SessionLoader.Load(path).Messages;
        Assert.Equal("plain", msgs[0].Text);
        Assert.Contains("[attachment no longer available]", msgs[1].Text);
        Assert.DoesNotContain("root:", msgs[1].Text);
    }

    [Fact]
    public void Attachments_are_copied_into_session_folder_per_turn()
    {
        var (t, g) = Make(("src/a.cs", "v1\n"));
        var logs = Path.Combine(t.Path, "logs");
        using var l = SessionLogger.Create(logs);

        var r1 = FileMentions.Expand("@src/a.cs", g);
        var saved1 = l.SaveAttachment(r1.Attachments.Single().FullPath, r1.Attachments.Single().Path);
        l.Log("user", ("text", "one"));

        File.WriteAllText(Path.Combine(t.Path, "src/a.cs"), "v2\n");
        var r2 = FileMentions.Expand("@src/a.cs", g);
        var saved2 = l.SaveAttachment(r2.Attachments.Single().FullPath, r2.Attachments.Single().Path);

        Assert.Equal(Path.Combine(logs, l.Id), l.Dir);
        Assert.Equal("attachments/0001_src__a.cs", saved1);
        Assert.Equal("attachments/0002_src__a.cs", saved2);
        Assert.Equal("v1\n", File.ReadAllText(Path.Combine(l.Dir, saved1)));
        Assert.Equal("v2\n", File.ReadAllText(Path.Combine(l.Dir, saved2)));
        Assert.Equal(Path.Combine(logs, l.Id, "session.jsonl"), l.Path);
        Assert.Equal(l.Path, SessionLogger.ResolvePath(logs, "latest"));
        Assert.Equal(l.Path, SessionLogger.ResolvePath(logs, l.Id));
    }

    [Fact]
    public void System_prompt_mentions_attached_files()
    {
        using var t = new TempRoot();
        Assert.Contains("<file path=", AxHarness.Agent.PromptBuilder.Build(t.Path, DateTimeOffset.Now));
    }
}
