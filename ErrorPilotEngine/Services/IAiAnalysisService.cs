using ErrorPilotEngine.Models;

namespace ErrorPilotEngine.Services;

public interface IAiAnalysisService
{
    Task<string> AnalyzeErrorAsync(ErrorRecord error, CancellationToken cancellationToken);
}
