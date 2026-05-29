namespace Betterfit.Services.Auth;

public enum AuthenticationOperationErrorKind
{
    None = 0,
    BadRequest = 1,
    Unauthorized = 2,
    Conflict = 3,
    NotFound = 4
}

public sealed record AuthenticationOperationResult<T>(
    T? Payload = default,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    AuthenticationOperationErrorKind ErrorKind = AuthenticationOperationErrorKind.None,
    IDictionary<string, string[]>? Details = null,
    IDictionary<string, string>? ResponseHeaders = null)
{
    public bool Succeeded => ErrorKind == AuthenticationOperationErrorKind.None;

    public static AuthenticationOperationResult<T> Success(T payload)
        => new(payload);

    public static AuthenticationOperationResult<T> BadRequest(
        string code,
        string message,
        IDictionary<string, string[]>? details = null,
        IDictionary<string, string>? responseHeaders = null)
        => new(default, code, message, AuthenticationOperationErrorKind.BadRequest, details, responseHeaders);

    public static AuthenticationOperationResult<T> Unauthorized(string code, string message)
        => new(default, code, message, AuthenticationOperationErrorKind.Unauthorized);

    public static AuthenticationOperationResult<T> Conflict(string code, string message)
        => new(default, code, message, AuthenticationOperationErrorKind.Conflict);

    public static AuthenticationOperationResult<T> NotFound(string code, string message)
        => new(default, code, message, AuthenticationOperationErrorKind.NotFound);
}
