using System.Text.Json.Serialization;

namespace ErrorPilotEngine.Services;

internal sealed class GeminiRequest
{
    [JsonPropertyName("contents")]
    public required IReadOnlyList<GeminiContent> Contents { get; init; }

    [JsonPropertyName("generationConfig")]
    public required GeminiGenerationConfig GenerationConfig { get; init; }
}

internal sealed class GeminiContent
{
    [JsonPropertyName("parts")]
    public required IReadOnlyList<GeminiPart> Parts { get; init; }
}

internal sealed class GeminiPart
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("thought")]
    public bool Thought { get; init; }
}

internal sealed class GeminiGenerationConfig
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; init; }

    [JsonPropertyName("maxOutputTokens")]
    public int MaxOutputTokens { get; init; }
}

internal sealed class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public IReadOnlyList<GeminiCandidate>? Candidates { get; init; }
}

internal sealed class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiResponseContent? Content { get; init; }

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; init; }
}

internal sealed class GeminiResponseContent
{
    [JsonPropertyName("parts")]
    public IReadOnlyList<GeminiPart>? Parts { get; init; }
}
