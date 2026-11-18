namespace AxHarness.Tools;

/// <summary>y / n / always prompt for dangerous tools. --yes bypasses it.</summary>
public sealed class Approval(bool autoApprove)
{
    private bool _always = autoApprove;
    public Func<string, string?> ReadLine { get; init; } = _ => Console.ReadLine();

    public bool Request(string what)
    {
        if (_always) return true;
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine();
        Console.WriteLine($"[approval] {what}");
        Console.ForegroundColor = prev;
        while (true)
        {
            var a = ReadLine("  allow? [y]es / [n]o / [a]lways: ")?.Trim().ToLowerInvariant();
            switch (a)
            {
                case "y": case "yes": return true;
                case "a": case "always": _always = true; return true;
                case "n": case "no": case null: case "": return false;
            }
        }
    }
}
