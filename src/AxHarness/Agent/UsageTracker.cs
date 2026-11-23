using System.Globalization;
using System.Text.Json;

namespace AxHarness.Agent;

/// <summary>Token usage of one session: running totals plus the size of the context after the latest model response.</summary>
public sealed class UsageTracker
{
    private long _turnInput;
    private long _turnOutput;

    public UsageTracker(long? contextLimit, bool limitAssumed = false)
    {
        ContextLimit = contextLimit;
        LimitAssumed = limitAssumed;
    }

    /// <summary>True when the limit is the built-in fallback rather than the model's real window.</summary>
    public bool LimitAssumed { get; }

    /// <summary>Context window of the model in tokens, or null when unknown.</summary>
    public long? ContextLimit { get; set; }

    public long TotalInput { get; private set; }
    public long TotalOutput { get; private set; }
    public int Requests { get; private set; }

    /// <summary>Tokens in the context after the latest response (its prompt plus its answer); 0 until a response reports usage.</summary>
    public long ContextUsed { get; private set; }

    public long TurnInput => _turnInput;
    public long TurnOutput => _turnOutput;

    public void StartTurn()
    {
        _turnInput = 0;
        _turnOutput = 0;
    }

    /// <summary>Records the usage of one model request (a turn with tool calls makes several).</summary>
    public void Add(long? input, long? output)
    {
        if (input == null && output == null)
        {
            return;
        }

        var inTokens = input ?? 0;
        var outTokens = output ?? 0;
        TotalInput += inTokens;
        TotalOutput += outTokens;
        _turnInput += inTokens;
        _turnOutput += outTokens;
        ContextUsed = inTokens + outTokens;
        Requests++;
    }

    /// <summary>Rebuilds the tracker from the <c>usage</c> events of a session log (used on --resume).</summary>
    public static UsageTracker FromLog(string path, long? contextLimit, bool limitAssumed = false)
    {
        var tracker = new UsageTracker(contextLimit, limitAssumed);

        foreach (var line in File.ReadLines(path))
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                if (root.GetProperty("type").GetString() == "usage")
                {
                    tracker.Add(Number(root, "input"), Number(root, "output"));
                }
            }
            catch (Exception e) when (e is JsonException || e is KeyNotFoundException || e is InvalidOperationException)
            {
                // Unreadable lines are already reported by SessionLoader.
            }
        }

        tracker.StartTurn();
        return tracker;
    }

    /// <summary>One line shown after each response.</summary>
    public string FormatTurn()
    {
        if (Requests == 0)
        {
            return "tokens: no usage reported by the model";
        }

        return $"tokens: context {FormatContext()} · turn in {Num(_turnInput)} out {Num(_turnOutput)} · session in {Num(TotalInput)} out {Num(TotalOutput)}";
    }

    /// <summary>Multi-line summary for /usage.</summary>
    public string FormatSession()
    {
        if (Requests == 0)
        {
            return "No token usage recorded yet.";
        }

        return $"context  {FormatContext()}\n" +
            $"input    {Num(TotalInput)}\n" +
            $"output   {Num(TotalOutput)}\n" +
            $"total    {Num(TotalInput + TotalOutput)}\n" +
            $"requests {Num(Requests)}";
    }

    private string FormatContext()
    {
        if (ContextLimit == null || ContextLimit <= 0)
        {
            return $"{Num(ContextUsed)} / unknown";
        }

        var percent = 100.0 * ContextUsed / ContextLimit.Value;
        var assumed = LimitAssumed ? " assumed" : "";
        return $"{Num(ContextUsed)} / {Num(ContextLimit.Value)}{assumed} ({percent.ToString("0.#", CultureInfo.InvariantCulture)}%)";
    }

    private static string Num(long n) => n.ToString("N0", CultureInfo.InvariantCulture);

    private static long? Number(JsonElement e, string name)
    {
        if (e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number)
        {
            return v.GetInt64();
        }

        return null;
    }
}
