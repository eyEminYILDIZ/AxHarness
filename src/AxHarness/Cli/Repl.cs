using AxHarness.Agent;
using AxHarness.Logging;
using AxHarness.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AxHarness.Cli;

public sealed class Repl(Options opt)
{
    private CancellationTokenSource? _turn;

    public async Task<int> RunAsync()
    {
        var guard = new PathGuard(opt.Root);
        var approval = new Approval(opt.AutoApprove);

        Console.CancelKeyPress += (_, e) =>
        {
            CancellationTokenSource? turn = _turn;

            if (turn != null && !turn.IsCancellationRequested)
            {
                e.Cancel = true;
                turn.Cancel();
            }
        };

        var (logger, history) = StartSession();
        var prompt = PromptBuilder.Build(opt.Root, DateTimeOffset.Now);
        var agent = AgentFactory.Create(opt, prompt, guard, approval);
        var session = await NewSession(agent, history);
        logger.Log(history == null ? "session_start" : "session_resume",
            ("model", opt.Model), ("root", opt.Root), ("yes", opt.AutoApprove), ("version", "0.1"));
        if (history == null) logger.Log("system_prompt", ("text", prompt));

        Console.WriteLine($"AxHarness | root: {opt.Root} | model: {opt.Model} | session: {logger.Id}");
        if (PromptBuilder.FindAgentsMd(opt.Root) is { } am) Console.WriteLine($"Loaded {Path.GetFileName(am)}");
        Console.WriteLine("Type /help for commands. Ctrl+C cancels a running turn.");

        while (true)
        {
            Console.Write("\n> ");
            var line = Console.ReadLine();
            if (line == null) break;
            line = line.Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith('/'))
            {
                switch (line.Split(' ')[0])
                {
                    case "/exit": case "/quit": goto done;
                    case "/help":
                        Console.WriteLine("/exit  /clear (new session)  /sessions (list logged sessions)  /help");
                        continue;
                    case "/sessions":
                        foreach (var s in SessionLogger.List(opt.LogDir))
                            Console.WriteLine($"{s.Id}  {s.Time:yyyy-MM-dd HH:mm}  {s.FirstUser}");
                        continue;
                    case "/clear":
                        logger.Log("session_end", ("reason", "clear")); logger.Dispose();
                        logger = SessionLogger.Create(opt.LogDir);
                        prompt = PromptBuilder.Build(opt.Root, DateTimeOffset.Now);
                        agent = AgentFactory.Create(opt, prompt, guard, approval);
                        session = await NewSession(agent, null);
                        logger.Log("session_start", ("model", opt.Model), ("root", opt.Root), ("yes", opt.AutoApprove), ("version", "0.1"));
                        logger.Log("system_prompt", ("text", prompt));
                        Console.WriteLine($"New session: {logger.Id}");
                        continue;
                    default:
                        Console.WriteLine("Unknown command. /help");
                        continue;
                }
            }

            await RunTurn(agent, session, logger, line);
        }
    done:
        logger.Log("session_end", ("reason", "exit"));
        logger.Dispose();
        return 0;
    }

    private (SessionLogger, List<ChatMessage>?) StartSession()
    {
        if (opt.Resume == null) return (SessionLogger.Create(opt.LogDir), null);
        var path = SessionLogger.ResolvePath(opt.LogDir, opt.Resume)
            ?? throw new InvalidOperationException($"Session '{opt.Resume}' not found in {opt.LogDir}");
        var loaded = SessionLoader.Load(path);
        if (loaded.Root != null && loaded.Root != opt.Root)
            throw new InvalidOperationException($"Session was recorded in a different root ({loaded.Root}); refusing to resume.");
        if (loaded.SkippedLines > 0) Console.WriteLine($"warning: skipped {loaded.SkippedLines} unreadable log line(s)");
        Console.WriteLine($"Resumed {loaded.Messages.Count} messages from {Path.GetFileName(path)}");
        return (SessionLogger.Open(path), loaded.Messages);
    }

    private static async Task<AgentSession> NewSession(AIAgent agent, List<ChatMessage>? history)
    {
        var s = await agent.CreateSessionAsync();
        if (history is { Count: > 0 }) s.SetInMemoryChatHistory(history);
        return s;
    }

    private async Task RunTurn(AIAgent agent, AgentSession session, SessionLogger log, string input)
    {
        log.Log("user", ("text", input));
        using var cts = _turn = new CancellationTokenSource();
        var text = new System.Text.StringBuilder();
        var reasoning = new System.Text.StringBuilder();
        var starts = new Dictionary<string, DateTime>();

        void FlushText()
        {
            if (reasoning.Length > 0) { log.Log("reasoning", ("text", reasoning.ToString())); reasoning.Clear(); }
            if (text.Length > 0) { log.Log("assistant", ("text", text.ToString())); text.Clear(); }
        }

        try
        {
            await foreach (var update in agent.RunStreamingAsync(input, session, cancellationToken: cts.Token))
            {
                foreach (var c in update.Contents)
                {
                    switch (c)
                    {
                        case TextContent t when t.GetType() == typeof(TextContent):
                            text.Append(t.Text); Console.Write(t.Text); break;
                        case TextReasoningContent r:
                            reasoning.Append(r.Text); break;
                        case FunctionCallContent fc:
                            FlushText();
                            starts[fc.CallId] = DateTime.UtcNow;
                            log.Log("tool_call", ("callId", fc.CallId), ("name", fc.Name), ("args", fc.Arguments));
                            Dim($"\n  ⚙ {fc.Name}({Brief(fc.Arguments)})");
                            break;
                        case FunctionResultContent fr:
                            var output = fr.Result?.ToString() ?? "";
                            var ms = starts.TryGetValue(fr.CallId, out var st) ? (int)(DateTime.UtcNow - st).TotalMilliseconds : -1;
                            log.Log("tool_result", ("callId", fr.CallId), ("output", output), ("ms", ms));
                            Dim($"  ↳ {Brief(output, 120)}\n");
                            break;
                        case UsageContent u:
                            log.Log("usage", ("input", u.Details.InputTokenCount), ("output", u.Details.OutputTokenCount), ("total", u.Details.TotalTokenCount));
                            break;
                    }
                }
            }
            Console.WriteLine();
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n[cancelled]");
            log.Log("error", ("message", "turn cancelled by user"));
        }
        catch (Exception e)
        {
            Console.WriteLine($"\n[error] {e.Message}");
            log.Log("error", ("message", e.Message), ("kind", e.GetType().Name));
        }
        finally
        {
            FlushText();
            _turn = null;
        }
    }

    private static string Brief(object? o, int max = 100)
    {
        var s = o switch
        {
            IDictionary<string, object?> d => string.Join(", ", d.Select(kv => $"{kv.Key}={kv.Value}")),
            _ => o?.ToString() ?? "",
        };
        s = s.ReplaceLineEndings(" ");
        return s.Length > max ? s[..max] + "…" : s;
    }

    private static void Dim(string s)
    {
        var p = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(s);
        Console.ForegroundColor = p;
    }
}
