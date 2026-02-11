namespace Betterfit.Contracts.Common;

/// <summary>
/// Error payload included in failed API responses.
/// </summary>
/// <param name="Code">Machine-readable error code.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Details">Optional field-level or contextual error details.</param>
public sealed record ApiError(string Code, string Message, IDictionary<string, string[]>? Details = null);
