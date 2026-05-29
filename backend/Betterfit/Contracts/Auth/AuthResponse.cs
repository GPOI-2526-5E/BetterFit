using Betterfit.Models;

namespace Betterfit.Contracts.Auth;

public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, CurrentAccountResponse Account);

public sealed record CurrentAccountResponse(
    UserSummaryResponse User,
    AccountAccessResponse Access);

public sealed record UserSummaryResponse(string Id, string Email, string? FullName);

public sealed record AccountAccessResponse(
    bool CanAccessMemberApp,
    bool CanAccessStaffApp);
