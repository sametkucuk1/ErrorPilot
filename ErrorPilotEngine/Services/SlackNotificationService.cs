using System.Net.Http.Json;
using ErrorPilotEngine.Models;
using Microsoft.Extensions.Options;

namespace ErrorPilotEngine.Services;

public class SlackNotificationService : ISlackNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly SlackOptions _options;
    private readonly ILogger<SlackNotificationService> _logger;

    public SlackNotificationService(
        HttpClient httpClient,
        IOptions<SlackOptions> options,
        ILogger<SlackNotificationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendErrorAnalysisAsync(AnalyzedError analyzedError, CancellationToken cancellationToken)
    {
        var problemId = analyzedError.Error.ProblemId;

        try
        {
            var payload = SlackMessageBuilder.Build(analyzedError);

            using var response = await _httpClient.PostAsJsonAsync(
                _options.WebhookUrl,
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogError(
                    "Slack webhook returned HTTP {StatusCode} for error {ProblemId}: {Response}",
                    (int)response.StatusCode,
                    problemId,
                    responseBody);

                return;
            }

            _logger.LogInformation("Slack notification sent for error {ProblemId}.", problemId);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Slack webhook request timed out for error {ProblemId}.", problemId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "Slack notification could not be delivered for error {ProblemId}.",
                problemId);
        }
    }
}
