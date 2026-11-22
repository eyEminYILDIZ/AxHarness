using System.ComponentModel;
using System.Text;

namespace AxHarness.Tools;

public sealed class FileTools(PathGuard guard, Approval approval)
{
    public const int MaxReadBytes = 256 * 1024;
    public const int MaxReadLines = 2000;

    [Description("Read a text file inside the project root. Returns numbered lines. Use offset/limit (1-based line offset) for big files.")]
    public string read_file(
        [Description("Path relative to the root directory")] string path,
        [Description("1-based first line to return (default 1)")] int offset = 1,
        [Description("Max lines to return (default 2000)")] int limit = MaxReadLines)
    {
        try
        {
            var full = guard.Resolve(path);
            if (!File.Exists(full))
                return $"Error: file not found: {path}";
            var len = new FileInfo(full).Length;
            var lines = File.ReadLines(full).Skip(Math.Max(offset, 1) - 1).Take(Math.Clamp(limit, 1, MaxReadLines));
            var sb = new StringBuilder();
            int n = Math.Max(offset, 1);
            bool truncated = false;
            foreach (var line in lines)
            {
                if (sb.Length > MaxReadBytes) { truncated = true; break; }
                sb.Append(n++).Append('\t').AppendLine(line);
            }
            if (truncated || (len > MaxReadBytes && sb.Length >= MaxReadBytes))
                sb.AppendLine($"[truncated at line {n - 1}; use offset to continue]");
            return sb.Length == 0 ? "(empty or offset past end of file)" : sb.ToString();
        }
        catch (Exception e) when (e is ToolException or IOException or UnauthorizedAccessException)
        {
            return "Error: " + e.Message;
        }
    }

    [Description("Create or overwrite a text file inside the project root (creates parent directories). Requires user approval.")]
    public string write_file(
        [Description("Path relative to the root directory")] string path,
        [Description("Full new file content")] string content)
    {
        try
        {
            var full = guard.Resolve(path);
            var existed = File.Exists(full);
            if (!approval.Request($"write_file {guard.Relative(full)} ({content.Length} chars, {(existed ? "overwrite" : "new file")})"))
                return "Denied: the user rejected this write.";
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
            return $"OK: wrote {content.Length} chars to {guard.Relative(full)}";
        }
        catch (Exception e) when (e is ToolException or IOException or UnauthorizedAccessException)
        {
            return "Error: " + e.Message;
        }
    }

    [Description("List entries of a directory inside the project root (directories end with '/').")]
    public string list_dir([Description("Path relative to the root directory (default '.')")] string path = ".")
    {
        try
        {
            var full = guard.Resolve(path);
            if (!Directory.Exists(full))
                return $"Error: directory not found: {path}";
            var entries = new DirectoryInfo(full).EnumerateFileSystemInfos()
                .OrderBy(e => e is not DirectoryInfo).ThenBy(e => e.Name, StringComparer.Ordinal)
                .Take(500)
                .Select(e => e is DirectoryInfo ? e.Name + "/" : e.Name);
            var text = string.Join('\n', entries);
            return text.Length == 0 ? "(empty)" : text;
        }
        catch (Exception e) when (e is ToolException or IOException or UnauthorizedAccessException)
        {
            return "Error: " + e.Message;
        }
    }
}
