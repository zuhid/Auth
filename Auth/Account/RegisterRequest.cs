using System.ComponentModel.DataAnnotations;

namespace Zuhid.Auth.Account;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    string FirstName,
    string LastName,
    string Phone
);
