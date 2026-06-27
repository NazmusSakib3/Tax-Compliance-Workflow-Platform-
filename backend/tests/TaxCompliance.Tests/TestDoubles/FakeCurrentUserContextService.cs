using TaxCompliance.Application.Auth;

namespace TaxCompliance.Tests.TestDoubles;

public class FakeCurrentUserContextService : ICurrentUserContextService
{
    public CurrentUserContext CurrentUser { get; set; } = new()
    {
        UserId = "system",
        DisplayName = "System",
        Email = "system@local",
        Roles = Array.Empty<string>()
    };

    public CurrentUserContext GetCurrentUser()
    {
        return CurrentUser;
    }
}

