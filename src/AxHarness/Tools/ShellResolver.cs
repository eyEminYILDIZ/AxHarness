namespace AxHarness.Tools;

/// <summary>
/// Picks the shell for <c>run_command</c>: bash (or sh) on Linux/macOS, PowerShell (or cmd) on Windows.
/// The env var <c>AXHARNESS_SHELL</c> (bash, sh, pwsh, powershell, cmd, or a path to one of them) overrides the choice.
/// </summary>
public static class ShellResolver
{
    public const string OverrideVariable = "AXHARNESS_SHELL";

    public static ShellInfo Resolve() => Resolve(Environment.GetEnvironmentVariable, FindOnPath, OperatingSystem.IsWindows());

    public static ShellInfo Resolve(Func<string, string?> env, Func<string, string?> find, bool windows)
    {
        var requested = env(OverrideVariable);
        if (!string.IsNullOrWhiteSpace(requested))
        {
            var name = requested.Trim();
            var kind = KindOf(name) ?? throw new InvalidOperationException(
                $"{OverrideVariable}='{name}' is not a supported shell (use bash, sh, pwsh, powershell or cmd).");
            var file = Path.IsPathRooted(name) ? name : find(name);
            if (file == null)
            {
                throw new InvalidOperationException($"{OverrideVariable}='{name}' was not found on PATH.");
            }
            return new ShellInfo(DisplayNameOf(kind, file), file, kind);
        }

        if (windows)
        {
            var pwsh = find("pwsh");
            if (pwsh != null)
            {
                return new ShellInfo("PowerShell (pwsh)", pwsh, ShellKind.PowerShell);
            }
            var ps = find("powershell");
            if (ps != null)
            {
                return new ShellInfo("Windows PowerShell 5.1", ps, ShellKind.PowerShell);
            }
            return new ShellInfo("cmd.exe", find("cmd") ?? "cmd.exe", ShellKind.Cmd);
        }

        var bash = find("bash");
        if (bash != null)
        {
            return new ShellInfo("bash", bash, ShellKind.Bash);
        }
        return new ShellInfo("sh", find("sh") ?? "/bin/sh", ShellKind.Sh);
    }

    private static ShellKind? KindOf(string nameOrPath)
    {
        var stem = Path.GetFileNameWithoutExtension(nameOrPath).ToLowerInvariant();
        return stem switch
        {
            "bash" => ShellKind.Bash,
            "sh" => ShellKind.Sh,
            "pwsh" or "powershell" => ShellKind.PowerShell,
            "cmd" => ShellKind.Cmd,
            _ => null,
        };
    }

    private static string DisplayNameOf(ShellKind kind, string file) => kind switch
    {
        ShellKind.PowerShell => "PowerShell (" + Path.GetFileNameWithoutExtension(file) + ")",
        ShellKind.Cmd => "cmd.exe",
        ShellKind.Sh => "sh",
        _ => "bash",
    };

    private static string? FindOnPath(string name)
    {
        var dirs = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        var names = OperatingSystem.IsWindows() ? new[] { name + ".exe", name } : new[] { name };
        foreach (var dir in dirs)
        {
            foreach (var n in names)
            {
                var candidate = Path.Combine(dir.Trim('"'), n);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }
        return null;
    }
}
