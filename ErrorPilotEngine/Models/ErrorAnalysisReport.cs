namespace ErrorPilotEngine.Models;

public class ErrorAnalysisReport
{
    public required IReadOnlyList<AnalyzedError> Errors { get; init; }

    public int TotalCount { get; init; }

    public int AnalyzedCount { get; init; }

    public bool StoppedByRateLimit { get; init; }
}
