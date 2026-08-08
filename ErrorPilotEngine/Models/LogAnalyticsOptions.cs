using System.ComponentModel.DataAnnotations;

namespace ErrorPilotEngine.Models;

public class LogAnalyticsOptions
{
    public const string SectionName = "LogAnalytics";

    [Required(AllowEmptyStrings = false)]
    public string WorkspaceId { get; set; } = string.Empty;

    [Range(1, 5000)]
    public int MaxNewErrorsPerQuery { get; set; } = 500;
}
