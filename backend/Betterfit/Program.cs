using System.Text;
using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Data;
using Betterfit.Infrastructure.Responses;
using Betterfit.Models;
using Betterfit.Services.Auth;
using Betterfit.Services.Roles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
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
        [new OpenApiSecuritySchemeReference("Bearer", document, null!)] = []
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
    })
    .AddSignInManager<SignInManager<ApplicationUser>>()
    .AddEntityFrameworkStores<AppDbContext>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException($"JWT settings are missing under '{JwtOptions.SectionName}'.");

if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
{
    throw new InvalidOperationException("JWT key must be at least 32 bytes.");
}

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IGymRoleBootstrapper, GymRoleBootstrapper>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuthorizationHandler, GymPermissionAuthorizationHandler>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
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
        policy => policy.RequireAuthenticatedUser().AddRequirements(new GymPermissionRequirement("gyms", "read")));

    options.AddPolicy(
        AuthorizationPolicies.GymsWrite,
        policy => policy.RequireAuthenticatedUser().AddRequirements(new GymPermissionRequirement("gyms", "write")));

    options.AddPolicy(
        AuthorizationPolicies.MembersRead,
        policy => policy.RequireAuthenticatedUser().AddRequirements(new GymPermissionRequirement("members", "read")));

    options.AddPolicy(
        AuthorizationPolicies.MembersWrite,
        policy => policy.RequireAuthenticatedUser().AddRequirements(new GymPermissionRequirement("members", "write")));

    options.AddPolicy(
        AuthorizationPolicies.RolesRead,
        policy => policy.RequireAuthenticatedUser().AddRequirements(new GymPermissionRequirement("roles", "read")));

    options.AddPolicy(
        AuthorizationPolicies.RolesWrite,
        policy => policy.RequireAuthenticatedUser().AddRequirements(new GymPermissionRequirement("roles", "write")));
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        _ = exceptionFeature?.Error;

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
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
