using System.Text.Json;
using AxHarness.Agent;
using AxHarness.Cli;

namespace AxHarness.Tests;

public class UsageTests
{
    [Fact]
    public void Tracker_sums_requests_and_reports_context_of_last_response()
    {
        var tracker = new UsageTracker(1000);
        tracker.StartTurn();
        tracker.Add(100, 20);
        tracker.Add(150, 30);

        Assert.Equal(250, tracker.TotalInput);
        Assert.Equal(50, tracker.TotalOutput);
        Assert.Equal(180, tracker.ContextUsed);
        Assert.Contains("180 / 1,000 (18%)", tracker.FormatTurn());

        tracker.StartTurn();
        Assert.Equal(0, tracker.TurnInput);
        Assert.Equal(250, tracker.TotalInput);
    }

    [Fact]
    public void Tracker_marks_assumed_limit()
    {
        var tracker = new UsageTracker(Options.DefaultContextLimit, true);
        tracker.Add(6553, 0);

        Assert.Contains("6,553 / 65,536 assumed (10%)", tracker.FormatTurn());
    }

    [Fact]
    public void Tracker_handles_unknown_limit_and_missing_usage()
    {
        var tracker = new UsageTracker(null);
        Assert.Contains("no usage", tracker.FormatTurn());

        tracker.Add(null, null);
        Assert.Equal(0, tracker.Requests);

        tracker.Add(10, 5);
        Assert.Contains("15 / unknown", tracker.FormatTurn());
    }

    [Fact]
    public void Tracker_is_rebuilt_from_session_log()
    {
        using var t = new TempRoot();
        var path = Path.Combine(t.Path, "session.jsonl");
        File.WriteAllLines(path,
        [
            """{"type":"user","text":"hi"}""",
            """{"type":"usage","input":100,"output":10,"total":110}""",
            "not json",
            """{"type":"usage","input":200,"output":null,"total":null}""",
        ]);

        var tracker = UsageTracker.FromLog(path, 4000);

        Assert.Equal(300, tracker.TotalInput);
        Assert.Equal(10, tracker.TotalOutput);
        Assert.Equal(200, tracker.ContextUsed);
        Assert.Equal(2, tracker.Requests);
    }

    [Fact]
    public void Catalog_finds_context_length_of_model()
    {
        using var doc = JsonDocument.Parse("""{"data":[{"id":"a/b","context_length":8192},{"id":"c/d","context_length":null}]}""");

        Assert.Equal(8192, ModelCatalog.FindContextLength(doc.RootElement, "a/b"));
        Assert.Null(ModelCatalog.FindContextLength(doc.RootElement, "c/d"));
        Assert.Null(ModelCatalog.FindContextLength(doc.RootElement, "missing"));
    }
}
