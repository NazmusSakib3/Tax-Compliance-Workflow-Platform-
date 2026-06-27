using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaxCompliance.Infrastructure.Persistence;

#nullable disable

namespace TaxCompliance.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("202606170001_MultiTenantAuthAndPaging")]
public partial class MultiTenantAuthAndPaging : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "Jurisdictions",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "ComplianceTemplates",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "AspNetUsers",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TotpSecret",
            table: "AspNetUsers",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsMfaEnabled",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ExpiresUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RevokedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Jurisdictions_OrganizationId_CountryCode_RegionCode",
            table: "Jurisdictions",
            columns: new[] { "OrganizationId", "CountryCode", "RegionCode" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ComplianceTemplates_OrganizationId_Name",
            table: "ComplianceTemplates",
            columns: new[] { "OrganizationId", "Name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_TokenHash",
            table: "RefreshTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId",
            table: "RefreshTokens",
            column: "UserId");

        migrationBuilder.DropIndex(
            name: "IX_Jurisdictions_CountryCode_RegionCode",
            table: "Jurisdictions");

        migrationBuilder.DropIndex(
            name: "IX_ComplianceTemplates_Name",
            table: "ComplianceTemplates");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RefreshTokens");

        migrationBuilder.DropIndex(
            name: "IX_Jurisdictions_OrganizationId_CountryCode_RegionCode",
            table: "Jurisdictions");

        migrationBuilder.DropIndex(
            name: "IX_ComplianceTemplates_OrganizationId_Name",
            table: "ComplianceTemplates");

        migrationBuilder.DropColumn(name: "OrganizationId", table: "Jurisdictions");
        migrationBuilder.DropColumn(name: "OrganizationId", table: "ComplianceTemplates");
        migrationBuilder.DropColumn(name: "OrganizationId", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "TotpSecret", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "IsMfaEnabled", table: "AspNetUsers");

        migrationBuilder.CreateIndex(
            name: "IX_Jurisdictions_CountryCode_RegionCode",
            table: "Jurisdictions",
            columns: new[] { "CountryCode", "RegionCode" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ComplianceTemplates_Name",
            table: "ComplianceTemplates",
            column: "Name",
            unique: true);
    }
}
