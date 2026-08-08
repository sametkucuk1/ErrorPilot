using System.Globalization;
using Azure;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using ErrorPilotEngine.Models;
using Microsoft.Extensions.Options;

namespace ErrorPilotEngine.Services;

public class LogAnalyticsService : ILogAnalyticsService
{
    private const string LatestErrorsQuery = "AppExceptions | order by TimeGenerated desc | take 10";

    private const string NewErrorsQueryFormat =
        "AppExceptions | where TimeGenerated > datetime({0}) | order by TimeGenerated asc | take {1}";

    private readonly LogsQueryClient _logsQueryClient;
    private readonly IErrorWatermarkStore _watermarkStore;
    private readonly LogAnalyticsOptions _options;
    private readonly ILogger<LogAnalyticsService> _logger;

    public LogAnalyticsService(
        LogsQueryClient logsQueryClient,
        IErrorWatermarkStore watermarkStore,
        IOptions<LogAnalyticsOptions> options,
        ILogger<LogAnalyticsService> logger)
    {
        _logsQueryClient = logsQueryClient;
        _watermarkStore = watermarkStore;
        _options = options.Value;
        _logger = logger;
    }

    public Task<IReadOnlyList<ErrorRecord>> GetLatestErrorsAsync(CancellationToken cancellationToken)
    {
        return RunQueryAsync(LatestErrorsQuery, cancellationToken);
    }

    public async Task<IReadOnlyList<ErrorRecord>> GetNewErrorsAsync(CancellationToken cancellationToken)
    {
        var watermark = _watermarkStore.Current;

        var query = string.Format(
            CultureInfo.InvariantCulture,
            NewErrorsQueryFormat,
            watermark.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            _options.MaxNewErrorsPerQuery);

        var errors = await RunQueryAsync(query, cancellationToken);

        AdvanceWatermark(errors, watermark);

        return errors;
    }

    private void AdvanceWatermark(IReadOnlyList<ErrorRecord> errors, DateTimeOffset watermark)
    {
        var latest = errors
            .Select(error => error.Timestamp)
            .Where(timestamp => timestamp.HasValue)
            .Max();

        if (!latest.HasValue)
        {
            return;
        }

        _watermarkStore.Advance(latest.Value);

        _logger.LogInformation(
            "Fetched {Count} new errors after {Watermark:O}. Watermark advanced to {NewWatermark:O}.",
            errors.Count,
            watermark,
            latest.Value);
    }

    private async Task<IReadOnlyList<ErrorRecord>> RunQueryAsync(
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
            Response<LogsQueryResult> response = await _logsQueryClient.QueryWorkspaceAsync(
                _options.WorkspaceId,
                query,
                QueryTimeRange.All,
                cancellationToken: cancellationToken);

            return ErrorRecordMapper.MapTable(response.Value.Table);
        }
        catch (RequestFailedException exception)
        {
            _logger.LogError(
                exception,
                "Log Analytics query failed for workspace {WorkspaceId} with status {StatusCode}.",
                _options.WorkspaceId,
                exception.Status);

            throw new LogAnalyticsQueryException(
                $"Log Analytics rejected the query (HTTP {exception.Status}): {exception.Message}",
                exception);
        }
        catch (AuthenticationFailedException exception)
        {
            _logger.LogError(exception, "Azure authentication failed while querying Log Analytics.");

            throw new LogAnalyticsQueryException(
                "Azure authentication failed. Sign in with 'az login' and try again.",
                exception);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Unexpected failure while querying Log Analytics.");

            throw new LogAnalyticsQueryException(
                "An unexpected failure occurred while querying Log Analytics.",
                exception);
        }
    }
}
