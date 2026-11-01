namespace Zuhid.Auth.Notification;

public class TwoFactorEmailMessage
{
  public required string Email { get; set; }
  public required string Token { get; set; }
}
