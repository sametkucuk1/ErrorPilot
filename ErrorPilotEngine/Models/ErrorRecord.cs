namespace ErrorPilotEngine.Models;

public class ErrorRecord
{
    public DateTimeOffset? Timestamp { get; init; }

    public string? Type { get; init; }

    public string? Message { get; init; }

    public string? ProblemId { get; init; }

    public string? OperationName { get; init; }

    public string? CloudRoleName { get; init; }
}
