using System.Text.Json.Serialization;

namespace Betterfit.Contracts.Common;

/// <summary>
/// Unified API response envelope used for both success and error outcomes.
/// </summary>
/// <typeparam name="T">Response data type.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>
    /// True when request succeeded; otherwise false.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// General message for the response.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; init; }

    /// <summary>
    /// Payload for successful responses. Null on failures.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Error payload for failed responses. Null on success.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiError? Error { get; init; }

    /// <summary>
    /// ASP.NET trace identifier for request correlation.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; init; }
}
