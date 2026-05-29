using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Betterfit.Data;
using Betterfit.Models;
using Betterfit.Services.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Services.Development;

public sealed class FitUpSeedService
{
    private const string SeedTag = "[DEV-SEED FITUP]";
    private const string DefaultPassword = "Betterfit123!";
    private static readonly string[] SeedStaffEmails =
    [
        "owner@fitup.local",
        "manager@fitup.local",
        "reception@fitup.local",
        "coach@fitup.local"
    ];

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGymRoleBootstrapper _roleBootstrapper;
    private readonly ILogger<FitUpSeedService> _logger;

    public FitUpSeedService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IGymRoleBootstrapper roleBootstrapper,
        ILogger<FitUpSeedService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleBootstrapper = roleBootstrapper;
        _logger = logger;
    }

    public async Task SeedAsync(Guid? targetGymId, CancellationToken cancellationToken)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);

        var gym = await EnsureGymAsync(targetGymId, cancellationToken);
        await _roleBootstrapper.SeedDefaultRolesForGymAsync(gym.Id, null, cancellationToken);
        await _roleBootstrapper.EnsureDefaultRoleTemplatePermissionsAsync(cancellationToken);
        _dbContext.ChangeTracker.Clear();
        gym = await _dbContext.Gyms.SingleAsync(item => item.Id == gym.Id, cancellationToken);

        var roles = await _dbContext.GymRoles
            .Where(role => role.GymId == gym.Id)
            .ToDictionaryAsync(role => role.NormalizedName, cancellationToken);

        var locations = await EnsureLocationsAsync(gym, cancellationToken);
        var staff = await EnsureStaffAsync(gym, roles, locations, cancellationToken);
        var memberships = await EnsureMembershipsAsync(gym, locations, cancellationToken);
        var memberCustomFields = await EnsureMemberCustomFieldsAsync(gym, cancellationToken);
        await EnsureMembershipCustomFieldValuesAsync(gym, memberships, memberCustomFields, cancellationToken);

        await EnsureSaleCatalogAsync(gym, locations, cancellationToken);
        await EnsureSalesAsync(gym, memberships, staff, cancellationToken);
        await EnsureAccessEventsAsync(gym, memberships, staff, cancellationToken);
        await EnsureActivitiesAsync(gym, locations, memberships, staff, cancellationToken);
        await EnsureTrainingAsync(gym, locations, memberships, staff, cancellationToken);
        await EnsureCrmAsync(gym, locations, memberships, staff, cancellationToken);
        await EnsureCrmCampaignsAsync(gym, locations, staff, cancellationToken);
        await EnsureCrmAutomationsAsync(gym, locations, staff, cancellationToken);
        await EnsureIntegrationsAsync(gym, locations, cancellationToken);

        _logger.LogInformation("FitUp seed completed for gym {GymId}.", gym.Id);
    }

    public Task SeedAsync(CancellationToken cancellationToken)
        => SeedAsync(null, cancellationToken);

    private async Task<Gym> EnsureGymAsync(Guid? targetGymId, CancellationToken cancellationToken)
    {
        if (targetGymId.HasValue)
        {
            var requestedGym = await _dbContext.Gyms
                .Include(item => item.AuthenticationPolicy)
                .SingleOrDefaultAsync(item => item.Id == targetGymId.Value, cancellationToken);

            if (requestedGym is null)
            {
                throw new InvalidOperationException($"Gym {targetGymId.Value} was not found.");
            }

            if (requestedGym.AuthenticationPolicy is null)
            {
                var now = DateTime.UtcNow;
                _dbContext.GymAuthenticationPolicies.Add(new GymAuthenticationPolicy
                {
                    GymId = requestedGym.Id,
                    RequireTwoFactorForStaff = true,
                    RequireTwoFactorForMembers = false,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
                requestedGym.AuthenticationPolicy = await _dbContext.GymAuthenticationPolicies
                    .SingleAsync(item => item.GymId == requestedGym.Id, cancellationToken);
            }

            return requestedGym;
        }

        var seededGymId = StableGuid("fitup-gym");
        var gym = await _dbContext.Gyms
            .Include(item => item.AuthenticationPolicy)
            .SingleOrDefaultAsync(item => item.Id == seededGymId, cancellationToken);

        if (gym is null)
        {
            var namedGyms = await _dbContext.Gyms
                .Include(item => item.AuthenticationPolicy)
                .Where(item => item.Name == "FitUp")
                .OrderBy(item => item.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            if (namedGyms.Count > 1)
            {
                var candidateGymIds = namedGyms.Select(item => item.Id).ToArray();
                var preferredGymId = await _dbContext.TenantRoleAssignments
                    .AsNoTracking()
                    .Where(assignment =>
                        candidateGymIds.Contains(assignment.GymId)
                        && assignment.Status == TenantRoleAssignmentStatus.Active
                        && assignment.RevokedAtUtc == null
                        && SeedStaffEmails.Contains(assignment.User.Email!))
                    .GroupBy(assignment => assignment.GymId)
                    .Select(group => new
                    {
                        GymId = group.Key,
                        AssignmentCount = group.Count(),
                        PrimaryOwnerCount = group.Count(assignment => assignment.IsPrimaryOwner)
                    })
                    .OrderByDescending(item => item.PrimaryOwnerCount)
                    .ThenByDescending(item => item.AssignmentCount)
                    .Select(item => (Guid?)item.GymId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (preferredGymId.HasValue)
                {
                    gym = namedGyms.FirstOrDefault(item => item.Id == preferredGymId.Value);
                }
            }

            if (namedGyms.Count > 1)
            {
                _logger.LogWarning(
                    "Multiple gyms named FitUp were found ({Count}). The seed will continue using record {GymId}.",
                    namedGyms.Count,
                    (gym ?? namedGyms[0]).Id);
            }

            gym ??= namedGyms.FirstOrDefault();
        }

        if (gym is null)
        {
            var now = DateTime.UtcNow;
            gym = new Gym
            {
                Id = seededGymId,
                Name = "FitUp",
                CreatedAtUtc = now,
                AuthenticationPolicy = new GymAuthenticationPolicy
                {
                    GymId = seededGymId,
                    RequireTwoFactorForStaff = true,
                    RequireTwoFactorForMembers = false,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }
            };

            _dbContext.Gyms.Add(gym);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (gym.AuthenticationPolicy is null)
        {
            var now = DateTime.UtcNow;
            _dbContext.GymAuthenticationPolicies.Add(new GymAuthenticationPolicy
            {
                GymId = gym.Id,
                RequireTwoFactorForStaff = true,
                RequireTwoFactorForMembers = false,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return gym;
    }

    private async Task<Dictionary<string, GymLocation>> EnsureLocationsAsync(
        Gym gym,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new LocationSeed("fitup-location-vr-centro", "FitUp Verona Centro", "VR-CENTRO", "Via Roma 14", "Verona"),
            new LocationSeed("fitup-location-vr-san-zeno", "FitUp San Zeno", "VR-ZENO", "Via San Zeno 51", "Verona"),
            new LocationSeed("fitup-location-vr-borgo-roma", "FitUp Borgo Roma", "VR-ROMA", "Viale delle Nazioni 23", "Verona")
        };

        var locations = new Dictionary<string, GymLocation>(StringComparer.OrdinalIgnoreCase);
        foreach (var spec in specs)
        {
            var locationId = StableGymSeedGuid(gym.Id, spec.Key);
            var location = await _dbContext.GymLocations
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == locationId
                        || (item.GymId == gym.Id && (item.Code == spec.Code || item.Name == spec.Name)),
                    cancellationToken);

            if (location is null)
            {
                location = new GymLocation
                {
                    Id = locationId,
                    GymId = gym.Id,
                    Name = spec.Name,
                    Code = spec.Code,
                    AddressLine1 = spec.AddressLine1,
                    City = spec.City,
                    CountryCode = "IT",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _dbContext.GymLocations.Add(location);
            }
            else
            {
                location.Name = spec.Name;
                location.Code = spec.Code;
                location.AddressLine1 = spec.AddressLine1;
                location.City = spec.City;
                location.CountryCode = "IT";
                location.IsActive = true;
            }

            locations[spec.Code] = location;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return locations;
    }

    private async Task<StaffSeedContext> EnsureStaffAsync(
        Gym gym,
        IReadOnlyDictionary<string, GymRole> roles,
        IReadOnlyDictionary<string, GymLocation> locations,
        CancellationToken cancellationToken)
    {
        var ownerUser = await EnsureUserAsync("owner@fitup.local", "Marco Farina", "3331111001", cancellationToken);
        var managerUser = await EnsureUserAsync("manager@fitup.local", "Giulia Conti", "3331111002", cancellationToken);
        var receptionUser = await EnsureUserAsync("reception@fitup.local", "Sara Rinaldi", "3331111003", cancellationToken);
        var coachUser = await EnsureUserAsync("coach@fitup.local", "Andrea Bassi", "3331111004", cancellationToken);

        var ownerProfile = await EnsureStaffProfileAsync(ownerUser, "Marco Farina", "Owner", "OWN-01", cancellationToken);
        var managerProfile = await EnsureStaffProfileAsync(managerUser, "Giulia Conti", "Club Manager", "MGR-01", cancellationToken);
        var receptionProfile = await EnsureStaffProfileAsync(receptionUser, "Sara Rinaldi", "Reception", "REC-01", cancellationToken);
        var coachProfile = await EnsureStaffProfileAsync(coachUser, "Andrea Bassi", "Head Coach", "COA-01", cancellationToken);

        var ownerAssignment = await EnsureStaffAssignmentAsync(
            "fitup-assignment-owner",
            gym,
            ownerUser,
            ownerProfile,
            roles["OWNER"],
            TenantRoleAssignmentScopeType.Tenant,
            null,
            isPrimaryOwner: true,
            cancellationToken);

        var managerAssignment = await EnsureStaffAssignmentAsync(
            "fitup-assignment-manager",
            gym,
            managerUser,
            managerProfile,
            roles["MANAGER"],
            TenantRoleAssignmentScopeType.Tenant,
            null,
            isPrimaryOwner: false,
            cancellationToken);

        var receptionAssignment = await EnsureStaffAssignmentAsync(
            "fitup-assignment-reception",
            gym,
            receptionUser,
            receptionProfile,
            roles["RECEPTION"],
            TenantRoleAssignmentScopeType.Tenant,
            null,
            isPrimaryOwner: false,
            cancellationToken);

        var coachAssignment = await EnsureStaffAssignmentAsync(
            "fitup-assignment-coach",
            gym,
            coachUser,
            coachProfile,
            roles["COACH"],
            TenantRoleAssignmentScopeType.Location,
            locations["VR-CENTRO"].Id,
            isPrimaryOwner: false,
            cancellationToken);

        return new StaffSeedContext(
            ownerUser,
            managerUser,
            receptionUser,
            coachUser,
            ownerAssignment,
            managerAssignment,
            receptionAssignment,
            coachAssignment);
    }

    private async Task<List<MemberSeedContext>> EnsureMembershipsAsync(
        Gym gym,
        IReadOnlyDictionary<string, GymLocation> locations,
        CancellationToken cancellationToken)
    {
        var seeds = new[]
        {
            new MemberSeed("fitup-member-luca-rossi", "Luca", "Rossi", "luca.rossi@fitup.local", "3381000001", new DateOnly(1995, 5, 12), locations["VR-CENTRO"], new[] { locations["VR-CENTRO"], locations["VR-ZENO"] }, "Preparazione estate e dimagrimento"),
            new MemberSeed("fitup-member-marta-gallo", "Marta", "Gallo", "marta.gallo@fitup.local", "3381000002", new DateOnly(1992, 8, 3), locations["VR-CENTRO"], new[] { locations["VR-CENTRO"] }, "Allenamento post lavoro 3 volte a settimana"),
            new MemberSeed("fitup-member-davide-ferri", "Davide", "Ferri", "davide.ferri@fitup.local", "3381000003", new DateOnly(1988, 1, 27), locations["VR-ZENO"], new[] { locations["VR-ZENO"] }, "Percorso forza e mobilita"),
            new MemberSeed("fitup-member-alice-greco", "Alice", "Greco", "alice.greco@fitup.local", "3381000004", new DateOnly(1999, 11, 9), locations["VR-ROMA"], new[] { locations["VR-ROMA"] }, "Combo sala e corsi"),
            new MemberSeed("fitup-member-nicolo-marchi", "Nicolo", "Marchi", "nicolo.marchi@fitup.local", "3381000005", new DateOnly(1990, 2, 14), locations["VR-ZENO"], new[] { locations["VR-ZENO"], locations["VR-ROMA"] }, "Recupero forma fisica"),
            new MemberSeed("fitup-member-elena-moro", "Elena", "Moro", "elena.moro@fitup.local", "3381000006", new DateOnly(1987, 12, 20), locations["VR-CENTRO"], new[] { locations["VR-CENTRO"], locations["VR-ROMA"] }, "Personal training e tonificazione"),
            new MemberSeed("fitup-member-paolo-zanetti", "Paolo", "Zanetti", "paolo.zanetti@fitup.local", "3381000007", new DateOnly(1985, 4, 8), locations["VR-ZENO"], new[] { locations["VR-ZENO"], locations["VR-CENTRO"] }, "Rientro in palestra con programma progressivo"),
            new MemberSeed("fitup-member-francesca-longo", "Francesca", "Longo", "francesca.longo@fitup.local", "3381000008", new DateOnly(1994, 9, 19), locations["VR-ROMA"], new[] { locations["VR-ROMA"] }, "Focus glutei e corso functional"),
            new MemberSeed("fitup-member-simone-vitali", "Simone", "Vitali", "simone.vitali@fitup.local", "3381000009", new DateOnly(1991, 6, 2), locations["VR-CENTRO"], new[] { locations["VR-CENTRO"], locations["VR-ROMA"] }, "Allenamento pausa pranzo"),
            new MemberSeed("fitup-member-irene-bellini", "Irene", "Bellini", "irene.bellini@fitup.local", "3381000010", new DateOnly(1997, 3, 28), locations["VR-ZENO"], new[] { locations["VR-ZENO"] }, "Percorso ricomposizione e controllo alimentare"),
            new MemberSeed("fitup-member-gabriele-fontana", "Gabriele", "Fontana", "gabriele.fontana@fitup.local", "3381000011", new DateOnly(1983, 10, 11), locations["VR-ROMA"], new[] { locations["VR-ROMA"], locations["VR-ZENO"] }, "Abbonamento aziendale e workout veloci"),
            new MemberSeed("fitup-member-beatrice-marchesan", "Beatrice", "Marchesan", "beatrice.marchesan@fitup.local", "3381000012", new DateOnly(2000, 1, 5), locations["VR-CENTRO"], new[] { locations["VR-CENTRO"] }, "Ingresso giovane e primi corsi sala")
        };

        var memberships = new List<MemberSeedContext>(seeds.Length);
        foreach (var seed in seeds)
        {
            var user = await EnsureUserAsync(seed.Email, $"{seed.FirstName} {seed.LastName}", seed.PhoneNumber, cancellationToken);
            var profile = await EnsureMemberProfileAsync(user, seed, cancellationToken);
            var membership = await EnsureMembershipAsync(gym, user, profile, seed, cancellationToken);

            foreach (var location in seed.AllowedLocations)
            {
                var existingLink = await _dbContext.GymMembershipLocations
                    .SingleOrDefaultAsync(
                        item => item.GymMembershipId == membership.Id && item.LocationId == location.Id,
                        cancellationToken);

                if (existingLink is null)
                {
                    _dbContext.GymMembershipLocations.Add(new GymMembershipLocation
                    {
                        Id = StableGuid($"fitup-membership-location-{membership.Id}-{location.Id}"),
                        GymMembershipId = membership.Id,
                        LocationId = location.Id,
                        AssignedAtUtc = membership.JoinedAtUtc ?? membership.CreatedAtUtc
                    });
                }
            }

            memberships.Add(new MemberSeedContext(user, profile, membership, seed.PrimaryLocation));
        }

        await EnsurePendingDeskMembershipAsync(gym, locations["VR-CENTRO"], cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return memberships;
    }

    private async Task<Dictionary<string, GymCustomFieldDefinition>> EnsureMemberCustomFieldsAsync(
        Gym gym,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var specs = new[]
        {
            new CustomFieldSeed(
                "fitup-member-custom-field-obiettivo",
                "obiettivo_principale",
                "Obiettivo principale",
                "Obiettivo dichiarato in fase commerciale o di onboarding.",
                GymCustomFieldValueType.Select,
                new[] { "Dimagrimento", "Tonificazione", "Forza", "Ricomposizione", "Benessere" },
                IsRequired: true,
                SortOrder: 10),
            new CustomFieldSeed(
                "fitup-member-custom-field-certificato",
                "certificato_medico_scadenza",
                "Scadenza certificato medico",
                "Data di scadenza del certificato medico consegnato dal cliente.",
                GymCustomFieldValueType.Date,
                [],
                IsRequired: false,
                SortOrder: 20),
            new CustomFieldSeed(
                "fitup-member-custom-field-provenienza",
                "provenienza_cliente",
                "Provenienza cliente",
                "Canale con cui il cliente e arrivato in palestra.",
                GymCustomFieldValueType.Select,
                new[] { "Passaparola", "Instagram", "Meta Ads", "Walk-in", "Azienda" },
                IsRequired: false,
                SortOrder: 30),
            new CustomFieldSeed(
                "fitup-member-custom-field-mattina",
                "preferisce_mattina",
                "Preferenza fascia mattina",
                "Indica se il cliente preferisce allenarsi nella fascia mattutina.",
                GymCustomFieldValueType.Boolean,
                [],
                IsRequired: false,
                SortOrder: 40)
        };

        var definitions = new Dictionary<string, GymCustomFieldDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var spec in specs)
        {
            var definitionId = StableGymSeedGuid(gym.Id, spec.StableKey);
            var definition = await _dbContext.GymCustomFieldDefinitions
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == definitionId
                        || (item.GymId == gym.Id
                            && item.EntityType == GymCustomFieldEntityType.Member
                            && item.Key == spec.Key),
                    cancellationToken);

            if (definition is null)
            {
                definition = new GymCustomFieldDefinition
                {
                    Id = definitionId,
                    GymId = gym.Id,
                    EntityType = GymCustomFieldEntityType.Member,
                    CreatedAtUtc = now
                };
                _dbContext.GymCustomFieldDefinitions.Add(definition);
            }

            definition.Key = spec.Key;
            definition.Label = spec.Label;
            definition.Description = spec.Description;
            definition.ValueType = spec.ValueType;
            definition.OptionsJson = spec.Options.Count == 0 ? null : JsonSerializer.Serialize(spec.Options);
            definition.IsRequired = spec.IsRequired;
            definition.IsActive = true;
            definition.SortOrder = spec.SortOrder;
            definition.UpdatedAtUtc = now;

            definitions[spec.Key] = definition;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return definitions;
    }

    private async Task EnsureMembershipCustomFieldValuesAsync(
        Gym gym,
        IReadOnlyList<MemberSeedContext> memberships,
        IReadOnlyDictionary<string, GymCustomFieldDefinition> definitions,
        CancellationToken cancellationToken)
    {
        foreach (var membership in memberships)
        {
            var email = membership.User.Email?.ToLowerInvariant() ?? string.Empty;
            var objective = email switch
            {
                "luca.rossi@fitup.local" => "Dimagrimento",
                "marta.gallo@fitup.local" => "Tonificazione",
                "davide.ferri@fitup.local" => "Forza",
                "alice.greco@fitup.local" => "Benessere",
                "nicolo.marchi@fitup.local" => "Ricomposizione",
                "elena.moro@fitup.local" => "Tonificazione",
                "paolo.zanetti@fitup.local" => "Benessere",
                "francesca.longo@fitup.local" => "Tonificazione",
                "simone.vitali@fitup.local" => "Benessere",
                "irene.bellini@fitup.local" => "Ricomposizione",
                "gabriele.fontana@fitup.local" => "Benessere",
                "beatrice.marchesan@fitup.local" => "Tonificazione",
                _ => "Benessere"
            };

            var source = email switch
            {
                "luca.rossi@fitup.local" => "Passaparola",
                "marta.gallo@fitup.local" => "Instagram",
                "davide.ferri@fitup.local" => "Walk-in",
                "alice.greco@fitup.local" => "Meta Ads",
                "nicolo.marchi@fitup.local" => "Passaparola",
                "elena.moro@fitup.local" => "Instagram",
                "paolo.zanetti@fitup.local" => "Azienda",
                "francesca.longo@fitup.local" => "Instagram",
                "simone.vitali@fitup.local" => "Walk-in",
                "irene.bellini@fitup.local" => "Meta Ads",
                "gabriele.fontana@fitup.local" => "Azienda",
                "beatrice.marchesan@fitup.local" => "Passaparola",
                _ => "Walk-in"
            };

            var prefersMorning = email switch
            {
                "simone.vitali@fitup.local" => "true",
                "luca.rossi@fitup.local" => "true",
                "paolo.zanetti@fitup.local" => "true",
                "beatrice.marchesan@fitup.local" => "true",
                _ => "false"
            };

            var certificateExpiry = membership.Membership.CreatedAtUtc.Date.AddMonths(6).AddDays(
                Math.Abs(membership.User.Id.GetHashCode()) % 45);

            await EnsureMembershipCustomFieldValueAsync(
                gym.Id,
                membership.Membership.Id,
                definitions["obiettivo_principale"],
                objective,
                cancellationToken);
            await EnsureMembershipCustomFieldValueAsync(
                gym.Id,
                membership.Membership.Id,
                definitions["certificato_medico_scadenza"],
                certificateExpiry.ToString("yyyy-MM-dd"),
                cancellationToken);
            await EnsureMembershipCustomFieldValueAsync(
                gym.Id,
                membership.Membership.Id,
                definitions["provenienza_cliente"],
                source,
                cancellationToken);
            await EnsureMembershipCustomFieldValueAsync(
                gym.Id,
                membership.Membership.Id,
                definitions["preferisce_mattina"],
                prefersMorning,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureMembershipCustomFieldValueAsync(
        Guid gymId,
        Guid membershipId,
        GymCustomFieldDefinition definition,
        string value,
        CancellationToken cancellationToken)
    {
        var itemId = StableGuid($"fitup-custom-field-value-{membershipId}-{definition.Id}");
        var item = await _dbContext.GymCustomFieldValues
            .SingleOrDefaultAsync(current => current.Id == itemId, cancellationToken);

        if (item is null)
        {
            item = new GymCustomFieldValue
            {
                Id = itemId,
                GymId = gymId,
                GymMembershipId = membershipId,
                FieldDefinitionId = definition.Id,
                CreatedAtUtc = DateTime.UtcNow
            };
            _dbContext.GymCustomFieldValues.Add(item);
        }

        item.Value = value;
        item.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task EnsureSalesAsync(
        Gym gym,
        IReadOnlyList<MemberSeedContext> memberships,
        StaffSeedContext staff,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        await EnsureSaleAsync(
            "FIT-S-24001",
            gym,
            memberships[0].Membership,
            memberships[0].PrimaryLocation,
            staff.ReceptionUser.Id,
            today.AddDays(-12).AddHours(18),
            new[]
            {
                new SaleLineSeed(GymSaleItemType.MembershipPeriodic, "Abbonamento mensile sala", 1, 69m, 0m, today.AddDays(-12), today.AddDays(18), null, null),
                new SaleLineSeed(GymSaleItemType.Product, "Telo microfibra Betterfit", 1, 12m, 0m, null, null, null, null)
            },
            new[]
            {
                new SalePaymentSeed(81m, GymSalePaymentMethod.Card, GymSalePaymentStatus.Paid, null, today.AddDays(-12).AddHours(18), "Incasso front desk")
            },
            $"{SeedTag} Vendita abbonamento mensile e accessorio.",
            cancellationToken);

        await EnsureSaleAsync(
            "FIT-S-24002",
            gym,
            memberships[1].Membership,
            memberships[1].PrimaryLocation,
            staff.ManagerUser.Id,
            today.AddDays(-8).AddHours(17),
            new[]
            {
                new SaleLineSeed(GymSaleItemType.Package, "Pacchetto 5 PT", 1, 180m, 20m, null, null, null, "Promo onboarding"),
            },
            new[]
            {
                new SalePaymentSeed(80m, GymSalePaymentMethod.Card, GymSalePaymentStatus.Paid, null, today.AddDays(-8).AddHours(17), null),
                new SalePaymentSeed(80m, GymSalePaymentMethod.BankTransfer, GymSalePaymentStatus.Pending, today.AddDays(7).AddHours(12), null, "Saldo entro una settimana")
            },
            $"{SeedTag} Vendita pacchetto PT con saldo residuo.",
            cancellationToken);

        await EnsureSaleAsync(
            "FIT-S-24003",
            gym,
            memberships[2].Membership,
            memberships[2].PrimaryLocation,
            staff.ReceptionUser.Id,
            today.AddDays(-4).AddHours(13),
            new[]
            {
                new SaleLineSeed(GymSaleItemType.MembershipEntries, "Carnet 10 ingressi", 1, 95m, 0m, today.AddDays(-4), today.AddMonths(3), 10, null)
            },
            new[]
            {
                new SalePaymentSeed(95m, GymSalePaymentMethod.Cash, GymSalePaymentStatus.Paid, null, today.AddDays(-4).AddHours(13), null)
            },
            $"{SeedTag} Carnet ingressi.",
            cancellationToken);

        await EnsureSaleAsync(
            "FIT-S-24004",
            gym,
            memberships[3].Membership,
            memberships[3].PrimaryLocation,
            staff.ManagerUser.Id,
            today.AddDays(-2).AddHours(19),
            new[]
            {
                new SaleLineSeed(GymSaleItemType.MembershipPeriodic, "Abbonamento trimestrale sala + corsi", 1, 189m, 10m, today.AddDays(-2), today.AddMonths(3).AddDays(-2), null, null)
            },
            new[]
            {
                new SalePaymentSeed(179m, GymSalePaymentMethod.DirectDebit, GymSalePaymentStatus.Paid, null, today.AddDays(-2).AddHours(19), "Mandato SEPA attivo")
            },
            $"{SeedTag} Trimestrale sala e corsi.",
            cancellationToken);

        await EnsureSaleAsync(
            "FIT-S-24005",
            gym,
            memberships[5].Membership,
            memberships[5].PrimaryLocation,
            staff.ReceptionUser.Id,
            today.AddDays(-1).AddHours(11),
            new[]
            {
                new SaleLineSeed(GymSaleItemType.Service, "Valutazione iniziale composizione corporea", 1, 35m, 0m, null, null, null, null),
                new SaleLineSeed(GymSaleItemType.Package, "Pacchetto 3 sessioni coaching", 1, 105m, 0m, null, null, null, null)
            },
            new[]
            {
                new SalePaymentSeed(70m, GymSalePaymentMethod.Card, GymSalePaymentStatus.Paid, null, today.AddDays(-1).AddHours(11), null),
                new SalePaymentSeed(70m, GymSalePaymentMethod.Card, GymSalePaymentStatus.Pending, today.AddDays(14).AddHours(12), null, "Seconda rata coaching")
            },
            $"{SeedTag} Servizi coaching con piano rateale.",
            cancellationToken);

        await EnsureSaleAsync(
            "FIT-S-24006",
            gym,
            memberships[4].Membership,
            memberships[4].PrimaryLocation,
            staff.ReceptionUser.Id,
            today.AddHours(9).AddMinutes(30),
            new[]
            {
                new SaleLineSeed(GymSaleItemType.MembershipPeriodic, "Rinnovo mensile open gym", 1, 79m, 0m, today, today.AddMonths(1), null, null),
                new SaleLineSeed(GymSaleItemType.Product, "Lucchetto armadietto", 1, 9m, 0m, null, null, null, null)
            },
            new[]
            {
                new SalePaymentSeed(88m, GymSalePaymentMethod.Card, GymSalePaymentStatus.Paid, null, today.AddHours(9).AddMinutes(30), "Rinnovo registrato in reception")
            },
            $"{SeedTag} Vendita registrata oggi per popolare la home dashboard.",
            cancellationToken);

        await EnsureSaleAsync(
            "FIT-S-24007",
            gym,
            memberships[6].Membership,
            memberships[6].PrimaryLocation,
            staff.ManagerUser.Id,
            today.AddDays(-21).AddHours(18),
            new[]
            {
                new SaleLineSeed(GymSaleItemType.MembershipPeriodic, "Abbonamento semestrale open gym", 1, 329m, 20m, today.AddDays(-21), today.AddMonths(6).AddDays(-21), null, "Promo rientro settembre")
            },
            new[]
            {
                new SalePaymentSeed(103m, GymSalePaymentMethod.BankTransfer, GymSalePaymentStatus.Paid, null, today.AddDays(-21).AddHours(18), "Prima rata ricevuta"),
                new SalePaymentSeed(103m, GymSalePaymentMethod.BankTransfer, GymSalePaymentStatus.Pending, today.AddDays(-3).AddHours(12), null, "Seconda rata scaduta"),
                new SalePaymentSeed(103m, GymSalePaymentMethod.BankTransfer, GymSalePaymentStatus.Pending, today.AddDays(18).AddHours(12), null, "Terza rata pianificata")
            },
            $"{SeedTag} Semestrale con piano rateale e rata scaduta.",
            cancellationToken);

        await EnsureSaleAsync(
            "FIT-S-24008",
            gym,
            memberships[7].Membership,
            memberships[7].PrimaryLocation,
            staff.ReceptionUser.Id,
            today.AddDays(-6).AddHours(20),
            new[]
            {
                new SaleLineSeed(GymSaleItemType.Service, "Consulenza nutrizionale iniziale", 1, 59m, 0m, null, null, null, null)
            },
            new[]
            {
                new SalePaymentSeed(59m, GymSalePaymentMethod.Card, GymSalePaymentStatus.Failed, today.AddDays(-6).AddHours(20), null, "POS rifiutato, da richiamare")
            },
            $"{SeedTag} Servizio con pagamento fallito da recuperare.",
            cancellationToken);

        await EnsureSaleAsync(
            "FIT-S-24009",
            gym,
            memberships[8].Membership,
            memberships[8].PrimaryLocation,
            staff.ReceptionUser.Id,
            today.AddDays(-3).AddHours(12),
            new[]
            {
                new SaleLineSeed(GymSaleItemType.Product, "Shaker Betterfit", 1, 14m, 0m, null, null, null, null),
                new SaleLineSeed(GymSaleItemType.CreditRecharge, "Ricarica 12 crediti small group", 1, 96m, 0m, null, null, 12, "Pacchetto small group lunch")
            },
            new[]
            {
                new SalePaymentSeed(110m, GymSalePaymentMethod.DigitalWallet, GymSalePaymentStatus.Paid, null, today.AddDays(-3).AddHours(12), "Wallet app FitUp")
            },
            $"{SeedTag} Ricarica crediti e accessorio.",
            cancellationToken);

        await EnsureSaleAsync(
            "FIT-S-24010",
            gym,
            memberships[9].Membership,
            memberships[9].PrimaryLocation,
            staff.ManagerUser.Id,
            today.AddDays(-15).AddHours(17),
            new[]
            {
                new SaleLineSeed(GymSaleItemType.Package, "Pacchetto 10 PT premium", 1, 390m, 40m, null, null, null, "Percorso premium con check mensile")
            },
            new[]
            {
                new SalePaymentSeed(175m, GymSalePaymentMethod.Card, GymSalePaymentStatus.Paid, null, today.AddDays(-15).AddHours(17), "Acconto premium"),
                new SalePaymentSeed(175m, GymSalePaymentMethod.Card, GymSalePaymentStatus.Pending, today.AddDays(5).AddHours(14), null, "Saldo previsto a fine mese")
            },
            $"{SeedTag} Pacchetto premium con saldo residuo.",
            cancellationToken);

        await EnsureSaleAsync(
            "FIT-S-24011",
            gym,
            memberships[10].Membership,
            memberships[10].PrimaryLocation,
            staff.ReceptionUser.Id,
            today.AddDays(-10).AddHours(10),
            new[]
            {
                new SaleLineSeed(GymSaleItemType.MembershipPeriodic, "Mensile corporate", 1, 89m, 10m, today.AddDays(-10), today.AddDays(20), null, null)
            },
            new[]
            {
                new SalePaymentSeed(79m, GymSalePaymentMethod.DirectDebit, GymSalePaymentStatus.Paid, null, today.AddDays(-10).AddHours(10), "Mandato aziendale confermato")
            },
            $"{SeedTag} Mensile corporate completamente saldato.",
            cancellationToken);

        await EnsureSaleAsync(
            "FIT-S-24012",
            gym,
            memberships[11].Membership,
            memberships[11].PrimaryLocation,
            staff.ReceptionUser.Id,
            today.AddDays(-5).AddHours(16),
            new[]
            {
                new SaleLineSeed(GymSaleItemType.MembershipEntries, "Carnet 20 ingressi studenti", 1, 149m, 10m, today.AddDays(-5), today.AddMonths(4), 20, null)
            },
            new[]
            {
                new SalePaymentSeed(70m, GymSalePaymentMethod.Cash, GymSalePaymentStatus.Paid, null, today.AddDays(-5).AddHours(16), "Acconto desk"),
                new SalePaymentSeed(69m, GymSalePaymentMethod.Cash, GymSalePaymentStatus.Pending, today.AddDays(-1).AddHours(13), null, "Saldo promesso in settimana")
            },
            $"{SeedTag} Carnet studenti con residuo quasi saldo.",
            cancellationToken);

        await EnsureSaleAsync(
            "FIT-S-24013",
            gym,
            memberships[0].Membership,
            memberships[0].PrimaryLocation,
            staff.ManagerUser.Id,
            today.AddDays(-30).AddHours(9),
            new[]
            {
                new SaleLineSeed(GymSaleItemType.Package, "Pacchetto recupero mobilita 4 sedute", 1, 140m, 0m, null, null, null, null)
            },
            new[]
            {
                new SalePaymentSeed(140m, GymSalePaymentMethod.Card, GymSalePaymentStatus.Paid, null, today.AddDays(-30).AddHours(9), "Incasso iniziale"),
                new SalePaymentSeed(140m, GymSalePaymentMethod.Card, GymSalePaymentStatus.Refunded, null, null, "Rimborso completo autorizzato")
            },
            $"{SeedTag} Vendita rimborsata per cambio programma.",
            cancellationToken);

        await EnsureSaleAsync(
            "FIT-S-24014",
            gym,
            memberships[3].Membership,
            memberships[3].PrimaryLocation,
            staff.ReceptionUser.Id,
            today.AddHours(18),
            new[]
            {
                new SaleLineSeed(GymSaleItemType.Product, "Fascia elastica premium", 1, 22m, 0m, null, null, null, null),
                new SaleLineSeed(GymSaleItemType.Service, "Drop-in mobility class", 1, 15m, 0m, today.AddHours(19), today.AddHours(20), null, null)
            },
            new[]
            {
                new SalePaymentSeed(37m, GymSalePaymentMethod.Cash, GymSalePaymentStatus.Paid, null, today.AddHours(18), "Acquisto desk serale")
            },
            $"{SeedTag} Vendita accessori e corso spot registrata oggi.",
            cancellationToken);

        await BackfillSeedSalesAsync(gym.Id, cancellationToken);
    }

    private async Task EnsureSaleCatalogAsync(
        Gym gym,
        IReadOnlyDictionary<string, GymLocation> locations,
        CancellationToken cancellationToken)
    {
        var seeds = new[]
        {
            new SaleCatalogSeed("fitup-catalog-vr-centro-mensile", locations["VR-CENTRO"], GymSaleItemType.MembershipPeriodic, "Abbonamento mensile open gym", 1, 69m, 0m, null, 30, "Ingresso sala pesi e cardio per 30 giorni.", true),
            new SaleCatalogSeed("fitup-catalog-vr-centro-pt5", locations["VR-CENTRO"], GymSaleItemType.Package, "Pacchetto 5 PT", 1, 180m, 10m, null, null, "Pacchetto onboarding personal trainer.", true),
            new SaleCatalogSeed("fitup-catalog-vr-centro-valutazione", locations["VR-CENTRO"], GymSaleItemType.Service, "Valutazione iniziale composizione corporea", 1, 35m, 0m, null, null, "Analisi iniziale con report desk.", true),
            new SaleCatalogSeed("fitup-catalog-vr-centro-corporate", locations["VR-CENTRO"], GymSaleItemType.MembershipPeriodic, "Mensile corporate", 1, 89m, 10m, null, 30, "Formula corporate per convenzioni aziendali.", true),
            new SaleCatalogSeed("fitup-catalog-vr-zeno-carnet10", locations["VR-ZENO"], GymSaleItemType.MembershipEntries, "Carnet 10 ingressi", 1, 95m, 0m, 10, 90, "Carnet ingressi valido 90 giorni.", true),
            new SaleCatalogSeed("fitup-catalog-vr-zeno-prova", locations["VR-ZENO"], GymSaleItemType.Service, "Sessione prova assistita", 1, 0m, 0m, null, null, "Servizio storico disattivato sostituito da onboarding nuovo.", false),
            new SaleCatalogSeed("fitup-catalog-vr-roma-trimestrale", locations["VR-ROMA"], GymSaleItemType.MembershipPeriodic, "Trimestrale sala + corsi", 1, 189m, 10m, null, 90, "Abbonamento combinato sala e corsi.", true),
            new SaleCatalogSeed("fitup-catalog-vr-roma-lucchetto", locations["VR-ROMA"], GymSaleItemType.Product, "Lucchetto armadietto", 1, 9m, 0m, null, null, "Accessorio banco reception.", true),
            new SaleCatalogSeed("fitup-catalog-vr-roma-small-group", locations["VR-ROMA"], GymSaleItemType.CreditRecharge, "Ricarica 8 crediti small group", 1, 72m, 0m, 8, 60, "Pacchetto crediti per mini-classi e mobility.", true)
        };

        foreach (var seed in seeds)
        {
            var itemId = StableGymSeedGuid(gym.Id, seed.Key);
            var item = await _dbContext.GymSaleCatalogItems
                .SingleOrDefaultAsync(
                    entry =>
                        entry.Id == itemId
                        || (entry.GymId == gym.Id
                            && entry.LocationId == seed.Location.Id
                            && entry.Name == seed.Name),
                    cancellationToken);

            if (item is null)
            {
                item = new GymSaleCatalogItem
                {
                    Id = itemId,
                    GymId = gym.Id,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _dbContext.GymSaleCatalogItems.Add(item);
            }

            item.LocationId = seed.Location.Id;
            item.ItemType = seed.ItemType;
            item.Name = seed.Name;
            item.UnitPriceAmount = seed.UnitPriceAmount;
            item.DefaultQuantity = seed.DefaultQuantity;
            item.DefaultDiscountAmount = seed.DefaultDiscountAmount;
            item.DefaultCreditsGranted = seed.DefaultCreditsGranted;
            item.ServicePeriodDays = seed.ServicePeriodDays;
            item.Notes = $"{SeedTag} {seed.Notes}";
            item.IsActive = seed.IsActive;
            item.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAccessEventsAsync(
        Gym gym,
        IReadOnlyList<MemberSeedContext> memberships,
        StaffSeedContext staff,
        CancellationToken cancellationToken)
    {
        var entries = new[]
        {
            new AccessSeed("fitup-access-1", memberships[0], memberships[0].PrimaryLocation, "Tornello ingresso", GymAccessEventResult.Granted, GymAccessOrigin.Badge, DateTime.UtcNow.AddHours(-3), null),
            new AccessSeed("fitup-access-2", memberships[1], memberships[1].PrimaryLocation, "Reception desk", GymAccessEventResult.ManualOverride, GymAccessOrigin.Desk, DateTime.UtcNow.AddHours(-6), "Badge dimenticato"),
            new AccessSeed("fitup-access-3", memberships[2], memberships[2].PrimaryLocation, "Tornello ingresso", GymAccessEventResult.Granted, GymAccessOrigin.AppQr, DateTime.UtcNow.AddDays(-1).AddHours(8), null),
            new AccessSeed("fitup-access-4", memberships[3], memberships[3].PrimaryLocation, "Tornello ingresso", GymAccessEventResult.Denied, GymAccessOrigin.Badge, DateTime.UtcNow.AddDays(-1).AddHours(19), "Tentativo fuori fascia corso"),
            new AccessSeed("fitup-access-5", memberships[4], memberships[4].PrimaryLocation, "Tornello ingresso", GymAccessEventResult.Granted, GymAccessOrigin.Badge, DateTime.UtcNow.AddDays(-2).AddHours(7), null),
            new AccessSeed("fitup-access-6", memberships[5], memberships[5].PrimaryLocation, "Reception desk", GymAccessEventResult.Granted, GymAccessOrigin.Desk, DateTime.UtcNow.AddDays(-2).AddHours(18), "Primo accesso coaching")
        };

        foreach (var entry in entries)
        {
            var eventId = StableGymSeedGuid(gym.Id, entry.Key);
            if (await _dbContext.GymAccessEvents.AnyAsync(
                    item =>
                        item.Id == eventId
                        || (item.GymId == gym.Id
                            && item.GymMembershipId == entry.Member.Membership.Id
                            && item.LocationId == entry.Location.Id
                            && item.GateName == entry.GateName
                            && item.OccurredAtUtc == entry.OccurredAtUtc),
                    cancellationToken))
            {
                continue;
            }

            _dbContext.GymAccessEvents.Add(new GymAccessEvent
            {
                Id = eventId,
                GymId = gym.Id,
                GymMembershipId = entry.Member.Membership.Id,
                LocationId = entry.Location.Id,
                GateName = entry.GateName,
                Result = entry.Result,
                Origin = entry.Origin,
                Reason = entry.Reason,
                OccurredAtUtc = entry.OccurredAtUtc,
                PerformedByUserId = entry.Origin == GymAccessOrigin.Desk ? staff.ReceptionUser.Id : null
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureActivitiesAsync(
        Gym gym,
        IReadOnlyDictionary<string, GymLocation> locations,
        IReadOnlyList<MemberSeedContext> memberships,
        StaffSeedContext staff,
        CancellationToken cancellationToken)
    {
        var templates = new[]
        {
            new ActivityTemplateSeed("fitup-activity-template-hiit", locations["VR-CENTRO"], "HIIT Lunch", "Corso", "#EA580C", 16, 45, "Sessione intensa in pausa pranzo"),
            new ActivityTemplateSeed("fitup-activity-template-yoga", locations["VR-ROMA"], "Yoga Flow", "Mind & Body", "#0F766E", 18, 60, "Corso serale per mobilita e respiro"),
            new ActivityTemplateSeed("fitup-activity-template-pilates", locations["VR-ZENO"], "Pilates Posturale", "Posturale", "#7C3AED", 14, 50, "Classe a bassa intensita per postura, core e respirazione"),
            new ActivityTemplateSeed("fitup-activity-template-functional", locations["VR-CENTRO"], "Functional Strength", "Functional", "#2563EB", 12, 55, "Circuito forza metabolica per piccoli gruppi"),
            new ActivityTemplateSeed("fitup-activity-template-mobility", locations["VR-ZENO"], "Mobility Reset", "Mobility", "#16A34A", 20, 40, "Sessione rapida per mobilita articolare e recupero attivo")
        };

        foreach (var templateSeed in templates)
        {
            var templateId = StableGymSeedGuid(gym.Id, templateSeed.Key);
            var template = await _dbContext.GymActivityTemplates
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == templateId
                        || (item.GymId == gym.Id
                            && item.LocationId == templateSeed.Location.Id
                            && item.Name == templateSeed.Name),
                    cancellationToken);

            if (template is null)
            {
                template = new GymActivityTemplate
                {
                    Id = templateId,
                    GymId = gym.Id,
                    LocationId = templateSeed.Location.Id,
                    InstructorAssignmentId = staff.CoachAssignment.Id,
                    Name = templateSeed.Name,
                    Category = templateSeed.Category,
                    Description = $"{templateSeed.Description} {SeedTag}",
                    ColorHex = templateSeed.ColorHex,
                    Capacity = templateSeed.Capacity,
                    DurationMinutes = templateSeed.DurationMinutes,
                    RequiresBooking = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-20),
                    UpdatedAtUtc = DateTime.UtcNow.AddDays(-2)
                };

                _dbContext.GymActivityTemplates.Add(template);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var hiitTemplateId = StableGymSeedGuid(gym.Id, "fitup-activity-template-hiit");
        var yogaTemplateId = StableGymSeedGuid(gym.Id, "fitup-activity-template-yoga");
        var pilatesTemplateId = StableGymSeedGuid(gym.Id, "fitup-activity-template-pilates");
        var functionalTemplateId = StableGymSeedGuid(gym.Id, "fitup-activity-template-functional");
        var mobilityTemplateId = StableGymSeedGuid(gym.Id, "fitup-activity-template-mobility");
        var sessionSeeds = new[]
        {
            new ActivitySessionSeed("fitup-activity-session-1", hiitTemplateId, locations["VR-CENTRO"], "HIIT Lunch", DateTime.UtcNow.AddDays(1).Date.AddHours(12).AddMinutes(30), 45),
            new ActivitySessionSeed("fitup-activity-session-2", hiitTemplateId, locations["VR-CENTRO"], "HIIT Lunch", DateTime.UtcNow.AddDays(3).Date.AddHours(12).AddMinutes(30), 45),
            new ActivitySessionSeed("fitup-activity-session-3", yogaTemplateId, locations["VR-ROMA"], "Yoga Flow", DateTime.UtcNow.AddDays(2).Date.AddHours(19), 60),
            new ActivitySessionSeed("fitup-activity-session-4", yogaTemplateId, locations["VR-ROMA"], "Yoga Flow", DateTime.UtcNow.AddDays(-3).Date.AddHours(19), 60),
            new ActivitySessionSeed("fitup-activity-session-5", pilatesTemplateId, locations["VR-ZENO"], "Pilates Posturale", DateTime.UtcNow.AddHours(-1), 50),
            new ActivitySessionSeed("fitup-activity-session-6", functionalTemplateId, locations["VR-CENTRO"], "Functional Strength", DateTime.UtcNow.AddDays(1).Date.AddHours(18).AddMinutes(30), 55),
            new ActivitySessionSeed("fitup-activity-session-7", mobilityTemplateId, locations["VR-ZENO"], "Mobility Reset", DateTime.UtcNow.AddDays(4).Date.AddHours(7).AddMinutes(15), 40),
            new ActivitySessionSeed("fitup-activity-session-8", functionalTemplateId, locations["VR-CENTRO"], "Functional Strength", DateTime.UtcNow.AddDays(-1).Date.AddHours(18), 55)
        };

        foreach (var seed in sessionSeeds)
        {
            var sessionId = StableGymSeedGuid(gym.Id, seed.Key);
            if (await _dbContext.GymActivitySessions.AnyAsync(
                    item =>
                        item.Id == sessionId
                        || (item.GymId == gym.Id
                            && item.GymActivityTemplateId == seed.TemplateId
                            && item.LocationId == seed.Location.Id
                            && item.StartsAtUtc == seed.StartsAtUtc),
                    cancellationToken))
            {
                continue;
            }

            _dbContext.GymActivitySessions.Add(new GymActivitySession
            {
                Id = sessionId,
                GymId = gym.Id,
                GymActivityTemplateId = seed.TemplateId,
                LocationId = seed.Location.Id,
                InstructorAssignmentId = staff.CoachAssignment.Id,
                Title = seed.Title,
                InstructorName = staff.CoachAssignment.StaffProfile.DisplayName ?? "Andrea Bassi",
                Capacity = seed.Location.Code == "VR-CENTRO" ? 16 : 18,
                StartsAtUtc = seed.StartsAtUtc,
                EndsAtUtc = seed.StartsAtUtc.AddMinutes(seed.DurationMinutes),
                Status = seed.StartsAtUtc < DateTime.UtcNow ? GymActivitySessionStatus.Completed : GymActivitySessionStatus.Scheduled,
                Notes = $"{SeedTag} Sessione seeded.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-7),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var bookingSeeds = new[]
        {
            new ActivityBookingSeed("fitup-activity-booking-1", "fitup-activity-session-1", memberships[0], GymActivityBookingStatus.Booked),
            new ActivityBookingSeed("fitup-activity-booking-2", "fitup-activity-session-1", memberships[1], GymActivityBookingStatus.Booked),
            new ActivityBookingSeed("fitup-activity-booking-3", "fitup-activity-session-3", memberships[3], GymActivityBookingStatus.Booked),
            new ActivityBookingSeed("fitup-activity-booking-4", "fitup-activity-session-4", memberships[5], GymActivityBookingStatus.CheckedIn),
            new ActivityBookingSeed("fitup-activity-booking-5", "fitup-activity-session-4", memberships[4], GymActivityBookingStatus.NoShow),
            new ActivityBookingSeed("fitup-activity-booking-6", "fitup-activity-session-5", memberships[2], GymActivityBookingStatus.CheckedIn),
            new ActivityBookingSeed("fitup-activity-booking-7", "fitup-activity-session-5", memberships[9], GymActivityBookingStatus.Booked),
            new ActivityBookingSeed("fitup-activity-booking-8", "fitup-activity-session-6", memberships[6], GymActivityBookingStatus.Booked),
            new ActivityBookingSeed("fitup-activity-booking-9", "fitup-activity-session-6", memberships[8], GymActivityBookingStatus.Booked),
            new ActivityBookingSeed("fitup-activity-booking-10", "fitup-activity-session-8", memberships[10], GymActivityBookingStatus.NoShow),
            new ActivityBookingSeed("fitup-activity-booking-11", "fitup-activity-session-8", memberships[11], GymActivityBookingStatus.CheckedIn)
        };

        foreach (var seed in bookingSeeds)
        {
            var bookingId = StableGymSeedGuid(gym.Id, seed.Key);
            var sessionId = StableGymSeedGuid(gym.Id, seed.SessionKey);
            if (await _dbContext.GymActivityBookings.AnyAsync(
                    item =>
                        item.Id == bookingId
                        || (item.GymId == gym.Id
                            && item.GymActivitySessionId == sessionId
                            && item.GymMembershipId == seed.Member.Membership.Id),
                    cancellationToken))
            {
                continue;
            }

            var bookedAt = seed.Status == GymActivityBookingStatus.CheckedIn
                ? DateTime.UtcNow.AddHours(-3)
                : DateTime.UtcNow.AddDays(-2);
            _dbContext.GymActivityBookings.Add(new GymActivityBooking
            {
                Id = bookingId,
                GymId = gym.Id,
                GymActivitySessionId = sessionId,
                GymMembershipId = seed.Member.Membership.Id,
                Status = seed.Status,
                BookedAtUtc = bookedAt,
                CheckedInAtUtc = seed.Status == GymActivityBookingStatus.CheckedIn ? DateTime.UtcNow.AddHours(-1) : null,
                CancelledAtUtc = seed.Status == GymActivityBookingStatus.Cancelled ? bookedAt.AddHours(4) : null,
                Notes = $"{SeedTag} Prenotazione esempio."
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureTrainingAsync(
        Gym gym,
        IReadOnlyDictionary<string, GymLocation> locations,
        IReadOnlyList<MemberSeedContext> memberships,
        StaffSeedContext staff,
        CancellationToken cancellationToken)
    {
        var exercises = new[]
        {
            new WorkoutExerciseSeed("fitup-workout-ex-squat", "Back Squat", "Forza", "Gambe", "Bilanciere"),
            new WorkoutExerciseSeed("fitup-workout-ex-row", "Seated Row", "Ipertrofia", "Schiena", "Macchina"),
            new WorkoutExerciseSeed("fitup-workout-ex-press", "Chest Press", "Ipertrofia", "Petto", "Macchina"),
            new WorkoutExerciseSeed("fitup-workout-ex-bike", "Air Bike Intervals", "Conditioning", "Cardio", "Air Bike"),
            new WorkoutExerciseSeed("fitup-workout-ex-rdl", "Romanian Deadlift", "Forza", "Posteriori coscia", "Bilanciere"),
            new WorkoutExerciseSeed("fitup-workout-ex-lat", "Lat Machine", "Ipertrofia", "Dorsali", "Macchina"),
            new WorkoutExerciseSeed("fitup-workout-ex-plank", "Plank Progression", "Core", "Addome", "Corpo libero"),
            new WorkoutExerciseSeed("fitup-workout-ex-lunge", "Walking Lunge", "Forza", "Gambe", "Manubri"),
            new WorkoutExerciseSeed("fitup-workout-ex-facepull", "Face Pull", "Prehab", "Spalle", "Cavo"),
            new WorkoutExerciseSeed("fitup-workout-ex-sled", "Sled Push", "Conditioning", "Full body", "Slitta")
        };

        foreach (var seed in exercises)
        {
            var exerciseId = StableGymSeedGuid(gym.Id, seed.Key);
            if (await _dbContext.GymWorkoutExercises.AnyAsync(
                    item =>
                        item.Id == exerciseId
                        || (item.GymId == gym.Id && item.Name == seed.Name),
                    cancellationToken))
            {
                continue;
            }

            _dbContext.GymWorkoutExercises.Add(new GymWorkoutExercise
            {
                Id = exerciseId,
                GymId = gym.Id,
                Name = seed.Name,
                Category = seed.Category,
                MuscleGroup = seed.MuscleGroup,
                Equipment = seed.Equipment,
                Instructions = $"{SeedTag} Esercizio libreria FitUp.",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-30),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-5)
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var templateId = StableGymSeedGuid(gym.Id, "fitup-workout-template-recomp");
        var strengthTemplateId = StableGymSeedGuid(gym.Id, "fitup-workout-template-strength-starter");
        if (!await _dbContext.GymWorkoutTemplates.AnyAsync(
                item =>
                    item.Id == templateId
                    || (item.GymId == gym.Id && item.Name == "Body Recomposition 3D"),
                cancellationToken))
        {
            _dbContext.GymWorkoutTemplates.Add(new GymWorkoutTemplate
            {
                Id = templateId,
                GymId = gym.Id,
                LocationId = locations["VR-CENTRO"].Id,
                CoachAssignmentId = staff.CoachAssignment.Id,
                Name = "Body Recomposition 3D",
                Goal = "Ricondizionamento",
                Level = GymWorkoutTemplateLevel.Intermediate,
                Description = $"{SeedTag} Template full body per ricomposizione.",
                DaysPerWeek = 3,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-21),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-5),
                Items =
                [
                    new GymWorkoutTemplateItem
                    {
                        Id = StableGymSeedGuid(gym.Id, "fitup-workout-template-item-1"),
                        ExerciseId = StableGymSeedGuid(gym.Id, "fitup-workout-ex-squat"),
                        DayNumber = 1,
                        SortOrder = 1,
                        ExerciseName = "Back Squat",
                        SetsPrescription = "4",
                        RepetitionsPrescription = "6",
                        RestSeconds = 120,
                        Tempo = "30X1",
                        Notes = "RPE 7"
                    },
                    new GymWorkoutTemplateItem
                    {
                        Id = StableGymSeedGuid(gym.Id, "fitup-workout-template-item-2"),
                        ExerciseId = StableGymSeedGuid(gym.Id, "fitup-workout-ex-row"),
                        DayNumber = 1,
                        SortOrder = 2,
                        ExerciseName = "Seated Row",
                        SetsPrescription = "4",
                        RepetitionsPrescription = "10",
                        RestSeconds = 75,
                        Tempo = "2111"
                    },
                    new GymWorkoutTemplateItem
                    {
                        Id = StableGymSeedGuid(gym.Id, "fitup-workout-template-item-3"),
                        ExerciseId = StableGymSeedGuid(gym.Id, "fitup-workout-ex-press"),
                        DayNumber = 2,
                        SortOrder = 1,
                        ExerciseName = "Chest Press",
                        SetsPrescription = "4",
                        RepetitionsPrescription = "8",
                        RestSeconds = 90,
                        Tempo = "3010"
                    },
                    new GymWorkoutTemplateItem
                    {
                        Id = StableGymSeedGuid(gym.Id, "fitup-workout-template-item-4"),
                        ExerciseId = StableGymSeedGuid(gym.Id, "fitup-workout-ex-bike"),
                        DayNumber = 3,
                        SortOrder = 1,
                        ExerciseName = "Air Bike Intervals",
                        SetsPrescription = "8",
                        RepetitionsPrescription = "20s on / 40s off",
                        RestSeconds = 40,
                        Tempo = null
                    }
                ]
            });
        }

        if (!await _dbContext.GymWorkoutTemplates.AnyAsync(
                item =>
                    item.Id == strengthTemplateId
                    || (item.GymId == gym.Id && item.Name == "Strength Starter 2D"),
                cancellationToken))
        {
            _dbContext.GymWorkoutTemplates.Add(new GymWorkoutTemplate
            {
                Id = strengthTemplateId,
                GymId = gym.Id,
                LocationId = locations["VR-ZENO"].Id,
                CoachAssignmentId = staff.CoachAssignment.Id,
                Name = "Strength Starter 2D",
                Goal = "Forza base",
                Level = GymWorkoutTemplateLevel.Beginner,
                Description = $"{SeedTag} Template guidato per neoiscritti con focus tecnica e carichi progressivi.",
                DaysPerWeek = 2,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-16),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-3),
                Items =
                [
                    new GymWorkoutTemplateItem
                    {
                        Id = StableGymSeedGuid(gym.Id, "fitup-workout-template-strength-item-1"),
                        ExerciseId = StableGymSeedGuid(gym.Id, "fitup-workout-ex-rdl"),
                        DayNumber = 1,
                        SortOrder = 1,
                        ExerciseName = "Romanian Deadlift",
                        SetsPrescription = "3",
                        RepetitionsPrescription = "8",
                        RestSeconds = 90,
                        Tempo = "3110",
                        Notes = "Tecnica prima del carico"
                    },
                    new GymWorkoutTemplateItem
                    {
                        Id = StableGymSeedGuid(gym.Id, "fitup-workout-template-strength-item-2"),
                        ExerciseId = StableGymSeedGuid(gym.Id, "fitup-workout-ex-lat"),
                        DayNumber = 1,
                        SortOrder = 2,
                        ExerciseName = "Lat Machine",
                        SetsPrescription = "3",
                        RepetitionsPrescription = "10-12",
                        RestSeconds = 75,
                        Tempo = "2111"
                    },
                    new GymWorkoutTemplateItem
                    {
                        Id = StableGymSeedGuid(gym.Id, "fitup-workout-template-strength-item-3"),
                        ExerciseId = StableGymSeedGuid(gym.Id, "fitup-workout-ex-lunge"),
                        DayNumber = 2,
                        SortOrder = 1,
                        ExerciseName = "Walking Lunge",
                        SetsPrescription = "3",
                        RepetitionsPrescription = "10 per lato",
                        RestSeconds = 75,
                        Tempo = "2020"
                    },
                    new GymWorkoutTemplateItem
                    {
                        Id = StableGymSeedGuid(gym.Id, "fitup-workout-template-strength-item-4"),
                        ExerciseId = StableGymSeedGuid(gym.Id, "fitup-workout-ex-plank"),
                        DayNumber = 2,
                        SortOrder = 2,
                        ExerciseName = "Plank Progression",
                        SetsPrescription = "4",
                        RepetitionsPrescription = "30-45s",
                        RestSeconds = 45,
                        Tempo = null,
                        Notes = "Aumentare il tempo solo se la postura resta stabile"
                    }
                ]
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var assignments = new[]
        {
            new WorkoutAssignmentSeed("fitup-workout-assignment-1", memberships[0], DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(20)),
            new WorkoutAssignmentSeed("fitup-workout-assignment-2", memberships[1], DateTime.UtcNow.AddDays(-6), DateTime.UtcNow.AddDays(14)),
            new WorkoutAssignmentSeed("fitup-workout-assignment-3", memberships[5], DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(28))
        };

        foreach (var seed in assignments)
        {
            var assignmentId = StableGymSeedGuid(gym.Id, seed.Key);
            if (await _dbContext.GymWorkoutAssignments.AnyAsync(
                    item =>
                        item.Id == assignmentId
                        || (item.GymId == gym.Id
                            && item.GymMembershipId == seed.Member.Membership.Id
                            && item.GymWorkoutTemplateId == templateId),
                    cancellationToken))
            {
                continue;
            }

            _dbContext.GymWorkoutAssignments.Add(new GymWorkoutAssignment
            {
                Id = assignmentId,
                GymId = gym.Id,
                GymMembershipId = seed.Member.Membership.Id,
                LocationId = seed.Member.PrimaryLocation.Id,
                GymWorkoutTemplateId = templateId,
                CoachAssignmentId = staff.CoachAssignment.Id,
                CreatedByUserId = staff.CoachUser.Id,
                Title = "Body Recomposition 3D",
                Goal = "Ricondizionamento",
                Status = GymWorkoutAssignmentStatus.Active,
                AssignedAtUtc = seed.AssignedAtUtc,
                StartsAtUtc = seed.AssignedAtUtc,
                RevisionDueAtUtc = seed.RevisionDueAtUtc,
                Notes = $"{SeedTag} Assegnazione iniziale.",
                CreatedAtUtc = seed.AssignedAtUtc,
                UpdatedAtUtc = seed.AssignedAtUtc,
                Items =
                [
                    new GymWorkoutAssignmentItem
                    {
                        Id = StableGymSeedGuid(gym.Id, $"{seed.Key}-item-1"),
                        ExerciseId = StableGymSeedGuid(gym.Id, "fitup-workout-ex-squat"),
                        DayNumber = 1,
                        SortOrder = 1,
                        ExerciseName = "Back Squat",
                        SetsPrescription = "4",
                        RepetitionsPrescription = "6",
                        RestSeconds = 120,
                        Tempo = "30X1"
                    },
                    new GymWorkoutAssignmentItem
                    {
                        Id = StableGymSeedGuid(gym.Id, $"{seed.Key}-item-2"),
                        ExerciseId = StableGymSeedGuid(gym.Id, "fitup-workout-ex-row"),
                        DayNumber = 1,
                        SortOrder = 2,
                        ExerciseName = "Seated Row",
                        SetsPrescription = "4",
                        RepetitionsPrescription = "10",
                        RestSeconds = 75,
                        Tempo = "2111"
                    }
                ]
            });
        }

        var strengthAssignmentId = StableGymSeedGuid(gym.Id, "fitup-workout-assignment-strength-1");
        if (!await _dbContext.GymWorkoutAssignments.AnyAsync(
                item =>
                    item.Id == strengthAssignmentId
                    || (item.GymId == gym.Id
                        && item.GymMembershipId == memberships[6].Membership.Id
                        && item.GymWorkoutTemplateId == strengthTemplateId),
                cancellationToken))
        {
            var assignedAtUtc = DateTime.UtcNow.AddDays(-18);
            _dbContext.GymWorkoutAssignments.Add(new GymWorkoutAssignment
            {
                Id = strengthAssignmentId,
                GymId = gym.Id,
                GymMembershipId = memberships[6].Membership.Id,
                LocationId = memberships[6].PrimaryLocation.Id,
                GymWorkoutTemplateId = strengthTemplateId,
                CoachAssignmentId = staff.CoachAssignment.Id,
                CreatedByUserId = staff.CoachUser.Id,
                Title = "Strength Starter 2D",
                Goal = "Forza base",
                Status = GymWorkoutAssignmentStatus.Completed,
                AssignedAtUtc = assignedAtUtc,
                StartsAtUtc = assignedAtUtc,
                RevisionDueAtUtc = DateTime.UtcNow.AddDays(-2),
                Notes = $"{SeedTag} Primo blocco completato, pronto per progressione successiva.",
                CreatedAtUtc = assignedAtUtc,
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-2),
                Items =
                [
                    new GymWorkoutAssignmentItem
                    {
                        Id = StableGymSeedGuid(gym.Id, "fitup-workout-assignment-strength-1-item-1"),
                        ExerciseId = StableGymSeedGuid(gym.Id, "fitup-workout-ex-rdl"),
                        DayNumber = 1,
                        SortOrder = 1,
                        ExerciseName = "Romanian Deadlift",
                        SetsPrescription = "3",
                        RepetitionsPrescription = "8",
                        RestSeconds = 90,
                        Tempo = "3110"
                    },
                    new GymWorkoutAssignmentItem
                    {
                        Id = StableGymSeedGuid(gym.Id, "fitup-workout-assignment-strength-1-item-2"),
                        ExerciseId = StableGymSeedGuid(gym.Id, "fitup-workout-ex-lunge"),
                        DayNumber = 2,
                        SortOrder = 1,
                        ExerciseName = "Walking Lunge",
                        SetsPrescription = "3",
                        RepetitionsPrescription = "10 per lato",
                        RestSeconds = 75,
                        Tempo = "2020"
                    }
                ]
            });
        }

        var assessmentSeeds = new[]
        {
            new WorkoutAssessmentSeed("fitup-workout-assessment-1", memberships[0], DateTime.UtcNow.AddDays(-10), 82.4m, 18.3m, "Valutazione iniziale"),
            new WorkoutAssessmentSeed("fitup-workout-assessment-2", memberships[0], DateTime.UtcNow.AddDays(-2), 80.9m, 17.5m, "Buona risposta al programma"),
            new WorkoutAssessmentSeed("fitup-workout-assessment-3", memberships[5], DateTime.UtcNow.AddDays(-1), 61.2m, 24.8m, "Setup percorso coaching"),
            new WorkoutAssessmentSeed("fitup-workout-assessment-4", memberships[6], DateTime.UtcNow.AddDays(-18), 78.1m, 21.4m, "Baseline forza base"),
            new WorkoutAssessmentSeed("fitup-workout-assessment-5", memberships[6], DateTime.UtcNow.AddDays(-2), 77.6m, 20.7m, "Fine primo blocco, carichi piu stabili"),
            new WorkoutAssessmentSeed("fitup-workout-assessment-6", memberships[9], DateTime.UtcNow.AddDays(-5), 68.8m, 22.1m, "Controllo ricomposizione e frequenza cardiaca")
        };

        foreach (var seed in assessmentSeeds)
        {
            var assessmentId = StableGymSeedGuid(gym.Id, seed.Key);
            if (await _dbContext.GymWorkoutAssessments.AnyAsync(
                    item =>
                        item.Id == assessmentId
                        || (item.GymId == gym.Id
                            && item.GymMembershipId == seed.Member.Membership.Id
                            && item.RecordedAtUtc == seed.RecordedAtUtc),
                    cancellationToken))
            {
                continue;
            }

            _dbContext.GymWorkoutAssessments.Add(new GymWorkoutAssessment
            {
                Id = assessmentId,
                GymId = gym.Id,
                GymMembershipId = seed.Member.Membership.Id,
                LocationId = seed.Member.PrimaryLocation.Id,
                CoachAssignmentId = staff.CoachAssignment.Id,
                RecordedByUserId = staff.CoachUser.Id,
                RecordedAtUtc = seed.RecordedAtUtc,
                WeightKg = seed.WeightKg,
                BodyFatPercentage = seed.BodyFatPercentage,
                LeanMassKg = seed.WeightKg * (1 - (seed.BodyFatPercentage / 100m)),
                ChestCm = 98.5m,
                WaistCm = 84.2m,
                HipsCm = 97.4m,
                ArmCm = 34.1m,
                ThighCm = 57.3m,
                CalfCm = 37.4m,
                RestingHeartRateBpm = 61,
                Notes = $"{SeedTag} {seed.Notes}",
                CreatedAtUtc = seed.RecordedAtUtc,
                UpdatedAtUtc = seed.RecordedAtUtc
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCrmAsync(
        Gym gym,
        IReadOnlyDictionary<string, GymLocation> locations,
        IReadOnlyList<MemberSeedContext> memberships,
        StaffSeedContext staff,
        CancellationToken cancellationToken)
    {
        var leads = new[]
        {
            new LeadSeed("fitup-lead-1", locations["VR-CENTRO"], staff.ReceptionAssignment, "Federico Alberti", "federico.alberti@example.com", "3494001001", GymLeadSource.Website, GymLeadStage.New, "Abbonamento annuale sala", DateTime.UtcNow.AddDays(1), "Ha chiesto prezzi tramite sito"),
            new LeadSeed("fitup-lead-2", locations["VR-CENTRO"], staff.ManagerAssignment, "Chiara Venturi", "chiara.venturi@example.com", "3494001002", GymLeadSource.MetaAds, GymLeadStage.Contacted, "Percorso dimagrimento", DateTime.UtcNow.AddDays(-1), "Contattata, vuole prova entro settimana"),
            new LeadSeed("fitup-lead-3", locations["VR-ZENO"], staff.ReceptionAssignment, "Tommaso Leoni", "tommaso.leoni@example.com", "3494001003", GymLeadSource.WalkIn, GymLeadStage.TrialBooked, "Sala pesi + corso mobility", DateTime.UtcNow.AddDays(2), "Trial prenotata giovedi ore 19"),
            new LeadSeed("fitup-lead-4", locations["VR-ROMA"], staff.ManagerAssignment, "Valentina Serra", "valentina.serra@example.com", "3494001004", GymLeadSource.Referral, GymLeadStage.Negotiation, "Pacchetto PT", DateTime.UtcNow.AddDays(3), "Preventivo inviato con pacchetto PT"),
            new LeadSeed("fitup-lead-5", locations["VR-CENTRO"], staff.ManagerAssignment, "Riccardo Pavan", "riccardo.pavan@example.com", "3494001005", GymLeadSource.WhatsApp, GymLeadStage.Won, "Abbonamento trimestrale", null, "Convertito dopo promo prova"),
            new LeadSeed("fitup-lead-6", locations["VR-ZENO"], staff.ReceptionAssignment, "Noemi Zordan", "noemi.zordan@example.com", "3494001006", GymLeadSource.Corporate, GymLeadStage.Lost, "Corporate wellness", null, "Budget bloccato dall'azienda")
        };

        foreach (var seed in leads)
        {
            var leadId = StableGymSeedGuid(gym.Id, seed.Key);
            var lead = await _dbContext.GymLeads
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == leadId
                        || (item.GymId == gym.Id && item.Email == seed.Email),
                    cancellationToken);

            if (lead is null)
            {
                lead = new GymLead
                {
                    Id = leadId,
                    GymId = gym.Id,
                    LocationId = seed.Location.Id,
                    OwnerAssignmentId = seed.OwnerAssignment.Id,
                    ConvertedMembershipId = seed.Stage == GymLeadStage.Won ? memberships[3].Membership.Id : null,
                    FullName = seed.FullName,
                    Email = seed.Email,
                    PhoneNumber = seed.PhoneNumber,
                    Source = seed.Source,
                    Stage = seed.Stage,
                    Interest = seed.Interest,
                    Notes = $"{SeedTag} {seed.Notes}",
                    LastContactedAtUtc = seed.Stage == GymLeadStage.New ? null : DateTime.UtcNow.AddDays(-2),
                    NextFollowUpAtUtc = seed.NextFollowUpAtUtc,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-9),
                    UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
                };

                _dbContext.GymLeads.Add(lead);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var taskSeeds = new[]
        {
            new LeadTaskSeed("fitup-lead-task-1", "fitup-lead-1", staff.ReceptionAssignment, "Richiamare per capire fascia oraria", GymLeadTaskStatus.Open, DateTime.UtcNow.AddDays(1), null),
            new LeadTaskSeed("fitup-lead-task-2", "fitup-lead-2", staff.ManagerAssignment, "Inviare promo prova 7 giorni", GymLeadTaskStatus.Completed, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2)),
            new LeadTaskSeed("fitup-lead-task-3", "fitup-lead-3", staff.ReceptionAssignment, "Confermare presenza alla trial", GymLeadTaskStatus.Open, DateTime.UtcNow.AddDays(1), null),
            new LeadTaskSeed("fitup-lead-task-4", "fitup-lead-4", staff.ManagerAssignment, "Follow-up sul preventivo PT", GymLeadTaskStatus.Open, DateTime.UtcNow.AddDays(3), null),
            new LeadTaskSeed("fitup-lead-task-5", "fitup-lead-6", staff.ManagerAssignment, "Ricontatto a fine trimestre", GymLeadTaskStatus.Cancelled, DateTime.UtcNow.AddDays(30), null)
        };

        foreach (var seed in taskSeeds)
        {
            var taskId = StableGymSeedGuid(gym.Id, seed.Key);
            var leadId = StableGymSeedGuid(gym.Id, seed.LeadKey);
            if (await _dbContext.GymLeadTasks.AnyAsync(
                    item =>
                        item.Id == taskId
                        || (item.GymId == gym.Id
                            && item.GymLeadId == leadId
                            && item.Title == seed.Title),
                    cancellationToken))
            {
                continue;
            }

            _dbContext.GymLeadTasks.Add(new GymLeadTask
            {
                Id = taskId,
                GymLeadId = leadId,
                GymId = gym.Id,
                AssignedAssignmentId = seed.Assignment.Id,
                Title = seed.Title,
                Notes = $"{SeedTag} Task commerciale.",
                Status = seed.Status,
                DueAtUtc = seed.DueAtUtc,
                CompletedAtUtc = seed.CompletedAtUtc,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCrmCampaignsAsync(
        Gym gym,
        IReadOnlyDictionary<string, GymLocation> locations,
        StaffSeedContext staff,
        CancellationToken cancellationToken)
    {
        var campaigns = new[]
        {
            new CampaignSeed(
                "fitup-campaign-1",
                locations["VR-CENTRO"],
                staff.ManagerAssignment,
                staff.ManagerUser.Id,
                "Promo rinnovi prossimi 30 giorni",
                GymCampaignChannel.Email,
                GymCampaignAudienceType.ExpiringMemberships,
                null,
                GymCampaignStatus.Scheduled,
                "Rinnova ora il tuo percorso FitUp",
                "Ciao! Il tuo abbonamento sta per scadere: passa in reception o rispondi a questo messaggio per bloccare la promo rinnovo.",
                DateTime.UtcNow.Date.AddDays(1).AddHours(9),
                null,
                "Campagna per accelerare i rinnovi in scadenza."
            ),
            new CampaignSeed(
                "fitup-campaign-2",
                locations["VR-ZENO"],
                staff.ReceptionAssignment,
                staff.ReceptionUser.Id,
                "Follow-up lead con trial prenotata",
                GymCampaignChannel.WhatsApp,
                GymCampaignAudienceType.LeadsInStage,
                GymLeadStage.TrialBooked,
                GymCampaignStatus.Draft,
                "Conferma trial FitUp",
                "Ti aspettiamo per la tua prova: se vuoi ti confermiamo subito slot, istruttore e promo attivazione valida al desk.",
                null,
                null,
                "Messaggio caldo per lead con trial gia fissata."
            ),
            new CampaignSeed(
                "fitup-campaign-3",
                locations["VR-ROMA"],
                staff.ManagerAssignment,
                staff.ManagerUser.Id,
                "Richiamo lead da seguire oggi",
                GymCampaignChannel.Sms,
                GymCampaignAudienceType.LeadsDueFollowUp,
                null,
                GymCampaignStatus.Sent,
                "FitUp ti ricontatta oggi",
                "Il team Betterfit FitUp ti ricontatta oggi per completare il tuo preventivo e fissare il prossimo step.",
                DateTime.UtcNow.Date.AddHours(8),
                DateTime.UtcNow.Date.AddHours(8).AddMinutes(30),
                "Campagna inviata per follow-up rapidi."
            ),
            new CampaignSeed(
                "fitup-campaign-4",
                locations["VR-CENTRO"],
                staff.ReceptionAssignment,
                staff.ReceptionUser.Id,
                "Riattivazione ex iscritti inverno",
                GymCampaignChannel.Email,
                GymCampaignAudienceType.ActiveMembers,
                null,
                GymCampaignStatus.Archived,
                "Torni ad allenarti con FitUp?",
                "Abbiamo archiviato questa proposta stagionale dopo il ciclo invernale, ma resta utile come storico testabile.",
                DateTime.UtcNow.Date.AddDays(-45).AddHours(10),
                DateTime.UtcNow.Date.AddDays(-45).AddHours(10).AddMinutes(20),
                "Campagna conclusa e archiviata per testare lo storico."
            )
        };

        foreach (var seed in campaigns)
        {
            var campaignId = StableGymSeedGuid(gym.Id, seed.Key);
            var campaign = await _dbContext.GymCampaigns
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == campaignId
                        || (item.GymId == gym.Id && item.Name == seed.Name),
                    cancellationToken);

            if (campaign is null)
            {
                campaign = new GymCampaign
                {
                    Id = campaignId,
                    GymId = gym.Id,
                    LocationId = seed.Location.Id,
                    OwnerAssignmentId = seed.OwnerAssignment.Id,
                    CreatedByUserId = seed.CreatedByUserId,
                    Name = seed.Name,
                    Channel = seed.Channel,
                    AudienceType = seed.AudienceType,
                    TargetLeadStage = seed.TargetLeadStage,
                    Status = seed.Status,
                    Subject = seed.Subject,
                    Message = seed.Message,
                    Notes = $"{SeedTag} {seed.Notes}",
                    ScheduledAtUtc = seed.ScheduledAtUtc,
                    SentAtUtc = seed.SentAtUtc,
                    LastAudienceCount = seed.Status == GymCampaignStatus.Sent ? 1 : null,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-4),
                    UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
                };

                _dbContext.GymCampaigns.Add(campaign);
            }
            else
            {
                campaign.LocationId = seed.Location.Id;
                campaign.OwnerAssignmentId = seed.OwnerAssignment.Id;
                campaign.CreatedByUserId = seed.CreatedByUserId;
                campaign.Name = seed.Name;
                campaign.Channel = seed.Channel;
                campaign.AudienceType = seed.AudienceType;
                campaign.TargetLeadStage = seed.TargetLeadStage;
                campaign.Status = seed.Status;
                campaign.Subject = seed.Subject;
                campaign.Message = seed.Message;
                campaign.Notes = $"{SeedTag} {seed.Notes}";
                campaign.ScheduledAtUtc = seed.ScheduledAtUtc;
                campaign.SentAtUtc = seed.SentAtUtc;
                campaign.LastAudienceCount = seed.Status == GymCampaignStatus.Sent ? 1 : null;
                campaign.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCrmAutomationsAsync(
        Gym gym,
        IReadOnlyDictionary<string, GymLocation> locations,
        StaffSeedContext staff,
        CancellationToken cancellationToken)
    {
        var automations = new[]
        {
            new AutomationRuleSeed(
                "fitup-automation-1",
                locations["VR-CENTRO"],
                staff.ManagerAssignment,
                staff.ManagerUser.Id,
                "Reminder rinnovi in scadenza",
                GymCampaignChannel.Email,
                GymCampaignAudienceType.ExpiringMemberships,
                null,
                GymAutomationScheduleType.Daily,
                GymAutomationStatus.Active,
                DateTime.UtcNow.Date.AddDays(1).AddHours(7).AddMinutes(30),
                "Il tuo abbonamento FitUp sta per scadere",
                "Ciao! Il tuo abbonamento e vicino alla scadenza: rispondi a questa email o passa al desk per bloccare il rinnovo.",
                null,
                null,
                "Automazione giornaliera per anticipare i rinnovi."
            ),
            new AutomationRuleSeed(
                "fitup-automation-2",
                locations["VR-CENTRO"],
                staff.ReceptionAssignment,
                staff.ReceptionUser.Id,
                "Richiamo lead con follow-up dovuto",
                GymCampaignChannel.WhatsApp,
                GymCampaignAudienceType.LeadsDueFollowUp,
                null,
                GymAutomationScheduleType.Daily,
                GymAutomationStatus.Active,
                DateTime.UtcNow.Date.AddHours(9),
                "Ti ricontattiamo oggi da FitUp",
                "Ti ricontattiamo oggi per completare il tuo percorso di ingresso e fissare il prossimo step con il team commerciale.",
                DateTime.UtcNow.Date.AddDays(-1).AddHours(9),
                2,
                "Automazione desk per non perdere follow-up caldi."
            ),
            new AutomationRuleSeed(
                "fitup-automation-3",
                locations["VR-ZENO"],
                staff.ManagerAssignment,
                staff.ManagerUser.Id,
                "Trial booked da riscaldare",
                GymCampaignChannel.Sms,
                GymCampaignAudienceType.LeadsInStage,
                GymLeadStage.TrialBooked,
                GymAutomationScheduleType.Weekly,
                GymAutomationStatus.Paused,
                DateTime.UtcNow.Date.AddDays(4).AddHours(11),
                "Promemoria trial FitUp",
                "Ti ricordiamo la tua prova FitUp: confermaci disponibilita e ti riserviamo promo attivazione al desk.",
                null,
                null,
                "Regola settimanale da attivare nei periodi con molte trial prenotate."
            )
        };

        foreach (var seed in automations)
        {
            var ruleId = StableGymSeedGuid(gym.Id, seed.Key);
            var rule = await _dbContext.GymAutomationRules
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == ruleId
                        || (item.GymId == gym.Id && item.Name == seed.Name),
                    cancellationToken);

            if (rule is null)
            {
                rule = new GymAutomationRule
                {
                    Id = ruleId,
                    GymId = gym.Id,
                    LocationId = seed.Location.Id,
                    OwnerAssignmentId = seed.OwnerAssignment.Id,
                    CreatedByUserId = seed.CreatedByUserId,
                    Name = seed.Name,
                    Channel = seed.Channel,
                    AudienceType = seed.AudienceType,
                    TargetLeadStage = seed.TargetLeadStage,
                    ScheduleType = seed.ScheduleType,
                    Status = seed.Status,
                    NextRunAtUtc = seed.NextRunAtUtc,
                    SubjectTemplate = seed.SubjectTemplate,
                    MessageTemplate = seed.MessageTemplate,
                    Notes = $"{SeedTag} {seed.Notes}",
                    LastRunAtUtc = seed.LastRunAtUtc,
                    LastAudienceCount = seed.LastAudienceCount,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
                    UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
                };

                _dbContext.GymAutomationRules.Add(rule);
            }
            else
            {
                rule.LocationId = seed.Location.Id;
                rule.OwnerAssignmentId = seed.OwnerAssignment.Id;
                rule.CreatedByUserId = seed.CreatedByUserId;
                rule.Name = seed.Name;
                rule.Channel = seed.Channel;
                rule.AudienceType = seed.AudienceType;
                rule.TargetLeadStage = seed.TargetLeadStage;
                rule.ScheduleType = seed.ScheduleType;
                rule.Status = seed.Status;
                rule.NextRunAtUtc = seed.NextRunAtUtc;
                rule.SubjectTemplate = seed.SubjectTemplate;
                rule.MessageTemplate = seed.MessageTemplate;
                rule.Notes = $"{SeedTag} {seed.Notes}";
                rule.LastRunAtUtc = seed.LastRunAtUtc;
                rule.LastAudienceCount = seed.LastAudienceCount;
                rule.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureIntegrationsAsync(
        Gym gym,
        IReadOnlyDictionary<string, GymLocation> locations,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var integrations = new[]
        {
            new IntegrationSeed(
                "fitup-integration-email",
                GymIntegrationType.EmailDelivery,
                null,
                "Delivery email CRM",
                "Brevo SMTP",
                GymIntegrationStatus.Active,
                "smtp://smtp-relay.brevo.local:587",
                "fitup-crm",
                "api-key-fitup-email",
                null,
                "crm@fitup.local",
                "Invio email per campagne e automazioni commerciali.",
                now.AddHours(-6),
                true,
                "Connessione SMTP valida e sender pronto all invio."),
            new IntegrationSeed(
                "fitup-integration-whatsapp",
                GymIntegrationType.WhatsAppMessaging,
                null,
                "Recall WhatsApp lead",
                "360dialog",
                GymIntegrationStatus.Active,
                "https://waba.fitup.local/api/messages",
                null,
                "api-key-fitup-whatsapp",
                "FITUP-WABA-01",
                "WhatsApp Business FitUp",
                "Canale primario per follow-up lead e reminder trial.",
                now.AddHours(-2),
                true,
                "Template e account WhatsApp allineati."),
            new IntegrationSeed(
                "fitup-integration-access",
                GymIntegrationType.AccessControl,
                locations["VR-CENTRO"],
                "Tornello Verona Centro",
                "GymGate Bridge",
                GymIntegrationStatus.Active,
                "https://bridge.fitup.local/access/vr-centro",
                null,
                "api-key-fitup-access",
                "VR-CENTRO-GATE",
                null,
                "Bridge accessi collegato al desk per override e check-in.",
                now.AddHours(-1),
                true,
                "Webhook accessi raggiungibile e token valido."),
            new IntegrationSeed(
                "fitup-integration-accounting",
                GymIntegrationType.AccountingExport,
                null,
                "Export contabilita vendite",
                "Fatture in Cloud",
                GymIntegrationStatus.Draft,
                "https://api.fattureincloud.local/v1/sales",
                "fitup-accounting",
                "api-key-fitup-accounting",
                "FITUP-AMM-2026",
                null,
                "Export corrispettivi e incassi da consolidare con l amministrazione.",
                now.AddDays(-2),
                false,
                "Verifica non superata: mappatura documento fiscale incompleta.")
        };

        foreach (var seed in integrations)
        {
            var integrationId = StableGymSeedGuid(gym.Id, seed.Key);
            var seedLocationId = seed.Location?.Id;
            var integration = await _dbContext.GymIntegrations
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == integrationId
                        || (item.GymId == gym.Id
                            && item.Type == seed.Type
                            && item.LocationId == seedLocationId),
                    cancellationToken);

            if (integration is null)
            {
                integration = new GymIntegration
                {
                    Id = integrationId,
                    GymId = gym.Id,
                    CreatedAtUtc = now.AddDays(-7)
                };

                _dbContext.GymIntegrations.Add(integration);
            }

            integration.GymId = gym.Id;
            integration.LocationId = seed.Location?.Id;
            integration.Type = seed.Type;
            integration.DisplayName = seed.DisplayName;
            integration.ProviderName = seed.ProviderName;
            integration.Status = seed.Status;
            integration.EndpointUrl = seed.EndpointUrl;
            integration.Username = seed.Username;
            integration.ApiKey = seed.ApiKey;
            integration.ExternalAccountId = seed.ExternalAccountId;
            integration.SenderIdentity = seed.SenderIdentity;
            integration.Notes = $"{SeedTag} {seed.Notes}";
            integration.LastSyncAttemptAtUtc = seed.LastSyncAttemptAtUtc;
            integration.LastSyncSucceeded = seed.LastSyncSucceeded;
            integration.LastSyncMessage = seed.LastSyncMessage;
            integration.UpdatedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser> EnsureUserAsync(
        string email,
        string fullName,
        string phoneNumber,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = StableGuid($"fitup-user-{email}").ToString(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                PhoneNumber = phoneNumber,
                PhoneNumberConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, DefaultPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Unable to create user {email}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }
        }
        else
        {
            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            user.EmailConfirmed = true;
            user.PhoneNumberConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        return user;
    }

    private async Task<StaffProfile> EnsureStaffProfileAsync(
        ApplicationUser user,
        string displayName,
        string jobTitle,
        string internalCode,
        CancellationToken cancellationToken)
    {
        var profile = await _dbContext.StaffProfiles.SingleOrDefaultAsync(item => item.UserId == user.Id, cancellationToken);
        if (profile is null)
        {
            profile = new StaffProfile
            {
                Id = StableGuid($"fitup-staff-profile-{user.Id}"),
                UserId = user.Id,
                DisplayName = displayName,
                JobTitle = jobTitle,
                InternalCode = internalCode,
                Active = true,
                CreatedAtUtc = DateTime.UtcNow.AddMonths(-2),
                UpdatedAtUtc = DateTime.UtcNow
            };

            _dbContext.StaffProfiles.Add(profile);
        }
        else
        {
            profile.DisplayName = displayName;
            profile.JobTitle = jobTitle;
            profile.InternalCode = internalCode;
            profile.Active = true;
            profile.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    private async Task<MemberProfile> EnsureMemberProfileAsync(
        ApplicationUser user,
        MemberSeed seed,
        CancellationToken cancellationToken)
    {
        var profile = await _dbContext.MemberProfiles.SingleOrDefaultAsync(item => item.UserId == user.Id, cancellationToken);
        if (profile is null)
        {
            profile = new MemberProfile
            {
                Id = StableGuid($"fitup-member-profile-{user.Id}"),
                UserId = user.Id,
                FirstName = seed.FirstName,
                LastName = seed.LastName,
                BirthDate = seed.BirthDate,
                EmergencyContactName = "Contatto Emergenza",
                EmergencyContactPhoneNumber = "0458000000",
                CreatedAtUtc = DateTime.UtcNow.AddMonths(-2),
                UpdatedAtUtc = DateTime.UtcNow
            };

            _dbContext.MemberProfiles.Add(profile);
        }
        else
        {
            profile.FirstName = seed.FirstName;
            profile.LastName = seed.LastName;
            profile.BirthDate = seed.BirthDate;
            profile.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    private async Task<GymMembership> EnsureMembershipAsync(
        Gym gym,
        ApplicationUser user,
        MemberProfile profile,
        MemberSeed seed,
        CancellationToken cancellationToken)
    {
        var membership = await _dbContext.GymMemberships
            .SingleOrDefaultAsync(item => item.GymId == gym.Id && item.UserId == user.Id, cancellationToken);

        var joinedAtUtc = DateTime.UtcNow.AddMonths(-2).AddDays(StableGuid(seed.Key).ToByteArray()[0] % 20);
        var endedAtUtc = ResolveMembershipEndAtUtc(seed, joinedAtUtc);
        var membershipStatus = ResolveMembershipStatus(seed);
        if (membership is null)
        {
            membership = new GymMembership
            {
                Id = StableGuid($"fitup-membership-{user.Id}-{gym.Id}"),
                UserId = user.Id,
                MemberProfileId = profile.Id,
                GymId = gym.Id,
                InvitationEmail = user.Email,
                Status = membershipStatus,
                PrimaryLocationId = seed.PrimaryLocation.Id,
                Source = ResolveMembershipSource(seed),
                Notes = $"{SeedTag} {seed.Notes}",
                JoinedAtUtc = joinedAtUtc,
                EndedAtUtc = endedAtUtc,
                CreatedAtUtc = joinedAtUtc,
                UpdatedAtUtc = DateTime.UtcNow,
                ClaimedAtUtc = joinedAtUtc,
                PendingFirstName = seed.FirstName,
                PendingLastName = seed.LastName,
                PendingPhoneNumber = seed.PhoneNumber,
                PendingDateOfBirth = seed.BirthDate
            };

            _dbContext.GymMemberships.Add(membership);
        }
        else
        {
            membership.MemberProfileId = profile.Id;
            membership.InvitationEmail = user.Email;
            membership.Status = membershipStatus;
            membership.PrimaryLocationId = seed.PrimaryLocation.Id;
            membership.Source = ResolveMembershipSource(seed);
            membership.Notes = $"{SeedTag} {seed.Notes}";
            membership.JoinedAtUtc = joinedAtUtc;
            membership.EndedAtUtc = endedAtUtc;
            membership.UpdatedAtUtc = DateTime.UtcNow;
            membership.ClaimedAtUtc = joinedAtUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return membership;
    }

    private async Task EnsurePendingDeskMembershipAsync(
        Gym gym,
        GymLocation location,
        CancellationToken cancellationToken)
    {
        var membershipId = StableGymSeedGuid(gym.Id, "fitup-membership-pending-chiara-belli");
        var membership = await _dbContext.GymMemberships
            .Include(item => item.Locations)
            .SingleOrDefaultAsync(
                item =>
                    item.Id == membershipId
                    || (item.GymId == gym.Id && item.InvitationEmail == "chiara.belli.pending@fitup.local"),
                cancellationToken);

        var createdAtUtc = DateTime.UtcNow.Date.AddDays(-1).AddHours(16);
        if (membership is null)
        {
            membership = new GymMembership
            {
                Id = membershipId,
                GymId = gym.Id,
                UserId = null,
                MemberProfileId = null,
                InvitationEmail = "chiara.belli.pending@fitup.local",
                Status = GymMembershipStatus.PendingClaim,
                PrimaryLocationId = location.Id,
                Source = GymMembershipSource.StaffInvite,
                Notes = $"{SeedTag} Preregistrazione desk da completare.",
                JoinedAtUtc = null,
                EndedAtUtc = null,
                CreatedAtUtc = createdAtUtc,
                UpdatedAtUtc = DateTime.UtcNow,
                ClaimedAtUtc = null,
                PendingFirstName = "Chiara",
                PendingLastName = "Belli",
                PendingPhoneNumber = "3381000099",
                PendingDateOfBirth = new DateOnly(1998, 7, 15),
                PendingEmergencyContactName = "Laura Belli",
                PendingEmergencyContactPhoneNumber = "3334445566"
            };

            _dbContext.GymMemberships.Add(membership);
        }
        else
        {
            membership.GymId = gym.Id;
            membership.InvitationEmail = "chiara.belli.pending@fitup.local";
            membership.Status = GymMembershipStatus.PendingClaim;
            membership.PrimaryLocationId = location.Id;
            membership.Source = GymMembershipSource.StaffInvite;
            membership.Notes = $"{SeedTag} Preregistrazione desk da completare.";
            membership.CreatedAtUtc = createdAtUtc;
            membership.UpdatedAtUtc = DateTime.UtcNow;
            membership.ClaimedAtUtc = null;
            membership.JoinedAtUtc = null;
            membership.EndedAtUtc = null;
            membership.PendingFirstName = "Chiara";
            membership.PendingLastName = "Belli";
            membership.PendingPhoneNumber = "3381000099";
            membership.PendingDateOfBirth = new DateOnly(1998, 7, 15);
            membership.PendingEmergencyContactName = "Laura Belli";
            membership.PendingEmergencyContactPhoneNumber = "3334445566";
        }

        var locationLink = membership.Locations.SingleOrDefault(item => item.LocationId == location.Id);
        if (locationLink is null)
        {
            membership.Locations.Add(new GymMembershipLocation
            {
                Id = StableGymSeedGuid(gym.Id, "fitup-membership-pending-chiara-belli-location"),
                GymMembershipId = membershipId,
                LocationId = location.Id,
                AssignedAtUtc = createdAtUtc
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static DateTime ResolveMembershipEndAtUtc(MemberSeed seed, DateTime joinedAtUtc)
    {
        if (string.Equals(seed.Email, "marta.gallo@fitup.local", StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.UtcNow.Date.AddDays(-12);
        }

        if (string.Equals(seed.Email, "alice.greco@fitup.local", StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.UtcNow.Date.AddDays(-18);
        }

        if (string.Equals(seed.Email, "nicolo.marchi@fitup.local", StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.UtcNow.Date.AddDays(8);
        }

        if (string.Equals(seed.Email, "davide.ferri@fitup.local", StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.UtcNow.Date.AddDays(21);
        }

        if (string.Equals(seed.Email, "paolo.zanetti@fitup.local", StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.UtcNow.Date.AddDays(5);
        }

        if (string.Equals(seed.Email, "francesca.longo@fitup.local", StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.UtcNow.Date.AddDays(17);
        }

        if (string.Equals(seed.Email, "simone.vitali@fitup.local", StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.UtcNow.Date.AddDays(27);
        }

        if (string.Equals(seed.Email, "beatrice.marchesan@fitup.local", StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.UtcNow.Date.AddDays(2);
        }

        return joinedAtUtc.AddMonths(3);
    }

    private static GymMembershipStatus ResolveMembershipStatus(MemberSeed seed)
    {
        if (string.Equals(seed.Email, "marta.gallo@fitup.local", StringComparison.OrdinalIgnoreCase))
        {
            return GymMembershipStatus.Archived;
        }

        if (string.Equals(seed.Email, "gabriele.fontana@fitup.local", StringComparison.OrdinalIgnoreCase))
        {
            return GymMembershipStatus.Suspended;
        }

        return GymMembershipStatus.Active;
    }

    private static GymMembershipSource ResolveMembershipSource(MemberSeed seed)
    {
        if (string.Equals(seed.Email, "alice.greco@fitup.local", StringComparison.OrdinalIgnoreCase))
        {
            return GymMembershipSource.SelfSignup;
        }

        if (string.Equals(seed.Email, "beatrice.marchesan@fitup.local", StringComparison.OrdinalIgnoreCase))
        {
            return GymMembershipSource.StaffInvite;
        }

        if (string.Equals(seed.Email, "gabriele.fontana@fitup.local", StringComparison.OrdinalIgnoreCase))
        {
            return GymMembershipSource.Migration;
        }

        return GymMembershipSource.Import;
    }

    private async Task<TenantRoleAssignment> EnsureStaffAssignmentAsync(
        string key,
        Gym gym,
        ApplicationUser user,
        StaffProfile profile,
        GymRole role,
        TenantRoleAssignmentScopeType scopeType,
        Guid? locationId,
        bool isPrimaryOwner,
        CancellationToken cancellationToken)
    {
        var assignmentId = StableGymSeedGuid(gym.Id, key);
        var assignment = await _dbContext.TenantRoleAssignments
            .Include(item => item.StaffProfile)
            .SingleOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);

        if (assignment is null && isPrimaryOwner)
        {
            assignment = await _dbContext.TenantRoleAssignments
                .Include(item => item.StaffProfile)
                .SingleOrDefaultAsync(
                    item =>
                        item.GymId == gym.Id
                        && item.IsPrimaryOwner
                        && item.Status == TenantRoleAssignmentStatus.Active
                        && item.RevokedAtUtc == null,
                    cancellationToken);

            if (assignment is not null)
            {
                return assignment;
            }
        }

        if (assignment is null)
        {
            assignment = await _dbContext.TenantRoleAssignments
                .Include(item => item.StaffProfile)
                .SingleOrDefaultAsync(
                    item =>
                        item.GymId == gym.Id
                        && item.UserId == user.Id
                        && item.RoleId == role.Id
                        && item.ScopeType == scopeType
                        && item.ScopeLocationId == (scopeType == TenantRoleAssignmentScopeType.Location ? locationId : null)
                        && item.Status == TenantRoleAssignmentStatus.Active
                        && item.RevokedAtUtc == null,
                    cancellationToken);
        }

        if (assignment is null)
        {
            assignment = new TenantRoleAssignment
            {
                Id = assignmentId,
                GymId = gym.Id,
                UserId = user.Id,
                StaffProfileId = profile.Id,
                RoleId = role.Id,
                ScopeType = scopeType,
                ScopeLocationId = scopeType == TenantRoleAssignmentScopeType.Location ? locationId : null,
                IsPrimaryOwner = isPrimaryOwner,
                Status = TenantRoleAssignmentStatus.Active,
                GrantedAtUtc = DateTime.UtcNow.AddMonths(-2),
                RevokedAtUtc = null
            };

            _dbContext.TenantRoleAssignments.Add(assignment);
        }
        else
        {
            assignment.GymId = gym.Id;
            assignment.UserId = user.Id;
            assignment.StaffProfileId = profile.Id;
            assignment.RoleId = role.Id;
            assignment.ScopeType = scopeType;
            assignment.ScopeLocationId = scopeType == TenantRoleAssignmentScopeType.Location ? locationId : null;
            assignment.IsPrimaryOwner = isPrimaryOwner;
            assignment.Status = TenantRoleAssignmentStatus.Active;
            assignment.RevokedAtUtc = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return assignment;
    }

    private async Task EnsureSaleAsync(
        string referenceCode,
        Gym gym,
        GymMembership membership,
        GymLocation location,
        string createdByUserId,
        DateTime soldAtUtc,
        IReadOnlyList<SaleLineSeed> lineSeeds,
        IReadOnlyList<SalePaymentSeed> paymentSeeds,
        string notes,
        CancellationToken cancellationToken)
    {
        var sale = await _dbContext.GymSales
            .AsSplitQuery()
            .Include(item => item.Lines)
            .Include(item => item.Payments)
            .SingleOrDefaultAsync(item => item.GymId == gym.Id && item.ReferenceCode == referenceCode, cancellationToken);

        if (sale is not null)
        {
            return;
        }

        var subtotal = decimal.Round(lineSeeds.Sum(line => line.Quantity * line.UnitPriceAmount), 2, MidpointRounding.AwayFromZero);
        var discount = decimal.Round(lineSeeds.Sum(line => line.DiscountAmount), 2, MidpointRounding.AwayFromZero);
        var total = subtotal - discount;
        var paid = decimal.Round(
            Math.Max(
                0m,
                paymentSeeds.Where(payment => payment.Status == GymSalePaymentStatus.Paid).Sum(payment => payment.Amount)
                - paymentSeeds.Where(payment => payment.Status == GymSalePaymentStatus.Refunded).Sum(payment => payment.Amount)),
            2,
            MidpointRounding.AwayFromZero);
        var paymentStatus = ResolveSeedPaymentStatus(paymentSeeds, total, paid);
        var remaining = paymentStatus == GymSalePaymentStatus.Refunded
            ? 0m
            : Math.Max(0m, total - paid);

        sale = new GymSale
        {
            Id = StableGymSeedGuid(gym.Id, $"fitup-sale-{referenceCode}"),
            GymId = gym.Id,
            GymMembershipId = membership.Id,
            LocationId = location.Id,
            ReferenceCode = referenceCode,
            CreatedByUserId = createdByUserId,
            Notes = notes,
            SoldAtUtc = soldAtUtc,
            CreatedAtUtc = soldAtUtc,
            UpdatedAtUtc = soldAtUtc,
            SubtotalAmount = subtotal,
            DiscountAmount = discount,
            TotalAmount = total,
            PaidAmount = paid,
            RemainingAmount = remaining,
            Status = ResolveSeedSaleStatus(paymentStatus),
            PaymentStatus = paymentStatus
        };

        sale.Lines = lineSeeds
            .Select((line, index) => new GymSaleLine
            {
                Id = StableGymSeedGuid(gym.Id, $"fitup-sale-line-{referenceCode}-{index + 1}"),
                GymSaleId = sale.Id,
                ItemType = line.ItemType,
                Name = line.Name,
                Quantity = line.Quantity,
                UnitPriceAmount = line.UnitPriceAmount,
                DiscountAmount = line.DiscountAmount,
                LineTotalAmount = (line.Quantity * line.UnitPriceAmount) - line.DiscountAmount,
                ServicePeriodStartUtc = line.ServicePeriodStartUtc,
                ServicePeriodEndUtc = line.ServicePeriodEndUtc,
                CreditsGranted = line.CreditsGranted,
                Notes = line.Notes
            })
            .ToList();

        sale.Payments = paymentSeeds
            .Select((payment, index) => new GymSalePayment
            {
                Id = StableGymSeedGuid(gym.Id, $"fitup-sale-payment-{referenceCode}-{index + 1}"),
                GymSaleId = sale.Id,
                Amount = payment.Amount,
                Method = payment.Method,
                Status = payment.Status,
                DueAtUtc = payment.DueAtUtc,
                PaidAtUtc = payment.PaidAtUtc,
                ReceiptCode = payment.Status == GymSalePaymentStatus.Paid
                    ? BuildSeedReceiptCode(referenceCode, StableGymSeedGuid(gym.Id, $"fitup-sale-payment-{referenceCode}-{index + 1}"))
                    : null,
                ReceiptIssuedAtUtc = payment.Status == GymSalePaymentStatus.Paid
                    ? payment.PaidAtUtc ?? soldAtUtc
                    : null,
                Notes = payment.Notes,
                CreatedAtUtc = soldAtUtc
            })
            .ToList();

        _dbContext.GymSales.Add(sale);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task BackfillSeedSalesAsync(Guid gymId, CancellationToken cancellationToken)
    {
        var sales = await _dbContext.GymSales
            .Include(item => item.Lines)
            .Include(item => item.Payments)
            .Where(item => item.GymId == gymId && item.Notes != null && EF.Functions.ILike(item.Notes, $"{SeedTag}%"))
            .ToListAsync(cancellationToken);

        foreach (var sale in sales)
        {
            sale.SubtotalAmount = decimal.Round(
                sale.Lines.Sum(line => line.Quantity * line.UnitPriceAmount),
                2,
                MidpointRounding.AwayFromZero);
            sale.DiscountAmount = decimal.Round(
                sale.Lines.Sum(line => line.DiscountAmount),
                2,
                MidpointRounding.AwayFromZero);
            sale.TotalAmount = decimal.Round(sale.SubtotalAmount - sale.DiscountAmount, 2, MidpointRounding.AwayFromZero);

            foreach (var payment in sale.Payments)
            {
                if (payment.Status == GymSalePaymentStatus.Paid)
                {
                    payment.ReceiptCode ??= BuildSeedReceiptCode(sale.ReferenceCode, payment.Id);
                    payment.ReceiptIssuedAtUtc ??= payment.PaidAtUtc ?? payment.CreatedAtUtc;
                }
                else
                {
                    payment.ReceiptCode = null;
                    payment.ReceiptIssuedAtUtc = null;
                }
            }

            var paidAmount = decimal.Round(
                Math.Max(
                    0m,
                    sale.Payments.Where(payment => payment.Status == GymSalePaymentStatus.Paid).Sum(payment => payment.Amount)
                    - sale.Payments.Where(payment => payment.Status == GymSalePaymentStatus.Refunded).Sum(payment => payment.Amount)),
                2,
                MidpointRounding.AwayFromZero);

            sale.PaidAmount = paidAmount;
            sale.PaymentStatus = ResolveSeedPaymentStatus(
                sale.Payments
                    .Select(payment => new SalePaymentSeed(
                        payment.Amount,
                        payment.Method,
                        payment.Status,
                        payment.DueAtUtc,
                        payment.PaidAtUtc,
                        payment.Notes))
                    .ToList(),
                sale.TotalAmount,
                paidAmount);
            sale.RemainingAmount = sale.PaymentStatus == GymSalePaymentStatus.Refunded
                ? 0m
                : decimal.Round(Math.Max(0m, sale.TotalAmount - paidAmount), 2, MidpointRounding.AwayFromZero);
            sale.Status = ResolveSeedSaleStatus(sale.PaymentStatus);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Guid StableGymSeedGuid(Guid gymId, string value)
    {
        return StableGuid($"fitup-seed:{gymId:N}:{value}");
    }

    private static Guid StableGuid(string value)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes);
    }

    private static string BuildSeedReceiptCode(string referenceCode, Guid paymentId)
        => $"RIC-{referenceCode}-{paymentId.ToString("N")[..6].ToUpperInvariant()}";

    private static GymSalePaymentStatus ResolveSeedPaymentStatus(
        IReadOnlyCollection<SalePaymentSeed> payments,
        decimal totalAmount,
        decimal paidAmount)
    {
        var refundedAmount = decimal.Round(
            payments.Where(payment => payment.Status == GymSalePaymentStatus.Refunded).Sum(payment => payment.Amount),
            2,
            MidpointRounding.AwayFromZero);

        if (refundedAmount >= totalAmount && totalAmount > 0m)
        {
            return GymSalePaymentStatus.Refunded;
        }

        if (paidAmount >= totalAmount && payments.All(payment => payment.Status == GymSalePaymentStatus.Paid))
        {
            return GymSalePaymentStatus.Paid;
        }

        if (paidAmount > 0m)
        {
            return GymSalePaymentStatus.PartiallyPaid;
        }

        if (payments.Any(payment => payment.Status == GymSalePaymentStatus.Failed))
        {
            return GymSalePaymentStatus.Failed;
        }

        return GymSalePaymentStatus.Pending;
    }

    private static GymSaleStatus ResolveSeedSaleStatus(GymSalePaymentStatus paymentStatus)
        => paymentStatus switch
        {
            GymSalePaymentStatus.Paid => GymSaleStatus.Paid,
            GymSalePaymentStatus.Refunded => GymSaleStatus.Refunded,
            _ => GymSaleStatus.PendingPayment
        };

    private sealed record LocationSeed(string Key, string Name, string Code, string AddressLine1, string City);
    private sealed record CustomFieldSeed(
        string StableKey,
        string Key,
        string Label,
        string Description,
        GymCustomFieldValueType ValueType,
        IReadOnlyCollection<string> Options,
        bool IsRequired,
        int SortOrder);
    private sealed record MemberSeed(
        string Key,
        string FirstName,
        string LastName,
        string Email,
        string PhoneNumber,
        DateOnly BirthDate,
        GymLocation PrimaryLocation,
        IReadOnlyList<GymLocation> AllowedLocations,
        string Notes);
    private sealed record MemberSeedContext(
        ApplicationUser User,
        MemberProfile Profile,
        GymMembership Membership,
        GymLocation PrimaryLocation);
    private sealed record StaffSeedContext(
        ApplicationUser OwnerUser,
        ApplicationUser ManagerUser,
        ApplicationUser ReceptionUser,
        ApplicationUser CoachUser,
        TenantRoleAssignment OwnerAssignment,
        TenantRoleAssignment ManagerAssignment,
        TenantRoleAssignment ReceptionAssignment,
        TenantRoleAssignment CoachAssignment);
    private sealed record SaleLineSeed(
        GymSaleItemType ItemType,
        string Name,
        int Quantity,
        decimal UnitPriceAmount,
        decimal DiscountAmount,
        DateTime? ServicePeriodStartUtc,
        DateTime? ServicePeriodEndUtc,
        int? CreditsGranted,
        string? Notes);
    private sealed record SalePaymentSeed(
        decimal Amount,
        GymSalePaymentMethod Method,
        GymSalePaymentStatus Status,
        DateTime? DueAtUtc,
        DateTime? PaidAtUtc,
        string? Notes);
    private sealed record SaleCatalogSeed(
        string Key,
        GymLocation Location,
        GymSaleItemType ItemType,
        string Name,
        int DefaultQuantity,
        decimal UnitPriceAmount,
        decimal DefaultDiscountAmount,
        int? DefaultCreditsGranted,
        int? ServicePeriodDays,
        string Notes,
        bool IsActive);
    private sealed record AccessSeed(
        string Key,
        MemberSeedContext Member,
        GymLocation Location,
        string GateName,
        GymAccessEventResult Result,
        GymAccessOrigin Origin,
        DateTime OccurredAtUtc,
        string? Reason);
    private sealed record ActivityTemplateSeed(
        string Key,
        GymLocation Location,
        string Name,
        string Category,
        string ColorHex,
        int Capacity,
        int DurationMinutes,
        string Description);
    private sealed record ActivitySessionSeed(
        string Key,
        Guid TemplateId,
        GymLocation Location,
        string Title,
        DateTime StartsAtUtc,
        int DurationMinutes);
    private sealed record ActivityBookingSeed(
        string Key,
        string SessionKey,
        MemberSeedContext Member,
        GymActivityBookingStatus Status);
    private sealed record WorkoutExerciseSeed(
        string Key,
        string Name,
        string Category,
        string MuscleGroup,
        string Equipment);
    private sealed record WorkoutAssignmentSeed(
        string Key,
        MemberSeedContext Member,
        DateTime AssignedAtUtc,
        DateTime RevisionDueAtUtc);
    private sealed record WorkoutAssessmentSeed(
        string Key,
        MemberSeedContext Member,
        DateTime RecordedAtUtc,
        decimal WeightKg,
        decimal BodyFatPercentage,
        string Notes);
    private sealed record LeadSeed(
        string Key,
        GymLocation Location,
        TenantRoleAssignment OwnerAssignment,
        string FullName,
        string Email,
        string PhoneNumber,
        GymLeadSource Source,
        GymLeadStage Stage,
        string Interest,
        DateTime? NextFollowUpAtUtc,
        string Notes);
    private sealed record LeadTaskSeed(
        string Key,
        string LeadKey,
        TenantRoleAssignment Assignment,
        string Title,
        GymLeadTaskStatus Status,
        DateTime? DueAtUtc,
        DateTime? CompletedAtUtc);
    private sealed record CampaignSeed(
        string Key,
        GymLocation Location,
        TenantRoleAssignment OwnerAssignment,
        string CreatedByUserId,
        string Name,
        GymCampaignChannel Channel,
        GymCampaignAudienceType AudienceType,
        GymLeadStage? TargetLeadStage,
        GymCampaignStatus Status,
        string Subject,
        string Message,
        DateTime? ScheduledAtUtc,
        DateTime? SentAtUtc,
        string Notes);
    private sealed record AutomationRuleSeed(
        string Key,
        GymLocation Location,
        TenantRoleAssignment OwnerAssignment,
        string CreatedByUserId,
        string Name,
        GymCampaignChannel Channel,
        GymCampaignAudienceType AudienceType,
        GymLeadStage? TargetLeadStage,
        GymAutomationScheduleType ScheduleType,
        GymAutomationStatus Status,
        DateTime NextRunAtUtc,
        string SubjectTemplate,
        string MessageTemplate,
        DateTime? LastRunAtUtc,
        int? LastAudienceCount,
        string Notes);
    private sealed record IntegrationSeed(
        string Key,
        GymIntegrationType Type,
        GymLocation? Location,
        string DisplayName,
        string ProviderName,
        GymIntegrationStatus Status,
        string EndpointUrl,
        string? Username,
        string ApiKey,
        string? ExternalAccountId,
        string? SenderIdentity,
        string Notes,
        DateTime? LastSyncAttemptAtUtc,
        bool? LastSyncSucceeded,
        string? LastSyncMessage);
}
