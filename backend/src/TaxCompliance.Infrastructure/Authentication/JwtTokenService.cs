using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaxCompliance.Application.Auth;

namespace TaxCompliance.Infrastructure.Authentication;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions jwtOptions;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions)
    {
        this.jwtOptions = jwtOptions.Value;
    }

    public Task<LoginResponse> CreateTokenAsync(
        string userId,
        string email,
        string displayName,
        Guid? organizationId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        var expirationUtc = DateTime.UtcNow.AddMinutes(jwtOptions.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, displayName)
        };

        if (organizationId.HasValue)
        {
            claims.Add(new Claim(AuthClaimTypes.OrganizationId, organizationId.Value.ToString()));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            expires: expirationUtc,
            signingCredentials: signingCredentials);

        var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        return Task.FromResult(new LoginResponse
        {
            AccessToken = token,
            ExpiresUtc = expirationUtc,
            UserId = userId,
            Email = email,
            DisplayName = displayName,
            OrganizationId = organizationId,
            Roles = roles
        });
    }
}
