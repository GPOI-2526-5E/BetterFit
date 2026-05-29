using Betterfit.Contracts.Auth;
using Betterfit.Data;
using Betterfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Services.Accounts;

public sealed class AccountSessionService : IAccountSessionService
{
    private readonly AppDbContext _dbContext;

    public AccountSessionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CurrentAccountResponse> BuildCurrentAccountAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new
            {
                x.Id,
                Email = x.Email ?? string.Empty,
                x.FullName
            })
            .SingleAsync(cancellationToken);

        var canAccessMemberApp = await _dbContext.GymMemberships
            .AsNoTracking()
            .AnyAsync(
                x => x.UserId == userId
                     && (x.Status == GymMembershipStatus.Active || x.Status == GymMembershipStatus.Suspended),
                cancellationToken);

        var canAccessStaffApp = await _dbContext.TenantRoleAssignments
            .AsNoTracking()
            .AnyAsync(
                x =>
                x.UserId == userId
                && x.Status == TenantRoleAssignmentStatus.Active
                && x.RevokedAtUtc == null,
                cancellationToken);

        return new CurrentAccountResponse(
            new UserSummaryResponse(user.Id, user.Email, user.FullName),
            new AccountAccessResponse(
                canAccessMemberApp,
                canAccessStaffApp));
    }
}
