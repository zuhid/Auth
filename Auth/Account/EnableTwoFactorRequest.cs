namespace Zuhid.Auth.Account;

public class EnableTwoFactorRequest
{
  public Guid UserId { get; set; }
  public string Code { get; set; } = string.Empty;
}
