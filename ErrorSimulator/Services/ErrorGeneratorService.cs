namespace ErrorSimulator.Services;

public class ErrorGeneratorService : BackgroundService
{
    private const int ErrorChancePercent = 30;
    private const int MinDelaySeconds = 15;
    private const int MaxDelaySeconds = 30;

    private readonly ILogger<ErrorGeneratorService> _logger;

    public ErrorGeneratorService(ILogger<ErrorGeneratorService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ErrorGeneratorService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delaySeconds = Random.Shared.Next(MinDelaySeconds, MaxDelaySeconds + 1);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (Random.Shared.Next(1, 101) <= ErrorChancePercent)
            {
                RandomErrorFactory.CreateAndLog(_logger, nameof(ErrorGeneratorService));
            }
        }

        _logger.LogInformation("ErrorGeneratorService stopping.");
    }
}
