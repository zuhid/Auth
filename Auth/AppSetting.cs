using Zuhid.Base;

namespace Zuhid.Auth;

public class AppSetting : SettingBase
{
    public string AppUrl { get; set; } = default!;
    public ConnectionString ConnectionStrings { get; set; } = default!;
    public NotificationConfig Notification { get; set; } = default!;

    public class NotificationConfig : IApiClientOptions
    {
        public string BaseUrl { get; set; } = default!;
        public string Authorization { get; set; } = string.Empty;
    }

    public class ConnectionString
    {
        public string Auth { get; set; } = default!;
        public string Log { get; set; } = default!;
    }

    public AppSetting(IConfiguration configuration) : base(configuration)
    {
        ConnectionStrings = new ConnectionString
        {
            Auth = ReplaceCredential(configuration, "Auth"),
            Log = ReplaceCredential(configuration, "Log"),
        };
    }

    private static string ReplaceCredential(IConfiguration configuration, string connString)
    {
        return configuration.GetConnectionString(connString)!
          .Replace("[postgres_credential]", configuration.GetValue<string>("postgres_credential"), StringComparison.Ordinal);
    }
}
