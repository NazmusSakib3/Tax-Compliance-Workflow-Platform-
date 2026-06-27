using System.ComponentModel.DataAnnotations;
using TaxCompliance.Application.Auth;

namespace TaxCompliance.Application.Users;

public class UserListItemDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; }
}

public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = RoleNames.Viewer;
}

public class UpdateUserRequest
{
    [Required]
    [StringLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = RoleNames.Viewer;

    public bool IsActive { get; set; } = true;
}
