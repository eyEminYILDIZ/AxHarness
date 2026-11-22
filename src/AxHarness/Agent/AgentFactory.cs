using System.ClientModel;
using AxHarness.Tools;
using AxHarness.Cli;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace AxHarness.Agent;

public static class AgentFactory
{
    public static readonly Uri OpenRouterEndpoint = new("https://openrouter.ai/api/v1");

    public static AIAgent Create(Options opt, string systemPrompt, PathGuard guard, Approval approval)
    {
        var client = new OpenAIClient(new ApiKeyCredential(opt.ApiKey), new OpenAIClientOptions { Endpoint = OpenRouterEndpoint });
        return Create(client.GetChatClient(opt.Model).AsIChatClient(), opt, systemPrompt, guard, approval);
    }

    /// <summary>Overload taking any IChatClient (used by tests with a fake client).</summary>
    public static AIAgent Create(IChatClient chat, Options opt, string systemPrompt, PathGuard guard, Approval approval)
    {
        var files = new FileTools(guard, approval);
        var shell = new ShellTool(guard, approval);
        var chatOptions = new ChatOptions
        {
            Instructions = systemPrompt,
            Tools =
            [
                AIFunctionFactory.Create(files.read_file),
                AIFunctionFactory.Create(files.write_file),
                AIFunctionFactory.Create(files.list_dir),
                AIFunctionFactory.Create(shell.run_command),
            ],
        };

        if (opt.Reasoning != null)
        {
            ReasoningEffort effort;
            bool parsed = Enum.TryParse<ReasoningEffort>(opt.Reasoning, true, out effort);

            if (parsed)
            {
                chatOptions.Reasoning = new ReasoningOptions { Effort = effort };
            }
        }

        var pipeline = chat.AsBuilder()
            .UseFunctionInvocation(null, f => f.MaximumIterationsPerRequest = opt.MaxIterations)
            .Build();

        return pipeline.AsAIAgent(new ChatClientAgentOptions { Name = "AxHarness", ChatOptions = chatOptions });
    }
}
