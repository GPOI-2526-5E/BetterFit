using Betterfit.Authorization;
using Betterfit.Contracts.Crm;
using Betterfit.Contracts.Access;
using Betterfit.Contracts.Auth;
using Betterfit.Contracts.Gyms;
using Betterfit.Contracts.Sales;
using Betterfit.Models;
using System.Text.Json;

namespace Betterfit.Infrastructure.Mapping;

/// <summary>
/// Shared mapper methods used across controllers to convert domain entities to response DTOs.
/// </summary>
public static class ResponseMappers
{
    public static GymLocationResponse MapLocationResponse(GymLocation location)
    {
        return new GymLocationResponse(
            location.Id,
            location.GymId,
            location.Name,
            location.Code,
            location.AddressLine1,
            location.City,
            location.CountryCode,
            location.IsActive,
            location.CreatedAtUtc);
    }

    public static MemberProfileResponse? MapMemberProfileResponse(MemberProfile? profile)
    {
        return profile is null
            ? null
            : new MemberProfileResponse(
                profile.Id,
                profile.UserId,
                profile.FirstName,
                profile.LastName,
                profile.BirthDate,
                profile.AvatarUrl,
                profile.EmergencyContactName,
                profile.EmergencyContactPhoneNumber,
                profile.CreatedAtUtc,
                profile.UpdatedAtUtc);
    }

    public static StaffProfileResponse? MapStaffProfileResponse(StaffProfile? profile)
    {
        return profile is null
            ? null
            : new StaffProfileResponse(
                profile.Id,
                profile.UserId,
                profile.DisplayName,
                profile.JobTitle,
                profile.InternalCode,
                profile.Active,
                profile.CreatedAtUtc,
                profile.UpdatedAtUtc);
    }

    public static GymResponse MapGymResponse(Gym gym)
    {
        return new GymResponse(
            gym.Id,
            gym.Name,
            gym.CreatedAtUtc);
    }

    public static GymMembershipResponse MapMembershipResponse(GymMembership membership, GymPermissionScope? scope = null)
    {
        var visibleLocations = membership.Locations
            .Where(location => scope is null || scope.CanAccessLocation(location.LocationId))
            .OrderBy(location => location.Location.Name)
            .Select(location => location.Location)
            .ToList();

        Guid? primaryLocationId = membership.PrimaryLocationId;
        if (primaryLocationId.HasValue && visibleLocations.All(location => location.Id != primaryLocationId.Value))
        {
            primaryLocationId = null;
        }

        return new GymMembershipResponse(
            membership.Id,
            membership.GymId,
            membership.Gym.Name,
            membership.UserId,
            membership.User?.Email,
            membership.InvitationEmail,
            membership.Status,
            primaryLocationId,
            membership.Source,
            membership.TaxCode,
            membership.Notes,
            membership.CreatedAtUtc,
            membership.UpdatedAtUtc,
            membership.ClaimedAtUtc,
            membership.JoinedAtUtc,
            membership.EndedAtUtc,
            visibleLocations.Select(MapLocationResponse).ToList(),
            MapMemberProfileResponse(membership.MemberProfile),
            membership.CustomFieldValues
                .OrderBy(value => value.FieldDefinition.SortOrder)
                .ThenBy(value => value.FieldDefinition.Label)
                .Select(MapCustomFieldValueResponse)
                .ToList());
    }

    public static GymStaffAssignmentResponse MapStaffAssignmentResponse(TenantRoleAssignment assignment)
    {
        return new GymStaffAssignmentResponse(
            assignment.Id,
            assignment.GymId,
            assignment.Gym.Name,
            assignment.UserId,
            assignment.User.Email ?? string.Empty,
            assignment.RoleId,
            assignment.Role.Name,
            assignment.ScopeType,
            assignment.ScopeLocationId,
            assignment.ScopeLocation?.Name,
            assignment.Status,
            assignment.GrantedAtUtc,
            assignment.RevokedAtUtc,
            MapStaffProfileResponse(assignment.StaffProfile));
    }

    public static GymSaleResponse MapSaleResponse(GymSale sale)
    {
        var memberName = MembershipDisplayName(sale.Membership);
        var memberEmail = sale.Membership.User?.Email?.Trim()
            ?? sale.Membership.InvitationEmail?.Trim()
            ?? "Email non disponibile";

        return new GymSaleResponse(
            sale.Id,
            sale.GymId,
            sale.GymMembershipId,
            sale.LocationId,
            sale.ReferenceCode,
            memberName,
            memberEmail,
            sale.Location.Name,
            sale.SoldAtUtc,
            sale.CreatedAtUtc,
            sale.UpdatedAtUtc,
            sale.SubtotalAmount,
            sale.DiscountAmount,
            sale.TotalAmount,
            sale.PaidAmount,
            sale.RemainingAmount,
            sale.Status,
            sale.PaymentStatus,
            sale.Notes,
            sale.Lines
                .OrderBy(line => line.Name)
                .Select(MapSaleLineResponse)
                .ToList(),
            sale.Payments
                .OrderBy(payment => payment.DueAtUtc ?? payment.CreatedAtUtc)
                .ThenBy(payment => payment.CreatedAtUtc)
                .Select(MapSalePaymentResponse)
                .ToList());
    }

    public static GymSaleCatalogItemResponse MapSaleCatalogItemResponse(GymSaleCatalogItem item)
    {
        return new GymSaleCatalogItemResponse(
            item.Id,
            item.GymId,
            item.LocationId,
            item.Location.Name,
            item.ItemType,
            item.Name,
            item.UnitPriceAmount,
            item.DefaultQuantity,
            item.DefaultDiscountAmount,
            item.DefaultCreditsGranted,
            item.ServicePeriodDays,
            item.Notes,
            item.IsActive,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
    }

    public static GymSaleLineResponse MapSaleLineResponse(GymSaleLine line)
    {
        return new GymSaleLineResponse(
            line.Id,
            line.ItemType,
            line.Name,
            line.Quantity,
            line.UnitPriceAmount,
            line.DiscountAmount,
            line.LineTotalAmount,
            line.ServicePeriodStartUtc,
            line.ServicePeriodEndUtc,
            line.CreditsGranted,
            line.Notes);
    }

    public static GymSalePaymentResponse MapSalePaymentResponse(GymSalePayment payment)
    {
        return new GymSalePaymentResponse(
            payment.Id,
            payment.Amount,
            payment.Method,
            payment.Status,
            payment.DueAtUtc,
            payment.PaidAtUtc,
            payment.ReceiptCode,
            payment.ReceiptIssuedAtUtc,
            payment.Notes,
            payment.CreatedAtUtc);
    }

    public static GymAccessEventResponse MapAccessEventResponse(GymAccessEvent accessEvent)
    {
        var memberName = MembershipDisplayName(accessEvent.Membership);
        var memberEmail = accessEvent.Membership.User?.Email?.Trim()
            ?? accessEvent.Membership.InvitationEmail?.Trim()
            ?? "Email non disponibile";

        return new GymAccessEventResponse(
            accessEvent.Id,
            accessEvent.GymId,
            accessEvent.GymMembershipId,
            accessEvent.LocationId,
            memberName,
            memberEmail,
            accessEvent.Location.Name,
            accessEvent.GateName,
            accessEvent.Result,
            accessEvent.Origin,
            accessEvent.Reason,
            accessEvent.OccurredAtUtc);
    }

    public static GymLeadResponse MapLeadResponse(GymLead lead)
    {
        return new GymLeadResponse(
            lead.Id,
            lead.GymId,
            lead.LocationId,
            lead.Location.Name,
            lead.OwnerAssignmentId,
            StaffAssignmentDisplayName(lead.OwnerAssignment),
            lead.ConvertedMembershipId,
            lead.FullName,
            lead.Email,
            lead.PhoneNumber,
            lead.Source,
            lead.Stage,
            lead.Interest,
            lead.Notes,
            lead.LastContactedAtUtc,
            lead.NextFollowUpAtUtc,
            lead.CreatedAtUtc,
            lead.UpdatedAtUtc,
            lead.Tasks
                .OrderBy(task => task.DueAtUtc ?? DateTime.MaxValue)
                .ThenBy(task => task.CreatedAtUtc)
                .Select(MapLeadTaskResponse)
                .ToList());
    }

    public static GymLeadTaskResponse MapLeadTaskResponse(GymLeadTask task)
    {
        return new GymLeadTaskResponse(
            task.Id,
            task.GymLeadId,
            task.GymId,
            task.AssignedAssignmentId,
            StaffAssignmentDisplayName(task.AssignedAssignment),
            task.Title,
            task.Notes,
            task.Status,
            task.DueAtUtc,
            task.CompletedAtUtc,
            task.CreatedAtUtc,
            task.UpdatedAtUtc);
    }

    private static string MembershipDisplayName(GymMembership membership)
    {
        var firstName = membership.MemberProfile?.FirstName?.Trim() ?? membership.PendingFirstName?.Trim();
        var lastName = membership.MemberProfile?.LastName?.Trim() ?? membership.PendingLastName?.Trim();
        var fullName = string.Join(" ", new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return membership.User?.Email?.Trim()
            ?? membership.InvitationEmail?.Trim()
            ?? "Cliente senza nome";
    }

    private static string? StaffAssignmentDisplayName(TenantRoleAssignment? assignment)
    {
        if (assignment is null)
        {
            return null;
        }

        return assignment.StaffProfile?.DisplayName?.Trim()
            ?? assignment.User?.FullName?.Trim()
            ?? assignment.User?.Email?.Trim();
    }

    private static GymCustomFieldValueResponse MapCustomFieldValueResponse(GymCustomFieldValue value)
    {
        return new GymCustomFieldValueResponse(
            value.FieldDefinitionId,
            value.FieldDefinition.Key,
            value.FieldDefinition.Label,
            value.FieldDefinition.Description,
            value.FieldDefinition.ValueType,
            ParseOptions(value.FieldDefinition.OptionsJson),
            value.FieldDefinition.IsRequired,
            value.FieldDefinition.IsActive,
            value.FieldDefinition.SortOrder,
            value.Value);
    }

    private static IReadOnlyCollection<string> ParseOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(optionsJson) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
