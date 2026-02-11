# Betterfit API

Betterfit is a CRM platform for gyms. This scaffold provides:
- JWT authentication (`register`/`login`)
- PostgreSQL persistence via Entity Framework Core
- Gym creation and user-to-gym assignment
- Role management scoped per gym
- Dedicated permission catalog table with FK-based role permissions
- Runtime permission enforcement on protected gym endpoints
- Unified API response envelope for both success and errors

## OpenAPI support

OpenAPI is integrated and exposed at:
- JSON spec: `http://localhost:5299/openapi/v1.json` (local `dotnet run` default)
- JSON spec: `http://localhost:8080/openapi/v1.json` (docker compose)

Protected endpoints require a JWT bearer token at runtime (`Authorization: Bearer <token>`).

## Tech stack

- ASP.NET Core Web API (.NET 10)
- ASP.NET Core Identity (custom `ApplicationUser`)
- Entity Framework Core (code-first model)
- PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- OpenAPI (`Microsoft.AspNetCore.OpenApi`)

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

Keys:
- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Key`
- `Jwt:ExpiresMinutes`

## Domain model

- `ApplicationUser`: Identity user with optional `FullName`
- `Gym`: gym entity
- `GymRole`: role owned by a specific gym (`GymId`)
- `PermissionCatalogItem`: central catalog of valid permissions (`resource`, `action`)
- `RolePermission`: assignment from `GymRole` to `PermissionCatalogItem` with `IsAllowed`
- `GymMembership`: assignment of user to gym and role

## Role and permission behavior

- Roles are scoped per gym.
- Role names are unique within a gym.
- Permissions are not free-form strings in role assignments.
- Role permissions reference a dedicated permission catalog via `PermissionId`.
- Permissions are enforced at runtime via authorization policies.
- Explicit deny wins over allow when evaluating the same permission in a gym.
- Single-owner invariant is enforced:
  - a gym cannot exist without an owner
  - a gym cannot have more than one owner
  - assigning Owner to another user transfers ownership atomically
- When a gym is created, default roles are auto-seeded for that gym:
  - Owner
  - Manager
  - Trainer
  - Member
- The creator of a gym is automatically assigned as Owner in that gym.

## API endpoints

### Auth

- `POST /api/auth/register`
- `POST /api/auth/login`

Both return:
- JWT token
- expiration timestamp
- user summary

### Permission catalog (per gym)

- `GET /api/gyms/{gymId}/permissions/catalog`

Requires `roles:read` in that gym.
Use this endpoint to retrieve valid permission IDs for role creation.

### Roles (per gym)

- `GET /api/gyms/{gymId}/roles` (requires `roles:read`)
- `GET /api/gyms/{gymId}/roles/{roleId}` (requires `roles:read`)
- `POST /api/gyms/{gymId}/roles` (requires `roles:write`)

Create custom role payload example:

```json
{
  "name": "Front Desk",
  "description": "Handles check-ins and member support",
  "permissions": [
    { "permissionId": "32291f01-7fe4-44cf-9f1c-82bbf8a37295", "isAllowed": true },
    { "permissionId": "f2895883-e3b6-4f31-95ba-e8aa183d9a31", "isAllowed": true },
    { "permissionId": "a5c55147-1d99-433e-b022-5164d7eff76d", "isAllowed": true }
  ]
}
```

### Gyms and memberships

- `GET /api/gyms` (returns only gyms where caller has `gyms:read`)
- `GET /api/gyms/{gymId}` (requires `gyms:read`)
- `POST /api/gyms` (authenticated users)
- `GET /api/gyms/{gymId}/memberships` (requires `members:read`)
- `POST /api/gyms/{gymId}/assignments` (requires `members:write`)

Assign payload example:

```json
{
  "email": "owner@betterfit.local",
  "roleId": "<role-id-from-gym-roles-endpoint>"
}
```

If `roleId` is the gym's Owner role and the gym already has an owner,
ownership is transferred to the target user while keeping exactly one owner.

## Auth flow

1. Register or login using `/api/auth/register` or `/api/auth/login`
2. Read `token` from response
3. Send header `Authorization: Bearer <token>` to protected endpoints

## Notes

- Database is initialized at startup with `EnsureCreated()` for fast scaffolding.
- For production-ready schema evolution, move to EF migrations.
- If you are using an existing Postgres volume from the older schema, recreate it once so the new tables/keys are applied.

## Unified response format

All endpoints return the same envelope shape:

```json
{
  "success": true,
  "message": "Request completed successfully.",
  "data": {},
  "error": null,
  "traceId": "00-...",
  "timestampUtc": "2026-02-11T12:34:56.0000000Z"
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
  "traceId": "00-...",
  "timestampUtc": "2026-02-11T12:34:56.0000000Z"
}
```
