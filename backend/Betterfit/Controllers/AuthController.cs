using System.Security.Claims;
using Betterfit.Contracts.Auth;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.Gyms;
using Betterfit.Data;
using Betterfit.Infrastructure.Security;
using Betterfit.Models;
using Betterfit.Services.Accounts;
using Betterfit.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccountProfileService _accountProfileService;
    private readonly IAccountSessionService _accountSessionService;
    private readonly IAuthenticationWorkflowService _authenticationWorkflowService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IAccountProfileService accountProfileService,
        IAccountSessionService accountSessionService,
        IAuthenticationWorkflowService authenticationWorkflowService,
        AppDbContext dbContext,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _accountProfileService = accountProfileService;
        _accountSessionService = accountSessionService;
        _authenticationWorkflowService = authenticationWorkflowService;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthenticationFlowResponse>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationWorkflowService.RegisterAsync(request, cancellationToken);
        return ToAuthResult(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthenticationFlowResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationWorkflowService.LoginAsync(request, cancellationToken);
        return ToAuthResult(result);
    }

    [HttpPost("verify-email-code")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthenticationFlowResponse>>> VerifyEmailCode(
        [FromBody] VerifyEmailCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationWorkflowService.VerifyEmailCodeAsync(request, cancellationToken);
        return ToAuthResult(result);
    }

    [HttpPost("resend-email-code")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<EmailVerificationChallengeResponse>>> ResendEmailVerificationCode(
        [FromBody] ResendEmailVerificationCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationWorkflowService.ResendEmailVerificationCodeAsync(request, cancellationToken);
        return ToAuthResult(result);
    }

    [HttpPost("verification-session/status")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<EmailVerificationChallengeResponse>>> GetEmailVerificationSessionStatus(
        [FromBody] GetEmailVerificationSessionStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationWorkflowService.GetEmailVerificationSessionStatusAsync(request, cancellationToken);
        return ToAuthResult(result);
    }

    [HttpPost("2fa/setup")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TwoFactorSetupResponse>>> BeginTwoFactorSetup(
        [FromBody] BeginTwoFactorSetupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationWorkflowService.BeginTwoFactorSetupAsync(request, cancellationToken);
        return ToAuthResult(result);
    }

    [HttpPost("2fa/enable")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TwoFactorSetupCompletionResponse>>> EnableTwoFactor(
        [FromBody] EnableTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationWorkflowService.EnableTwoFactorAsync(request, cancellationToken);
        return ToAuthResult(result);
    }

    [HttpPost("2fa/verify")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> VerifyTwoFactor(
        [FromBody] VerifyTwoFactorCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationWorkflowService.VerifyTwoFactorAsync(request, cancellationToken);
        return ToAuthResult(result);
    }

    [HttpPost("2fa/recovery-code")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> UseRecoveryCode(
        [FromBody] UseRecoveryCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationWorkflowService.UseRecoveryCodeAsync(request, cancellationToken);
        return ToAuthResult(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CurrentAccountResponse>>> GetCurrentAccount(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedError<CurrentAccountResponse>();
        }

        var account = await _accountSessionService.BuildCurrentAccountAsync(userId, cancellationToken);
        return Success(account);
    }

    [HttpPost("claim-invitation")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CurrentAccountResponse>>> ClaimMembershipInvitation(
        [FromBody] ClaimGymInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var claimingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(claimingUserId))
        {
            return UnauthorizedError<CurrentAccountResponse>();
        }

        var claimingUser = await _userManager.FindByIdAsync(claimingUserId);
        if (claimingUser is null)
        {
            return UnauthorizedError<CurrentAccountResponse>();
        }

        var now = DateTime.UtcNow;
        var tokenHash = TokenHasher.ComputeTokenHash(request.Token.Trim());

        var invitation = await _dbContext.GymInvitations
            .Include(x => x.GymMembership)
                .ThenInclude(x => x!.Locations)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash && x.ExpiresAtUtc > now, cancellationToken);

        if (invitation is null)
        {
            return NotFoundError<CurrentAccountResponse>("Invitation not found or expired.");
        }

        var membership = invitation.GymMembership;
        if (membership is null)
        {
            return ConflictError<CurrentAccountResponse>("The linked membership no longer exists.");
        }

        if (membership.UserId is not null)
        {
            return ConflictError<CurrentAccountResponse>("This membership is already linked to an account.");
        }

        if (membership.Status != GymMembershipStatus.PendingClaim)
        {
            return ConflictError<CurrentAccountResponse>("Only pending-claim memberships can be claimed.");
        }

        // Merge pending profile fields into the global member profile
        var profile = await _accountProfileService.EnsureMemberProfileAsync(
            claimingUser,
            new MemberProfileRequest
            {
                FirstName = membership.PendingFirstName,
                LastName = membership.PendingLastName,
                BirthDate = membership.PendingDateOfBirth,
                EmergencyContactName = membership.PendingEmergencyContactName,
                EmergencyContactPhoneNumber = membership.PendingEmergencyContactPhoneNumber
            },
            membership,
            cancellationToken);

        membership.UserId = claimingUser.Id;
        membership.MemberProfileId = profile.Id;
        membership.Status = GymMembershipStatus.Active;
        membership.ClaimedAtUtc = now;
        membership.JoinedAtUtc ??= now;
        membership.UpdatedAtUtc = now;
        MembershipHelpers.ClearPendingMemberFields(membership);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {UserId} claimed invitation {InvitationId} for gym {GymId}",
            claimingUserId, invitation.Id, membership.GymId);

        var account = await _accountSessionService.BuildCurrentAccountAsync(claimingUser.Id, cancellationToken);
        return Success(account);
    }

    private ActionResult<ApiResponse<T>> ToAuthResult<T>(AuthenticationOperationResult<T> result)
    {
        ApplyResponseHeaders(result.ResponseHeaders);

        return result.ErrorKind switch
        {
            AuthenticationOperationErrorKind.None => Success(result.Payload!),
            AuthenticationOperationErrorKind.BadRequest => BadRequestError<T>(
                result.ErrorCode ?? "bad_request",
                result.ErrorMessage ?? "The request is invalid.",
                result.Details),
            AuthenticationOperationErrorKind.Unauthorized => UnauthorizedError<T>(
                result.ErrorMessage ?? "Authentication failed.",
                result.ErrorCode ?? "unauthorized"),
            AuthenticationOperationErrorKind.Conflict => ConflictError<T>(
                result.ErrorMessage ?? "Conflict.",
                result.Details,
                result.ErrorCode ?? "conflict"),
            AuthenticationOperationErrorKind.NotFound => NotFoundError<T>(
                result.ErrorMessage ?? "Resource not found.",
                result.ErrorCode ?? "not_found"),
            _ => BadRequestError<T>("bad_request", "The request is invalid.")
        };
    }

    private void ApplyResponseHeaders(IDictionary<string, string>? responseHeaders)
    {
        if (responseHeaders is null || responseHeaders.Count == 0)
        {
            return;
        }

        foreach (var (key, value) in responseHeaders)
        {
            Response.Headers[key] = value;
        }
    }
}
