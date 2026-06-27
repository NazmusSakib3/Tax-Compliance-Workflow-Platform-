namespace TaxCompliance.Application.Auth;

public interface IJwtTokenService
{
    Task<LoginResponse> CreateTokenAsync(
        string userId,
        string email,
        string displayName,
        Guid? organizationId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken);
}

public interface IRefreshTokenService
{
    Task<string> CreateRefreshTokenAsync(string userId, CancellationToken cancellationToken);

    Task<RefreshTokenUser?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
}

public class RefreshTokenUser
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
}

public interface IMfaService
{
    MfaSetupResponse GenerateSetup(string email, string? existingSecret);

    bool ValidateCode(string secret, string code);

    string GenerateSecret();
}
