using System.Diagnostics;

namespace AxHarness.Tools;

/// <summary>A shell that <c>run_command</c> can use: how to start it and how to describe it to the model.</summary>
public sealed record ShellInfo(string DisplayName, string FileName, ShellKind Kind)
{
    private const string PowerShellPreamble =
        "$ProgressPreference='SilentlyContinue'; [Console]::OutputEncoding=[System.Text.Encoding]::UTF8; ";

    /// <summary>One line for the system prompt and the tool description, so the model writes commands in the right dialect.</summary>
    public string Hint => Kind switch
    {
        ShellKind.PowerShell => $"{DisplayName} (use PowerShell syntax; chain with ';', not '&&'; Unix tools like grep/find/cat are not available)",
        ShellKind.Cmd => $"{DisplayName} (use cmd.exe syntax, e.g. dir, type, findstr; chain with '&')",
        ShellKind.Sh => $"{DisplayName} (POSIX sh syntax)",
        _ => $"{DisplayName} (bash syntax)",
    };

    public void Configure(ProcessStartInfo psi, string command)
    {
        psi.FileName = FileName;
        switch (Kind)
        {
            case ShellKind.PowerShell:
                psi.ArgumentList.Add("-NoProfile");
                psi.ArgumentList.Add("-NonInteractive");
                psi.ArgumentList.Add("-Command");
                psi.ArgumentList.Add(PowerShellPreamble + command);
                break;
            case ShellKind.Cmd:
                // cmd.exe does not understand the escaping ArgumentList applies, so pass the command line verbatim.
                psi.Arguments = $"/d /s /c \"{command}\"";
                break;
            default:
                psi.ArgumentList.Add("-c");
                psi.ArgumentList.Add(command);
                break;
        }
    }
}
