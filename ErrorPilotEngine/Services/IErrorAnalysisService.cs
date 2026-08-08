using ErrorPilotEngine.Models;

namespace ErrorPilotEngine.Services;

public interface IErrorAnalysisService
{
    Task<ErrorAnalysisReport> AnalyzeNewErrorsAsync(CancellationToken cancellationToken);
}
