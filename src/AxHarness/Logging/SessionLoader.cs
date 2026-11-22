using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AxHarness.Logging;

public sealed record LoadedSession(List<ChatMessage> Messages, string? Root, string? Model, int SkippedLines);

/// <summary>Rebuilds chat history (no system prompt) from a session JSONL file.</summary>
public static class SessionLoader
{
    public static LoadedSession Load(string path)
    {
        var msgs = new List<ChatMessage>();
        string? root = null, model = null;
        int skipped = 0;
        ChatMessage? asst = null;

        void CloseAssistant()
        {
            if (asst != null && asst.Contents.Count > 0)
                msgs.Add(asst);
            asst = null;
        }

        foreach (var line in File.ReadLines(path))
        {
            JsonElement e;
            try
            {
                e = JsonDocument.Parse(line).RootElement;
            }
            catch (JsonException)
            {
                skipped++; continue;
            }

            if (!e.TryGetProperty("type", out var t))
            {
                skipped++; continue;
            }

            switch (t.GetString())
            {
                case "session_start":
                    root = Str(e, "root");
                    model = Str(e, "model");
                    break;
                case "user":
                    CloseAssistant();
                    msgs.Add(new ChatMessage(ChatRole.User, Str(e, "text") ?? ""));
                    break;
                case "assistant":
                    asst ??= new ChatMessage(ChatRole.Assistant, []);
                    asst.Contents.Add(new TextContent(Str(e, "text") ?? ""));
                    break;
                case "tool_call":
                    {
                        asst ??= new ChatMessage(ChatRole.Assistant, []);
                        IDictionary<string, object?>? args = null;
                        if (e.TryGetProperty("args", out var a) && a.ValueKind == JsonValueKind.Object)
                            args = a.EnumerateObject().ToDictionary(p => p.Name, p => (object?)p.Value.Clone());
                        asst.Contents.Add(new FunctionCallContent(Str(e, "callId") ?? "", Str(e, "name") ?? "", args));
                        break;
                    }
                case "tool_result":
                    CloseAssistant();
                    msgs.Add(new ChatMessage(ChatRole.Tool, [new FunctionResultContent(Str(e, "callId") ?? "", Str(e, "output") ?? "")]));
                    break;
            }
        }
        CloseAssistant();
        DropOrphanCalls(msgs);
        return new LoadedSession(msgs, root, model, skipped);
    }

    /// <summary>Remove tool calls without a result (e.g. crash mid-turn) and results without a call.</summary>
    private static void DropOrphanCalls(List<ChatMessage> msgs)
    {
        var results = msgs.SelectMany(m => m.Contents).OfType<FunctionResultContent>().Select(r => r.CallId).ToHashSet();
        var calls = msgs.SelectMany(m => m.Contents).OfType<FunctionCallContent>().Select(c => c.CallId).ToHashSet();
        foreach (var m in msgs)
            for (int i = m.Contents.Count - 1; i >= 0; i--)
                if ((m.Contents[i] is FunctionCallContent c && !results.Contains(c.CallId)) ||
                    (m.Contents[i] is FunctionResultContent r && !calls.Contains(r.CallId)) ||
                    (m.Contents[i] is TextContent { Text: "" } && m.Role == ChatRole.Assistant && m.Contents.Count > 1))
                    m.Contents.RemoveAt(i);
        msgs.RemoveAll(m => m.Contents.Count == 0);
    }

    private static string? Str(JsonElement e, string name)
    {
        if (e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String)
        {
            return v.GetString();
        }
        else
        {
            return null;
        }
    }
}
