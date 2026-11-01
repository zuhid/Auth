namespace Zuhid.Auth.Notification;

public class VerifyPhoneMessage
{
  public required string Phone { get; set; }
  public required string Token { get; set; }
}
