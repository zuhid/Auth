using Zuhid.Base;

namespace Zuhid.Auth.Notification;

public class NotificationApiClient(HttpClient httpClient, AppSetting appSetting, ILogger<NotificationApiClient> logger)
    : ApiClient(httpClient, appSetting.Notification, logger)
{
    public override Task<bool> Login() => Task.FromResult(true);

    public async Task VerifyEmail(VerifyEmailMessage message) => await Post("Auth/VerifyEmail", message);

    public async Task VerifyPhone(VerifyPhoneMessage message) => await Post("Auth/VerifyPhone", message);

    public async Task SendTwoFactorEmail(TwoFactorEmailMessage message) => await Post("Auth/SendTwoFactorEmail", message);

    public async Task SendTwoFactorSms(TwoFactorSmsMessage message) => await Post("Auth/SendTwoFactorSms", message);
}
