using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Betterfit.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffAssignmentPermissionsAndOwnerInvariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantRoleAssignments_LocationScopeUnique",
                table: "TenantRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_TenantRoleAssignments_TenantScopeUnique",
                table: "TenantRoleAssignments");

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimaryOwner",
                table: "TenantRoleAssignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE "TenantRoleAssignments" AS assignment
                SET
                    "IsPrimaryOwner" = TRUE,
                    "ScopeType" = 'Tenant',
                    "ScopeLocationId" = NULL
                FROM "GymRoles" AS role
                WHERE assignment."RoleId" = role."Id"
                  AND role."NormalizedName" = 'OWNER';
                """);

            migrationBuilder.Sql(
                """
                WITH ranked_owners AS (
                    SELECT
                        assignment."Id",
                        ROW_NUMBER() OVER (
                            PARTITION BY assignment."GymId"
                            ORDER BY assignment."GrantedAtUtc" DESC, assignment."Id" DESC) AS row_number
                    FROM "TenantRoleAssignments" AS assignment
                    WHERE assignment."IsPrimaryOwner"
                      AND assignment."Status" = 'Active'
                      AND assignment."RevokedAtUtc" IS NULL
                )
                UPDATE "TenantRoleAssignments" AS assignment
                SET
                    "Status" = 'Revoked',
                    "RevokedAtUtc" = NOW()
                FROM ranked_owners
                WHERE assignment."Id" = ranked_owners."Id"
                  AND ranked_owners.row_number > 1;
                """);

            migrationBuilder.InsertData(
                table: "PermissionCatalogItems",
                columns: new[] { "Id", "Action", "Description", "NormalizedAction", "NormalizedResource", "Resource" },
                values: new object[,]
                {
                    { new Guid("98b12021-bad0-49ec-bd1f-2d8d4e9b3f81"), "write", "Create or update tenant staff assignments", "WRITE", "STAFF_ASSIGNMENTS", "staff_assignments" },
                    { new Guid("f4146dab-43bc-494c-86d2-c0256744996a"), "read", "View tenant staff assignments", "READ", "STAFF_ASSIGNMENTS", "staff_assignments" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoleAssignments_ActivePrimaryOwnerPerGym",
                table: "TenantRoleAssignments",
                column: "GymId",
                unique: true,
                filter: "\"IsPrimaryOwner\" AND \"Status\" = 'Active' AND \"RevokedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoleAssignments_LocationScopeUnique",
                table: "TenantRoleAssignments",
                columns: new[] { "GymId", "UserId", "RoleId", "ScopeLocationId" },
                unique: true,
                filter: "\"ScopeType\" = 'Location' AND \"ScopeLocationId\" IS NOT NULL AND \"Status\" = 'Active' AND \"RevokedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoleAssignments_TenantScopeUnique",
                table: "TenantRoleAssignments",
                columns: new[] { "GymId", "UserId", "RoleId" },
                unique: true,
                filter: "\"ScopeType\" = 'Tenant' AND \"ScopeLocationId\" IS NULL AND \"Status\" = 'Active' AND \"RevokedAtUtc\" IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TenantRoleAssignments_PrimaryOwnerScope",
                table: "TenantRoleAssignments",
                sql: "NOT \"IsPrimaryOwner\" OR (\"ScopeType\" = 'Tenant' AND \"ScopeLocationId\" IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantRoleAssignments_ActivePrimaryOwnerPerGym",
                table: "TenantRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_TenantRoleAssignments_LocationScopeUnique",
                table: "TenantRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_TenantRoleAssignments_TenantScopeUnique",
                table: "TenantRoleAssignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TenantRoleAssignments_PrimaryOwnerScope",
                table: "TenantRoleAssignments");

            migrationBuilder.DeleteData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("98b12021-bad0-49ec-bd1f-2d8d4e9b3f81"));

            migrationBuilder.DeleteData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("f4146dab-43bc-494c-86d2-c0256744996a"));

            migrationBuilder.DropColumn(
                name: "IsPrimaryOwner",
                table: "TenantRoleAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoleAssignments_LocationScopeUnique",
                table: "TenantRoleAssignments",
                columns: new[] { "GymId", "UserId", "RoleId", "ScopeLocationId" },
                unique: true,
                filter: "\"ScopeType\" = 'Location' AND \"ScopeLocationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoleAssignments_TenantScopeUnique",
                table: "TenantRoleAssignments",
                columns: new[] { "GymId", "UserId", "RoleId" },
                unique: true,
                filter: "\"ScopeType\" = 'Tenant' AND \"ScopeLocationId\" IS NULL");
        }
    }
}
