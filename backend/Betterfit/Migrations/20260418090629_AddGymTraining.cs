using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betterfit.Migrations
{
    /// <inheritdoc />
    public partial class AddGymTraining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GymWorkoutExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    MuscleGroup = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Equipment = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VideoUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymWorkoutExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymWorkoutExercises_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GymWorkoutTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Goal = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Level = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    DaysPerWeek = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymWorkoutTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymWorkoutTemplates_GymLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "GymLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymWorkoutTemplates_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymWorkoutTemplates_TenantRoleAssignments_CoachAssignmentId",
                        column: x => x.CoachAssignmentId,
                        principalTable: "TenantRoleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GymWorkoutAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    GymMembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    GymWorkoutTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    CoachAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Goal = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevisionDueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymWorkoutAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymWorkoutAssignments_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymWorkoutAssignments_GymLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "GymLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymWorkoutAssignments_GymMemberships_GymMembershipId",
                        column: x => x.GymMembershipId,
                        principalTable: "GymMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymWorkoutAssignments_GymWorkoutTemplates_GymWorkoutTemplat~",
                        column: x => x.GymWorkoutTemplateId,
                        principalTable: "GymWorkoutTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GymWorkoutAssignments_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymWorkoutAssignments_TenantRoleAssignments_CoachAssignment~",
                        column: x => x.CoachAssignmentId,
                        principalTable: "TenantRoleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GymWorkoutTemplateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymWorkoutTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: true),
                    DayNumber = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ExerciseName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SetsPrescription = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RepetitionsPrescription = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RestSeconds = table.Column<int>(type: "integer", nullable: true),
                    Tempo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymWorkoutTemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymWorkoutTemplateItems_GymWorkoutExercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "GymWorkoutExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GymWorkoutTemplateItems_GymWorkoutTemplates_GymWorkoutTempl~",
                        column: x => x.GymWorkoutTemplateId,
                        principalTable: "GymWorkoutTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GymWorkoutAssignmentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymWorkoutAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: true),
                    DayNumber = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ExerciseName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SetsPrescription = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RepetitionsPrescription = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RestSeconds = table.Column<int>(type: "integer", nullable: true),
                    Tempo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymWorkoutAssignmentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymWorkoutAssignmentItems_GymWorkoutAssignments_GymWorkoutA~",
                        column: x => x.GymWorkoutAssignmentId,
                        principalTable: "GymWorkoutAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymWorkoutAssignmentItems_GymWorkoutExercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "GymWorkoutExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutAssignmentItems_ExerciseId",
                table: "GymWorkoutAssignmentItems",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutAssignmentItems_GymWorkoutAssignmentId_DayNumber_~",
                table: "GymWorkoutAssignmentItems",
                columns: new[] { "GymWorkoutAssignmentId", "DayNumber", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutAssignments_CoachAssignmentId",
                table: "GymWorkoutAssignments",
                column: "CoachAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutAssignments_CreatedByUserId",
                table: "GymWorkoutAssignments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutAssignments_GymId_AssignedAtUtc",
                table: "GymWorkoutAssignments",
                columns: new[] { "GymId", "AssignedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutAssignments_GymMembershipId_AssignedAtUtc",
                table: "GymWorkoutAssignments",
                columns: new[] { "GymMembershipId", "AssignedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutAssignments_GymWorkoutTemplateId",
                table: "GymWorkoutAssignments",
                column: "GymWorkoutTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutAssignments_LocationId_AssignedAtUtc",
                table: "GymWorkoutAssignments",
                columns: new[] { "LocationId", "AssignedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutExercises_GymId_Name",
                table: "GymWorkoutExercises",
                columns: new[] { "GymId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutTemplateItems_ExerciseId",
                table: "GymWorkoutTemplateItems",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutTemplateItems_GymWorkoutTemplateId_DayNumber_Sort~",
                table: "GymWorkoutTemplateItems",
                columns: new[] { "GymWorkoutTemplateId", "DayNumber", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutTemplates_CoachAssignmentId",
                table: "GymWorkoutTemplates",
                column: "CoachAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutTemplates_GymId_LocationId_Name",
                table: "GymWorkoutTemplates",
                columns: new[] { "GymId", "LocationId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutTemplates_LocationId",
                table: "GymWorkoutTemplates",
                column: "LocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymWorkoutAssignmentItems");

            migrationBuilder.DropTable(
                name: "GymWorkoutTemplateItems");

            migrationBuilder.DropTable(
                name: "GymWorkoutAssignments");

            migrationBuilder.DropTable(
                name: "GymWorkoutExercises");

            migrationBuilder.DropTable(
                name: "GymWorkoutTemplates");
        }
    }
}
