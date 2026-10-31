using System.ComponentModel.DataAnnotations;

namespace NurseryManagement.Api.Contracts.Roles;

public class CreateRoleRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public List<Guid> PermissionIds { get; set; } = [];
}

public class UpdateRoleRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public List<Guid> PermissionIds { get; set; } = [];
}
