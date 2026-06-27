namespace TaxCompliance.Application.Auth;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string RuleManagement = "RuleManagement";
    public const string ContributorAccess = "ContributorAccess";
    public const string ReaderAccess = "ReaderAccess";
}

