using System.Reflection;
using System.Text;

namespace AxHarness.Agent;

public static class PromptBuilder
{
    public const int MaxAgentsMdChars = 40_000;

    public static string Build(string root, DateTimeOffset now)
    {
        var sb = new StringBuilder(LoadStatic());
        sb.AppendLine().AppendLine();
        sb.AppendLine("# Runtime");
        sb.AppendLine($"- Root directory: {root}");
        sb.AppendLine($"- OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        sb.AppendLine($"- Date: {now:yyyy-MM-dd}");

        var agentsMd = FindAgentsMd(root);
        if (agentsMd != null)
        {
            var text = File.ReadAllText(agentsMd);
            if (text.Length > MaxAgentsMdChars) text = text[..MaxAgentsMdChars] + "\n[Agents.md truncated]";
            sb.AppendLine().AppendLine("# Agents.md (project instructions)").AppendLine(text.TrimEnd());
        }
        return sb.ToString();
    }

    public static string? FindAgentsMd(string root) =>
        Directory.EnumerateFiles(root).FirstOrDefault(f => string.Equals(Path.GetFileName(f), "Agents.md", StringComparison.OrdinalIgnoreCase));

    private static string LoadStatic()
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames().Single(n => n.EndsWith("system.md", StringComparison.Ordinal));
        using var s = asm.GetManifestResourceStream(name)!;
        using var r = new StreamReader(s);
        return r.ReadToEnd().TrimEnd();
    }
}
