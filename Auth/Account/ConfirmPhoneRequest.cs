using System.ComponentModel.DataAnnotations;

namespace Zuhid.Auth.Account;

public record ConfirmPhoneRequest
(
    [Required] string Token
);
