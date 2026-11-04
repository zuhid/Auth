namespace Zuhid.Auth.Account;

public class EnableTwoFactorResponse
{
    public string SharedKey { get; init; } = string.Empty;
    public string AuthenticatorUri { get; init; } = string.Empty;
}
