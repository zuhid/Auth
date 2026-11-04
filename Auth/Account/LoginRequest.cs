using System.ComponentModel.DataAnnotations;

namespace Zuhid.Auth.Account;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);
