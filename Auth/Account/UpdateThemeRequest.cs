using System.ComponentModel.DataAnnotations;

namespace Zuhid.Auth.Account;

public record UpdateThemeRequest([Required] string Theme);
