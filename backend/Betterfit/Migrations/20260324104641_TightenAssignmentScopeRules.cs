using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betterfit.Migrations
{
    /// <inheritdoc />
    public partial class TightenAssignmentScopeRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantRoleAssignments_GymId_UserId_RoleId_ScopeType_ScopeLo~",
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

            migrationBuilder.AddCheckConstraint(
                name: "CK_TenantRoleAssignments_ScopeConsistency",
                table: "TenantRoleAssignments",
                sql: "(\"ScopeType\" = 'Tenant' AND \"ScopeLocationId\" IS NULL) OR (\"ScopeType\" = 'Location' AND \"ScopeLocationId\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantRoleAssignments_LocationScopeUnique",
                table: "TenantRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_TenantRoleAssignments_TenantScopeUnique",
                table: "TenantRoleAssignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TenantRoleAssignments_ScopeConsistency",
                table: "TenantRoleAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoleAssignments_GymId_UserId_RoleId_ScopeType_ScopeLo~",
                table: "TenantRoleAssignments",
                columns: new[] { "GymId", "UserId", "RoleId", "ScopeType", "ScopeLocationId" },
                unique: true);
        }
    }
}
