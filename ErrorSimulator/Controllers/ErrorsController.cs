using ErrorSimulator.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErrorSimulator.Controllers;

[ApiController]
[Route("api/errors")]
public class ErrorsController : ControllerBase
{
    private readonly ILogger<ErrorsController> _logger;

    public ErrorsController(ILogger<ErrorsController> logger)
    {
        _logger = logger;
    }

    [HttpGet("trigger")]
    public IActionResult Trigger()
    {
        var exception = RandomErrorFactory.CreateAndLog(_logger, "manual-trigger");

        return Problem(
            title: exception.GetType().Name,
            detail: exception.Message,
            statusCode: StatusCodes.Status500InternalServerError);
    }
}
