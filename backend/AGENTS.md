# Betterfit Backend Guide

This file explains how the backend should be approached by humans and coding agents working in `/backend`.

Use this as an implementation guide, not just a style note. When in doubt, prefer the domain and privacy model over convenience.

## Read Order

Before changing backend behavior, read these files in this order:

1. `docs/betterfit-modello-account.md`
2. `docs/betterfit-architettura-privacy.md`
3. `docs/betterfit-autenticazione.md` for auth/session work
4. `backend/README.md`
5. this file

The docs define the product and privacy model. The code must follow that model, not drift away from it.

## Core Domain Rules

- There is one global Betterfit account per person: `ApplicationUser`.
- Member and staff concerns are separate:
  - member identity lives in `MemberProfile`
  - staff identity lives in `StaffProfile`
- Tenant-local customer relationships live in `GymMembership`.
- Tenant-local staff permissions live in `TenantRoleAssignment`.
- A person can be member only, staff only, or both.
- Do not introduce `is_staff`, `is_admin`, `hybrid`, or similar flattened flags as core domain truth.
- Locations are first-class children of a tenant. Both memberships and staff work can be scoped to locations.
- Cross-tenant reuse of data must be explicit and governed. Do not silently share tenant-local data between gyms.

## Architectural Boundaries

Keep the codebase organized by responsibility:

- `Controllers/`
  HTTP boundary only. Parse input, call services, map response, enforce endpoint-level authorization.
- `Services/`
  Business workflows, transactions, multi-step orchestration, invariant-preserving operations.
- `Authorization/`
  Permission evaluation, policy names, scope rules, minimum-scope rules.
- `Infrastructure/`
  Shared mappers and technical helpers. Put reusable DTO mapping in `Infrastructure/Mapping/ResponseMappers.cs`.
- `Data/`
  `AppDbContext`, EF configuration, design-time factory, seed definitions.
- `Models/`
  Persistence/domain entities only.
- `Contracts/`
  Request/response DTOs only. Never expose EF entities directly from controllers.
- `Betterfit.Tests/`
  Unit-style and database constraint tests.

## Keep Controllers Thin

The earlier backend review was correct about one thing that should remain a hard rule: do not let controllers become orchestration layers.

If an endpoint needs any of the following, move the logic into a service:

- a transaction
- multiple aggregate updates
- ownership transfer
- invitation claiming
- merge logic
- location-sync logic
- permission-sensitive write workflows

Controllers should stay feature-focused:

- `GymsController` for tenant creation/read
- `LocationsController` for physical places
- `MembershipsController` for member relationships
- `StaffAssignmentsController` for staff role assignment flows
- `RolesController` and `PermissionsController` for RBAC definitions
- `AuthController` for account/auth flows

## Authorization Rules

- Always use `AuthorizationPolicies`, `PermissionResources`, and `PermissionActions`. Do not scatter raw permission strings.
- Apply authorization at the controller/action boundary and keep service-layer code authorization-agnostic unless there is a specific reason.
- Staff assignments have their own permission surface:
  - `staff_assignments:read`
  - `staff_assignments:write`
- Do not authorize staff-assignment endpoints with member permissions.
- Be explicit about minimum scope:
  - tenant-wide writes should require tenant-wide permission
  - location-scoped reads must filter results to the allowed locations
- When returning lists of location-scoped resources, never assume access to the whole tenant if the caller only has location scope.

## Invariants That Must Hold

These are important enough to protect in both code and the database where possible.

- `TenantRoleAssignment` scope must be internally consistent:
  - tenant scope => no `ScopeLocationId`
  - location scope => required `ScopeLocationId`
- Duplicate active assignments for the same gym/user/role/scope are not allowed.
- Revoked assignments must be re-creatable later. Do not use uniqueness rules that block re-granting forever.
- There must be at most one active primary owner per gym.
- Owner assignments must be tenant-scoped.
- Ownership transfer is a workflow, not just a blind insert.

Current owner protection is intentionally layered:

- service-level workflow in `Services/StaffAssignments/StaffAssignmentService.cs`
- database constraints and partial unique indexes in `Data/AppDbContext.cs`
- migration backfill/cleanup in `Migrations/`

Do not weaken one layer without replacing it thoughtfully.

## Database And Migrations

- Use EF Core migrations. Do not switch back to `EnsureCreated()` for real environments.
- When changing the model:
  1. update entities/configuration
  2. generate a migration
  3. inspect the migration manually
  4. think about existing data, not just empty databases
- If a new invariant matters in production, prefer a DB constraint/index in addition to app logic.
- Keep seed/catalog/template data outside `AppDbContext` when possible. `PermissionCatalogSeed.cs` is the pattern to follow.
- Remember that PostgreSQL is the real target. SQLite tests are useful, but they do not replace checking PostgreSQL-specific behavior such as partial indexes and migration SQL.

Useful commands:

```bash
dotnet build backend/Betterfit.sln
dotnet test backend/Betterfit.sln --no-build
dotnet tool run dotnet-ef migrations add <Name> --project Betterfit/Betterfit.csproj --startup-project Betterfit/Betterfit.csproj
dotnet tool run dotnet-ef database update --project Betterfit/Betterfit.csproj --startup-project Betterfit/Betterfit.csproj
```

## DTOs, Mapping, And API Shape

- Keep request/response contracts in `Contracts/`.
- Reuse shared mapping helpers instead of duplicating mapper methods in controllers.
- Prefer `ApiControllerBase` helpers and the unified `ApiResponse` envelope.
- Do not mutate request DTOs to normalize them. Normalize inside local variables or service inputs.
- Do not use auth/session endpoints as generic "return everything about the user" endpoints.
- `GET /api/auth/me`, login, register, and invitation-claim responses should stay lightweight:
  - user identity summary
  - minimal app-access flags only
- Do not issue the real bearer JWT before the required auth factors are complete.
  - email verification and 2FA setup/login should use short-lived opaque challenge tokens in the request body, not bearer authorization
  - only the final, fully authenticated step should mint the normal API JWT
- Email verification uses two different lifetimes:
  - a longer-lived verification session
  - a shorter-lived verification code inside that session
- Keep those lifetimes explicit in both the schema and the API contract. Do not collapse them back into one ambiguous `expiresAtUtc` field.
- Browser resume should rely on the verification session token plus `POST /api/auth/verification-session/status`.
  - keep pre-auth tokens in the request body, not query strings
  - the status endpoint should return only the metadata needed to resume the flow
- Keep account auth state separate from tenant auth policy:
  - `EmailConfirmed` and `TwoFactorEnabled` live on the global account
  - tenant requirements like `RequireTwoFactorForStaff` and `RequireTwoFactorForMembers` live in the tenant authentication policy
- Keep parent resources and child resources separate in the API shape:
  - `POST /api/gyms` creates the tenant only
  - physical places are created with `POST /api/gyms/{gymId}/locations`
  - gym responses should not embed full location collections by default
- Avoid returning full `MemberProfileResponse` or `StaffProfileResponse` from bootstrap/list endpoints by default. Those DTOs can expose fields like emergency contacts, internal codes, and audit timestamps that many clients do not need.
- Similar caution applies to other list-style endpoints that currently embed rich nested profiles, especially membership and staff-assignment responses. Prefer purpose-built summary DTOs when the consumer only needs list or picker data.
- If you change request or response shapes, update:
  - `backend/README.md`
  - `backend/Betterfit/Betterfit.http`
- If you change the backend API contract shape in a way that affects the frontend client, run `pnpm openapi` from `dashboard/` to regenerate the TypeScript API bindings before closing the task.

## Configuration And Security

- Keep secrets out of committed default config.
- Bind options through the options system and validate on startup.
- Prefer fail-fast startup over weak runtime defaults for auth and database settings.
- Authentication belongs at the global account layer; tenant access belongs in authorization and domain relationships.

## Testing Expectations

Every non-trivial backend change should come with verification.

- Permission changes should usually update `PermissionServiceTests`.
- Constraint or schema-invariant changes should usually update `TenantRoleAssignmentConstraintTests`.
- Workflow changes should usually get a focused service test.
- Build and test the solution before closing the task.

Good candidates for future integration coverage:

- auth register/login/claim flows
- membership invitation claim flows
- owner transfer
- location-scoped authorization

## Practical Change Heuristics

- Capture `var now = DateTime.UtcNow` once per workflow.
- Use structured logging in services handling important workflows or failure paths.
- Prefer adding a focused service over adding another private controller helper when logic starts branching.
- Prefer feature cohesion over generic utility sprawl.
- If a change touches account/member/staff boundaries, re-check the docs before coding.

## What To Avoid

- giant multi-purpose controllers
- duplicated mapping code
- duplicated permission strings
- tenant logic hidden in UI assumptions
- schema changes without migrations
- app-only enforcement for invariants that the database can reliably protect
- introducing global fields for tenant-local business data

The long-term goal is a backend that stays boring to maintain: explicit boundaries, explicit invariants, and changes that are easy to trace from docs to controllers to services to tests.
