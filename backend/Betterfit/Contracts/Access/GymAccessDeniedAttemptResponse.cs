namespace Betterfit.Contracts.Access;

public sealed record GymAccessDeniedAttemptResponse(
    string MemberName,
    string Reason,
    int Attempts,
    DateTime LastAttemptAtUtc);
