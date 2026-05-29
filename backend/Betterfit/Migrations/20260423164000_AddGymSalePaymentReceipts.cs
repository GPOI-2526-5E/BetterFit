using System;
using Betterfit.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betterfit.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260423164000_AddGymSalePaymentReceipts")]
    public partial class AddGymSalePaymentReceipts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptCode",
                table: "GymSalePayments",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiptIssuedAtUtc",
                table: "GymSalePayments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymSalePayments_ReceiptCode",
                table: "GymSalePayments",
                column: "ReceiptCode",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GymSalePayments_ReceiptCode",
                table: "GymSalePayments");

            migrationBuilder.DropColumn(
                name: "ReceiptCode",
                table: "GymSalePayments");

            migrationBuilder.DropColumn(
                name: "ReceiptIssuedAtUtc",
                table: "GymSalePayments");
        }

        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
        }
    }
}
