namespace AxHarness.Tools;

/// <summary>Resolves model-supplied paths and rejects anything outside the root directory.</summary>
public sealed class PathGuard
{
    private static readonly string[] ReservedDeviceNames =
        ["CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
         "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"];

    private readonly string _rootWithSeparator;

    /// <summary>Windows and (by default) macOS file systems ignore case, so paths must be compared that way there.</summary>
    public static StringComparison Comparison { get; } =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static StringComparer Comparer { get; } =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public string Root { get; }

    public PathGuard(string root)
    {
        Root = Path.TrimEndingDirectorySeparator(ResolveReal(Path.GetFullPath(root)));
        _rootWithSeparator = WithTrailingSeparator(Root);
    }

    public string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ToolException("path is empty");
        }
        if (OperatingSystem.IsWindows() && WindowsPathProblem(path) is { } problem)
        {
            throw new ToolException($"path '{path}' is not allowed: {problem}");
        }

        var full = Path.GetFullPath(path, Root);
        var real = ResolveReal(full);
        if (!IsInside(real))
        {
            throw new ToolException($"path '{path}' is outside the root directory");
        }
        return real;
    }

    /// <summary>True when <paramref name="fullPath"/> is the root or lies below it (using the platform's case rules).</summary>
    public bool IsInside(string fullPath) =>
        string.Equals(Path.TrimEndingDirectorySeparator(fullPath), Root, Comparison)
        || fullPath.StartsWith(_rootWithSeparator, Comparison);

    public string Relative(string full) => Path.GetRelativePath(Root, full);

    /// <summary>
    /// Describes why a path is unsafe on Windows (device paths, reserved device names, NTFS alternate data streams), or null when it is fine.
    /// Pure string logic, so it can be tested on any OS.
    /// </summary>
    public static string? WindowsPathProblem(string path)
    {
        if (path.StartsWith(@"\\?\", StringComparison.Ordinal) || path.StartsWith(@"\\.\", StringComparison.Ordinal)
            || path.StartsWith("//?/", StringComparison.Ordinal) || path.StartsWith("//./", StringComparison.Ordinal))
        {
            return "device paths are not supported";
        }

        var rest = path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':' ? path[2..] : path;
        if (rest.Contains(':'))
        {
            return "':' is only allowed in a drive prefix (alternate data streams are blocked)";
        }

        foreach (var segment in rest.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries))
        {
            var name = segment.TrimEnd(' ', '.');
            var dot = name.IndexOf('.');
            var stem = dot >= 0 ? name[..dot] : name;
            if (Array.Exists(ReservedDeviceNames, d => string.Equals(d, stem.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)))
            {
                return $"'{segment}' is a reserved device name";
            }
        }
        return null;
    }

    private static string WithTrailingSeparator(string path) =>
        Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar;

    /// <summary>Follows symlinks for the deepest existing ancestor, then re-appends the non-existent tail.</summary>
    private static string ResolveReal(string full)
    {
        var tail = new Stack<string>();
        var cur = full;
        while (!Path.Exists(cur) && !IsLink(cur))
        {
            var parent = Path.GetDirectoryName(cur);
            if (parent == null)
            {
                return full;
            }
            tail.Push(Path.GetFileName(cur));
            cur = parent;
        }
        var info = new DirectoryInfo(cur);
        var target = (info.Exists ? info.ResolveLinkTarget(true) : new FileInfo(cur).ResolveLinkTarget(true))?.FullName ?? cur;
        // Resolve links in parent components too.
        target = ResolveParents(target);
        while (tail.Count > 0)
        {
            target = Path.Combine(target, tail.Pop());
        }
        return Path.TrimEndingDirectorySeparator(target);
    }

    private static bool IsLink(string p) => new FileInfo(p).LinkTarget != null;

    private static string ResolveParents(string path)
    {
        // Start at the real root ("/", "C:\" or "\\server\share\") instead of assuming a Unix root.
        var pathRoot = Path.GetPathRoot(path) ?? "";
        var parts = path[pathRoot.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var cur = pathRoot;
        foreach (var part in parts)
        {
            cur = Path.Combine(cur, part);
            var t = new DirectoryInfo(cur).ResolveLinkTarget(true) ?? new FileInfo(cur).ResolveLinkTarget(true);
            if (t != null)
            {
                cur = t.FullName;
            }
        }
        return cur;
    }
}

public sealed class ToolException(string message) : Exception(message);
