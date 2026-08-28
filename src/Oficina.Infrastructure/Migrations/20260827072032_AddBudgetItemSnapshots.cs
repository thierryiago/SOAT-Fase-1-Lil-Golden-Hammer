using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Oficina.Infrastructure.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class AddBudgetItemSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "BudgetWorkshopServices"
                    ADD COLUMN IF NOT EXISTS "Name" character varying(100) NOT NULL DEFAULT '';
                ALTER TABLE "BudgetWorkshopServices"
                    ADD COLUMN IF NOT EXISTS "UnitPrice" numeric(18,2) NOT NULL DEFAULT 0;
                ALTER TABLE "BudgetParts"
                    ADD COLUMN IF NOT EXISTS "Name" character varying(100) NOT NULL DEFAULT '';
                ALTER TABLE "BudgetParts"
                    ADD COLUMN IF NOT EXISTS "UnitPrice" numeric(18,2) NOT NULL DEFAULT 0;
                """);

            migrationBuilder.Sql("""
                UPDATE "BudgetParts" AS budget_part
                SET "Name" = part."Name",
                    "UnitPrice" = part."UnitPrice"
                FROM "Parts" AS part
                WHERE budget_part."PartId" = part."Id";
                """);

            migrationBuilder.Sql("""
                UPDATE "BudgetWorkshopServices" AS budget_service
                SET "Name" = workshop_service."Name",
                    "UnitPrice" = workshop_service."UnitPrice"
                FROM "WorkshopServices" AS workshop_service
                WHERE budget_service."WorkshopServiceId" = workshop_service."Id";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "BudgetWorkshopServices" ALTER COLUMN "Name" DROP DEFAULT;
                ALTER TABLE "BudgetWorkshopServices" ALTER COLUMN "UnitPrice" DROP DEFAULT;
                ALTER TABLE "BudgetParts" ALTER COLUMN "Name" DROP DEFAULT;
                ALTER TABLE "BudgetParts" ALTER COLUMN "UnitPrice" DROP DEFAULT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "BudgetWorkshopServices" DROP COLUMN IF EXISTS "Name";
                ALTER TABLE "BudgetWorkshopServices" DROP COLUMN IF EXISTS "UnitPrice";
                ALTER TABLE "BudgetParts" DROP COLUMN IF EXISTS "Name";
                ALTER TABLE "BudgetParts" DROP COLUMN IF EXISTS "UnitPrice";
                """);
        }
    }
}
