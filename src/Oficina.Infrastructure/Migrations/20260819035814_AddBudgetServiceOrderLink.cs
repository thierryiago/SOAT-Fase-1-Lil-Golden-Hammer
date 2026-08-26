using System;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Oficina.Infrastructure.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class AddBudgetServiceOrderLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceOrderId",
                table: "Budgets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_ServiceOrderId",
                table: "Budgets",
                column: "ServiceOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_ServiceOrders_ServiceOrderId",
                table: "Budgets",
                column: "ServiceOrderId",
                principalTable: "ServiceOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_ServiceOrders_ServiceOrderId",
                table: "Budgets");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_ServiceOrderId",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "ServiceOrderId",
                table: "Budgets");
        }
    }
}
