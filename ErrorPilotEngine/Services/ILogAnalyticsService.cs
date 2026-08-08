using ErrorPilotEngine.Models;

namespace ErrorPilotEngine.Services;

public interface ILogAnalyticsService
{
    Task<IReadOnlyList<ErrorRecord>> GetLatestErrorsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ErrorRecord>> GetNewErrorsAsync(CancellationToken cancellationToken);
}
