using Betterfit.Contracts.Common;
using Betterfit.Infrastructure.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Betterfit.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> Success<T>(T data)
    {
        return Ok(ApiResponseFactory.Success(data, HttpContext));
    }

    protected ActionResult<ApiResponse<T>> CreatedAt<T>(
        string actionName,
        object routeValues,
        T data)
    {
        return CreatedAtAction(actionName, routeValues, ApiResponseFactory.Success(data, HttpContext));
    }

    protected ActionResult<ApiResponse<T>> BadRequestError<T>(
        string code,
        string message,
        IDictionary<string, string[]>? details = null)
    {
        return BadRequest(ApiResponseFactory.Failure<T>(code, message, HttpContext, details));
    }

    protected ActionResult<ApiResponse<T>> UnauthorizedError<T>(string message = "Authentication is required.")
    {
        return StatusCode(StatusCodes.Status401Unauthorized,
            ApiResponseFactory.Failure<T>("unauthorized", message, HttpContext));
    }

    protected ActionResult<ApiResponse<T>> ForbiddenError<T>(string message = "Access to this resource is forbidden.")
    {
        return StatusCode(StatusCodes.Status403Forbidden,
            ApiResponseFactory.Failure<T>("forbidden", message, HttpContext));
    }

    protected ActionResult<ApiResponse<T>> NotFoundError<T>(string message = "Resource not found.")
    {
        return NotFound(ApiResponseFactory.Failure<T>("not_found", message, HttpContext));
    }

    protected ActionResult<ApiResponse<T>> ConflictError<T>(
        string message,
        IDictionary<string, string[]>? details = null,
        string code = "conflict")
    {
        return Conflict(ApiResponseFactory.Failure<T>(code, message, HttpContext, details));
    }
}
