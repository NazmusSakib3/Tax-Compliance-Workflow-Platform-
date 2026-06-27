using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.Notifications;
using TaxCompliance.Infrastructure.Identity;

namespace TaxCompliance.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IJwtTokenService jwtTokenService;
    private readonly IRefreshTokenService refreshTokenService;
    private readonly IMfaService mfaService;
    private readonly IMfaSecretProtector mfaSecretProtector;
    private readonly IEmailSender emailSender;
    private readonly PasswordResetOptions passwordResetOptions;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IMfaService mfaService,
        IMfaSecretProtector mfaSecretProtector,
        IEmailSender emailSender,
        IOptions<PasswordResetOptions> passwordResetOptions)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.jwtTokenService = jwtTokenService;
        this.refreshTokenService = refreshTokenService;
        this.mfaService = mfaService;
        this.mfaSecretProtector = mfaSecretProtector;
        this.emailSender = emailSender;
        this.passwordResetOptions = passwordResetOptions.Value;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("Login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid email or password."
            });
        }

        var passwordResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (passwordResult.IsLockedOut)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Account is temporarily locked. Try again later."
            });
        }

        if (!passwordResult.Succeeded)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid email or password."
            });
        }

        if (user.IsMfaEnabled)
        {
            var totpSecret = mfaSecretProtector.Unprotect(user.TotpSecret);
            if (string.IsNullOrWhiteSpace(request.MfaCode) || string.IsNullOrWhiteSpace(totpSecret) ||
                !mfaService.ValidateCode(totpSecret, request.MfaCode))
            {
                return Unauthorized(new ApiResponse<LoginResponse>
                {
                    Success = false,
                    Message = "A valid MFA code is required.",
                    Data = new LoginResponse { RequiresMfa = true, Email = user.Email ?? string.Empty }
                });
            }

            await ProtectLegacyMfaSecretAsync(user, totpSecret);
        }

        return Ok(new ApiResponse<LoginResponse>
        {
            Success = true,
            Message = "Login successful.",
            Data = await BuildLoginResponseAsync(user, cancellationToken)
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var refreshUser = await refreshTokenService.ValidateRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (refreshUser is null)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Refresh token is invalid or expired."
            });
        }

        await refreshTokenService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);

        var user = await userManager.FindByIdAsync(refreshUser.UserId);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new ApiResponse<LoginResponse>
        {
            Success = true,
            Message = "Token refreshed.",
            Data = await BuildLoginResponseAsync(user, cancellationToken)
        });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("Login")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await SendPasswordResetEmailAsync(user, token, cancellationToken);
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "If the account exists, password reset instructions have been sent."
        });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("Login")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(DecodePasswordResetValue(request.Email));
        if (user is null)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Unable to reset password for the provided account."
            });
        }

        var result = await userManager.ResetPasswordAsync(user, DecodePasswordResetValue(request.Token), request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = string.Join("; ", result.Errors.Select(error => error.Description))
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Password reset successful."
        });
    }

    [HttpPost("mfa/setup")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<MfaSetupResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MfaSetupResponse>>> SetupMfa(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync();
        var existingSecret = mfaSecretProtector.Unprotect(user.TotpSecret);
        var setup = mfaService.GenerateSetup(user.Email ?? string.Empty, existingSecret ?? mfaService.GenerateSecret());
        user.TotpSecret = mfaSecretProtector.Protect(setup.SharedKey);
        await userManager.UpdateAsync(user);

        return Ok(new ApiResponse<MfaSetupResponse>
        {
            Success = true,
            Message = "Scan the authenticator URI and verify with a code to enable MFA.",
            Data = setup
        });
    }

    [HttpPost("mfa/enable")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> EnableMfa([FromBody] MfaVerifyRequest request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync();
        var totpSecret = mfaSecretProtector.Unprotect(user.TotpSecret);
        if (string.IsNullOrWhiteSpace(totpSecret) || !mfaService.ValidateCode(totpSecret, request.Code))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid MFA verification code."
            });
        }

        user.IsMfaEnabled = true;
        user.TotpSecret = mfaSecretProtector.Protect(totpSecret);
        await userManager.UpdateAsync(user);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "MFA enabled."
        });
    }

    [HttpPost("mfa/disable")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> DisableMfa([FromBody] MfaVerifyRequest request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync();
        var totpSecret = mfaSecretProtector.Unprotect(user.TotpSecret);
        if (!user.IsMfaEnabled || string.IsNullOrWhiteSpace(totpSecret) ||
            !mfaService.ValidateCode(totpSecret, request.Code))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid MFA verification code."
            });
        }

        user.IsMfaEnabled = false;
        user.TotpSecret = null;
        await userManager.UpdateAsync(user);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "MFA disabled."
        });
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AuthenticatedUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthenticatedUserDto>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync();
        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var response = new AuthenticatedUserDto
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            OrganizationId = user.OrganizationId,
            Roles = roles,
            IsMfaEnabled = user.IsMfaEnabled
        };

        return Ok(new ApiResponse<AuthenticatedUserDto>
        {
            Success = true,
            Message = "Authenticated user loaded.",
            Data = response
        });
    }

    private async Task<LoginResponse> BuildLoginResponseAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var token = await jwtTokenService.CreateTokenAsync(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.OrganizationId,
            roles,
            cancellationToken);

        token.RefreshToken = await refreshTokenService.CreateRefreshTokenAsync(user.Id, cancellationToken);
        return token;
    }

    private async Task SendPasswordResetEmailAsync(ApplicationUser user, string token, CancellationToken cancellationToken)
    {
        var email = user.Email ?? throw new InvalidOperationException("Cannot send a password reset email without a user email.");
        if (string.IsNullOrWhiteSpace(passwordResetOptions.ClientResetUrl))
        {
            throw new InvalidOperationException("PasswordReset:ClientResetUrl must be configured before password reset emails can be sent.");
        }

        var encodedEmail = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(email));
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var resetUrl = $"{passwordResetOptions.ClientResetUrl}?email={encodedEmail}&token={encodedToken}";
        var body = new StringBuilder()
            .AppendLine("A password reset was requested for your Tax Compliance Workflow Platform account.")
            .AppendLine()
            .AppendLine($"Open this link to reset your password: {resetUrl}")
            .AppendLine()
            .AppendLine("If you did not request this reset, you can ignore this email.")
            .ToString();

        await emailSender.SendAsync(email, "Password reset instructions", body, cancellationToken);
    }

    private async Task ProtectLegacyMfaSecretAsync(ApplicationUser user, string totpSecret)
    {
        if (mfaSecretProtector.IsProtected(user.TotpSecret))
        {
            return;
        }

        user.TotpSecret = mfaSecretProtector.Protect(totpSecret);
        await userManager.UpdateAsync(user);
    }

    private static string DecodePasswordResetValue(string value)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(value));
        }
        catch (FormatException)
        {
            return value;
        }
    }

    private async Task<ApplicationUser> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException();
        }

        return await userManager.FindByIdAsync(userId)
            ?? throw new UnauthorizedAccessException();
    }
}
