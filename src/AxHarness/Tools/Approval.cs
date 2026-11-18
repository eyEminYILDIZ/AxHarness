namespace AxHarness.Tools;

/// <summary>yes / no prompt (also accepts "always") for dangerous tools. --yes bypasses it.</summary>
public sealed class Approval(bool autoApprove)
{
    private bool _always = autoApprove;
    public Func<string, string?> ReadLine { get; init; } = prompt =>
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(prompt);
        Console.ForegroundColor = prev;
        return Console.ReadLine();
    };

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
            var a = ReadLine("need to approval (yes/no): ")?.Trim().ToLowerInvariant();
            switch (a)
            {
                case "y": case "yes": return true;
                case "a": case "always": _always = true; return true;
                case "n": case "no": case null: case "": return false;
            }
        }
    }
}
