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

    [Fact]
    public void Session_log_context_roundtrips_and_legacy_events_load()
    {
        using var t = new TempRoot();
        string path;
        using (var l = SessionLogger.Create(t.Path))
        {
            path = l.Path;
            l.Log("user", ("text", "legacy"));
            l.Log("user", ("text", "fix @a.txt"), ("context", "<file path=\"a.txt\">\n1\tx\n</file>"));
        }
        var msgs = SessionLoader.Load(path).Messages;
        Assert.Equal("legacy", msgs[0].Text);
        Assert.Contains("<file path=\"a.txt\">", msgs[1].Text);
        Assert.StartsWith("fix @a.txt", msgs[1].Text);
    }

    [Fact]
    public void System_prompt_mentions_attached_files()
    {
        using var t = new TempRoot();
        Assert.Contains("<file path=", AxHarness.Agent.PromptBuilder.Build(t.Path, DateTimeOffset.Now));
    }
}
