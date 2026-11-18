using AxHarness.Agent;
using AxHarness.Logging;
using Microsoft.Extensions.AI;

namespace AxHarness.Tests;

public class PromptBuilderTests
{
    [Fact]
    public void Includes_static_prompt_and_agents_md_case_insensitive()
    {
        using var t = new TempRoot();
        File.WriteAllText(Path.Combine(t.Path, "AGENTS.md"), "ALWAYS-SHOUT");
        var p = PromptBuilder.Build(t.Path, DateTimeOffset.Now);
        Assert.Contains("You are AxHarness", p);
        Assert.Contains("ALWAYS-SHOUT", p);
        Assert.Contains(t.Path, p);
    }

    [Fact]
    public void Works_without_agents_md()
    {
        using var t = new TempRoot();
        Assert.DoesNotContain("Agents.md (project", PromptBuilder.Build(t.Path, DateTimeOffset.Now));
    }
}

public class SessionLogTests
{
    [Fact]
    public void Log_roundtrips_to_messages_and_drops_orphans()
    {
        using var t = new TempRoot();
        string path;
        using (var l = SessionLogger.Create(t.Path))
        {
            path = l.Path;
            l.Log("session_start", ("root", "/r"), ("model", "m"));
            l.Log("user", ("text", "hi"));
            l.Log("assistant", ("text", "let me look"));
            l.Log("tool_call", ("callId", "c1"), ("name", "list_dir"), ("args", new Dictionary<string, object?> { ["path"] = "." }));
            l.Log("tool_result", ("callId", "c1"), ("output", "a.txt"));
            l.Log("assistant", ("text", "done"));
            l.Log("user", ("text", "again"));
            l.Log("tool_call", ("callId", "c2"), ("name", "run_command"), ("args", new Dictionary<string, object?>()));
        }
        File.AppendAllText(path, "{not json\n");

        var s = SessionLoader.Load(path);
        Assert.Equal("/r", s.Root);
        Assert.Equal(1, s.SkippedLines);
        Assert.Collection(s.Messages,
            m => Assert.Equal(ChatRole.User, m.Role),
            m => { Assert.Equal(ChatRole.Assistant, m.Role); Assert.Single(m.Contents.OfType<FunctionCallContent>()); },
            m => Assert.Equal(ChatRole.Tool, m.Role),
            m => Assert.Equal("done", m.Text),
            m => Assert.Equal("again", m.Text)); // orphan c2 call dropped
    }

    [Fact]
    public void Resume_appends_with_continuing_seq_and_never_logs_key_field()
    {
        using var t = new TempRoot();
        var l = SessionLogger.Create(t.Path);
        l.Log("user", ("text", "a")); var path = l.Path; l.Dispose();
        using var l2 = SessionLogger.Open(path);
        l2.Log("user", ("text", "b"));
        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        Assert.Contains("\"seq\":2", lines[1]);
        Assert.Equal(path, SessionLogger.ResolvePath(t.Path, "latest"));
    }
}
