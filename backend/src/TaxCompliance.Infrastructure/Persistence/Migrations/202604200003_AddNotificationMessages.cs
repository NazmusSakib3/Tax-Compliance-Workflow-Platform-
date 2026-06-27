using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaxCompliance.Infrastructure.Persistence;

#nullable disable

namespace TaxCompliance.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("202604200003_AddNotificationMessages")]
public partial class AddNotificationMessages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NotificationMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ComplianceTaskOccurrenceId = table.Column<Guid>(type: "uuid", nullable: false),
                NotificationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                RecipientEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                ProcessedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotificationMessages", x => x.Id);
                table.ForeignKey(
                    name: "FK_NotificationMessages_ComplianceTaskOccurrences_ComplianceTaskOccurrenceId",
                    column: x => x.ComplianceTaskOccurrenceId,
                    principalTable: "ComplianceTaskOccurrences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NotificationMessages_ComplianceTaskOccurrenceId_NotificationType",
            table: "NotificationMessages",
            columns: new[] { "ComplianceTaskOccurrenceId", "NotificationType" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "NotificationMessages");
    }
}
