using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Oficina.Infrastructure.Persistence;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Oficina.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260829120000_AllowMultipleBudgetsPerServiceOrder")]
[ExcludeFromCodeCoverage]
public sealed class AllowMultipleBudgetsPerServiceOrder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Budgets_ServiceOrderId",
            table: "Budgets");

        migrationBuilder.CreateIndex(
            name: "IX_Budgets_ServiceOrderId",
            table: "Budgets",
            column: "ServiceOrderId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
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
}
