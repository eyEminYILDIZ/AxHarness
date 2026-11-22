using AxHarness.Agent;
using AxHarness.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AxHarness.Tests;

/// <summary>Fake model: first asks for write_file, then answers with text. No network, no API key.</summary>
file sealed class ScriptedClient : IChatClient
{
    public int Calls;
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        var msg = Calls++ == 0
            ? new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("c1", "write_file",
                new Dictionary<string, object?> { ["path"] = "out.txt", ["content"] = "hello" })])
            : new ChatMessage(ChatRole.Assistant, "all done");
        return Task.FromResult(new ChatResponse(msg));
    }
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var u in (await GetResponseAsync(messages, options, ct)).ToChatResponseUpdates())
            yield return u;
    }
    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}

public class AgentLoopTests
{
    [Fact]
    public async Task Agent_runs_tool_loop_end_to_end()
    {
        using var t = new TempRoot();
        var opt = new Options(t.Path, "m", "k", true, 10, t.Path, null, null);
        var agent = AgentFactory.Create(new ScriptedClient(), opt, "sys", new PathGuard(t.Path), new Approval(true));
        var session = await agent.CreateSessionAsync();
        var resp = await agent.RunAsync("make a file", session);
        Assert.Equal("hello", File.ReadAllText(Path.Combine(t.Path, "out.txt")));
        Assert.Contains("all done", resp.Text);
    }
}
