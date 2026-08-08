namespace ErrorPilotEngine.Models;

public class AnalyzedError
{
    public required ErrorRecord Error { get; init; }

    public AnalysisStatus Status { get; init; }

    public string? Analysis { get; init; }

    public string? FailureReason { get; init; }
}
