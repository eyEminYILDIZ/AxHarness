using System.Text.Json;

namespace AxHarness.Agent;

/// <summary>Looks up a model's context window in OpenRouter's public model list. Sends no credentials and no user data.</summary>
public static class ModelCatalog
{
    public static async Task<long?> FetchContextLengthAsync(string model, CancellationToken cancellationToken = default)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            await using var stream = await http.GetStreamAsync(new Uri(AgentFactory.OpenRouterEndpoint, "models"), cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return FindContextLength(doc.RootElement, model);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    public static long? FindContextLength(JsonElement root, string model)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var m in data.EnumerateArray())
        {
            if (m.TryGetProperty("id", out var id) && id.GetString() == model &&
                m.TryGetProperty("context_length", out var len) && len.ValueKind == JsonValueKind.Number)
            {
                return len.GetInt64();
            }
        }

        return null;
    }
}
