using AxHarness.Logging;
using AxHarness.Tools;

namespace AxHarness.Tests;

public class ShellResolverTests
{
    private static ShellInfo Resolve(bool windows, Func<string, string?> find, string? overrideShell = null) =>
        ShellResolver.Resolve(n => n == ShellResolver.OverrideVariable ? overrideShell : null, find, windows);

    [Fact]
    public void Unix_prefers_bash_then_sh()
    {
        Assert.Equal(ShellKind.Bash, Resolve(false, n => "/x/" + n).Kind);
        Assert.Equal(ShellKind.Sh, Resolve(false, n => n == "sh" ? "/bin/sh" : null).Kind);
    }

    [Fact]
    public void Windows_prefers_pwsh_then_powershell_then_cmd()
    {
        Assert.Equal("PowerShell (pwsh)", Resolve(true, n => "C:\\" + n + ".exe").DisplayName);
        Assert.Equal("Windows PowerShell 5.1", Resolve(true, n => n == "powershell" ? "C:\\ps.exe" : null).DisplayName);
        Assert.Equal(ShellKind.Cmd, Resolve(true, _ => null).Kind);
    }

    [Fact]
    public void Windows_ignores_bash_on_path_unless_overridden()
    {
        Assert.Equal(ShellKind.PowerShell, Resolve(true, n => n is "bash" or "powershell" ? "C:\\" + n : null).Kind);
        Assert.Equal(ShellKind.Bash, Resolve(true, n => "C:\\" + n, "bash").Kind);
    }

    [Fact]
    public void Unknown_override_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => Resolve(false, n => n, "fish"));
    }

    [Fact]
    public void Hint_names_the_dialect()
    {
        Assert.Contains("PowerShell", new ShellInfo("PowerShell (pwsh)", "pwsh", ShellKind.PowerShell).Hint);
        Assert.Contains("bash", new ShellInfo("bash", "bash", ShellKind.Bash).Hint);
    }
}

public class WindowsPathRuleTests
{
    [Theory]
    [InlineData(@"\\?\C:\x")]
    [InlineData(@"\\.\pipe\x")]
    [InlineData("file.txt:stream")]
    [InlineData(@"C:\a\b.txt:evil")]
    [InlineData("nul")]
    [InlineData(@"src\CON.txt")]
    [InlineData("lpt1")]
    public void Unsafe_windows_paths_are_flagged(string path)
    {
        Assert.NotNull(PathGuard.WindowsPathProblem(path));
    }

    [Theory]
    [InlineData("src/Foo.cs")]
    [InlineData(@"C:\work\proj\a.txt")]
    [InlineData("console/readme.md")]
    [InlineData("a.b.c")]
    public void Normal_paths_pass(string path)
    {
        Assert.Null(PathGuard.WindowsPathProblem(path));
    }
}

public class PathGuardPlatformTests
{
    [Fact]
    public void Root_comparison_follows_platform_case_rules()
    {
        using var t = new TempRoot();
        var g = new PathGuard(t.Path);
        var upper = g.Root.ToUpperInvariant();
        Assert.Equal(!OperatingSystem.IsLinux(), g.IsInside(Path.Combine(upper, "a.txt")));
    }

    [Fact]
    public void Root_itself_and_children_are_inside_but_siblings_are_not()
    {
        using var t = new TempRoot();
        var g = new PathGuard(t.Path);
        Assert.True(g.IsInside(g.Root));
        Assert.True(g.IsInside(Path.Combine(g.Root, "x")));
        Assert.False(g.IsInside(g.Root + "-sibling"));
    }

    [Fact]
    public void Filesystem_root_as_guard_root_works()
    {
        var fsRoot = Path.GetPathRoot(Path.GetTempPath())!;
        var g = new PathGuard(fsRoot);
        Assert.True(g.IsInside(Path.Combine(fsRoot, "anything")));
    }
}

public class FlattenNameTests
{
    [Theory]
    [InlineData("src/a.cs", "src__a.cs")]
    [InlineData(@"src\a.cs", "src__a.cs")]
    [InlineData("a:b?.cs", "a_b_.cs")]
    public void Flattens_separators_and_illegal_chars(string input, string expected)
    {
        Assert.Equal(expected, SessionLogger.FlattenName(input));
    }
}
