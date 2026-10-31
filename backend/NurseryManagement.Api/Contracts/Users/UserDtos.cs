using System.ComponentModel.DataAnnotations;

namespace NurseryManagement.Api.Contracts.Users;

public class CreateUserRequest
{
    [Required, MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<Guid> RoleIds { get; set; } = [];
}

public class UpdateUserRequest
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<Guid> RoleIds { get; set; } = [];
}

public class ResetPasswordRequest
{
    [Required, MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
