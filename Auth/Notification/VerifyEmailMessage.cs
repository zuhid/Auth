namespace Zuhid.Auth.Notification;

public class VerifyEmailMessage
{
    public required string Email { get; set; }
    public required string AppUrl { get; set; }
    public required string Token { get; set; }
}
