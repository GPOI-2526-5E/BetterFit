using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betterfit.Migrations
{
    /// <inheritdoc />
    public partial class AddGymCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GymCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    AudienceType = table.Column<string>(type: "text", nullable: false),
                    TargetLeadStage = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ScheduledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAudienceCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymCampaigns_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymCampaigns_GymLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "GymLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymCampaigns_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymCampaigns_TenantRoleAssignments_OwnerAssignmentId",
                        column: x => x.OwnerAssignmentId,
                        principalTable: "TenantRoleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GymCampaigns_CreatedByUserId",
                table: "GymCampaigns",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GymCampaigns_GymId_Channel_Status",
                table: "GymCampaigns",
                columns: new[] { "GymId", "Channel", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GymCampaigns_GymId_Status_LocationId",
                table: "GymCampaigns",
                columns: new[] { "GymId", "Status", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_GymCampaigns_LocationId_ScheduledAtUtc",
                table: "GymCampaigns",
                columns: new[] { "LocationId", "ScheduledAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymCampaigns_OwnerAssignmentId",
                table: "GymCampaigns",
                column: "OwnerAssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymCampaigns");
        }
    }
}
