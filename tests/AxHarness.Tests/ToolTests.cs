using AxHarness.Tools;

namespace AxHarness.Tests;

public sealed class TempRoot : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("axh").FullName;
    public void Dispose() => Directory.Delete(Path, true);
}

public class PathGuardTests
{
    [Fact]
    public void Allows_inside_and_rejects_outside()
    {
        using var t = new TempRoot();
        var g = new PathGuard(t.Path);
        Assert.StartsWith(g.Root, g.Resolve("a/b.txt"));
        Assert.Throws<ToolException>(() => g.Resolve("../x"));
        Assert.Throws<ToolException>(() => g.Resolve(Path.Combine(Path.GetPathRoot(t.Path)!, "etc", "passwd")));
        Assert.Throws<ToolException>(() => g.Resolve(""));
    }

    [Fact]
    public void Rejects_symlink_escape()
    {
        using var t = new TempRoot();
        using var outside = new TempRoot();
        File.WriteAllText(Path.Combine(outside.Path, "s.txt"), "secret");
        if (!TestSupport.TryCreateDirectorySymlink(Path.Combine(t.Path, "link"), outside.Path))
        {
            return;
        }
        var g = new PathGuard(t.Path);
        Assert.Throws<ToolException>(() => g.Resolve("link/s.txt"));
        Assert.Throws<ToolException>(() => g.Resolve("link/new.txt"));
    }
}

public class FileToolTests
{
    private static (FileTools, Approval) Make(string root, bool yes = true, string answer = "n")
    {
        var a = new Approval(yes) { ReadLine = _ => answer };
        return (new FileTools(new PathGuard(root), a), a);
    }

    [Fact]
    public void Write_then_read_numbered()
    {
        using var t = new TempRoot();
        var (f, _) = Make(t.Path);
        Assert.StartsWith("OK", f.write_file("sub/a.txt", "one\ntwo\nthree"));
        var r = f.read_file("sub/a.txt", offset: 2, limit: 1);
        Assert.Equal("2\ttwo\n", r.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Write_denied_does_not_create_file()
    {
        using var t = new TempRoot();
        var (f, _) = Make(t.Path, yes: false, answer: "n");
        Assert.StartsWith("Denied", f.write_file("a.txt", "x"));
        Assert.False(File.Exists(Path.Combine(t.Path, "a.txt")));
    }

    [Fact]
    public void Outside_root_returns_error()
    {
        using var t = new TempRoot();
        var (f, _) = Make(t.Path);
        Assert.StartsWith("Error", f.read_file("../../etc/passwd"));
        Assert.StartsWith("Error", f.write_file(Path.Combine(Path.GetTempPath(), "axh-evil.txt"), "x"));
    }

    [Fact]
    public void List_dir_marks_directories()
    {
        using var t = new TempRoot();
        Directory.CreateDirectory(Path.Combine(t.Path, "d"));
        File.WriteAllText(Path.Combine(t.Path, "f.txt"), "");
        var (f, _) = Make(t.Path);
        Assert.Equal("d/\nf.txt", f.list_dir().ReplaceLineEndings("\n"));
    }
}

public class ShellToolTests
{
    [Fact]
    public async Task Runs_in_root_and_reports_exit_code()
    {
        using var t = new TempRoot();
        var s = new ShellTool(new PathGuard(t.Path), new Approval(true));
        var r = await s.run_command(OperatingSystem.IsWindows() ? "(Get-Location).Path; exit 3" : "pwd; exit 3");
        Assert.Contains("[exit code 3]", r);
        Assert.Contains(new PathGuard(t.Path).Root, r, PathGuard.Comparison);
    }

    [Fact]
    public async Task Timeout_kills_process()
    {
        using var t = new TempRoot();
        var s = new ShellTool(new PathGuard(t.Path), new Approval(true));
        var r = await s.run_command(OperatingSystem.IsWindows() ? "Start-Sleep -Seconds 30" : "sleep 30", timeout_seconds: 1);
        Assert.Contains("timed out", r);
    }

    [Fact]
    public async Task Runs_command_with_quotes_and_unicode()
    {
        using var t = new TempRoot();
        var s = new ShellTool(new PathGuard(t.Path), new Approval(true));
        var r = await s.run_command("echo \"héllo wörld\"");
        Assert.Contains("[exit code 0]", r);
        Assert.Contains("héllo wörld", r);
    }

    [Fact]
    public async Task Denied_does_not_run()
    {
        using var t = new TempRoot();
        var s = new ShellTool(new PathGuard(t.Path), new Approval(false) { ReadLine = _ => "n" });
        var r = await s.run_command(OperatingSystem.IsWindows() ? "New-Item x -ItemType File" : "touch x");
        Assert.StartsWith("Denied", r);
        Assert.False(File.Exists(Path.Combine(t.Path, "x")));
    }
}
