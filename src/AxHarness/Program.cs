using AxHarness;
using AxHarness.Cli;

internal sealed class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (!OperatingSystem.IsLinux())
        {
            Console.Error.WriteLine("AxHarness supports Linux only.");
            return ExitCodes.UnsupportedPlatform;
        }

        if (args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine(Options.Usage);
            return ExitCodes.Ok;
        }

        try
        {
            return await new Repl(Options.Parse(args)).RunAsync();
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or FormatException)
        {
            Console.Error.WriteLine(e.Message);
            return ExitCodes.BadArguments;
        }
    }
}
