using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Oficina.Infrastructure.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class EnsureSingleBudgetPerServiceOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Budgets_ServiceOrderId",
                table: "Budgets");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_ServiceOrderId",
                table: "Budgets",
                column: "ServiceOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Budgets_ServiceOrderId",
                table: "Budgets");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_ServiceOrderId",
                table: "Budgets",
                column: "ServiceOrderId");
        }
    }
}
