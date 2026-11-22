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

    private SessionLogger(string path, int seq)
    {
        Path = path;
        Id = System.IO.Path.GetFileNameWithoutExtension(path);
        _seq = seq;
        _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
    }

    public static SessionLogger Create(string dir)
    {
        Directory.CreateDirectory(dir);
        var id = $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6]}";
        return new SessionLogger(System.IO.Path.Combine(dir, id + ".jsonl"), 0);
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

    public void Dispose() { lock (_gate) _writer.Dispose(); }

    // ---- session lookup helpers ----
    public static string? ResolvePath(string dir, string idOrLatest)
    {
        if (!Directory.Exists(dir)) return null;
        if (idOrLatest == "latest")
            return new DirectoryInfo(dir).EnumerateFiles("*.jsonl").OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault()?.FullName;
        var p = System.IO.Path.Combine(dir, idOrLatest.EndsWith(".jsonl") ? idOrLatest : idOrLatest + ".jsonl");
        return File.Exists(p) ? p : null;
    }

    public static IEnumerable<(string Id, DateTime Time, string FirstUser)> List(string dir, int max = 20)
    {
        if (!Directory.Exists(dir)) yield break;
        foreach (var f in new DirectoryInfo(dir).EnumerateFiles("*.jsonl").OrderByDescending(f => f.LastWriteTimeUtc).Take(max))
        {
            string first = "";
            foreach (var line in File.ReadLines(f.FullName))
            {
                try
                {
                    using var d = JsonDocument.Parse(line);
                    if (d.RootElement.GetProperty("type").GetString() == "user") { first = d.RootElement.GetProperty("text").GetString() ?? ""; break; }
                }
                catch (JsonException) { }
            }
            yield return (System.IO.Path.GetFileNameWithoutExtension(f.Name), f.LastWriteTime, first.Length > 70 ? first[..70] + "…" : first);
        }
    }
}
