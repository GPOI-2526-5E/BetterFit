using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betterfit.Migrations
{
    /// <inheritdoc />
    public partial class AddGymActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GymActivityTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstructorAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ColorHex = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    RequiresBooking = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymActivityTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymActivityTemplates_GymLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "GymLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymActivityTemplates_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymActivityTemplates_TenantRoleAssignments_InstructorAssign~",
                        column: x => x.InstructorAssignmentId,
                        principalTable: "TenantRoleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GymActivitySessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    GymActivityTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstructorAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    InstructorName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymActivitySessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymActivitySessions_GymActivityTemplates_GymActivityTemplat~",
                        column: x => x.GymActivityTemplateId,
                        principalTable: "GymActivityTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymActivitySessions_GymLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "GymLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymActivitySessions_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymActivitySessions_TenantRoleAssignments_InstructorAssignm~",
                        column: x => x.InstructorAssignmentId,
                        principalTable: "TenantRoleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GymActivityBookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    GymActivitySessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GymMembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    BookedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckedInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymActivityBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymActivityBookings_GymActivitySessions_GymActivitySessionId",
                        column: x => x.GymActivitySessionId,
                        principalTable: "GymActivitySessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymActivityBookings_GymMemberships_GymMembershipId",
                        column: x => x.GymMembershipId,
                        principalTable: "GymMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymActivityBookings_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GymActivityBookings_GymActivitySessionId_GymMembershipId",
                table: "GymActivityBookings",
                columns: new[] { "GymActivitySessionId", "GymMembershipId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymActivityBookings_GymId_BookedAtUtc",
                table: "GymActivityBookings",
                columns: new[] { "GymId", "BookedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymActivityBookings_GymMembershipId",
                table: "GymActivityBookings",
                column: "GymMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_GymActivitySessions_GymActivityTemplateId_StartsAtUtc",
                table: "GymActivitySessions",
                columns: new[] { "GymActivityTemplateId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymActivitySessions_GymId_StartsAtUtc",
                table: "GymActivitySessions",
                columns: new[] { "GymId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymActivitySessions_InstructorAssignmentId",
                table: "GymActivitySessions",
                column: "InstructorAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GymActivitySessions_LocationId_StartsAtUtc",
                table: "GymActivitySessions",
                columns: new[] { "LocationId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymActivityTemplates_GymId_Name_LocationId",
                table: "GymActivityTemplates",
                columns: new[] { "GymId", "Name", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymActivityTemplates_InstructorAssignmentId",
                table: "GymActivityTemplates",
                column: "InstructorAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GymActivityTemplates_LocationId",
                table: "GymActivityTemplates",
                column: "LocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymActivityBookings");

            migrationBuilder.DropTable(
                name: "GymActivitySessions");

            migrationBuilder.DropTable(
                name: "GymActivityTemplates");
        }
    }
}
