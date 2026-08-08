using ErrorPilotEngine.Models;
using Microsoft.Extensions.Options;

namespace ErrorPilotEngine.Services;

public class ErrorMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ErrorMonitorOptions _options;
    private readonly ILogger<ErrorMonitorService> _logger;

    public ErrorMonitorService(
        IServiceScopeFactory scopeFactory,
        IOptions<ErrorMonitorOptions> options,
        ILogger<ErrorMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ErrorMonitorService started with a {PollIntervalSeconds}s poll interval.",
            _options.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.PollIntervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunCycleAsync(stoppingToken);
        }

        _logger.LogInformation("ErrorMonitorService stopping.");
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var errorAnalysisService = scope.ServiceProvider
                .GetRequiredService<IErrorAnalysisService>();

            var report = await errorAnalysisService.AnalyzeNewErrorsAsync(cancellationToken);

            if (report.TotalCount == 0)
            {
                _logger.LogInformation("No new errors found.");

                return;
            }

            _logger.LogInformation(
                "Processed {TotalCount} new errors, {AnalyzedCount} analyzed.",
                report.TotalCount,
                report.AnalyzedCount);
        }
        catch (LogAnalyticsQueryException exception)
        {
            _logger.LogError(exception, "Poll cycle skipped because the Log Analytics query failed.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Poll cycle failed unexpectedly.");
        }
    }
}
