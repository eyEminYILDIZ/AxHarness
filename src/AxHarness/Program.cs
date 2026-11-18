using AxHarness;
using AxHarness.Cli;

if (!OperatingSystem.IsLinux())
{
    Console.Error.WriteLine("AxHarness supports Linux only.");
    return 1;
}
if (args.Contains("-h") || args.Contains("--help"))
{
    Console.WriteLine(Options.Usage);
    return 0;
}

try
{
    return await new Repl(Options.Parse(args)).RunAsync();
}
catch (Exception e) when (e is ArgumentException or InvalidOperationException or FormatException)
{
    Console.Error.WriteLine(e.Message);
    return 2;
}
