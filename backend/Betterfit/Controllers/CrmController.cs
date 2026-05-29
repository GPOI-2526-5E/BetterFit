using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.Crm;
using Betterfit.Data;
using Betterfit.Infrastructure.Mapping;
using Betterfit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/gyms/{gymId:guid}/crm")]
public sealed class CrmController : ApiControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CrmController(
        AppDbContext dbContext,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _userManager = userManager;
    }

    [HttpGet("overview")]
    [Authorize(Policy = AuthorizationPolicies.CrmRead)]
    public async Task<ActionResult<ApiResponse<GymCrmOverviewResponse>>> GetOverview(
        Guid gymId,
        [FromQuery] Guid? locationId,
        CancellationToken cancellationToken)
    {
        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Read, cancellationToken);
        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymCrmOverviewResponse>();
        }

        var allowedLocationIds = await ResolveAllowedLocationIdsAsync(gymId, scope, locationId, cancellationToken);
        if (allowedLocationIds.Count == 0)
        {
            return Success(new GymCrmOverviewResponse(0, 0, 0, 0, BuildEmptyPipeline(), [], DateTime.UtcNow));
        }

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfToday = now.Date;

        var leads = await LoadLeadsQuery(gymId)
            .Where(lead => allowedLocationIds.Contains(lead.LocationId))
            .OrderByDescending(lead => lead.UpdatedAtUtc)
            .ThenByDescending(lead => lead.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var openTasksCount = leads.Sum(lead => lead.Tasks.Count(task => task.Status == GymLeadTaskStatus.Open));
        var followUpCount = leads.Count(lead =>
            lead.Stage is not (GymLeadStage.Won or GymLeadStage.Lost)
            && lead.NextFollowUpAtUtc.HasValue
            && lead.NextFollowUpAtUtc.Value <= startOfToday.AddDays(1));

        var overview = new GymCrmOverviewResponse(
            leads.Count,
            followUpCount,
            leads.Count(lead => lead.Stage == GymLeadStage.Won && lead.UpdatedAtUtc >= startOfMonth),
            openTasksCount,
            Enum.GetValues<GymLeadStage>()
                .Select(stage => new GymLeadStageSummaryResponse(stage, leads.Count(lead => lead.Stage == stage)))
                .ToList(),
            leads.Take(8).Select(ResponseMappers.MapLeadResponse).ToList(),
            now);

        return Success(overview);
    }

    [HttpGet("leads")]
    [Authorize(Policy = AuthorizationPolicies.CrmRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymLeadResponse>>>> GetLeads(
        Guid gymId,
        [FromQuery] Guid? locationId,
        [FromQuery] Guid? ownerAssignmentId,
        [FromQuery] Guid? membershipId,
        [FromQuery] GymLeadStage? stage,
        [FromQuery] bool dueOnly,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Read, cancellationToken);
        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymLeadResponse>>();
        }

        var allowedLocationIds = await ResolveAllowedLocationIdsAsync(gymId, scope, locationId, cancellationToken);
        var query = LoadLeadsQuery(gymId)
            .Where(lead => allowedLocationIds.Contains(lead.LocationId));

        if (ownerAssignmentId.HasValue)
        {
            query = query.Where(lead => lead.OwnerAssignmentId == ownerAssignmentId.Value);
        }

        if (stage.HasValue)
        {
            query = query.Where(lead => lead.Stage == stage.Value);
        }

        if (membershipId.HasValue)
        {
            var membership = await _dbContext.GymMemberships
                .AsNoTracking()
                .Include(item => item.User)
                .SingleOrDefaultAsync(
                    item => item.Id == membershipId.Value && item.GymId == gymId,
                    cancellationToken);

            if (membership is null)
            {
                return NotFoundError<IReadOnlyCollection<GymLeadResponse>>("Membership not found.");
            }

            var membershipEmails = new[]
                {
                    membership.User?.Email?.Trim().ToLowerInvariant(),
                    membership.InvitationEmail?.Trim().ToLowerInvariant()
                }
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct()
                .ToList();

            query = membershipEmails.Count == 0
                ? query.Where(lead => lead.ConvertedMembershipId == membershipId.Value)
                : query.Where(lead =>
                    lead.ConvertedMembershipId == membershipId.Value
                    || (lead.Email != null && membershipEmails.Contains(lead.Email.ToLower())));
        }

        if (dueOnly)
        {
            var dueCutoff = DateTime.UtcNow.Date.AddDays(1);
            query = query.Where(lead =>
                lead.Stage != GymLeadStage.Won
                && lead.Stage != GymLeadStage.Lost
                && lead.NextFollowUpAtUtc.HasValue
                && lead.NextFollowUpAtUtc.Value <= dueCutoff);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(lead =>
                lead.FullName.ToLower().Contains(normalizedSearch)
                || (lead.Email != null && lead.Email.ToLower().Contains(normalizedSearch))
                || (lead.PhoneNumber != null && lead.PhoneNumber.ToLower().Contains(normalizedSearch))
                || (lead.Interest != null && lead.Interest.ToLower().Contains(normalizedSearch))
                || (lead.Notes != null && lead.Notes.ToLower().Contains(normalizedSearch)));
        }

        var leads = await query
            .OrderBy(lead => lead.Stage)
            .ThenBy(lead => lead.NextFollowUpAtUtc ?? DateTime.MaxValue)
            .ThenByDescending(lead => lead.CreatedAtUtc)
            .Take(250)
            .ToListAsync(cancellationToken);

        return Success<IReadOnlyCollection<GymLeadResponse>>(leads.Select(ResponseMappers.MapLeadResponse).ToList());
    }

    [HttpPost("leads")]
    [Authorize(Policy = AuthorizationPolicies.CrmWrite)]
    public async Task<ActionResult<ApiResponse<GymLeadResponse>>> CreateLead(
        Guid gymId,
        [FromBody] CreateGymLeadRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Write, cancellationToken);
        if (!scope.HasAnyAccess || !scope.CanAccessLocation(request.LocationId))
        {
            return ForbiddenError<GymLeadResponse>();
        }

        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequestError<GymLeadResponse>(
                "missing_contact",
                "At least one contact detail between email and phone number is required.");
        }

        var location = await _dbContext.GymLocations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == request.LocationId && item.GymId == gymId && item.IsActive,
                cancellationToken);

        if (location is null)
        {
            return BadRequestError<GymLeadResponse>("invalid_location", "The selected location is not valid for this gym.");
        }

        var ownerAssignment = await ResolveAssignmentAsync(gymId, request.OwnerAssignmentId, cancellationToken);
        if (request.OwnerAssignmentId.HasValue && ownerAssignment is null)
        {
            return BadRequestError<GymLeadResponse>("invalid_owner", "The selected owner assignment is not valid.");
        }

        if (ownerAssignment is not null && !AssignmentCanOperateOnLocation(ownerAssignment, request.LocationId))
        {
            return ConflictError<GymLeadResponse>("The selected owner assignment cannot operate on the chosen location.");
        }

        var now = DateTime.UtcNow;
        var lead = new GymLead
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            LocationId = request.LocationId,
            OwnerAssignmentId = ownerAssignment?.Id,
            FullName = request.FullName.Trim(),
            Email = NormalizeOptional(request.Email),
            PhoneNumber = NormalizeOptional(request.PhoneNumber),
            Source = request.Source,
            Stage = GymLeadStage.New,
            Interest = NormalizeOptional(request.Interest),
            Notes = NormalizeOptional(request.Notes),
            NextFollowUpAtUtc = request.NextFollowUpAtUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.GymLeads.Add(lead);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadLeadsQuery(gymId)
            .SingleAsync(item => item.Id == lead.Id, cancellationToken);

        return Success(ResponseMappers.MapLeadResponse(saved));
    }

    [HttpPatch("leads/{leadId:guid}/stage")]
    [Authorize(Policy = AuthorizationPolicies.CrmWrite)]
    public async Task<ActionResult<ApiResponse<GymLeadResponse>>> UpdateLeadStage(
        Guid gymId,
        Guid leadId,
        [FromBody] UpdateGymLeadStageRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Write, cancellationToken);
        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymLeadResponse>();
        }

        var lead = await _dbContext.GymLeads
            .Include(item => item.Tasks)
            .SingleOrDefaultAsync(item => item.Id == leadId && item.GymId == gymId, cancellationToken);

        if (lead is null)
        {
            return NotFoundError<GymLeadResponse>("Lead not found.");
        }

        if (!scope.CanAccessLocation(lead.LocationId))
        {
            return ForbiddenError<GymLeadResponse>();
        }

        if (request.ConvertedMembershipId.HasValue)
        {
            var membership = await _dbContext.GymMemberships
                .Include(item => item.Locations)
                .SingleOrDefaultAsync(
                    item => item.Id == request.ConvertedMembershipId.Value && item.GymId == gymId,
                    cancellationToken);

            if (membership is null)
            {
                return BadRequestError<GymLeadResponse>("invalid_membership", "The selected membership is not valid.");
            }

            if (!membership.Locations.Any(item => item.LocationId == lead.LocationId) && membership.PrimaryLocationId != lead.LocationId)
            {
                return ConflictError<GymLeadResponse>("The selected membership is not enabled on the lead location.");
            }

            lead.ConvertedMembershipId = membership.Id;
        }
        else if (request.Stage != GymLeadStage.Won)
        {
            lead.ConvertedMembershipId = null;
        }

        lead.Stage = request.Stage;
        lead.LastContactedAtUtc = request.LastContactedAtUtc;
        lead.NextFollowUpAtUtc = request.NextFollowUpAtUtc;
        lead.Notes = NormalizeOptional(request.Notes);
        lead.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadLeadsQuery(gymId)
            .SingleAsync(item => item.Id == lead.Id, cancellationToken);

        return Success(ResponseMappers.MapLeadResponse(saved));
    }

    [HttpPost("leads/{leadId:guid}/tasks")]
    [Authorize(Policy = AuthorizationPolicies.CrmWrite)]
    public async Task<ActionResult<ApiResponse<GymLeadResponse>>> CreateLeadTask(
        Guid gymId,
        Guid leadId,
        [FromBody] CreateGymLeadTaskRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Write, cancellationToken);
        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymLeadResponse>();
        }

        var lead = await _dbContext.GymLeads
            .SingleOrDefaultAsync(item => item.Id == leadId && item.GymId == gymId, cancellationToken);

        if (lead is null)
        {
            return NotFoundError<GymLeadResponse>("Lead not found.");
        }

        if (!scope.CanAccessLocation(lead.LocationId))
        {
            return ForbiddenError<GymLeadResponse>();
        }

        var assignment = await ResolveAssignmentAsync(gymId, request.AssignedAssignmentId, cancellationToken);
        if (request.AssignedAssignmentId.HasValue && assignment is null)
        {
            return BadRequestError<GymLeadResponse>("invalid_assignment", "The selected staff assignment is not valid.");
        }

        if (assignment is not null && !AssignmentCanOperateOnLocation(assignment, lead.LocationId))
        {
            return ConflictError<GymLeadResponse>("The selected staff assignment cannot operate on the lead location.");
        }

        var now = DateTime.UtcNow;
        _dbContext.GymLeadTasks.Add(new GymLeadTask
        {
            Id = Guid.NewGuid(),
            GymLeadId = lead.Id,
            GymId = gymId,
            AssignedAssignmentId = assignment?.Id,
            Title = request.Title.Trim(),
            Notes = NormalizeOptional(request.Notes),
            Status = GymLeadTaskStatus.Open,
            DueAtUtc = request.DueAtUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        lead.UpdatedAtUtc = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadLeadsQuery(gymId)
            .SingleAsync(item => item.Id == lead.Id, cancellationToken);

        return Success(ResponseMappers.MapLeadResponse(saved));
    }

    [HttpPatch("leads/{leadId:guid}/tasks/{taskId:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.CrmWrite)]
    public async Task<ActionResult<ApiResponse<GymLeadResponse>>> UpdateLeadTaskStatus(
        Guid gymId,
        Guid leadId,
        Guid taskId,
        [FromBody] UpdateGymLeadTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Write, cancellationToken);
        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymLeadResponse>();
        }

        var lead = await _dbContext.GymLeads
            .Include(item => item.Tasks)
            .SingleOrDefaultAsync(item => item.Id == leadId && item.GymId == gymId, cancellationToken);

        if (lead is null)
        {
            return NotFoundError<GymLeadResponse>("Lead not found.");
        }

        if (!scope.CanAccessLocation(lead.LocationId))
        {
            return ForbiddenError<GymLeadResponse>();
        }

        var task = lead.Tasks.SingleOrDefault(item => item.Id == taskId);
        if (task is null)
        {
            return NotFoundError<GymLeadResponse>("Lead task not found.");
        }

        task.Status = request.Status;
        task.CompletedAtUtc = request.Status == GymLeadTaskStatus.Completed
            ? request.CompletedAtUtc ?? DateTime.UtcNow
            : null;
        task.Notes = NormalizeOptional(request.Notes);
        task.UpdatedAtUtc = DateTime.UtcNow;
        lead.UpdatedAtUtc = task.UpdatedAtUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadLeadsQuery(gymId)
            .SingleAsync(item => item.Id == lead.Id, cancellationToken);

        return Success(ResponseMappers.MapLeadResponse(saved));
    }

    [HttpGet("campaigns")]
    [Authorize(Policy = AuthorizationPolicies.CrmRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymCampaignResponse>>>> GetCampaigns(
        Guid gymId,
        [FromQuery] Guid? locationId,
        [FromQuery] GymCampaignStatus? status,
        [FromQuery] GymCampaignChannel? channel,
        CancellationToken cancellationToken)
    {
        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Read, cancellationToken);
        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymCampaignResponse>>();
        }

        var allowedLocationIds = await ResolveAllowedLocationIdsAsync(gymId, scope, locationId, cancellationToken);
        var query = LoadCampaignsQuery(gymId)
            .Where(campaign => allowedLocationIds.Contains(campaign.LocationId));

        if (status.HasValue)
        {
            query = query.Where(campaign => campaign.Status == status.Value);
        }

        if (channel.HasValue)
        {
            query = query.Where(campaign => campaign.Channel == channel.Value);
        }

        var campaigns = await query
            .OrderByDescending(campaign => campaign.UpdatedAtUtc)
            .ThenByDescending(campaign => campaign.CreatedAtUtc)
            .Take(120)
            .ToListAsync(cancellationToken);

        var response = new List<GymCampaignResponse>(campaigns.Count);
        foreach (var campaign in campaigns)
        {
            response.Add(await MapCampaignResponseAsync(campaign, cancellationToken));
        }

        return Success<IReadOnlyCollection<GymCampaignResponse>>(response);
    }

    [HttpPost("campaigns")]
    [Authorize(Policy = AuthorizationPolicies.CrmWrite)]
    public async Task<ActionResult<ApiResponse<GymCampaignResponse>>> CreateCampaign(
        Guid gymId,
        [FromBody] CreateGymCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymCampaignResponse>();
        }

        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Write, cancellationToken);
        if (!scope.HasAnyAccess || !scope.CanAccessLocation(request.LocationId))
        {
            return ForbiddenError<GymCampaignResponse>();
        }

        var location = await _dbContext.GymLocations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == request.LocationId && item.GymId == gymId && item.IsActive,
                cancellationToken);

        if (location is null)
        {
            return BadRequestError<GymCampaignResponse>("invalid_location", "The selected location is not valid for this gym.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequestError<GymCampaignResponse>("missing_name", "Campaign name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            return BadRequestError<GymCampaignResponse>("missing_subject", "Campaign subject is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequestError<GymCampaignResponse>("missing_message", "Campaign message is required.");
        }

        if (request.AudienceType == GymCampaignAudienceType.LeadsInStage && !request.TargetLeadStage.HasValue)
        {
            return BadRequestError<GymCampaignResponse>("missing_target_stage", "Lead stage is required for lead-stage campaigns.");
        }

        var ownerAssignment = await ResolveAssignmentAsync(gymId, request.OwnerAssignmentId, cancellationToken);
        if (request.OwnerAssignmentId.HasValue && ownerAssignment is null)
        {
            return BadRequestError<GymCampaignResponse>("invalid_owner", "The selected owner assignment is not valid.");
        }

        if (ownerAssignment is not null && !AssignmentCanOperateOnLocation(ownerAssignment, request.LocationId))
        {
            return ConflictError<GymCampaignResponse>("The selected owner assignment cannot operate on the chosen location.");
        }

        if (request.ScheduledAtUtc.HasValue && request.ScheduledAtUtc.Value <= DateTime.UtcNow.AddMinutes(-1))
        {
            return BadRequestError<GymCampaignResponse>("invalid_schedule", "Scheduled campaigns must use a future date.");
        }

        var now = DateTime.UtcNow;
        var campaign = new GymCampaign
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            LocationId = request.LocationId,
            OwnerAssignmentId = ownerAssignment?.Id,
            CreatedByUserId = actorUserId,
            Name = request.Name.Trim(),
            Channel = request.Channel,
            AudienceType = request.AudienceType,
            TargetLeadStage = request.AudienceType == GymCampaignAudienceType.LeadsInStage
                ? request.TargetLeadStage
                : null,
            Status = request.ScheduledAtUtc.HasValue ? GymCampaignStatus.Scheduled : GymCampaignStatus.Draft,
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            Notes = NormalizeOptional(request.Notes),
            ScheduledAtUtc = request.ScheduledAtUtc,
            SentAtUtc = null,
            LastAudienceCount = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.GymCampaigns.Add(campaign);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadCampaignsQuery(gymId)
            .SingleAsync(item => item.Id == campaign.Id, cancellationToken);

        return Success(await MapCampaignResponseAsync(saved, cancellationToken));
    }

    [HttpPost("campaigns/{campaignId:guid}/schedule")]
    [Authorize(Policy = AuthorizationPolicies.CrmWrite)]
    public async Task<ActionResult<ApiResponse<GymCampaignResponse>>> ScheduleCampaign(
        Guid gymId,
        Guid campaignId,
        [FromBody] ScheduleGymCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Write, cancellationToken);
        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymCampaignResponse>();
        }

        if (request.ScheduledAtUtc <= DateTime.UtcNow.AddMinutes(-1))
        {
            return BadRequestError<GymCampaignResponse>("invalid_schedule", "Scheduled campaigns must use a future date.");
        }

        var campaign = await _dbContext.GymCampaigns
            .SingleOrDefaultAsync(item => item.Id == campaignId && item.GymId == gymId, cancellationToken);

        if (campaign is null)
        {
            return NotFoundError<GymCampaignResponse>("Campaign not found.");
        }

        if (!scope.CanAccessLocation(campaign.LocationId))
        {
            return ForbiddenError<GymCampaignResponse>();
        }

        if (campaign.Status is GymCampaignStatus.Sent or GymCampaignStatus.Archived)
        {
            return ConflictError<GymCampaignResponse>("Sent or archived campaigns cannot be scheduled again.");
        }

        campaign.Status = GymCampaignStatus.Scheduled;
        campaign.ScheduledAtUtc = request.ScheduledAtUtc;
        campaign.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadCampaignsQuery(gymId)
            .SingleAsync(item => item.Id == campaign.Id, cancellationToken);

        return Success(await MapCampaignResponseAsync(saved, cancellationToken));
    }

    [HttpPost("campaigns/{campaignId:guid}/send")]
    [Authorize(Policy = AuthorizationPolicies.CrmWrite)]
    public async Task<ActionResult<ApiResponse<GymCampaignResponse>>> SendCampaign(
        Guid gymId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Write, cancellationToken);
        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymCampaignResponse>();
        }

        var campaign = await _dbContext.GymCampaigns
            .SingleOrDefaultAsync(item => item.Id == campaignId && item.GymId == gymId, cancellationToken);

        if (campaign is null)
        {
            return NotFoundError<GymCampaignResponse>("Campaign not found.");
        }

        if (!scope.CanAccessLocation(campaign.LocationId))
        {
            return ForbiddenError<GymCampaignResponse>();
        }

        if (campaign.Status == GymCampaignStatus.Archived)
        {
            return ConflictError<GymCampaignResponse>("Archived campaigns cannot be sent.");
        }

        var audienceCount = await ResolveCampaignAudienceCountAsync(campaign, cancellationToken);
        var now = DateTime.UtcNow;
        campaign.Status = GymCampaignStatus.Sent;
        campaign.SentAtUtc = now;
        campaign.LastAudienceCount = audienceCount;
        campaign.UpdatedAtUtc = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadCampaignsQuery(gymId)
            .SingleAsync(item => item.Id == campaign.Id, cancellationToken);

        return Success(await MapCampaignResponseAsync(saved, cancellationToken));
    }

    [HttpPost("campaigns/{campaignId:guid}/archive")]
    [Authorize(Policy = AuthorizationPolicies.CrmWrite)]
    public async Task<ActionResult<ApiResponse<GymCampaignResponse>>> ArchiveCampaign(
        Guid gymId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Write, cancellationToken);
        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymCampaignResponse>();
        }

        var campaign = await _dbContext.GymCampaigns
            .SingleOrDefaultAsync(item => item.Id == campaignId && item.GymId == gymId, cancellationToken);

        if (campaign is null)
        {
            return NotFoundError<GymCampaignResponse>("Campaign not found.");
        }

        if (!scope.CanAccessLocation(campaign.LocationId))
        {
            return ForbiddenError<GymCampaignResponse>();
        }

        campaign.Status = GymCampaignStatus.Archived;
        campaign.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadCampaignsQuery(gymId)
            .SingleAsync(item => item.Id == campaign.Id, cancellationToken);

        return Success(await MapCampaignResponseAsync(saved, cancellationToken));
    }

    [HttpGet("automations")]
    [Authorize(Policy = AuthorizationPolicies.CrmRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymAutomationRuleResponse>>>> GetAutomationRules(
        Guid gymId,
        [FromQuery] Guid? locationId,
        [FromQuery] GymAutomationStatus? status,
        [FromQuery] GymCampaignChannel? channel,
        CancellationToken cancellationToken)
    {
        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Read, cancellationToken);
        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymAutomationRuleResponse>>();
        }

        var allowedLocationIds = await ResolveAllowedLocationIdsAsync(gymId, scope, locationId, cancellationToken);
        var query = LoadAutomationRulesQuery(gymId)
            .Where(rule => allowedLocationIds.Contains(rule.LocationId));

        if (status.HasValue)
        {
            query = query.Where(rule => rule.Status == status.Value);
        }

        if (channel.HasValue)
        {
            query = query.Where(rule => rule.Channel == channel.Value);
        }

        var rules = await query
            .OrderBy(rule => rule.NextRunAtUtc)
            .ThenByDescending(rule => rule.UpdatedAtUtc)
            .Take(120)
            .ToListAsync(cancellationToken);

        var response = new List<GymAutomationRuleResponse>(rules.Count);
        foreach (var rule in rules)
        {
            response.Add(await MapAutomationRuleResponseAsync(rule, cancellationToken));
        }

        return Success<IReadOnlyCollection<GymAutomationRuleResponse>>(response);
    }

    [HttpPost("automations")]
    [Authorize(Policy = AuthorizationPolicies.CrmWrite)]
    public async Task<ActionResult<ApiResponse<GymAutomationRuleResponse>>> CreateAutomationRule(
        Guid gymId,
        [FromBody] CreateGymAutomationRuleRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymAutomationRuleResponse>();
        }

        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Write, cancellationToken);
        if (!scope.HasAnyAccess || !scope.CanAccessLocation(request.LocationId))
        {
            return ForbiddenError<GymAutomationRuleResponse>();
        }

        var location = await _dbContext.GymLocations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == request.LocationId && item.GymId == gymId && item.IsActive,
                cancellationToken);

        if (location is null)
        {
            return BadRequestError<GymAutomationRuleResponse>("invalid_location", "The selected location is not valid for this gym.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequestError<GymAutomationRuleResponse>("missing_name", "Automation name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.SubjectTemplate))
        {
            return BadRequestError<GymAutomationRuleResponse>("missing_subject", "Automation subject is required.");
        }

        if (string.IsNullOrWhiteSpace(request.MessageTemplate))
        {
            return BadRequestError<GymAutomationRuleResponse>("missing_message", "Automation message is required.");
        }

        if (request.AudienceType == GymCampaignAudienceType.LeadsInStage && !request.TargetLeadStage.HasValue)
        {
            return BadRequestError<GymAutomationRuleResponse>("missing_target_stage", "Lead stage is required for lead-stage automations.");
        }

        if (request.NextRunAtUtc <= DateTime.UtcNow.AddMinutes(-1))
        {
            return BadRequestError<GymAutomationRuleResponse>("invalid_next_run", "Automations must start from a future run date.");
        }

        var ownerAssignment = await ResolveAssignmentAsync(gymId, request.OwnerAssignmentId, cancellationToken);
        if (request.OwnerAssignmentId.HasValue && ownerAssignment is null)
        {
            return BadRequestError<GymAutomationRuleResponse>("invalid_owner", "The selected owner assignment is not valid.");
        }

        if (ownerAssignment is not null && !AssignmentCanOperateOnLocation(ownerAssignment, request.LocationId))
        {
            return ConflictError<GymAutomationRuleResponse>("The selected owner assignment cannot operate on the chosen location.");
        }

        var now = DateTime.UtcNow;
        var rule = new GymAutomationRule
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            LocationId = request.LocationId,
            OwnerAssignmentId = ownerAssignment?.Id,
            CreatedByUserId = actorUserId,
            Name = request.Name.Trim(),
            Channel = request.Channel,
            AudienceType = request.AudienceType,
            TargetLeadStage = request.AudienceType == GymCampaignAudienceType.LeadsInStage
                ? request.TargetLeadStage
                : null,
            ScheduleType = request.ScheduleType,
            Status = GymAutomationStatus.Active,
            NextRunAtUtc = request.NextRunAtUtc,
            SubjectTemplate = request.SubjectTemplate.Trim(),
            MessageTemplate = request.MessageTemplate.Trim(),
            Notes = NormalizeOptional(request.Notes),
            LastRunAtUtc = null,
            LastAudienceCount = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.GymAutomationRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadAutomationRulesQuery(gymId)
            .SingleAsync(item => item.Id == rule.Id, cancellationToken);

        return Success(await MapAutomationRuleResponseAsync(saved, cancellationToken));
    }

    [HttpPost("automations/{automationRuleId:guid}/pause")]
    [Authorize(Policy = AuthorizationPolicies.CrmWrite)]
    public async Task<ActionResult<ApiResponse<GymAutomationRuleResponse>>> PauseAutomationRule(
        Guid gymId,
        Guid automationRuleId,
        CancellationToken cancellationToken)
    {
        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Write, cancellationToken);
        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymAutomationRuleResponse>();
        }

        var rule = await _dbContext.GymAutomationRules
            .SingleOrDefaultAsync(item => item.Id == automationRuleId && item.GymId == gymId, cancellationToken);

        if (rule is null)
        {
            return NotFoundError<GymAutomationRuleResponse>("Automation rule not found.");
        }

        if (!scope.CanAccessLocation(rule.LocationId))
        {
            return ForbiddenError<GymAutomationRuleResponse>();
        }

        rule.Status = GymAutomationStatus.Paused;
        rule.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadAutomationRulesQuery(gymId)
            .SingleAsync(item => item.Id == rule.Id, cancellationToken);

        return Success(await MapAutomationRuleResponseAsync(saved, cancellationToken));
    }

    [HttpPost("automations/{automationRuleId:guid}/resume")]
    [Authorize(Policy = AuthorizationPolicies.CrmWrite)]
    public async Task<ActionResult<ApiResponse<GymAutomationRuleResponse>>> ResumeAutomationRule(
        Guid gymId,
        Guid automationRuleId,
        CancellationToken cancellationToken)
    {
        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Write, cancellationToken);
        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymAutomationRuleResponse>();
        }

        var rule = await _dbContext.GymAutomationRules
            .SingleOrDefaultAsync(item => item.Id == automationRuleId && item.GymId == gymId, cancellationToken);

        if (rule is null)
        {
            return NotFoundError<GymAutomationRuleResponse>("Automation rule not found.");
        }

        if (!scope.CanAccessLocation(rule.LocationId))
        {
            return ForbiddenError<GymAutomationRuleResponse>();
        }

        var nextRunAtUtc = rule.NextRunAtUtc;
        var now = DateTime.UtcNow;
        while (nextRunAtUtc <= now.AddMinutes(1))
        {
            nextRunAtUtc = AdvanceAutomationNextRunAtUtc(nextRunAtUtc, rule.ScheduleType);
        }

        rule.Status = GymAutomationStatus.Active;
        rule.NextRunAtUtc = nextRunAtUtc;
        rule.UpdatedAtUtc = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadAutomationRulesQuery(gymId)
            .SingleAsync(item => item.Id == rule.Id, cancellationToken);

        return Success(await MapAutomationRuleResponseAsync(saved, cancellationToken));
    }

    [HttpPost("automations/{automationRuleId:guid}/run")]
    [Authorize(Policy = AuthorizationPolicies.CrmWrite)]
    public async Task<ActionResult<ApiResponse<GymAutomationRuleResponse>>> RunAutomationRule(
        Guid gymId,
        Guid automationRuleId,
        CancellationToken cancellationToken)
    {
        var scope = await GetCrmScopeAsync(gymId, PermissionActions.Write, cancellationToken);
        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymAutomationRuleResponse>();
        }

        var rule = await _dbContext.GymAutomationRules
            .SingleOrDefaultAsync(item => item.Id == automationRuleId && item.GymId == gymId, cancellationToken);

        if (rule is null)
        {
            return NotFoundError<GymAutomationRuleResponse>("Automation rule not found.");
        }

        if (!scope.CanAccessLocation(rule.LocationId))
        {
            return ForbiddenError<GymAutomationRuleResponse>();
        }

        var audienceCount = await ResolveAutomationAudienceCountAsync(rule, cancellationToken);
        var now = DateTime.UtcNow;
        rule.LastRunAtUtc = now;
        rule.LastAudienceCount = audienceCount;
        if (rule.Status == GymAutomationStatus.Active)
        {
            var baseline = rule.NextRunAtUtc > now ? rule.NextRunAtUtc : now;
            rule.NextRunAtUtc = AdvanceAutomationNextRunAtUtc(baseline, rule.ScheduleType);
        }

        rule.UpdatedAtUtc = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadAutomationRulesQuery(gymId)
            .SingleAsync(item => item.Id == rule.Id, cancellationToken);

        return Success(await MapAutomationRuleResponseAsync(saved, cancellationToken));
    }

    private async Task<GymPermissionScope> GetCrmScopeAsync(
        Guid gymId,
        string action,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return GymPermissionScope.None;
        }

        return await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Crm,
            action,
            cancellationToken);
    }

    private async Task<HashSet<Guid>> ResolveAllowedLocationIdsAsync(
        Guid gymId,
        GymPermissionScope scope,
        Guid? requestedLocationId,
        CancellationToken cancellationToken)
    {
        var locationIds = await _dbContext.GymLocations
            .AsNoTracking()
            .Where(location => location.GymId == gymId && location.IsActive)
            .Select(location => location.Id)
            .ToListAsync(cancellationToken);

        var allowedLocationIds = locationIds
            .Where(scope.CanAccessLocation)
            .ToHashSet();

        if (requestedLocationId.HasValue)
        {
            return allowedLocationIds.Contains(requestedLocationId.Value)
                ? [requestedLocationId.Value]
                : [];
        }

        return allowedLocationIds;
    }

    private async Task<TenantRoleAssignment?> ResolveAssignmentAsync(
        Guid gymId,
        Guid? assignmentId,
        CancellationToken cancellationToken)
    {
        if (!assignmentId.HasValue)
        {
            return null;
        }

        return await _dbContext.TenantRoleAssignments
            .Include(item => item.StaffProfile)
            .Include(item => item.User)
            .SingleOrDefaultAsync(
                item =>
                    item.Id == assignmentId.Value
                    && item.GymId == gymId
                    && item.Status == TenantRoleAssignmentStatus.Active
                    && item.RevokedAtUtc == null,
                cancellationToken);
    }

    private IQueryable<GymLead> LoadLeadsQuery(Guid gymId)
    {
        return _dbContext.GymLeads
            .AsNoTracking()
            .Where(lead => lead.GymId == gymId)
            .Include(lead => lead.Location)
            .Include(lead => lead.OwnerAssignment)
                .ThenInclude(assignment => assignment!.StaffProfile)
            .Include(lead => lead.OwnerAssignment)
                .ThenInclude(assignment => assignment!.User)
            .Include(lead => lead.Tasks)
                .ThenInclude(task => task.AssignedAssignment)
                    .ThenInclude(assignment => assignment!.StaffProfile)
            .Include(lead => lead.Tasks)
                .ThenInclude(task => task.AssignedAssignment)
                    .ThenInclude(assignment => assignment!.User);
    }

    private IQueryable<GymCampaign> LoadCampaignsQuery(Guid gymId)
    {
        return _dbContext.GymCampaigns
            .AsNoTracking()
            .Where(campaign => campaign.GymId == gymId)
            .Include(campaign => campaign.Location)
            .Include(campaign => campaign.OwnerAssignment)
                .ThenInclude(assignment => assignment!.StaffProfile)
            .Include(campaign => campaign.OwnerAssignment)
                .ThenInclude(assignment => assignment!.User)
            .Include(campaign => campaign.CreatedByUser);
    }

    private IQueryable<GymAutomationRule> LoadAutomationRulesQuery(Guid gymId)
    {
        return _dbContext.GymAutomationRules
            .AsNoTracking()
            .Where(rule => rule.GymId == gymId)
            .Include(rule => rule.Location)
            .Include(rule => rule.OwnerAssignment)
                .ThenInclude(assignment => assignment!.StaffProfile)
            .Include(rule => rule.OwnerAssignment)
                .ThenInclude(assignment => assignment!.User)
            .Include(rule => rule.CreatedByUser);
    }

    private async Task<int> ResolveCampaignAudienceCountAsync(
        GymCampaign campaign,
        CancellationToken cancellationToken)
    {
        return await ResolveAudienceCountAsync(
            campaign.GymId,
            campaign.LocationId,
            campaign.AudienceType,
            campaign.TargetLeadStage,
            cancellationToken);
    }

    private async Task<int> ResolveAutomationAudienceCountAsync(
        GymAutomationRule rule,
        CancellationToken cancellationToken)
    {
        return await ResolveAudienceCountAsync(
            rule.GymId,
            rule.LocationId,
            rule.AudienceType,
            rule.TargetLeadStage,
            cancellationToken);
    }

    private async Task<int> ResolveAudienceCountAsync(
        Guid gymId,
        Guid locationId,
        GymCampaignAudienceType audienceType,
        GymLeadStage? targetLeadStage,
        CancellationToken cancellationToken)
    {
        var startOfToday = DateTime.UtcNow.Date;
        var renewalWindowEnd = startOfToday.AddDays(30);

        return audienceType switch
        {
            GymCampaignAudienceType.ActiveMembers => await _dbContext.GymMemberships
                .AsNoTracking()
                .CountAsync(
                    membership =>
                        membership.GymId == gymId
                        && membership.Status == GymMembershipStatus.Active
                        && membership.Locations.Any(location => location.LocationId == locationId),
                    cancellationToken),
            GymCampaignAudienceType.ExpiringMemberships => await _dbContext.GymMemberships
                .AsNoTracking()
                .CountAsync(
                    membership =>
                        membership.GymId == gymId
                        && membership.Status == GymMembershipStatus.Active
                        && membership.EndedAtUtc.HasValue
                        && membership.EndedAtUtc.Value >= startOfToday
                        && membership.EndedAtUtc.Value <= renewalWindowEnd
                        && membership.Locations.Any(location => location.LocationId == locationId),
                    cancellationToken),
            GymCampaignAudienceType.LeadsDueFollowUp => await _dbContext.GymLeads
                .AsNoTracking()
                .CountAsync(
                    lead =>
                        lead.GymId == gymId
                        && lead.LocationId == locationId
                        && lead.Stage != GymLeadStage.Won
                        && lead.Stage != GymLeadStage.Lost
                        && lead.NextFollowUpAtUtc.HasValue
                        && lead.NextFollowUpAtUtc.Value <= startOfToday.AddDays(1),
                    cancellationToken),
            GymCampaignAudienceType.LeadsInStage when targetLeadStage.HasValue => await _dbContext.GymLeads
                .AsNoTracking()
                .CountAsync(
                    lead =>
                        lead.GymId == gymId
                        && lead.LocationId == locationId
                        && lead.Stage == targetLeadStage.Value,
                    cancellationToken),
            _ => 0
        };
    }

    private async Task<GymCampaignResponse> MapCampaignResponseAsync(
        GymCampaign campaign,
        CancellationToken cancellationToken)
    {
        var audienceCount = await ResolveCampaignAudienceCountAsync(campaign, cancellationToken);

        return new GymCampaignResponse(
            campaign.Id,
            campaign.GymId,
            campaign.LocationId,
            campaign.Location.Name,
            campaign.OwnerAssignmentId,
            campaign.OwnerAssignment?.StaffProfile?.DisplayName?.Trim()
                ?? campaign.OwnerAssignment?.User?.FullName?.Trim()
                ?? campaign.OwnerAssignment?.User?.Email?.Trim(),
            campaign.CreatedByUserId,
            campaign.CreatedByUser.FullName?.Trim()
                ?? campaign.CreatedByUser.Email?.Trim()
                ?? "Staff Betterfit",
            campaign.Name,
            campaign.Channel,
            campaign.AudienceType,
            campaign.TargetLeadStage,
            campaign.Status,
            campaign.Subject,
            campaign.Message,
            campaign.Notes,
            campaign.ScheduledAtUtc,
            campaign.SentAtUtc,
            audienceCount,
            campaign.LastAudienceCount,
            campaign.CreatedAtUtc,
            campaign.UpdatedAtUtc);
    }

    private async Task<GymAutomationRuleResponse> MapAutomationRuleResponseAsync(
        GymAutomationRule rule,
        CancellationToken cancellationToken)
    {
        var audienceCount = await ResolveAutomationAudienceCountAsync(rule, cancellationToken);

        return new GymAutomationRuleResponse(
            rule.Id,
            rule.GymId,
            rule.LocationId,
            rule.Location.Name,
            rule.OwnerAssignmentId,
            rule.OwnerAssignment?.StaffProfile?.DisplayName?.Trim()
                ?? rule.OwnerAssignment?.User?.FullName?.Trim()
                ?? rule.OwnerAssignment?.User?.Email?.Trim(),
            rule.CreatedByUserId,
            rule.CreatedByUser.FullName?.Trim()
                ?? rule.CreatedByUser.Email?.Trim()
                ?? "Staff Betterfit",
            rule.Name,
            rule.Channel,
            rule.AudienceType,
            rule.TargetLeadStage,
            rule.ScheduleType,
            rule.Status,
            rule.SubjectTemplate,
            rule.MessageTemplate,
            rule.Notes,
            rule.NextRunAtUtc,
            rule.LastRunAtUtc,
            audienceCount,
            rule.LastAudienceCount,
            rule.CreatedAtUtc,
            rule.UpdatedAtUtc);
    }

    private static IReadOnlyCollection<GymLeadStageSummaryResponse> BuildEmptyPipeline()
    {
        return Enum.GetValues<GymLeadStage>()
            .Select(stage => new GymLeadStageSummaryResponse(stage, 0))
            .ToList();
    }

    private static bool AssignmentCanOperateOnLocation(TenantRoleAssignment assignment, Guid locationId)
    {
        return assignment.ScopeType == TenantRoleAssignmentScopeType.Tenant || assignment.ScopeLocationId == locationId;
    }

    private static DateTime AdvanceAutomationNextRunAtUtc(DateTime baselineUtc, GymAutomationScheduleType scheduleType)
    {
        var normalizedBaseline = baselineUtc.Kind == DateTimeKind.Utc
            ? baselineUtc
            : baselineUtc.ToUniversalTime();

        return scheduleType == GymAutomationScheduleType.Weekly
            ? normalizedBaseline.AddDays(7)
            : normalizedBaseline.AddDays(1);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
