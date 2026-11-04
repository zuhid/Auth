using System.ComponentModel.DataAnnotations;

namespace Zuhid.Auth.Account;

public record VerifyEmailRequest(
    [Required] string Email,
    [Required] string Token
);

