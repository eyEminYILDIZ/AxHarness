using AxHarness;
using AxHarness.Cli;

internal sealed class Program
{
    private static async Task<int> Main(string[] args)
    {
        UseUtf8Console();

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

    private static void UseUtf8Console()
    {
        try
        {
            var utf8 = new System.Text.UTF8Encoding(false);
            Console.OutputEncoding = utf8;
            Console.InputEncoding = utf8;
        }
        catch (IOException)
        {
            // No console attached (redirected or detached); the default encoding is fine.
        }
    }
}
