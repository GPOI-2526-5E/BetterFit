# Betterfit API

Betterfit is a multi-tenant CRM platform for gyms and sports clubs. The backend now models:
- one global Betterfit account per person;
- one optional global `member_profile`;
- one optional global `staff_profile`;
- tenant-scoped `gym_membership` records for client relationships;
- separate `tenant_role_assignment` records for staff permissions;
- many physical locations per tenant;
- secure invitation-based membership claiming.

## Tech stack

- ASP.NET Core Web API (.NET 10)
- ASP.NET Core Identity (custom `ApplicationUser`)
- Entity Framework Core with PostgreSQL
- JWT bearer authentication
- OpenAPI + Swagger

## OpenAPI support

OpenAPI is exposed at:
- `http://localhost:5299/openapi/v1.json` for local `dotnet run`
- `http://localhost:8080/openapi/v1.json` for Docker Compose

Protected endpoints require `Authorization: Bearer <token>`.

## Quick start

### 1) Start PostgreSQL

```bash
docker compose -f Betterfit/docker-compose.dev.yml up -d
```

### 2) Run API

```bash
dotnet run --project Betterfit/Betterfit.csproj
```

### 3) Call endpoints

Use `Betterfit/Betterfit.http` for ready-to-run requests.

## Configuration

Main settings live in:
- `Betterfit/appsettings.json`
- `Betterfit/appsettings.Development.json`

`appsettings.json` is intentionally non-runnable by default. Supply production settings through environment-specific configuration or a secret store, and use local development settings only in development.

Keys:
- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Key`
- `Jwt:ExpiresMinutes`
- `AuthenticationFlow:*`

## Domain model

- `ApplicationUser`: global Betterfit account used for authentication
- `AuthenticationChallenge`: pre-auth session for email verification or 2FA completion
- `MemberProfile`: customer-side identity profile attached to the account
- `StaffProfile`: operator-side identity profile attached to the account
- `Gym`: tenant organization
- `GymAuthenticationPolicy`: tenant-local 2FA requirements
- `GymLocation`: physical place under a tenant
- `GymMembership`: tenant-scoped customer relationship
- `TenantRoleAssignment`: staff permissions inside a tenant or a specific location
- `GymRole`: reusable staff role definition per tenant
- `PermissionCatalogItem` + `RolePermission`: permission catalog and role rules
- `GymInvitation`: secure token-based claim invitation for memberships

## Architectural rules

- Authentication lives on the global account.
- Member concerns and staff concerns are modeled separately.
- A person can be:
  - only member
  - only staff
  - both member and staff
- Client relationship is represented by `GymMembership`.
- Staff permissions are represented by `TenantRoleAssignment`.
- Locations are tenant children and can scope both memberships and staff work.
- No `is_staff` / `is_admin` / `hybrid` flags are required in the core model.

## Default staff roles

When a tenant is created, these roles are seeded:
- `Owner`
- `Manager`
- `Reception`
- `Coach`
- `Support Operator`

The creator of the gym becomes the initial tenant `Owner` through a staff assignment, not through a member role.

## Privacy-oriented modeling choices

- Betterfit authentication stays global at `ApplicationUser` level.
- Member/staff profiles stay separate from tenant permissions and subscriptions.
- Tenant-sensitive data such as tax code, operational notes, subscriptions, contracts, payments, and certificates belong to the membership or its child entities, not to the base account.
- Invitation claiming is explicit and token-based.

## Main endpoints

### Auth

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/verification-session/status`
- `POST /api/auth/verify-email-code`
- `POST /api/auth/resend-email-code`
- `POST /api/auth/2fa/setup`
- `POST /api/auth/2fa/enable`
- `POST /api/auth/2fa/verify`
- `POST /api/auth/2fa/recovery-code`
- `GET /api/auth/me`
- `POST /api/auth/claim-invitation`

Register and login now return an auth flow response instead of always returning a JWT immediately.

Auth flow rules:
- email verification is mandatory before a real API JWT is issued;
- register and unverified login return a `verificationToken`, `sessionExpiresAtUtc`, `codeExpiresAtUtc`, and `resendAvailableAtUtc`;
- the verification session is intentionally longer-lived than the email code so browser clients can resume after refresh or reopen;
- the verification token is body-bound and only valid for verification endpoints;
- clients can rehydrate a stored verification token with `POST /api/auth/verification-session/status`;
- once the email is confirmed, the backend evaluates whether 2FA is required;
- 2FA is required if the account has it enabled, or if any active tenant policy requires it for the user's active memberships or staff assignments.

Detailed auth/client integration guidance lives in `docs/betterfit-autenticazione.md`.

Development note:
- in development, email verification codes are logged by the backend instead of being sent through a real provider;
- production should replace the default sender with a real delivery implementation.

`GET /api/auth/me` is intentionally a lightweight session/bootstrap endpoint. It should not return full member or staff profile data, tenant-local operational data, or audit timestamps.

### Gyms and locations

- `GET /api/gyms`
- `GET /api/gyms/{gymId}`
- `POST /api/gyms`
- `GET /api/gyms/{gymId}/locations`
- `POST /api/gyms/{gymId}/locations`

`POST /api/gyms` creates the tenant only. Physical places are managed separately through the locations endpoints.

`GET /api/gyms` and `GET /api/gyms/{gymId}` return tenant summaries, not embedded location collections. Clients should fetch locations explicitly when needed.

### Tenant security policy

- `GET /api/gyms/{gymId}/security/authentication-policy`
- `PUT /api/gyms/{gymId}/security/authentication-policy`

Tenant authentication policy currently supports:
- `requireTwoFactorForStaff`
- `requireTwoFactorForMembers`

### Memberships

- `GET /api/gyms/{gymId}/memberships`
- `POST /api/gyms/{gymId}/memberships`
- `POST /api/gyms/{gymId}/memberships/{membershipId}/invitations`

Membership creation supports:
- existing registered users via `userId` or `email`
- pending-claim members via `email`
- multi-location access via `locationIds`
- member profile creation/update when an account exists
- invitation-based onboarding when an account does not exist yet

### Staff assignments

- `GET /api/gyms/{gymId}/staff-assignments`
- `POST /api/gyms/{gymId}/staff-assignments`
- `POST /api/gyms/{gymId}/assignments`

Staff assignments support:
- tenant-scoped roles
- location-scoped roles
- owner transfer through dedicated staff assignments
- optional global staff profile enrichment
- database-backed scope integrity for tenant vs location assignments

### Roles and permissions

- `GET /api/gyms/{gymId}/permissions/catalog`
- `GET /api/gyms/{gymId}/roles`
- `GET /api/gyms/{gymId}/roles/{roleId}`
- `POST /api/gyms/{gymId}/roles`

Roles are now staff roles only.

## Schema evolution

The app applies EF Core migrations at startup through `Database.MigrateAsync()`.

Keep using migrations for schema changes. Do not switch back to `EnsureCreated()` if the database might already exist.

## Unified response format

All endpoints return the same envelope shape:

```json
{
  "success": true,
  "data": {},
  "error": null,
  "traceId": "00-..."
}
```

Error example:

```json
{
  "success": false,
  "message": "Validation failed.",
  "data": null,
  "error": {
    "code": "validation_error",
    "message": "Validation failed.",
    "details": {
      "Email": [
        "The Email field is not a valid e-mail address."
      ]
    }
  },
  "traceId": "00-..."
}
```
