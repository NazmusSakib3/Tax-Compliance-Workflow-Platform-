namespace TaxCompliance.Application.Auth;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string ComplianceManager = "ComplianceManager";
    public const string Contributor = "Contributor";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [Admin, ComplianceManager, Contributor, Viewer];
}
