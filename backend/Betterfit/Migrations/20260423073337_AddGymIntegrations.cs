using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Betterfit.Migrations
{
    /// <inheritdoc />
    public partial class AddGymIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GymIntegrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    EndpointUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Username = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ApiKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExternalAccountId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    SenderIdentity = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LastSyncAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncSucceeded = table.Column<bool>(type: "boolean", nullable: true),
                    LastSyncMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymIntegrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymIntegrations_GymLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "GymLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GymIntegrations_Gyms_GymId",
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
                    { new Guid("5adf6f11-4129-4333-a76e-a4c752d8fd54"), "write", "Create or update external integrations", "WRITE", "INTEGRATIONS", "integrations" },
                    { new Guid("bbd8883e-d299-41ef-8fd7-e39db8a9dab4"), "read", "View external integrations and connection status", "READ", "INTEGRATIONS", "integrations" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GymIntegrations_GymId_LocationId",
                table: "GymIntegrations",
                columns: new[] { "GymId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_GymIntegrations_GymId_Type",
                table: "GymIntegrations",
                columns: new[] { "GymId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymIntegrations_LocationId",
                table: "GymIntegrations",
                column: "LocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymIntegrations");

            migrationBuilder.DeleteData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("5adf6f11-4129-4333-a76e-a4c752d8fd54"));

            migrationBuilder.DeleteData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("bbd8883e-d299-41ef-8fd7-e39db8a9dab4"));
        }
    }
}
