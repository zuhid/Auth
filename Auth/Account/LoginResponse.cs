using System.Text.Json.Serialization;

namespace Zuhid.Auth.Account;

/// <summary>
/// Returned by Login and LoginTwoFactor. Either the token fields (Token, Theme) or the
/// 2FA fields (RequiresTwoFactor, UserId, Providers) are set; null fields are omitted so
/// each shape serializes exactly as before.
/// </summary>
public class LoginResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Token { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Theme { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? RequiresTwoFactor { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? UserId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Providers { get; init; }
}
