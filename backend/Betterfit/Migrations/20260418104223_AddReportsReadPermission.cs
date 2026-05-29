using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betterfit.Migrations
{
    /// <inheritdoc />
    public partial class AddReportsReadPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PermissionCatalogItems",
                columns: new[] { "Id", "Action", "Description", "NormalizedAction", "NormalizedResource", "Resource" },
                values: new object[] { new Guid("7689fa0a-38ae-4d10-b8fc-e6db96ca15ec"), "read", "View reports and KPI dashboards", "READ", "REPORTS", "reports" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("7689fa0a-38ae-4d10-b8fc-e6db96ca15ec"));
        }
    }
}
