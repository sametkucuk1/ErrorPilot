using ErrorPilotEngine.Models;

namespace ErrorPilotEngine.Services;

public interface ISlackNotificationService
{
    Task SendErrorAnalysisAsync(AnalyzedError analyzedError, CancellationToken cancellationToken);
}
