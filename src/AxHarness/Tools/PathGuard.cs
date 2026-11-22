namespace AxHarness.Tools;

/// <summary>Resolves model-supplied paths and rejects anything outside the root directory.</summary>
public sealed class PathGuard
{
    public string Root { get; }

    public PathGuard(string root) => Root = Path.TrimEndingDirectorySeparator(ResolveReal(Path.GetFullPath(root)));

    public string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ToolException("path is empty");
        var full = Path.GetFullPath(path, Root);
        var real = ResolveReal(full);
        if (real != Root && !real.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ToolException($"path '{path}' is outside the root directory");
        return real;
    }

    public string Relative(string full) => Path.GetRelativePath(Root, full);

    /// <summary>Follows symlinks for the deepest existing ancestor, then re-appends the non-existent tail.</summary>
    private static string ResolveReal(string full)
    {
        var tail = new Stack<string>();
        var cur = full;
        while (!Path.Exists(cur) && !IsLink(cur))
        {
            var parent = Path.GetDirectoryName(cur);
            if (parent == null) return full;
            tail.Push(Path.GetFileName(cur));
            cur = parent;
        }
        var info = new DirectoryInfo(cur);
        var target = (info.Exists ? info.ResolveLinkTarget(true) : new FileInfo(cur).ResolveLinkTarget(true))?.FullName ?? cur;
        // Resolve links in parent components too.
        target = ResolveParents(target);
        while (tail.Count > 0) target = Path.Combine(target, tail.Pop());
        return Path.TrimEndingDirectorySeparator(target);
    }

    private static bool IsLink(string p) => new FileInfo(p).LinkTarget != null;

    private static string ResolveParents(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var cur = Path.DirectorySeparatorChar.ToString();
        foreach (var part in parts)
        {
            cur = Path.Combine(cur, part);
            var t = new DirectoryInfo(cur).ResolveLinkTarget(true) ?? new FileInfo(cur).ResolveLinkTarget(true);
            if (t != null) cur = t.FullName;
        }
        return cur;
    }
}

public sealed class ToolException(string message) : Exception(message);
