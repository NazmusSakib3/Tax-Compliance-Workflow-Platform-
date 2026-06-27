using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaxCompliance.Infrastructure.Persistence;

#nullable disable

namespace TaxCompliance.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("202604200001_AddComplianceTaskOccurrences")]
public partial class AddComplianceTaskOccurrences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ComplianceTaskOccurrences",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ComplianceTaskRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                PeriodStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                PeriodEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComplianceTaskOccurrences", x => x.Id);
                table.ForeignKey(
                    name: "FK_ComplianceTaskOccurrences_ComplianceTaskRules_ComplianceTaskRuleId",
                    column: x => x.ComplianceTaskRuleId,
                    principalTable: "ComplianceTaskRules",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ComplianceTaskOccurrences_ComplianceTaskRuleId_PeriodStartDate_PeriodEndDate",
            table: "ComplianceTaskOccurrences",
            columns: new[] { "ComplianceTaskRuleId", "PeriodStartDate", "PeriodEndDate" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ComplianceTaskOccurrences");
    }
}
