using System.Text.Json.Serialization;

namespace ErrorPilotEngine.Services;

internal sealed class SlackMessage
{
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("blocks")]
    public required IReadOnlyList<SlackBlock> Blocks { get; init; }
}

internal sealed class SlackBlock
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SlackTextObject? Text { get; init; }

    [JsonPropertyName("fields")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<SlackTextObject>? Fields { get; init; }
}

internal sealed class SlackTextObject
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
