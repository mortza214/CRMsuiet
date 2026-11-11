using System.Reflection;
using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace CrmSuite.Modules.Foundation.Api.Controllers;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    private readonly IDateTimeProvider _clock;
    private readonly IJalaliDateService _jalaliDate;
    private readonly IHostEnvironment _environment;

    public HealthController(
        IDateTimeProvider clock,
        IJalaliDateService jalaliDate,
        IHostEnvironment environment)
    {
        _clock = clock;
        _jalaliDate = jalaliDate;
        _environment = environment;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthDto>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<HealthDto>> Get()
    {
        var now = _clock.UtcNow;
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";

        var payload = new HealthDto(
            "Healthy",
            now,
            _jalaliDate.ToJalali(now),
            _environment.EnvironmentName,
            version);

        return Ok(ApiResponse<HealthDto>.Ok(
            payload,
            "سیستم فعال است.",
            HttpContext.TraceIdentifier));
    }
}
