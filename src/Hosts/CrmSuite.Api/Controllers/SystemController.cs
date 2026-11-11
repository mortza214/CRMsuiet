using CrmSuite.Api.Maintenance;
using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Api.Controllers;

[ApiController]
[Route("api/system")]
[Authorize(Roles = Roles.SuperAdmin)]
public sealed class SystemController : ControllerBase
{
    private readonly TestDataResetService _resetService;
    private readonly IHostEnvironment _environment;

    public SystemController(
        TestDataResetService resetService,
        IHostEnvironment environment)
    {
        _resetService = resetService;
        _environment = environment;
    }

    [HttpPost("reset-requests")]
    public async Task<IActionResult> ResetRequests(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await _resetService.ResetRequestsAsync(cancellationToken);
        return Ok(ApiResponse<TestRequestResetResult>.Ok(
            result,
            "درخواست‌ها و داده‌های تراکنشی پاک شدند."));
    }

    [HttpPost("reset-test-data")]
    public async Task<IActionResult> ResetTestData(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await _resetService.ResetAsync(cancellationToken);
        return Ok(ApiResponse<TestDataResetResult>.Ok(
            result,
            "داده‌های تست پاک شدند."));
    }
}
