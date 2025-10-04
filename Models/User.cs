using System.ComponentModel.DataAnnotations;
namespace UserManagementAPI.Models;

public class User
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Role { get; set; } = string.Empty;
}