using Betterfit.Contracts.Auth;
using Betterfit.Contracts.Common;
using Betterfit.Models;
using Betterfit.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Betterfit.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        var email = request.Email.Trim();

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return ConflictError<AuthResponse>("A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim()
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var details = createResult.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.Description).ToArray());

            return BadRequestError<AuthResponse>(
                code: "identity_validation_failed",
                message: "Registration failed due to validation errors.",
                details: details);
        }

        var tokenResult = _jwtTokenService.GenerateToken(user);
        return Success(ToAuthResponse(user, tokenResult));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return UnauthorizedError<AuthResponse>("Invalid credentials.");
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!signInResult.Succeeded)
        {
            return UnauthorizedError<AuthResponse>("Invalid credentials.");
        }

        var tokenResult = _jwtTokenService.GenerateToken(user);
        return Success(ToAuthResponse(user, tokenResult));
    }

    private static AuthResponse ToAuthResponse(ApplicationUser user, TokenResult tokenResult)
    {
        return new AuthResponse(
            tokenResult.AccessToken,
            tokenResult.ExpiresAtUtc,
            new UserSummaryResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.FullName));
    }
}
