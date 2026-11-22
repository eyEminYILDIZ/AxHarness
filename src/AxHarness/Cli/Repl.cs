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
        if (history == null)
            logger.Log("system_prompt", ("text", prompt));

        PrintBanner(logger.Id);

        while (true)
        {
            Console.WriteLine();
            WriteColored("> ", ConsoleColor.Green);
            var line = Console.ReadLine();
            if (line == null)
                break;
            line = line.Trim();
            if (line.Length == 0)
                continue;

            if (line.StartsWith('/'))
            {
                switch (line.Split(' ')[0])
                {
                    case "/exit":
                    case "/quit":
                        goto done;
                    case "/help":
                        Console.WriteLine("/exit  /clear (new session)  /sessions (list logged sessions)  /help");
                        Console.WriteLine("@path  attach a file's contents to your message, e.g. fix the bugs in @src/Foo.cs  (\\@ = literal @)");
                        continue;
                    case "/sessions":
                        foreach (var s in SessionLogger.List(opt.LogDir))
                            Console.WriteLine($"{s.Id}  {s.Time:yyyy-MM-dd HH:mm}  {s.FirstUser}");
                        continue;
                    case "/clear":
                        // close current session before starting a new one
                        logger.Log("session_end", ("reason", "clear"));
                        logger.Dispose();
                        // start a new session
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

            var mentions = FileMentions.Expand(line, guard);
            if (!mentions.Ok)
            {
                foreach (var err in mentions.Errors)
                    WriteColored($"  ✗ {err}\n", ConsoleColor.Red);
                Dim("  Message not sent. Fix the @path (or write \\@ for a literal @) and try again.\n");
                continue;
            }
            foreach (var a in mentions.Attachments)
                Dim($"  📎 attached {a.Path} ({a.Lines} lines{(a.Truncated ? ", truncated" : "")})\n");

            await RunTurn(agent, session, logger, line, mentions);
        }
    done:
        logger.Log("session_end", ("reason", "exit"));
        logger.Dispose();
        return 0;
    }

    private (SessionLogger, List<ChatMessage>?) StartSession()
    {
        if (opt.Resume == null)
            return (SessionLogger.Create(opt.LogDir), null);
        var path = SessionLogger.ResolvePath(opt.LogDir, opt.Resume)
            ?? throw new InvalidOperationException($"Session '{opt.Resume}' not found in {opt.LogDir}");
        var loaded = SessionLoader.Load(path);
        if (loaded.Root != null && loaded.Root != opt.Root)
            throw new InvalidOperationException($"Session was recorded in a different root ({loaded.Root}); refusing to resume.");
        if (loaded.SkippedLines > 0)
            Console.WriteLine($"warning: skipped {loaded.SkippedLines} unreadable log line(s)");
        Console.WriteLine($"Resumed {loaded.Messages.Count} messages from {Path.GetFileName(path)}");
        return (SessionLogger.Open(path), loaded.Messages);
    }

    private static async Task<AgentSession> NewSession(AIAgent agent, List<ChatMessage>? history)
    {
        var s = await agent.CreateSessionAsync();
        if (history != null && history.Count > 0)
            s.SetInMemoryChatHistory(history);
        return s;
    }

    private async Task RunTurn(AIAgent agent, AgentSession session, SessionLogger log, string input, MentionResult mentions)
    {
        // Snapshot attachments into <session-id>/attachments/ before logging the turn (names use the turn's first seq).
        var saved = new List<string?>();
        foreach (var a in mentions.Attachments)
        {
            try
            {
                saved.Add(log.SaveAttachment(a.FullPath, a.Path));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                saved.Add(null);
                Dim($"  ! could not save a copy of {a.Path}: {e.Message}\n");
            }
        }
        // file_attached events come first: on resume they are matched to the next user event.
        for (int i = 0; i < mentions.Attachments.Count; i++)
        {
            var a = mentions.Attachments[i];
            log.Log("file_attached", ("path", a.Path), ("lines", a.Lines), ("bytes", a.Bytes), ("truncated", a.Truncated), ("saved", saved[i]));
        }
        log.Log("user", ("text", input));
        using var cts = _turn = new CancellationTokenSource();
        var text = new System.Text.StringBuilder();
        var reasoning = new System.Text.StringBuilder();
        var starts = new Dictionary<string, DateTime>();

        void FlushText()
        {
            if (reasoning.Length > 0)
            {
                log.Log("reasoning", ("text", reasoning.ToString()));
                reasoning.Clear();
            }

            if (text.Length > 0)
            {
                log.Log("assistant", ("text", text.ToString()));
                text.Clear();
            }
        }

        try
        {
            await foreach (var update in agent.RunStreamingAsync(mentions.Message, session, cancellationToken: cts.Token))
            {
                foreach (var c in update.Contents)
                {
                    switch (c)
                    {
                        case TextContent t when t.GetType() == typeof(TextContent):
                            text.Append(t.Text);
                            Console.Write(t.Text);
                            break;
                        case TextReasoningContent r:
                            reasoning.Append(r.Text);
                            break;
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
        string s;
        if (o is IDictionary<string, object?> dict)
        {
            var pairs = new List<string>();
            foreach (var kv in dict)
                pairs.Add($"{kv.Key}={kv.Value}");
            s = string.Join(", ", pairs);
        }
        else if (o != null)
            s = o.ToString() ?? "";
        else
            s = "";

        s = s.ReplaceLineEndings(" ");
        return s.Length > max ? s[..max] + "…" : s;
    }

    private static void WriteColored(string s, ConsoleColor c)
    {
        var p = Console.ForegroundColor;
        Console.ForegroundColor = c;
        Console.Write(s);
        Console.ForegroundColor = p;
    }

    private static void Field(string label, string value, ConsoleColor valueColor = ConsoleColor.White)
    {
        // ",-8" left-aligns the label and pads it to 8 characters so values line up in a column
        WriteColored($"  {label,-8}", ConsoleColor.DarkGray);
        WriteColored(value, valueColor);
        Console.WriteLine();
    }

    private void PrintBanner(string sessionId)
    {
        Console.WriteLine();
        WriteColored("  AxHarness", ConsoleColor.Cyan);
        Console.WriteLine();
        Console.WriteLine();
        Field("root", opt.Root, ConsoleColor.Blue);
        Field("model", opt.Model, ConsoleColor.Magenta);
        Field("session", sessionId, ConsoleColor.DarkYellow);
        if (PromptBuilder.FindAgentsMd(opt.Root) is { } am)
            Field("loaded", Path.GetFileName(am), ConsoleColor.Green);
        Console.WriteLine();
        WriteColored("  Type ", ConsoleColor.DarkGray);
        WriteColored("/help", ConsoleColor.Yellow);
        WriteColored(" for commands. ", ConsoleColor.DarkGray);
        WriteColored("Ctrl+C", ConsoleColor.Yellow);
        WriteColored(" cancels a running turn.", ConsoleColor.DarkGray);
        Console.WriteLine();
    }

    private static void Dim(string s)
    {
        var p = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(s);
        Console.ForegroundColor = p;
    }
}
