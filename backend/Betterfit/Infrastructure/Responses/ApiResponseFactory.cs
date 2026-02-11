using Betterfit.Contracts.Common;

namespace Betterfit.Infrastructure.Responses;

public static class ApiResponseFactory
{
    public static ApiResponse<T> Success<T>(T data, HttpContext httpContext)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
        };
    }

    public static ApiResponse<T> Failure<T>(
        string code,
        string message,
        HttpContext httpContext,
        IDictionary<string, string[]>? details = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Error = new ApiError(code, message, details),
            TraceId = httpContext.TraceIdentifier,
        };
    }
}
