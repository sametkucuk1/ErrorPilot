using System.ComponentModel.DataAnnotations;

namespace ErrorPilotEngine.Models;

public class ErrorMonitorOptions
{
    public const string SectionName = "ErrorMonitor";

    public bool Enabled { get; set; } = true;

    [Range(30, 3600)]
    public int PollIntervalSeconds { get; set; } = 120;
}
