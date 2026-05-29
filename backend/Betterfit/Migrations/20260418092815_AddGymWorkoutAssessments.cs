using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betterfit.Migrations
{
    /// <inheritdoc />
    public partial class AddGymWorkoutAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GymWorkoutAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    GymMembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<string>(type: "text", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    BodyFatPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    LeanMassKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    ChestCm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    WaistCm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    HipsCm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    ArmCm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    ThighCm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    CalfCm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    RestingHeartRateBpm = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymWorkoutAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymWorkoutAssessments_AspNetUsers_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymWorkoutAssessments_GymLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "GymLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymWorkoutAssessments_GymMemberships_GymMembershipId",
                        column: x => x.GymMembershipId,
                        principalTable: "GymMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymWorkoutAssessments_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymWorkoutAssessments_TenantRoleAssignments_CoachAssignment~",
                        column: x => x.CoachAssignmentId,
                        principalTable: "TenantRoleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutAssessments_CoachAssignmentId",
                table: "GymWorkoutAssessments",
                column: "CoachAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutAssessments_GymId_RecordedAtUtc",
                table: "GymWorkoutAssessments",
                columns: new[] { "GymId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutAssessments_GymMembershipId_RecordedAtUtc",
                table: "GymWorkoutAssessments",
                columns: new[] { "GymMembershipId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutAssessments_LocationId_RecordedAtUtc",
                table: "GymWorkoutAssessments",
                columns: new[] { "LocationId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutAssessments_RecordedByUserId",
                table: "GymWorkoutAssessments",
                column: "RecordedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymWorkoutAssessments");
        }
    }
}
