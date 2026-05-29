using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betterfit.Migrations
{
    /// <inheritdoc />
    public partial class AddGymCrmAutomations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GymAutomationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    AudienceType = table.Column<string>(type: "text", nullable: false),
                    TargetLeadStage = table.Column<string>(type: "text", nullable: true),
                    ScheduleType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    NextRunAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubjectTemplate = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    MessageTemplate = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LastRunAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAudienceCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymAutomationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymAutomationRules_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymAutomationRules_GymLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "GymLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymAutomationRules_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymAutomationRules_TenantRoleAssignments_OwnerAssignmentId",
                        column: x => x.OwnerAssignmentId,
                        principalTable: "TenantRoleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GymAutomationRules_CreatedByUserId",
                table: "GymAutomationRules",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GymAutomationRules_GymId_Channel_Status",
                table: "GymAutomationRules",
                columns: new[] { "GymId", "Channel", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GymAutomationRules_GymId_Status_LocationId",
                table: "GymAutomationRules",
                columns: new[] { "GymId", "Status", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_GymAutomationRules_LocationId_NextRunAtUtc",
                table: "GymAutomationRules",
                columns: new[] { "LocationId", "NextRunAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymAutomationRules_OwnerAssignmentId",
                table: "GymAutomationRules",
                column: "OwnerAssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymAutomationRules");
        }
    }
}
