using Betterfit.Models;
using Betterfit.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Betterfit.Tests.TestInfrastructure;

internal static class IdentityTestFactory
{
    public static UserManager<ApplicationUser> CreateUserManager(Data.AppDbContext context)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        var options = Options.Create(new IdentityOptions
        {
            Password =
            {
                RequiredLength = 8,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = false,
                RequireNonAlphanumeric = false
            },
            Lockout =
            {
                AllowedForNewUsers = true,
                MaxFailedAccessAttempts = 5,
                DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)
            },
            SignIn =
            {
                RequireConfirmedEmail = true
            },
            Tokens =
            {
                AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider
            }
        });

        return new UserManager<ApplicationUser>(
            new UserStore<ApplicationUser>(context),
            options,
            new PasswordHasher<ApplicationUser>(),
            new List<IUserValidator<ApplicationUser>> { new UserValidator<ApplicationUser>() },
            new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services,
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }
}
