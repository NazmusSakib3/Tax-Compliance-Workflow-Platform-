using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.Notifications;
using TaxCompliance.Infrastructure.Identity;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Tests.Integration;

public class AuthIntegrationTests : IClassFixture<TaxComplianceApiFactory>
{
    private readonly TaxComplianceApiFactory factory;

    public AuthIntegrationTests(TaxComplianceApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Login_ShouldReturnJwtToken_ForSeededAdminUser()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "admin@taxplatform.local",
            Password = "Admin123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        payload.Data.Roles.Should().Contain(RoleNames.Admin);
    }

    [Fact]
    public async Task AuthenticatedRequest_ShouldReturnCurrentUserProfile()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(client));

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthenticatedUserDto>>();
        payload.Should().NotBeNull();
        payload!.Data.Should().NotBeNull();
        payload.Data!.Email.Should().Be("admin@taxplatform.local");
        payload.Data.Roles.Should().Contain(RoleNames.Admin);
    }

    [Fact]
    public async Task ForgotPassword_ShouldSendResetEmail_WithoutReturningTokenInResponse()
    {
        var emailSender = new RecordingEmailSender();
        using var client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IEmailSender>();
                    services.AddSingleton<IEmailSender>(emailSender);
                });
            })
            .CreateClient();
        var email = $"reset-{Guid.NewGuid():N}@example.com";
        await CreateUserAsync(client, email, "Reset User");

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest
        {
            Email = email
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().NotContain("resetToken", because: "password reset tokens must not be exposed in API responses");
        emailSender.Messages.Should().ContainSingle(message =>
            message.RecipientEmail == email &&
            message.Subject.Contains("password reset", StringComparison.OrdinalIgnoreCase) &&
            message.Body.Contains("reset your password", StringComparison.OrdinalIgnoreCase) &&
            message.Body.Contains("token=", StringComparison.OrdinalIgnoreCase));
        emailSender.Messages.Single().Body.Should().NotContain("reset token in the password reset form");
    }

    [Fact]
    public async Task ResetPassword_ShouldRejectInvalidToken()
    {
        using var client = factory.CreateClient();
        var email = $"invalid-reset-{Guid.NewGuid():N}@example.com";
        await CreateUserAsync(client, email, "Reset User");

        var response = await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest
        {
            Email = email,
            Token = "invalid-token",
            NewPassword = "NewValidPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().NotContain("resetToken");
    }

    [Fact]
    public async Task ResetPassword_ShouldSucceed_WithBase64UrlEncodedEmailAndToken()
    {
        using var client = factory.CreateClient();
        var email = $"encoded-reset-{Guid.NewGuid():N}@example.com";
        await CreateUserAsync(client, email, "Reset User");

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user!);
        var encodedEmail = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(email));
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));

        var response = await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest
        {
            Email = encodedEmail,
            Token = encodedToken,
            NewPassword = "NewValidPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "NewValidPass123!"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ShouldLockOutUserAfterRepeatedInvalidPasswordAttempts()
    {
        using var client = factory.CreateClient();
        var email = $"lockout-{Guid.NewGuid():N}@example.com";
        await CreateUserAsync(client, email, "Lockout User");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var invalidResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Email = email,
                Password = "WrongPassword123!"
            });

            invalidResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "ValidPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("locked", because: "locked-out users should not be allowed to authenticate with a valid password");
    }

    [Fact]
    public async Task SetupMfa_ShouldStoreProtectedSecretInsteadOfPlainSharedKey()
    {
        using var client = factory.CreateClient();
        var email = $"mfa-{Guid.NewGuid():N}@example.com";
        await CreateUserAsync(client, email, "MFA User");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await GetAccessTokenAsync(client, email, "ValidPass123!"));

        var response = await client.PostAsync("/api/auth/mfa/setup", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<MfaSetupResponse>>();
        payload.Should().NotBeNull();
        payload!.Data.Should().NotBeNull();
        payload.Data!.SharedKey.Should().NotBeNullOrWhiteSpace();

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();
        user!.TotpSecret.Should().NotBeNullOrWhiteSpace();
        user.TotpSecret.Should().NotBe(payload.Data.SharedKey, because: "TOTP secrets should be protected at rest");
    }

    private async Task CreateUserAsync(HttpClient client, string email, string displayName)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var organizationId = dbContext.Organizations.Single().Id;
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true,
            LockoutEnabled = true,
            OrganizationId = organizationId
        };

        var result = await userManager.CreateAsync(user, "ValidPass123!");
        result.Succeeded.Should().BeTrue(string.Join("; ", result.Errors.Select(error => error.Description)));
    }

    private static async Task<string> GetAccessTokenAsync(HttpClient client)
    {
        return await GetAccessTokenAsync(client, "admin@taxplatform.local", "Admin123!");
    }

    private static async Task<string> GetAccessTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return payload?.Data?.AccessToken ?? string.Empty;
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<EmailMessage> Messages { get; } = [];

        public Task SendAsync(string recipientEmail, string subject, string body, CancellationToken cancellationToken)
        {
            Messages.Add(new EmailMessage(recipientEmail, subject, body));
            return Task.CompletedTask;
        }
    }

    private sealed record EmailMessage(string RecipientEmail, string Subject, string Body);
}
