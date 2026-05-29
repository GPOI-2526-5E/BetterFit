using System.Text;
using System.Text.Json.Serialization;
using System.Net.NetworkInformation;
using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Data;
using Betterfit.Infrastructure.Responses;
using Betterfit.Infrastructure.Swagger;
using Betterfit.Models;
using Betterfit.Services.Accounts;
using Betterfit.Services.Auth;
using Betterfit.Services.Development;
using Betterfit.Services.Gyms;
using Betterfit.Services.Roles;
using Betterfit.Services.StaffAssignments;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
const string DevelopmentCorsPolicy = "DevelopmentAllowAnyOrigin";
const int DevelopmentApiPort = 5299;

var shouldSeedFitup = args.Contains("--seed-fitup", StringComparer.OrdinalIgnoreCase)
    || TryGetArgumentValue(args, "--seed-fitup-gym-id", out _);
Guid? seedFitupGymId = null;
if (TryGetArgumentValue(args, "--seed-fitup-gym-id", out var seedFitupGymIdText))
{
    if (!Guid.TryParse(seedFitupGymIdText, out var parsedGymId))
    {
        throw new InvalidOperationException(
            $"Invalid value '{seedFitupGymIdText}' for --seed-fitup-gym-id. Expected a GUID.");
    }

    seedFitupGymId = parsedGymId;
}

if (!shouldSeedFitup && builder.Environment.IsDevelopment() && IsPortInUse(DevelopmentApiPort))
{
    Console.WriteLine($"Betterfit backend already running on http://localhost:{DevelopmentApiPort}. Duplicate startup skipped.");
    return;
}

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var details = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "The input was not valid."
                            : error.ErrorMessage)
                        .ToArray());

            var payload = ApiResponseFactory.Failure<object>(
                code: "validation_error",
                message: "Validation failed.",
                httpContext: context.HttpContext,
                details: details);

            return new BadRequestObjectResult(payload);
        };
    });
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy(
            DevelopmentCorsPolicy,
            policy => policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod());
    }
});
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(options =>
        !string.IsNullOrWhiteSpace(options.Issuer)
        && !string.IsNullOrWhiteSpace(options.Audience)
        && !string.IsNullOrWhiteSpace(options.Key)
        && Encoding.UTF8.GetByteCount(options.Key) >= 32,
        "JWT settings are invalid. Configure Jwt:Issuer, Jwt:Audience, and a Jwt:Key with at least 32 bytes.")
    .ValidateOnStart();
builder.Services
    .AddOptions<AuthenticationFlowOptions>()
    .Bind(builder.Configuration.GetSection(AuthenticationFlowOptions.SectionName))
    .Validate(options =>
        options.EmailVerificationCodeLength is >= 4 and <= 8
        && options.EmailVerificationCodeMinutes > 0
        && options.EmailVerificationSessionHours > 0
        && options.EmailVerificationMaxAttempts > 0
        && options.EmailVerificationResendCooldownSeconds >= 0
        && options.TwoFactorChallengeMinutes > 0
        && options.TwoFactorMaxAttempts > 0
        && options.RecoveryCodeCount > 0
        && !string.IsNullOrWhiteSpace(options.AuthenticatorIssuer),
        "AuthenticationFlow settings are invalid.")
    .ValidateOnStart();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Betterfit API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
    });

    options.OperationFilter<BearerAuthOperationFilter>();
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddDefaultTokenProviders()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddScoped<IAccountProfileService, AccountProfileService>();
builder.Services.AddScoped<IAccountSessionService, AccountSessionService>();
builder.Services.AddScoped<IAuthenticationChallengeService, AuthenticationChallengeService>();
builder.Services.AddScoped<IAuthenticationWorkflowService, AuthenticationWorkflowService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IGymProvisioningService, GymProvisioningService>();
builder.Services.AddScoped<IGymAuthenticationPolicyService, GymAuthenticationPolicyService>();
builder.Services.AddScoped<IGymRoleBootstrapper, GymRoleBootstrapper>();
builder.Services.AddScoped<FitUpSeedService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IStaffAssignmentService, StaffAssignmentService>();
builder.Services.AddScoped<IAuthorizationHandler, GymPermissionAuthorizationHandler>();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IEmailVerificationSender, DevelopmentEmailVerificationSender>();
}
else
{
    builder.Services.AddScoped<IEmailVerificationSender, UnconfiguredEmailVerificationSender>();
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
        var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");
        var jwtIssuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is required.");
        var jwtAudience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is required.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                if (context.Response.HasStarted)
                {
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                var payload = ApiResponseFactory.Failure<object>(
                    code: "unauthorized",
                    message: "Authentication is required to access this resource.",
                    httpContext: context.HttpContext);

                await context.Response.WriteAsJsonAsync(payload);
            },
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                var payload = ApiResponseFactory.Failure<object>(
                    code: "forbidden",
                    message: "You do not have permission to access this resource.",
                    httpContext: context.HttpContext);

                await context.Response.WriteAsJsonAsync(payload);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicies.GymsRead,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Gyms, PermissionActions.Read)));

    options.AddPolicy(
        AuthorizationPolicies.GymsWrite,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Gyms, PermissionActions.Write, GymPermissionMinimumScope.TenantWide)));

    options.AddPolicy(
        AuthorizationPolicies.LocationsRead,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Locations, PermissionActions.Read)));

    options.AddPolicy(
        AuthorizationPolicies.LocationsWrite,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Locations, PermissionActions.Write, GymPermissionMinimumScope.TenantWide)));

    options.AddPolicy(
        AuthorizationPolicies.MembersRead,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Members, PermissionActions.Read)));

    options.AddPolicy(
        AuthorizationPolicies.MembersWrite,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Members, PermissionActions.Write)));

    options.AddPolicy(
        AuthorizationPolicies.CrmRead,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Crm, PermissionActions.Read)));

    options.AddPolicy(
        AuthorizationPolicies.CrmWrite,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Crm, PermissionActions.Write)));

    options.AddPolicy(
        AuthorizationPolicies.StaffAssignmentsRead,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.StaffAssignments, PermissionActions.Read)));

    options.AddPolicy(
        AuthorizationPolicies.StaffAssignmentsWrite,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.StaffAssignments, PermissionActions.Write, GymPermissionMinimumScope.TenantWide)));

    options.AddPolicy(
        AuthorizationPolicies.RolesRead,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Roles, PermissionActions.Read, GymPermissionMinimumScope.TenantWide)));

    options.AddPolicy(
        AuthorizationPolicies.RolesWrite,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Roles, PermissionActions.Write, GymPermissionMinimumScope.TenantWide)));

    options.AddPolicy(
        AuthorizationPolicies.BillingRead,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Billing, PermissionActions.Read)));

    options.AddPolicy(
        AuthorizationPolicies.BillingWrite,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Billing, PermissionActions.Write)));

    options.AddPolicy(
        AuthorizationPolicies.ClassesRead,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Classes, PermissionActions.Read)));

    options.AddPolicy(
        AuthorizationPolicies.ClassesWrite,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Classes, PermissionActions.Write)));

    options.AddPolicy(
        AuthorizationPolicies.ReportsRead,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Reports, PermissionActions.Read)));

    options.AddPolicy(
        AuthorizationPolicies.WorkoutsRead,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Workouts, PermissionActions.Read)));

    options.AddPolicy(
        AuthorizationPolicies.WorkoutsWrite,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Workouts, PermissionActions.Write)));

    options.AddPolicy(
        AuthorizationPolicies.CheckinsApprove,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Checkins, PermissionActions.Approve)));

    options.AddPolicy(
        AuthorizationPolicies.SecurityPolicyRead,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.SecurityPolicy, PermissionActions.Read, GymPermissionMinimumScope.TenantWide)));

    options.AddPolicy(
        AuthorizationPolicies.SecurityPolicyWrite,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.SecurityPolicy, PermissionActions.Write, GymPermissionMinimumScope.TenantWide)));

    options.AddPolicy(
        AuthorizationPolicies.IntegrationsRead,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Integrations, PermissionActions.Read)));

    options.AddPolicy(
        AuthorizationPolicies.IntegrationsWrite,
        policy => policy.RequireAuthenticatedUser().AddRequirements(
            new GymPermissionRequirement(PermissionResources.Integrations, PermissionActions.Write)));
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (exceptionFeature?.Error is { } error)
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ExceptionHandler");
            logger.LogError(error, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var payload = ApiResponseFactory.Failure<object>(
            code: "internal_server_error",
            message: "An unexpected error occurred.",
            httpContext: context);

        await context.Response.WriteAsJsonAsync(payload);
    });
});

app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;

    var (code, message) = httpContext.Response.StatusCode switch
    {
        StatusCodes.Status400BadRequest => ("bad_request", "The request could not be processed."),
        StatusCodes.Status401Unauthorized => ("unauthorized", "Authentication is required to access this resource."),
        StatusCodes.Status403Forbidden => ("forbidden", "You do not have permission to access this resource."),
        StatusCodes.Status404NotFound => ("not_found", "The requested resource was not found."),
        StatusCodes.Status405MethodNotAllowed => ("method_not_allowed", "The HTTP method is not allowed for this endpoint."),
        _ => ("http_error", "The request could not be completed.")
    };

    var payload = ApiResponseFactory.Failure<object>(code, message, httpContext);
    await httpContext.Response.WriteAsJsonAsync(payload);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(DevelopmentCorsPolicy);
}

if (shouldSeedFitup)
{
    await using var scope = app.Services.CreateAsyncScope();
    var seedService = scope.ServiceProvider.GetRequiredService<FitUpSeedService>();
    await seedService.SeedAsync(seedFitupGymId, CancellationToken.None);
    return;
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var roleBootstrapper = scope.ServiceProvider.GetRequiredService<IGymRoleBootstrapper>();
    await roleBootstrapper.EnsureDefaultRoleTemplatePermissionsAsync(CancellationToken.None);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static bool IsPortInUse(int port)
{
    var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
    return ipGlobalProperties
        .GetActiveTcpListeners()
        .Any(endpoint => endpoint.Port == port);
}

static bool TryGetArgumentValue(string[] args, string optionName, out string value)
{
    for (var index = 0; index < args.Length; index++)
    {
        var argument = args[index];
        if (string.Equals(argument, optionName, StringComparison.OrdinalIgnoreCase))
        {
            if (index + 1 >= args.Length)
            {
                break;
            }

            value = args[index + 1];
            return true;
        }

        if (argument.StartsWith($"{optionName}=", StringComparison.OrdinalIgnoreCase))
        {
            value = argument[(optionName.Length + 1)..];
            return true;
        }
    }

    value = string.Empty;
    return false;
}

public partial class Program;
