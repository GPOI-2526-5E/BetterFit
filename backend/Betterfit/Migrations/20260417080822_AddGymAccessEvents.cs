using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betterfit.Migrations
{
    /// <inheritdoc />
    public partial class AddGymAccessEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GymAccessEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    GymMembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    GateName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Result = table.Column<string>(type: "text", nullable: false),
                    Origin = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PerformedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymAccessEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymAccessEvents_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GymAccessEvents_GymLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "GymLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymAccessEvents_GymMemberships_GymMembershipId",
                        column: x => x.GymMembershipId,
                        principalTable: "GymMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymAccessEvents_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GymAccessEvents_GymId_OccurredAtUtc",
                table: "GymAccessEvents",
                columns: new[] { "GymId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymAccessEvents_GymMembershipId_OccurredAtUtc",
                table: "GymAccessEvents",
                columns: new[] { "GymMembershipId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymAccessEvents_LocationId_OccurredAtUtc",
                table: "GymAccessEvents",
                columns: new[] { "LocationId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymAccessEvents_PerformedByUserId",
                table: "GymAccessEvents",
                column: "PerformedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymAccessEvents");
        }
    }
}
