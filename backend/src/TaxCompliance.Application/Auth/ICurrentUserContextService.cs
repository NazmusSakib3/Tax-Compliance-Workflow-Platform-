namespace TaxCompliance.Application.Auth;

public interface ICurrentUserContextService
{
    CurrentUserContext GetCurrentUser();
}

