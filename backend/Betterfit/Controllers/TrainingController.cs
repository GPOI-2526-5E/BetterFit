using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.Training;
using Betterfit.Data;
using Betterfit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/gyms/{gymId:guid}/training")]
public sealed class TrainingController : ApiControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public TrainingController(
        AppDbContext dbContext,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _userManager = userManager;
    }

    [HttpGet("overview")]
    [Authorize(Policy = AuthorizationPolicies.WorkoutsRead)]
    public async Task<ActionResult<ApiResponse<GymTrainingOverviewResponse>>> GetOverview(
        Guid gymId,
        [FromQuery] Guid? locationId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymTrainingOverviewResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Workouts,
            PermissionActions.Read,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymTrainingOverviewResponse>();
        }

        var allowedLocationIds = await ResolveAllowedLocationIdsAsync(gymId, scope, locationId, cancellationToken);
        var now = DateTime.UtcNow;
        if (allowedLocationIds.Count == 0)
        {
            return Success(new GymTrainingOverviewResponse(0, 0, 0, 0, [], now));
        }

        var recentAssignments = await LoadAssignmentsQuery(gymId)
            .Where(assignment => allowedLocationIds.Contains(assignment.LocationId))
            .OrderByDescending(assignment => assignment.AssignedAtUtc)
            .Take(12)
            .ToListAsync(cancellationToken);

        var overview = new GymTrainingOverviewResponse(
            await _dbContext.GymWorkoutExercises
                .AsNoTracking()
                .CountAsync(exercise => exercise.GymId == gymId && exercise.IsActive, cancellationToken),
            await _dbContext.GymWorkoutTemplates
                .AsNoTracking()
                .CountAsync(
                    template =>
                        template.GymId == gymId
                        && template.IsActive
                        && allowedLocationIds.Contains(template.LocationId),
                    cancellationToken),
            await _dbContext.GymWorkoutAssignments
                .AsNoTracking()
                .CountAsync(
                    assignment =>
                        assignment.GymId == gymId
                        && assignment.Status == GymWorkoutAssignmentStatus.Active
                        && allowedLocationIds.Contains(assignment.LocationId),
                    cancellationToken),
            await _dbContext.GymWorkoutAssignments
                .AsNoTracking()
                .CountAsync(
                    assignment =>
                        assignment.GymId == gymId
                        && assignment.Status == GymWorkoutAssignmentStatus.Active
                        && assignment.RevisionDueAtUtc.HasValue
                        && assignment.RevisionDueAtUtc.Value <= now
                        && allowedLocationIds.Contains(assignment.LocationId),
                    cancellationToken),
            recentAssignments.Select(MapAssignmentResponse).ToList(),
            now);

        return Success(overview);
    }

    [HttpGet("exercises")]
    [Authorize(Policy = AuthorizationPolicies.WorkoutsRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymWorkoutExerciseResponse>>>> GetExercises(
        Guid gymId,
        [FromQuery] string? search,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<IReadOnlyCollection<GymWorkoutExerciseResponse>>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Workouts,
            PermissionActions.Read,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymWorkoutExerciseResponse>>();
        }

        var query = _dbContext.GymWorkoutExercises
            .AsNoTracking()
            .Where(exercise => exercise.GymId == gymId && exercise.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(exercise =>
                exercise.Name.ToLower().Contains(normalizedSearch)
                || (exercise.MuscleGroup != null && exercise.MuscleGroup.ToLower().Contains(normalizedSearch))
                || (exercise.Equipment != null && exercise.Equipment.ToLower().Contains(normalizedSearch)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim().ToLowerInvariant();
            query = query.Where(exercise => exercise.Category != null && exercise.Category.ToLower() == normalizedCategory);
        }

        var exercises = await query
            .OrderBy(exercise => exercise.Name)
            .ToListAsync(cancellationToken);

        return Success<IReadOnlyCollection<GymWorkoutExerciseResponse>>(
            exercises.Select(MapExerciseResponse).ToList());
    }

    [HttpPost("exercises")]
    [Authorize(Policy = AuthorizationPolicies.WorkoutsWrite)]
    public async Task<ActionResult<ApiResponse<GymWorkoutExerciseResponse>>> CreateExercise(
        Guid gymId,
        [FromBody] CreateGymWorkoutExerciseRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymWorkoutExerciseResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Workouts,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymWorkoutExerciseResponse>();
        }

        var now = DateTime.UtcNow;
        var exercise = new GymWorkoutExercise
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            Name = request.Name.Trim(),
            Category = NormalizeOptional(request.Category),
            MuscleGroup = NormalizeOptional(request.MuscleGroup),
            Equipment = NormalizeOptional(request.Equipment),
            Instructions = NormalizeOptional(request.Instructions),
            VideoUrl = NormalizeOptional(request.VideoUrl),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.GymWorkoutExercises.Add(exercise);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Success(MapExerciseResponse(exercise));
    }

    [HttpGet("templates")]
    [Authorize(Policy = AuthorizationPolicies.WorkoutsRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymWorkoutTemplateResponse>>>> GetTemplates(
        Guid gymId,
        [FromQuery] Guid? locationId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<IReadOnlyCollection<GymWorkoutTemplateResponse>>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Workouts,
            PermissionActions.Read,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymWorkoutTemplateResponse>>();
        }

        var allowedLocationIds = await ResolveAllowedLocationIdsAsync(gymId, scope, locationId, cancellationToken);
        var templates = await LoadTemplatesQuery(gymId)
            .Where(template => allowedLocationIds.Contains(template.LocationId))
            .OrderBy(template => template.Name)
            .ToListAsync(cancellationToken);

        return Success<IReadOnlyCollection<GymWorkoutTemplateResponse>>(
            templates.Select(MapTemplateResponse).ToList());
    }

    [HttpPost("templates")]
    [Authorize(Policy = AuthorizationPolicies.WorkoutsWrite)]
    public async Task<ActionResult<ApiResponse<GymWorkoutTemplateResponse>>> CreateTemplate(
        Guid gymId,
        [FromBody] CreateGymWorkoutTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymWorkoutTemplateResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Workouts,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess || !scope.CanAccessLocation(request.LocationId))
        {
            return ForbiddenError<GymWorkoutTemplateResponse>();
        }

        var location = await _dbContext.GymLocations
            .SingleOrDefaultAsync(
                item => item.Id == request.LocationId && item.GymId == gymId && item.IsActive,
                cancellationToken);

        if (location is null)
        {
            return BadRequestError<GymWorkoutTemplateResponse>("invalid_location", "The selected location is not valid for this gym.");
        }

        var templateItemsValidation = ValidateTemplateItems(request);
        if (templateItemsValidation is not null)
        {
            return BadRequestError<GymWorkoutTemplateResponse>(templateItemsValidation.Value.Code, templateItemsValidation.Value.Message);
        }

        var coachAssignment = await ResolveCoachAssignmentAsync(gymId, request.CoachAssignmentId, cancellationToken);
        if (request.CoachAssignmentId.HasValue && coachAssignment is null)
        {
            return BadRequestError<GymWorkoutTemplateResponse>("invalid_coach", "The selected coach assignment is not valid.");
        }

        if (coachAssignment is not null && !CoachCanOperateOnLocation(coachAssignment, request.LocationId))
        {
            return BadRequestError<GymWorkoutTemplateResponse>("invalid_coach_scope", "The selected coach is not enabled for this location.");
        }

        var exerciseMap = await LoadExerciseMapAsync(gymId, request.Items, cancellationToken);
        if (exerciseMap.Count != request.Items.Count(item => item.ExerciseId.HasValue))
        {
            return BadRequestError<GymWorkoutTemplateResponse>("invalid_exercise", "One or more exercises are not valid for this gym.");
        }

        var now = DateTime.UtcNow;
        var template = new GymWorkoutTemplate
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            LocationId = request.LocationId,
            CoachAssignmentId = coachAssignment?.Id,
            Name = request.Name.Trim(),
            Goal = NormalizeOptional(request.Goal),
            Level = request.Level,
            Description = NormalizeOptional(request.Description),
            DaysPerWeek = request.DaysPerWeek,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Items = request.Items
                .Select(item =>
                {
                    var exercise = item.ExerciseId.HasValue
                        ? exerciseMap[item.ExerciseId.Value]
                        : null;

                    return new GymWorkoutTemplateItem
                    {
                        Id = Guid.NewGuid(),
                        ExerciseId = item.ExerciseId,
                        DayNumber = item.DayNumber,
                        SortOrder = item.SortOrder,
                        ExerciseName = exercise?.Name ?? item.ExerciseName.Trim(),
                        SetsPrescription = item.SetsPrescription.Trim(),
                        RepetitionsPrescription = item.RepetitionsPrescription.Trim(),
                        RestSeconds = item.RestSeconds,
                        Tempo = NormalizeOptional(item.Tempo),
                        Notes = NormalizeOptional(item.Notes)
                    };
                })
                .ToList()
        };

        _dbContext.GymWorkoutTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadTemplatesQuery(gymId)
            .SingleAsync(item => item.Id == template.Id, cancellationToken);

        return Success(MapTemplateResponse(saved));
    }

    [HttpPatch("templates/{templateId:guid}/activation")]
    [Authorize(Policy = AuthorizationPolicies.WorkoutsWrite)]
    public async Task<ActionResult<ApiResponse<GymWorkoutTemplateResponse>>> UpdateTemplateActivation(
        Guid gymId,
        Guid templateId,
        [FromBody] UpdateGymWorkoutTemplateActivationRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymWorkoutTemplateResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Workouts,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymWorkoutTemplateResponse>();
        }

        var template = await _dbContext.GymWorkoutTemplates
            .Include(item => item.Location)
            .Include(item => item.CoachAssignment)
                .ThenInclude(assignment => assignment!.StaffProfile)
            .Include(item => item.CoachAssignment)
                .ThenInclude(assignment => assignment!.User)
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == templateId && item.GymId == gymId, cancellationToken);

        if (template is null)
        {
            return NotFoundError<GymWorkoutTemplateResponse>("Template not found.");
        }

        if (!scope.CanAccessLocation(template.LocationId))
        {
            return ForbiddenError<GymWorkoutTemplateResponse>();
        }

        if (template.IsActive == request.IsActive)
        {
            return ConflictError<GymWorkoutTemplateResponse>("The template is already in the selected state.");
        }

        template.IsActive = request.IsActive;
        template.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Success(MapTemplateResponse(template));
    }

    [HttpGet("assignments")]
    [Authorize(Policy = AuthorizationPolicies.WorkoutsRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymWorkoutAssignmentResponse>>>> GetAssignments(
        Guid gymId,
        [FromQuery] Guid? locationId,
        [FromQuery] Guid? membershipId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<IReadOnlyCollection<GymWorkoutAssignmentResponse>>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Workouts,
            PermissionActions.Read,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymWorkoutAssignmentResponse>>();
        }

        var allowedLocationIds = await ResolveAllowedLocationIdsAsync(gymId, scope, locationId, cancellationToken);
        var query = LoadAssignmentsQuery(gymId)
            .Where(assignment => allowedLocationIds.Contains(assignment.LocationId));

        if (membershipId.HasValue)
        {
            query = query.Where(assignment => assignment.GymMembershipId == membershipId.Value);
        }

        var assignments = await query
            .OrderByDescending(assignment => assignment.AssignedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return Success<IReadOnlyCollection<GymWorkoutAssignmentResponse>>(
            assignments.Select(MapAssignmentResponse).ToList());
    }

    [HttpPost("assignments")]
    [Authorize(Policy = AuthorizationPolicies.WorkoutsWrite)]
    public async Task<ActionResult<ApiResponse<GymWorkoutAssignmentResponse>>> CreateAssignment(
        Guid gymId,
        [FromBody] CreateGymWorkoutAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymWorkoutAssignmentResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Workouts,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymWorkoutAssignmentResponse>();
        }

        var template = await LoadTemplatesQuery(gymId)
            .SingleOrDefaultAsync(item => item.Id == request.TemplateId && item.IsActive, cancellationToken);

        if (template is null)
        {
            return BadRequestError<GymWorkoutAssignmentResponse>("invalid_template", "The selected workout template is not valid.");
        }

        if (!scope.CanAccessLocation(template.LocationId))
        {
            return ForbiddenError<GymWorkoutAssignmentResponse>("This template belongs to a location outside your current scope.");
        }

        var membership = await _dbContext.GymMemberships
            .Include(item => item.User)
            .Include(item => item.MemberProfile)
            .Include(item => item.Locations)
            .SingleOrDefaultAsync(item => item.Id == request.MembershipId && item.GymId == gymId, cancellationToken);

        if (membership is null)
        {
            return NotFoundError<GymWorkoutAssignmentResponse>("Membership not found.");
        }

        if (membership.Status != GymMembershipStatus.Active)
        {
            return ConflictError<GymWorkoutAssignmentResponse>("Only active memberships can receive workout plans.");
        }

        if (!membership.Locations.Any(item => item.LocationId == template.LocationId))
        {
            return ConflictError<GymWorkoutAssignmentResponse>("The selected member is not enabled for this plan location.");
        }

        var now = DateTime.UtcNow;
        var assignment = new GymWorkoutAssignment
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            GymMembershipId = membership.Id,
            LocationId = template.LocationId,
            GymWorkoutTemplateId = template.Id,
            CoachAssignmentId = template.CoachAssignmentId,
            CreatedByUserId = actorUserId,
            Title = template.Name,
            Goal = template.Goal,
            Status = GymWorkoutAssignmentStatus.Active,
            AssignedAtUtc = now,
            StartsAtUtc = request.StartsAtUtc,
            RevisionDueAtUtc = request.RevisionDueAtUtc,
            Notes = NormalizeOptional(request.Notes),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Items = template.Items
                .OrderBy(item => item.DayNumber)
                .ThenBy(item => item.SortOrder)
                .Select(item => new GymWorkoutAssignmentItem
                {
                    Id = Guid.NewGuid(),
                    ExerciseId = item.ExerciseId,
                    DayNumber = item.DayNumber,
                    SortOrder = item.SortOrder,
                    ExerciseName = item.ExerciseName,
                    SetsPrescription = item.SetsPrescription,
                    RepetitionsPrescription = item.RepetitionsPrescription,
                    RestSeconds = item.RestSeconds,
                    Tempo = item.Tempo,
                    Notes = item.Notes
                })
                .ToList()
        };

        _dbContext.GymWorkoutAssignments.Add(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadAssignmentsQuery(gymId)
            .SingleAsync(item => item.Id == assignment.Id, cancellationToken);

        return Success(MapAssignmentResponse(saved));
    }

    [HttpPatch("assignments/{assignmentId:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.WorkoutsWrite)]
    public async Task<ActionResult<ApiResponse<GymWorkoutAssignmentResponse>>> UpdateAssignmentStatus(
        Guid gymId,
        Guid assignmentId,
        [FromBody] UpdateGymWorkoutAssignmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymWorkoutAssignmentResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Workouts,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymWorkoutAssignmentResponse>();
        }

        var assignment = await _dbContext.GymWorkoutAssignments
            .Include(item => item.Location)
            .SingleOrDefaultAsync(item => item.Id == assignmentId && item.GymId == gymId, cancellationToken);

        if (assignment is null)
        {
            return NotFoundError<GymWorkoutAssignmentResponse>("Workout assignment not found.");
        }

        if (!scope.CanAccessLocation(assignment.LocationId))
        {
            return ForbiddenError<GymWorkoutAssignmentResponse>();
        }

        if (assignment.Status == request.Status)
        {
            return ConflictError<GymWorkoutAssignmentResponse>("The workout assignment is already in the selected status.");
        }

        var now = DateTime.UtcNow;
        assignment.Status = request.Status;
        assignment.CompletedAtUtc = request.Status switch
        {
            GymWorkoutAssignmentStatus.Completed => request.CompletedAtUtc ?? now,
            GymWorkoutAssignmentStatus.Active => null,
            _ => assignment.CompletedAtUtc
        };
        assignment.UpdatedAtUtc = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadAssignmentsQuery(gymId)
            .SingleAsync(item => item.Id == assignment.Id, cancellationToken);

        return Success(MapAssignmentResponse(saved));
    }

    [HttpGet("measurements")]
    [Authorize(Policy = AuthorizationPolicies.WorkoutsRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymWorkoutAssessmentResponse>>>> GetMeasurements(
        Guid gymId,
        [FromQuery] Guid? locationId,
        [FromQuery] Guid? membershipId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<IReadOnlyCollection<GymWorkoutAssessmentResponse>>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Workouts,
            PermissionActions.Read,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymWorkoutAssessmentResponse>>();
        }

        var allowedLocationIds = await ResolveAllowedLocationIdsAsync(gymId, scope, locationId, cancellationToken);
        var query = LoadAssessmentsQuery(gymId)
            .Where(assessment => allowedLocationIds.Contains(assessment.LocationId));

        if (membershipId.HasValue)
        {
            query = query.Where(assessment => assessment.GymMembershipId == membershipId.Value);
        }

        var measurements = await query
            .OrderByDescending(assessment => assessment.RecordedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return Success<IReadOnlyCollection<GymWorkoutAssessmentResponse>>(
            measurements.Select(MapAssessmentResponse).ToList());
    }

    [HttpPost("measurements")]
    [Authorize(Policy = AuthorizationPolicies.WorkoutsWrite)]
    public async Task<ActionResult<ApiResponse<GymWorkoutAssessmentResponse>>> CreateMeasurement(
        Guid gymId,
        [FromBody] CreateGymWorkoutAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymWorkoutAssessmentResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Workouts,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess || !scope.CanAccessLocation(request.LocationId))
        {
            return ForbiddenError<GymWorkoutAssessmentResponse>();
        }

        if (!HasAssessmentContent(request))
        {
            return BadRequestError<GymWorkoutAssessmentResponse>(
                "missing_measurements",
                "At least one measurement or note is required.");
        }

        var location = await _dbContext.GymLocations
            .SingleOrDefaultAsync(
                item => item.Id == request.LocationId && item.GymId == gymId && item.IsActive,
                cancellationToken);

        if (location is null)
        {
            return BadRequestError<GymWorkoutAssessmentResponse>(
                "invalid_location",
                "The selected location is not valid for this gym.");
        }

        var membership = await _dbContext.GymMemberships
            .Include(item => item.User)
            .Include(item => item.MemberProfile)
            .Include(item => item.Locations)
            .SingleOrDefaultAsync(item => item.Id == request.MembershipId && item.GymId == gymId, cancellationToken);

        if (membership is null)
        {
            return NotFoundError<GymWorkoutAssessmentResponse>("Membership not found.");
        }

        if (!CanRecordAssessmentForMembership(membership))
        {
            return ConflictError<GymWorkoutAssessmentResponse>(
                "Only active or suspended memberships can receive training assessments.");
        }

        var membershipHasLocation = membership.PrimaryLocationId == request.LocationId
            || membership.Locations.Any(item => item.LocationId == request.LocationId);
        if (!membershipHasLocation)
        {
            return ConflictError<GymWorkoutAssessmentResponse>(
                "The selected member is not enabled for this assessment location.");
        }

        var coachAssignment = await ResolveCoachAssignmentAsync(gymId, request.CoachAssignmentId, cancellationToken);
        if (request.CoachAssignmentId.HasValue && coachAssignment is null)
        {
            return BadRequestError<GymWorkoutAssessmentResponse>(
                "invalid_coach",
                "The selected coach assignment is not valid.");
        }

        if (coachAssignment is not null && !CoachCanOperateOnLocation(coachAssignment, request.LocationId))
        {
            return BadRequestError<GymWorkoutAssessmentResponse>(
                "invalid_coach_scope",
                "The selected coach is not enabled for this location.");
        }

        var recordedAtUtc = request.RecordedAtUtc ?? DateTime.UtcNow;
        var now = DateTime.UtcNow;
        var assessment = new GymWorkoutAssessment
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            GymMembershipId = membership.Id,
            LocationId = request.LocationId,
            CoachAssignmentId = coachAssignment?.Id,
            RecordedByUserId = actorUserId,
            RecordedAtUtc = recordedAtUtc,
            WeightKg = request.WeightKg,
            BodyFatPercentage = request.BodyFatPercentage,
            LeanMassKg = request.LeanMassKg,
            ChestCm = request.ChestCm,
            WaistCm = request.WaistCm,
            HipsCm = request.HipsCm,
            ArmCm = request.ArmCm,
            ThighCm = request.ThighCm,
            CalfCm = request.CalfCm,
            RestingHeartRateBpm = request.RestingHeartRateBpm,
            Notes = NormalizeOptional(request.Notes),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.GymWorkoutAssessments.Add(assessment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadAssessmentsQuery(gymId)
            .SingleAsync(item => item.Id == assessment.Id, cancellationToken);

        return Success(MapAssessmentResponse(saved));
    }

    private async Task<HashSet<Guid>> ResolveAllowedLocationIdsAsync(
        Guid gymId,
        GymPermissionScope scope,
        Guid? requestedLocationId,
        CancellationToken cancellationToken)
    {
        var locations = await _dbContext.GymLocations
            .AsNoTracking()
            .Where(location => location.GymId == gymId && location.IsActive)
            .Select(location => location.Id)
            .ToListAsync(cancellationToken);

        var allowedLocationIds = locations
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

    private async Task<TenantRoleAssignment?> ResolveCoachAssignmentAsync(
        Guid gymId,
        Guid? coachAssignmentId,
        CancellationToken cancellationToken)
    {
        if (!coachAssignmentId.HasValue)
        {
            return null;
        }

        return await _dbContext.TenantRoleAssignments
            .Include(item => item.StaffProfile)
            .Include(item => item.User)
            .SingleOrDefaultAsync(
                item =>
                    item.Id == coachAssignmentId.Value
                    && item.GymId == gymId
                    && item.Status == TenantRoleAssignmentStatus.Active
                    && item.RevokedAtUtc == null,
                cancellationToken);
    }

    private async Task<Dictionary<Guid, GymWorkoutExercise>> LoadExerciseMapAsync(
        Guid gymId,
        IEnumerable<CreateGymWorkoutTemplateItemRequest> items,
        CancellationToken cancellationToken)
    {
        var exerciseIds = items
            .Where(item => item.ExerciseId.HasValue)
            .Select(item => item.ExerciseId!.Value)
            .Distinct()
            .ToList();

        if (exerciseIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.GymWorkoutExercises
            .Where(exercise => exercise.GymId == gymId && exercise.IsActive && exerciseIds.Contains(exercise.Id))
            .ToDictionaryAsync(exercise => exercise.Id, cancellationToken);
    }

    private IQueryable<GymWorkoutTemplate> LoadTemplatesQuery(Guid gymId)
    {
        return _dbContext.GymWorkoutTemplates
            .AsNoTracking()
            .Where(template => template.GymId == gymId)
            .Include(template => template.Location)
            .Include(template => template.CoachAssignment)
                .ThenInclude(assignment => assignment!.StaffProfile)
            .Include(template => template.CoachAssignment)
                .ThenInclude(assignment => assignment!.User)
            .Include(template => template.Items);
    }

    private IQueryable<GymWorkoutAssignment> LoadAssignmentsQuery(Guid gymId)
    {
        return _dbContext.GymWorkoutAssignments
            .AsNoTracking()
            .Where(assignment => assignment.GymId == gymId)
            .Include(assignment => assignment.Location)
            .Include(assignment => assignment.Template)
            .Include(assignment => assignment.CoachAssignment)
                .ThenInclude(coach => coach!.StaffProfile)
            .Include(assignment => assignment.CoachAssignment)
                .ThenInclude(coach => coach!.User)
            .Include(assignment => assignment.Membership)
                .ThenInclude(membership => membership.User)
            .Include(assignment => assignment.Membership)
                .ThenInclude(membership => membership.MemberProfile)
            .Include(assignment => assignment.Items);
    }

    private IQueryable<GymWorkoutAssessment> LoadAssessmentsQuery(Guid gymId)
    {
        return _dbContext.GymWorkoutAssessments
            .AsNoTracking()
            .Where(assessment => assessment.GymId == gymId)
            .Include(assessment => assessment.Location)
            .Include(assessment => assessment.CoachAssignment)
                .ThenInclude(coach => coach!.StaffProfile)
            .Include(assessment => assessment.CoachAssignment)
                .ThenInclude(coach => coach!.User)
            .Include(assessment => assessment.Membership)
                .ThenInclude(membership => membership.User)
            .Include(assessment => assessment.Membership)
                .ThenInclude(membership => membership.MemberProfile)
            .Include(assessment => assessment.RecordedByUser);
    }

    private static (string Code, string Message)? ValidateTemplateItems(CreateGymWorkoutTemplateRequest request)
    {
        if (request.Items.Count == 0)
        {
            return ("missing_items", "At least one workout exercise is required.");
        }

        if (request.Items.Any(item => item.DayNumber > request.DaysPerWeek))
        {
            return ("invalid_day_number", "One or more items use a day number outside the template schedule.");
        }

        if (request.Items.GroupBy(item => new { item.DayNumber, item.SortOrder }).Any(group => group.Count() > 1))
        {
            return ("duplicate_sort_order", "Each workout day must use unique sort positions.");
        }

        return null;
    }

    private static bool CoachCanOperateOnLocation(TenantRoleAssignment coachAssignment, Guid locationId)
    {
        return coachAssignment.ScopeType == TenantRoleAssignmentScopeType.Tenant
            || coachAssignment.ScopeLocationId == locationId;
    }

    private static bool CanRecordAssessmentForMembership(GymMembership membership)
    {
        return membership.Status is GymMembershipStatus.Active or GymMembershipStatus.Suspended;
    }

    private static bool HasAssessmentContent(CreateGymWorkoutAssessmentRequest request)
    {
        return request.WeightKg.HasValue
            || request.BodyFatPercentage.HasValue
            || request.LeanMassKg.HasValue
            || request.ChestCm.HasValue
            || request.WaistCm.HasValue
            || request.HipsCm.HasValue
            || request.ArmCm.HasValue
            || request.ThighCm.HasValue
            || request.CalfCm.HasValue
            || request.RestingHeartRateBpm.HasValue
            || !string.IsNullOrWhiteSpace(request.Notes);
    }

    private static GymWorkoutExerciseResponse MapExerciseResponse(GymWorkoutExercise exercise)
    {
        return new GymWorkoutExerciseResponse(
            exercise.Id,
            exercise.GymId,
            exercise.Name,
            exercise.Category,
            exercise.MuscleGroup,
            exercise.Equipment,
            exercise.Instructions,
            exercise.VideoUrl,
            exercise.IsActive,
            exercise.CreatedAtUtc,
            exercise.UpdatedAtUtc);
    }

    private static GymWorkoutTemplateResponse MapTemplateResponse(GymWorkoutTemplate template)
    {
        return new GymWorkoutTemplateResponse(
            template.Id,
            template.GymId,
            template.LocationId,
            template.Location.Name,
            template.CoachAssignmentId,
            ResolveCoachName(template.CoachAssignment),
            template.Name,
            template.Goal,
            template.Level,
            template.Description,
            template.DaysPerWeek,
            template.IsActive,
            template.CreatedAtUtc,
            template.UpdatedAtUtc,
            template.Items
                .OrderBy(item => item.DayNumber)
                .ThenBy(item => item.SortOrder)
                .Select(MapTemplateItemResponse)
                .ToList());
    }

    private static GymWorkoutTemplateItemResponse MapTemplateItemResponse(GymWorkoutTemplateItem item)
    {
        return new GymWorkoutTemplateItemResponse(
            item.Id,
            item.ExerciseId,
            item.ExerciseName,
            item.DayNumber,
            item.SortOrder,
            item.SetsPrescription,
            item.RepetitionsPrescription,
            item.RestSeconds,
            item.Tempo,
            item.Notes);
    }

    private static GymWorkoutAssignmentResponse MapAssignmentResponse(GymWorkoutAssignment assignment)
    {
        return new GymWorkoutAssignmentResponse(
            assignment.Id,
            assignment.GymId,
            assignment.GymMembershipId,
            assignment.LocationId,
            assignment.Location.Name,
            assignment.GymWorkoutTemplateId,
            assignment.Template?.Name,
            assignment.CoachAssignmentId,
            ResolveCoachName(assignment.CoachAssignment),
            MembershipDisplayName(assignment.Membership),
            assignment.Membership.User?.Email?.Trim()
                ?? assignment.Membership.InvitationEmail?.Trim()
                ?? "Email non disponibile",
            assignment.Title,
            assignment.Goal,
            assignment.Status,
            assignment.AssignedAtUtc,
            assignment.StartsAtUtc,
            assignment.RevisionDueAtUtc,
            assignment.CompletedAtUtc,
            assignment.Notes,
            assignment.Items
                .OrderBy(item => item.DayNumber)
                .ThenBy(item => item.SortOrder)
                .Select(MapAssignmentItemResponse)
                .ToList());
    }

    private static GymWorkoutAssignmentItemResponse MapAssignmentItemResponse(GymWorkoutAssignmentItem item)
    {
        return new GymWorkoutAssignmentItemResponse(
            item.Id,
            item.ExerciseId,
            item.ExerciseName,
            item.DayNumber,
            item.SortOrder,
            item.SetsPrescription,
            item.RepetitionsPrescription,
            item.RestSeconds,
            item.Tempo,
            item.Notes);
    }

    private static GymWorkoutAssessmentResponse MapAssessmentResponse(GymWorkoutAssessment assessment)
    {
        return new GymWorkoutAssessmentResponse(
            assessment.Id,
            assessment.GymId,
            assessment.GymMembershipId,
            assessment.LocationId,
            assessment.Location.Name,
            assessment.CoachAssignmentId,
            ResolveCoachName(assessment.CoachAssignment),
            MembershipDisplayName(assessment.Membership),
            assessment.Membership.User?.Email?.Trim()
                ?? assessment.Membership.InvitationEmail?.Trim()
                ?? "Email non disponibile",
            assessment.RecordedByUserId,
            ResolveUserDisplayName(assessment.RecordedByUser),
            assessment.RecordedAtUtc,
            assessment.WeightKg,
            assessment.BodyFatPercentage,
            assessment.LeanMassKg,
            assessment.ChestCm,
            assessment.WaistCm,
            assessment.HipsCm,
            assessment.ArmCm,
            assessment.ThighCm,
            assessment.CalfCm,
            assessment.RestingHeartRateBpm,
            assessment.Notes);
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

    private static string ResolveCoachName(TenantRoleAssignment? coachAssignment)
    {
        var displayName = coachAssignment?.StaffProfile?.DisplayName?.Trim();
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        return coachAssignment?.User?.FullName?.Trim()
            ?? coachAssignment?.User?.Email?.Trim()
            ?? "Coach da assegnare";
    }

    private static string ResolveUserDisplayName(ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            return user.FullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            return user.Email.Trim();
        }

        return user.UserName?.Trim() ?? "Operatore Betterfit";
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
