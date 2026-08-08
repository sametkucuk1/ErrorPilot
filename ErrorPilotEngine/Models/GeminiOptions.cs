using System.ComponentModel.DataAnnotations;

namespace ErrorPilotEngine.Models;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Model { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Range(0, 30)]
    public int DelayBetweenRequestsSeconds { get; set; } = 3;

    [Range(1, 8192)]
    public int MaxOutputTokens { get; set; } = 600;

    [Range(5, 300)]
    public int RequestTimeoutSeconds { get; set; } = 100;

    [Range(0, 2)]
    public double Temperature { get; set; } = 0.2;
}
