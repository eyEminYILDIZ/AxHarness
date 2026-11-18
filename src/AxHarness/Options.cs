using Microsoft.Extensions.Configuration;

namespace AxHarness;

public sealed record Options(
    string Root,
    string Model,
    string ApiKey,
    bool AutoApprove,
    int MaxIterations,
    string LogDir,
    string? Resume,
    string? Reasoning)
{
    public const string DefaultModel = "nvidia/nemotron-3.5-lightning:free";

    public static string Usage => """
        Usage: AxHarness [options]
          --model <id>          OpenRouter model id (env AXHARNESS_MODEL, default nvidia/nemotron-3.5-lightning:free)
          --yes                 Auto-approve write_file / run_command (no prompts)
          --max-iterations <n>  Max model<->tool round trips per user turn (default 40)
          --log-dir <path>      Session log dir (default <root>/.axharness/sessions)
          --resume <id|latest>  Resume a logged session
          --reasoning <level>   low | medium | high (optional)
          -h, --help
        API key: dotnet user-secrets set "OpenRouter:ApiKey" "<key>" --project <path to src/AxHarness>
                 (fallback: env OPENROUTER_API_KEY)
        """;

    public static Options Parse(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>(optional: true)
            .Build();

        string? model = null, logDir = null, resume = null, reasoning = null;
        bool yes = false;
        int maxIter = 40;

        for (int i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"Missing value for {args[i]}");
            switch (args[i])
            {
                case "--model": model = Next(); break;
                case "--yes": yes = true; break;
                case "--max-iterations": maxIter = int.Parse(Next()); break;
                case "--log-dir": logDir = Next(); break;
                case "--resume": resume = Next(); break;
                case "--reasoning": reasoning = Next(); break;
                default: throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }

        // Start directory = root directory.
        var root = Path.GetFullPath(Directory.GetCurrentDirectory());
        var apiKey = config["OpenRouter:ApiKey"] ?? config["OPENROUTER_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "Missing API key 'OpenRouter:ApiKey'. Set it with: dotnet user-secrets set \"OpenRouter:ApiKey\" \"<key>\" --project <path to src/AxHarness>");

        return new Options(
            root,
            model ?? config["AXHARNESS_MODEL"] ?? DefaultModel,
            apiKey,
            yes,
            maxIter,
            Path.GetFullPath(logDir ?? Path.Combine(root, ".axharness", "sessions"), root),
            resume,
            reasoning);
    }
}
