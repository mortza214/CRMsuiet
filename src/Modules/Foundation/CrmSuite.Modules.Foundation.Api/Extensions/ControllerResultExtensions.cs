using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Domain.Results;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Foundation.Api.Extensions;

public static class ControllerResultExtensions
{
    public static IActionResult ToApiResult(
        this ControllerBase controller,
        Result result,
        string? successMessage = null)
    {
        var traceId = controller.HttpContext.TraceIdentifier;

        if (result.IsSuccess)
        {
            return controller.Ok(
                ApiResponse<object?>.Ok(
                    (object?)null,
                    result.Message ?? successMessage,
                    traceId));
        }

        return controller.BadRequest(
            ApiResponse<object?>.Fail(
                result.Message ?? "عملیات انجام نشد.",
                result.Errors,
                traceId));
    }

    public static IActionResult ToApiResult<T>(
        this ControllerBase controller,
        Result<T> result,
        string? successMessage = null)
    {
        var traceId = controller.HttpContext.TraceIdentifier;

        if (result.IsSuccess)
        {
            return controller.Ok(
                ApiResponse<T>.Ok(result.Value!, successMessage, traceId));
        }

        return controller.BadRequest(
            ApiResponse<T>.Fail(
                result.Message ?? "عملیات انجام نشد.",
                result.Errors,
                traceId));
    }
}
