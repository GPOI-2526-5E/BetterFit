using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betterfit.Migrations
{
    /// <inheritdoc />
    public partial class SplitEmailVerificationSessionAndCodeExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExpiresAtUtc",
                table: "AuthenticationChallenges",
                newName: "SessionExpiresAtUtc");

            migrationBuilder.AddColumn<DateTime>(
                name: "CodeExpiresAtUtc",
                table: "AuthenticationChallenges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "AuthenticationChallenges"
                SET "CodeExpiresAtUtc" = "SessionExpiresAtUtc"
                WHERE "Type" = 'EmailVerification'
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuthenticationChallenges_CodeExpiryByType",
                table: "AuthenticationChallenges",
                sql: "(\"Type\" = 'EmailVerification' AND \"CodeExpiresAtUtc\" IS NOT NULL) OR (\"Type\" <> 'EmailVerification' AND \"CodeExpiresAtUtc\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuthenticationChallenges_CodeExpiryWithinSession",
                table: "AuthenticationChallenges",
                sql: "\"CodeExpiresAtUtc\" IS NULL OR \"CodeExpiresAtUtc\" <= \"SessionExpiresAtUtc\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AuthenticationChallenges_CodeExpiryByType",
                table: "AuthenticationChallenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AuthenticationChallenges_CodeExpiryWithinSession",
                table: "AuthenticationChallenges");

            migrationBuilder.DropColumn(
                name: "CodeExpiresAtUtc",
                table: "AuthenticationChallenges");

            migrationBuilder.RenameColumn(
                name: "SessionExpiresAtUtc",
                table: "AuthenticationChallenges",
                newName: "ExpiresAtUtc");
        }
    }
}
