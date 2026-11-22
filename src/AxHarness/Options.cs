using Microsoft.Extensions.Configuration;

namespace AxHarness;

public sealed class Options
{
    public const string DefaultModel = "nvidia/nemotron-3.5-lightning:free";

    public Options(
        string root,
        string model,
        string apiKey,
        bool autoApprove,
        int maxIterations,
        string logDir,
        string? resume,
        string? reasoning)
    {
        Root = root;
        Model = model;
        ApiKey = apiKey;
        AutoApprove = autoApprove;
        MaxIterations = maxIterations;
        LogDir = logDir;
        Resume = resume;
        Reasoning = reasoning;
    }

    // Fixed at startup.
    public string Root { get; }
    public string ApiKey { get; }
    public string LogDir { get; }
    public string? Resume { get; }

    // Can be changed during a session (e.g. by chat commands).
    public string Model { get; set; }
    public bool AutoApprove { get; set; }
    public int MaxIterations { get; set; }
    public string? Reasoning { get; set; }

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

        int i = 0;

        string Next()
        {
            if (i + 1 >= args.Length)
                throw new ArgumentException($"Missing value for {args[i]}");

            i++;
            return args[i];
        }

        for (; i < args.Length; i++)
        {
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
