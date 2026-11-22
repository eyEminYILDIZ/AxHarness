using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace AxHarness.Tools;

public sealed class ShellTool(PathGuard guard, Approval approval, ShellInfo? shellInfo = null)
{
    private readonly ShellInfo _shell = shellInfo ?? ShellResolver.Resolve();

    /// <summary>Tool description shown to the model; names the shell so it uses the right syntax.</summary>
    public string Description =>
        $"Run a shell command with the project root as working directory. Shell: {_shell.Hint}. Returns exit code plus combined stdout/stderr. Requires user approval.";

    public const int MaxOutputChars = 30_000;
    public const int DefaultTimeoutSeconds = 120;

    [Description("Run a shell command with the project root as working directory. Returns exit code plus combined stdout/stderr. Requires user approval.")]
    public async Task<string> run_command(
        [Description("The shell command line")] string command,
        [Description("Timeout in seconds (default 120, max 600)")] int timeout_seconds = DefaultTimeoutSeconds)
    {
        if (string.IsNullOrWhiteSpace(command))
            return "Error: command is empty";
        if (!approval.Request($"run_command: {command}"))
            return "Denied: the user rejected this command.";

        var psi = new ProcessStartInfo
        {
            WorkingDirectory = guard.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
        };
        _shell.Configure(psi, command);

        var output = new StringBuilder();
        var gate = new object();

        void Append(string? s)
        {
            if (s == null)
                return;
            lock (gate)
                output.AppendLine(s);
        }

        using var proc = new Process { StartInfo = psi };
        proc.OutputDataReceived += (_, e) => Append(e.Data);
        proc.ErrorDataReceived += (_, e) => Append(e.Data);

        try
        {
            proc.Start();
        }
        catch (Exception e)
        {
            return "Error: failed to start: " + e.Message;

        }

        proc.StandardInput.Close();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(timeout_seconds, 1, 600)));
        bool timedOut = false;

        try
        {
            await proc.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = true;
            try
            {
                proc.Kill(entireProcessTree: true);
            }
            catch { }

            await proc.WaitForExitAsync();
        }

        proc.WaitForExit(); // flush async readers

        string text;
        lock (gate)
            text = output.ToString();

        if (text.Length > MaxOutputChars)
        {
            var half = MaxOutputChars / 2;
            text = text[..half] + $"\n[... {text.Length - MaxOutputChars} chars truncated ...]\n" + text[^half..];
        }
        var header = timedOut ? $"[timed out after {timeout_seconds}s and was killed]" : $"[exit code {proc.ExitCode}]";
        return header + "\n" + text;
    }
}
