using ErrorPilotEngine.Models;
using Microsoft.Extensions.Options;

namespace ErrorPilotEngine.Services;

public class ErrorAnalysisService : IErrorAnalysisService
{
    private const string RateLimitSkipReason =
        "Skipped because the Gemini rate limit was reached earlier in this run.";

    private readonly ILogAnalyticsService _logAnalyticsService;
    private readonly IAiAnalysisService _aiAnalysisService;
    private readonly ISlackNotificationService _slackNotificationService;
    private readonly GeminiOptions _options;
    private readonly ILogger<ErrorAnalysisService> _logger;

    public ErrorAnalysisService(
        ILogAnalyticsService logAnalyticsService,
        IAiAnalysisService aiAnalysisService,
        ISlackNotificationService slackNotificationService,
        IOptions<GeminiOptions> options,
        ILogger<ErrorAnalysisService> logger)
    {
        _logAnalyticsService = logAnalyticsService;
        _aiAnalysisService = aiAnalysisService;
        _slackNotificationService = slackNotificationService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ErrorAnalysisReport> AnalyzeNewErrorsAsync(CancellationToken cancellationToken)
    {
        var errors = await _logAnalyticsService.GetNewErrorsAsync(cancellationToken);
        var results = new List<AnalyzedError>(errors.Count);
        var rateLimitReached = false;

        for (var index = 0; index < errors.Count; index++)
        {
            var error = errors[index];

            // Gemini'nin ücretsiz kotası dolduğunda tekrar denemek anlamsız; kalan hataları
            // "atlandı" olarak işaretleyip raporda gösteriyoruz ki hangileri işlenmedi belli olsun.
            if (rateLimitReached)
            {
                results.Add(CreateSkippedResult(error));
                continue;
            }

            // İstekler arasında kısa bir bekleme, dakikalık istek limitine takılmayı geciktiriyor.
            if (index > 0)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.DelayBetweenRequestsSeconds),
                    cancellationToken);
            }

            try
            {
                var analysis = await _aiAnalysisService.AnalyzeErrorAsync(error, cancellationToken);

                results.Add(new AnalyzedError
                {
                    Error = error,
                    Status = AnalysisStatus.Analyzed,
                    Analysis = analysis,
                });
            }
            catch (AiRateLimitExceededException exception)
            {
                _logger.LogError(
                    exception,
                    "Gemini rate limit reached after {AnalyzedCount} analyzed errors. Remaining errors will be skipped.",
                    results.Count(result => result.Status == AnalysisStatus.Analyzed));

                rateLimitReached = true;

                results.Add(new AnalyzedError
                {
                    Error = error,
                    Status = AnalysisStatus.Skipped,
                    FailureReason = exception.Message,
                });
            }
            catch (AiAnalysisException exception)
            {
                _logger.LogError(
                    exception,
                    "Gemini analysis failed for error {ProblemId}.",
                    error.ProblemId);

                results.Add(new AnalyzedError
                {
                    Error = error,
                    Status = AnalysisStatus.Failed,
                    FailureReason = exception.Message,
                });
            }
        }

        await NotifyAnalyzedErrorsAsync(results, cancellationToken);

        return new ErrorAnalysisReport
        {
            Errors = results,
            TotalCount = results.Count,
            AnalyzedCount = results.Count(result => result.Status == AnalysisStatus.Analyzed),
            StoppedByRateLimit = rateLimitReached,
        };
    }

    private async Task NotifyAnalyzedErrorsAsync(
        IReadOnlyList<AnalyzedError> results,
        CancellationToken cancellationToken)
    {
        foreach (var result in results.Where(result => result.Status == AnalysisStatus.Analyzed))
        {
            await _slackNotificationService.SendErrorAnalysisAsync(result, cancellationToken);
        }
    }

    private static AnalyzedError CreateSkippedResult(ErrorRecord error)
    {
        return new AnalyzedError
        {
            Error = error,
            Status = AnalysisStatus.Skipped,
            FailureReason = RateLimitSkipReason,
        };
    }
}
