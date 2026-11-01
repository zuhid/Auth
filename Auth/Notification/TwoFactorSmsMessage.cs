namespace Zuhid.Auth.Notification;

public class TwoFactorSmsMessage
{
  public required string Phone { get; set; }
  public required string Token { get; set; }
}
