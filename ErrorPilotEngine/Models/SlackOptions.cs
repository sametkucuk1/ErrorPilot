using System.ComponentModel.DataAnnotations;

namespace ErrorPilotEngine.Models;

public class SlackOptions
{
    public const string SectionName = "Slack";

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string WebhookUrl { get; set; } = string.Empty;

    [Range(5, 300)]
    public int RequestTimeoutSeconds { get; set; } = 15;
}
