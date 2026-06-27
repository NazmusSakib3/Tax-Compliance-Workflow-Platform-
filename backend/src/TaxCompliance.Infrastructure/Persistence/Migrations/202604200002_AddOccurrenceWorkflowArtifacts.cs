using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaxCompliance.Infrastructure.Persistence;

#nullable disable

namespace TaxCompliance.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("202604200002_AddOccurrenceWorkflowArtifacts")]
public partial class AddOccurrenceWorkflowArtifacts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AssignedToUserId",
            table: "ComplianceTaskOccurrences",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateTable(
            name: "AuditLogEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ComplianceTaskOccurrenceId = table.Column<Guid>(type: "uuid", nullable: false),
                ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                PerformedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                PerformedByDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_AuditLogEntries_ComplianceTaskOccurrences_ComplianceTaskOccurrenceId",
                    column: x => x.ComplianceTaskOccurrenceId,
                    principalTable: "ComplianceTaskOccurrences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TaskComments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ComplianceTaskOccurrenceId = table.Column<Guid>(type: "uuid", nullable: false),
                CommentText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CreatedByDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TaskComments", x => x.Id);
                table.ForeignKey(
                    name: "FK_TaskComments_ComplianceTaskOccurrences_ComplianceTaskOccurrenceId",
                    column: x => x.ComplianceTaskOccurrenceId,
                    principalTable: "ComplianceTaskOccurrences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TaskDocuments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ComplianceTaskOccurrenceId = table.Column<Guid>(type: "uuid", nullable: false),
                FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                StoredPath = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                UploadedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                UploadedByDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TaskDocuments", x => x.Id);
                table.ForeignKey(
                    name: "FK_TaskDocuments_ComplianceTaskOccurrences_ComplianceTaskOccurrenceId",
                    column: x => x.ComplianceTaskOccurrenceId,
                    principalTable: "ComplianceTaskOccurrences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogEntries_ComplianceTaskOccurrenceId",
            table: "AuditLogEntries",
            column: "ComplianceTaskOccurrenceId");

        migrationBuilder.CreateIndex(
            name: "IX_TaskComments_ComplianceTaskOccurrenceId",
            table: "TaskComments",
            column: "ComplianceTaskOccurrenceId");

        migrationBuilder.CreateIndex(
            name: "IX_TaskDocuments_ComplianceTaskOccurrenceId",
            table: "TaskDocuments",
            column: "ComplianceTaskOccurrenceId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AuditLogEntries");
        migrationBuilder.DropTable(name: "TaskComments");
        migrationBuilder.DropTable(name: "TaskDocuments");

        migrationBuilder.DropColumn(
            name: "AssignedToUserId",
            table: "ComplianceTaskOccurrences");
    }
}
