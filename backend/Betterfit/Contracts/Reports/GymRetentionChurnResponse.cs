using Betterfit.Models;

namespace Betterfit.Contracts.Reports;

public sealed record GymRetentionChurnResponse(
    int ActiveMembersCount,
    int ExpiringNext30DaysCount,
    int AtRiskMembersCount,
    int RenewedLast90DaysCount,
    int ChurnedLast90DaysCount,
    decimal RetentionRatePercentage,
    decimal ChurnRatePercentage,
    IReadOnlyCollection<GymRetentionLocationSummaryResponse> Locations,
    IReadOnlyCollection<GymRetentionRiskMemberResponse> AtRiskMembers,
    DateTime GeneratedAtUtc);

public sealed record GymRetentionLocationSummaryResponse(
    Guid LocationId,
    string LocationName,
    int ActiveMembersCount,
    int ExpiringNext30DaysCount,
    int AtRiskMembersCount,
    int RenewedLast90DaysCount,
    int ChurnedLast90DaysCount,
    decimal RetentionRatePercentage,
    decimal ChurnRatePercentage);

public sealed record GymRetentionRiskMemberResponse(
    Guid MembershipId,
    Guid? LocationId,
    string LocationName,
    string MemberName,
    string MemberEmail,
    GymMembershipStatus Status,
    DateTime? MembershipEndsAtUtc,
    DateTime? LastAccessAtUtc,
    int DaysUntilExpiry,
    bool HasRenewalSale);
