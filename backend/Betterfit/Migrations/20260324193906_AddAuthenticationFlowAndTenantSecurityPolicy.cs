using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Betterfit.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticationFlowAndTenantSecurityPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthenticationChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthenticationChallenges_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GymAuthenticationPolicies",
                columns: table => new
                {
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequireTwoFactorForStaff = table.Column<bool>(type: "boolean", nullable: false),
                    RequireTwoFactorForMembers = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymAuthenticationPolicies", x => x.GymId);
                    table.ForeignKey(
                        name: "FK_GymAuthenticationPolicies_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "PermissionCatalogItems",
                columns: new[] { "Id", "Action", "Description", "NormalizedAction", "NormalizedResource", "Resource" },
                values: new object[,]
                {
                    { new Guid("54e4a2dd-8e6d-4919-b7d6-dd5b7b8ed5f7"), "write", "Create or update tenant authentication and security policy", "WRITE", "SECURITY_POLICY", "security_policy" },
                    { new Guid("6fcf5996-b724-44aa-bab7-7754505dcf3e"), "read", "View tenant authentication and security policy", "READ", "SECURITY_POLICY", "security_policy" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationChallenges_TokenHash",
                table: "AuthenticationChallenges",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationChallenges_UserId_Type",
                table: "AuthenticationChallenges",
                columns: new[] { "UserId", "Type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthenticationChallenges");

            migrationBuilder.DropTable(
                name: "GymAuthenticationPolicies");

            migrationBuilder.DeleteData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("54e4a2dd-8e6d-4919-b7d6-dd5b7b8ed5f7"));

            migrationBuilder.DeleteData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("6fcf5996-b724-44aa-bab7-7754505dcf3e"));
        }
    }
}
