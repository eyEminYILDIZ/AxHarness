using System.Text.Json;
using System.Text.Json.Nodes;

namespace AxHarness.Logging;

/// <summary>Append-only JSONL log, one event per line, flushed per event. Never receives the API key.</summary>
public sealed class SessionLogger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _gate = new();
    private int _seq;

    public string Id { get; }
    public string Path { get; }

    /// <summary>Per-session folder (<c>&lt;log dir&gt;/&lt;session-id&gt;/</c>); created lazily. Holds copies of everything attached.</summary>
    public string Dir { get; }

    public string AttachmentsDir => System.IO.Path.Combine(Dir, "attachments");

    public const string LogFileName = "session.jsonl";

    private SessionLogger(string path, int seq)
    {
        Path = path;
        Dir = System.IO.Path.GetDirectoryName(path)!;
        Id = System.IO.Path.GetFileName(Dir);
        _seq = seq;
        _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
    }

    public static SessionLogger Create(string dir)
    {
        var id = $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6]}";
        var sessionDir = System.IO.Path.Combine(dir, id);
        Directory.CreateDirectory(sessionDir);
        return new SessionLogger(System.IO.Path.Combine(sessionDir, LogFileName), 0);
    }

    public static SessionLogger Open(string path) => new(path, File.ReadLines(path).Count());

    public void Log(string type, params (string Key, object? Value)[] fields)
    {
        var obj = new JsonObject();
        lock (_gate)
        {
            obj["ts"] = DateTimeOffset.Now.ToString("o");
            obj["seq"] = ++_seq;
            obj["type"] = type;

            foreach (var (k, v) in fields)
                obj[k] = v == null ? null : JsonSerializer.SerializeToNode(v);

            _writer.WriteLine(obj.ToJsonString());
        }
    }

    /// <summary>
    /// Copies an attached file into <c>&lt;session-id&gt;/attachments/</c> as <c>NNNN_&lt;path with / replaced by __&gt;</c>
    /// (NNNN = sequence number of the first event logged for that turn, so the same file attached in two turns keeps both snapshots).
    /// Returns the saved path relative to the session folder (forward slashes).
    /// </summary>
    public string SaveAttachment(string sourceFullPath, string relativePath)
    {
        int turnSeq;
        lock (_gate)
            turnSeq = _seq + 1;
        var flat = relativePath.Replace("/", "__");
        var name = $"{turnSeq:D4}_{flat}";
        Directory.CreateDirectory(AttachmentsDir);
        File.Copy(sourceFullPath, System.IO.Path.Combine(AttachmentsDir, name), overwrite: true);
        return "attachments/" + name;
    }

    public void Dispose()
    {
        lock (_gate)
            _writer.Dispose();
    }

    // ---- session lookup helpers ----

    /// <summary>All sessions in <paramref name="dir"/> (<c>&lt;id&gt;/session.jsonl</c>), newest first.</summary>
    private static List<(string Id, string Path, DateTime Time)> Sessions(string dir)
    {
        var found = new List<(string, string, DateTime)>();
        if (!Directory.Exists(dir))
            return found;
        foreach (var d in new DirectoryInfo(dir).EnumerateDirectories())
        {
            var f = new FileInfo(System.IO.Path.Combine(d.FullName, LogFileName));
            if (f.Exists)
                found.Add((d.Name, f.FullName, f.LastWriteTime));
        }
        return found.OrderByDescending(x => x.Item3).ToList();
    }

    public static string? ResolvePath(string dir, string idOrLatest)
    {
        if (idOrLatest == "latest")
            return Sessions(dir).FirstOrDefault().Path;
        if (idOrLatest.Contains('/') || idOrLatest.Contains('\\') || idOrLatest == "..")
            return null;
        var path = System.IO.Path.Combine(dir, idOrLatest, LogFileName);
        return File.Exists(path) ? path : null;
    }

    public static IEnumerable<(string Id, DateTime Time, string FirstUser)> List(string dir, int max = 20)
    {
        foreach (var (id, path, time) in Sessions(dir).Take(max))
        {
            string first = "";
            foreach (var line in File.ReadLines(path))
            {
                try
                {
                    using var d = JsonDocument.Parse(line);
                    if (d.RootElement.GetProperty("type").GetString() == "user")
                    {
                        first = d.RootElement.GetProperty("text").GetString() ?? ""; break;
                    }
                }
                catch (JsonException) { }
            }
            yield return (id, time, first.Length > 70 ? first[..70] + "…" : first);
        }
    }
}
