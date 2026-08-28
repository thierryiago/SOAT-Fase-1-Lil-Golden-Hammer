using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Oficina.Infrastructure.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class CorrectBudgetItemSnapshotColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "BudgetParts"
                    ADD COLUMN IF NOT EXISTS "PartName" character varying(100) NOT NULL DEFAULT '';
                ALTER TABLE "BudgetWorkshopServices"
                    ADD COLUMN IF NOT EXISTS "WorkshopServiceName" character varying(100) NOT NULL DEFAULT '';

                UPDATE "BudgetParts" AS budget_part
                SET "PartName" = part."Name",
                    "UnitPrice" = part."UnitPrice"
                FROM "Parts" AS part
                WHERE budget_part."PartId" = part."Id";

                UPDATE "BudgetWorkshopServices" AS budget_service
                SET "WorkshopServiceName" = workshop_service."Name",
                    "UnitPrice" = workshop_service."UnitPrice"
                FROM "WorkshopServices" AS workshop_service
                WHERE budget_service."WorkshopServiceId" = workshop_service."Id";

                ALTER TABLE "BudgetParts" DROP COLUMN IF EXISTS "Name";
                ALTER TABLE "BudgetWorkshopServices" DROP COLUMN IF EXISTS "Name";
                ALTER TABLE "BudgetParts" ALTER COLUMN "PartName" DROP DEFAULT;
                ALTER TABLE "BudgetWorkshopServices" ALTER COLUMN "WorkshopServiceName" DROP DEFAULT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
