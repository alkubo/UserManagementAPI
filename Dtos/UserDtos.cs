using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Dtos;


public class UserCreateDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Role { get; set; } = string.Empty;
}

public class UserUpdateDto
{
    // Optional fields for partial updates
    [StringLength(100)]
    public string? Name { get; set; }

    [EmailAddress, StringLength(200)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? Role { get; set; }
}