using ErrorPilotEngine.Models;
using ErrorPilotEngine.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErrorPilotEngine.Controllers;

[ApiController]
[Route("api/errors")]
public class ErrorsController : ControllerBase
{
    private readonly ILogAnalyticsService _logAnalyticsService;
    private readonly IErrorAnalysisService _errorAnalysisService;

    public ErrorsController(
        ILogAnalyticsService logAnalyticsService,
        IErrorAnalysisService errorAnalysisService)
    {
        _logAnalyticsService = logAnalyticsService;
        _errorAnalysisService = errorAnalysisService;
    }

    [HttpGet("latest")]
    [ProducesResponseType(typeof(IReadOnlyList<ErrorRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetLatestErrors(CancellationToken cancellationToken)
    {
        try
        {
            var errors = await _logAnalyticsService.GetLatestErrorsAsync(cancellationToken);

            return Ok(errors);
        }
        catch (LogAnalyticsQueryException exception)
        {
            return Problem(
                title: "Unable to retrieve errors from Log Analytics",
                detail: exception.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpGet("analyzed")]
    [ProducesResponseType(typeof(ErrorAnalysisReport), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetAnalyzedErrors(CancellationToken cancellationToken)
    {
        try
        {
            var report = await _errorAnalysisService.AnalyzeNewErrorsAsync(cancellationToken);

            return Ok(report);
        }
        catch (LogAnalyticsQueryException exception)
        {
            return Problem(
                title: "Unable to retrieve errors from Log Analytics",
                detail: exception.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
