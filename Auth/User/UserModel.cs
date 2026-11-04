using System.ComponentModel.DataAnnotations;

namespace Zuhid.Auth.User;

public class UserModel
{
    public Guid Id { get; set; }
    [Required, EmailAddress]
    public string Email { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
}
