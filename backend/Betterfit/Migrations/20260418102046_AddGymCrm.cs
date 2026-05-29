using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Betterfit.Migrations
{
    /// <inheritdoc />
    public partial class AddGymCrm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GymLeads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConvertedMembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Stage = table.Column<string>(type: "text", nullable: false),
                    Interest = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastContactedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextFollowUpAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymLeads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymLeads_GymLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "GymLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymLeads_GymMemberships_ConvertedMembershipId",
                        column: x => x.ConvertedMembershipId,
                        principalTable: "GymMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GymLeads_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymLeads_TenantRoleAssignments_OwnerAssignmentId",
                        column: x => x.OwnerAssignmentId,
                        principalTable: "TenantRoleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GymLeadTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymLeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymLeadTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymLeadTasks_GymLeads_GymLeadId",
                        column: x => x.GymLeadId,
                        principalTable: "GymLeads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymLeadTasks_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymLeadTasks_TenantRoleAssignments_AssignedAssignmentId",
                        column: x => x.AssignedAssignmentId,
                        principalTable: "TenantRoleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "PermissionCatalogItems",
                columns: new[] { "Id", "Action", "Description", "NormalizedAction", "NormalizedResource", "Resource" },
                values: new object[,]
                {
                    { new Guid("75d7dd0d-9997-4f7a-8258-8a98b2e8717f"), "write", "Create or update leads and commercial tasks", "WRITE", "CRM", "crm" },
                    { new Guid("99727c60-2d9b-4c92-bc55-d1cc633a09f9"), "read", "View leads and commercial tasks", "READ", "CRM", "crm" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GymLeads_ConvertedMembershipId",
                table: "GymLeads",
                column: "ConvertedMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_GymLeads_GymId_CreatedAtUtc",
                table: "GymLeads",
                columns: new[] { "GymId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymLeads_GymId_Stage_LocationId",
                table: "GymLeads",
                columns: new[] { "GymId", "Stage", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_GymLeads_LocationId_NextFollowUpAtUtc",
                table: "GymLeads",
                columns: new[] { "LocationId", "NextFollowUpAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymLeads_OwnerAssignmentId",
                table: "GymLeads",
                column: "OwnerAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GymLeadTasks_AssignedAssignmentId",
                table: "GymLeadTasks",
                column: "AssignedAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GymLeadTasks_GymId_Status_DueAtUtc",
                table: "GymLeadTasks",
                columns: new[] { "GymId", "Status", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymLeadTasks_GymLeadId_Status_DueAtUtc",
                table: "GymLeadTasks",
                columns: new[] { "GymLeadId", "Status", "DueAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymLeadTasks");

            migrationBuilder.DropTable(
                name: "GymLeads");

            migrationBuilder.DeleteData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("75d7dd0d-9997-4f7a-8258-8a98b2e8717f"));

            migrationBuilder.DeleteData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("99727c60-2d9b-4c92-bc55-d1cc633a09f9"));
        }
    }
}
