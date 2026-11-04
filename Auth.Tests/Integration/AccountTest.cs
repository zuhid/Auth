using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Zuhid.Auth.Account;
using Zuhid.Auth.Entities;

namespace Zuhid.Auth.Tests.Integration;

// End-to-end tests through the real HTTP pipeline. Outgoing mail/SMS is sent by the Notification
// service (localhost:5102) to MailHog (localhost:8025); SMS messages land in the phone@test.com inbox.
public class AccountTest(WebApplicationFactory<Program> factory) : BaseIntegrationTest(factory)
{
    private const string Password = "Password123!";
    private const string PhoneInbox = "phone@test.com";
    private static readonly HttpClient MailHogClient = new() { BaseAddress = new Uri("http://localhost:8025") };

    #region MailHog helpers

    private static async Task<List<(string Id, string Body)>> GetMailHogMessages(string to)
    {
        var response = await MailHogClient.GetAsync($"/api/v2/search?kind=to&query={Uri.EscapeDataString(to)}");
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = doc.RootElement.GetProperty("items");
        return items.ValueKind != JsonValueKind.Array
            ? []
            : [.. items.EnumerateArray().Select(item =>
        {
            var content = item.GetProperty("Content");
            var headers = content.GetProperty("Headers");
            var body = content.GetProperty("Body").GetString() ?? string.Empty;
            var encoding = headers.TryGetProperty("Content-Transfer-Encoding", out var cte) ? cte[0].GetString() : null;
            var decoded = encoding switch
            {
                "base64" => Encoding.UTF8.GetString(Convert.FromBase64String(body)),
                "quoted-printable" => DecodeQuotedPrintable(body),
                _ => body,
            };
            return (item.GetProperty("ID").GetString()!, decoded);
        })];
    }

    private static async Task DeleteMailHogMessages(string to)
    {
        foreach (var (id, _) in await GetMailHogMessages(to))
        {
            await MailHogClient.DeleteAsync($"/api/v1/messages/{id}");
        }
    }

    // Polls MailHog until a message to `to` matches `pattern`, returning the first capture group.
    private static async Task<string?> WaitForMatch(string to, string pattern)
    {
        for (var i = 0; i < 20; i++)
        {
            await Task.Delay(500);
            foreach (var (_, body) in await GetMailHogMessages(to))
            {
                var match = Regex.Match(body, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
        }
        return null;
    }

    private static string DecodeQuotedPrintable(string input)
    {
        input = input.Replace("=\r\n", "").Replace("=\n", "");
        var bytes = new List<byte>();
        for (var i = 0; i < input.Length; i++)
        {
            if (input[i] == '=' && i + 2 < input.Length && Uri.IsHexDigit(input[i + 1]) && Uri.IsHexDigit(input[i + 2]))
            {
                bytes.Add(Convert.ToByte(input.Substring(i + 1, 2), 16));
                i += 2;
            }
            else
            {
                bytes.AddRange(Encoding.UTF8.GetBytes(input[i].ToString()));
            }
        }
        return Encoding.UTF8.GetString([.. bytes]);
    }

    private static Task<string?> WaitForEmailVerificationToken(string email) =>
        WaitForMatch(email, @"/account/verify-email/[^/]+/([^""\s<]+)""");

    private static Task<string?> WaitForSmsCode(string pattern) => WaitForMatch(PhoneInbox, pattern);

    #endregion

    #region Account helpers

    private async Task<HttpResponseMessage> Register(string email) =>
        await Client.PostAsJsonAsync("/Account/Register",
            new { Email = email, Password, FirstName = "Test", LastName = "User", Phone = "1234567890" });

    private async Task<UserEntity> GetUser(string email)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();
        return (await userManager.FindByEmailAsync(email))!;
    }

    // Mint a JWT the same way the API does after login, without going through /Account/Login.
    private async Task<string> GenerateJwt(UserEntity user)
    {
        using var scope = Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<JwtService>();
        return await jwtService.GenerateToken(user);
    }

    private async Task<(UserEntity user, string jwt)> RegisterAndVerifyEmail(string email)
    {
        await DeleteMailHogMessages(email);
        var registerResponse = await Register(email);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var token = await WaitForEmailVerificationToken(email);
        Assert.False(string.IsNullOrEmpty(token), $"Did not receive verification email for {email}");

        var verifyResponse = await Client.PostAsJsonAsync("/Account/VerifyEmail",
            new { Email = email, Token = Uri.UnescapeDataString(token!) });
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var user = await GetUser(email);
        return (user, await GenerateJwt(user));
    }

    private async Task<(Guid userId, string sharedKey)> RegisterAndEnableTwoFactor(string email)
    {
        var (user, jwt) = await RegisterAndVerifyEmail(email);
        var authHeader = new AuthenticationHeaderValue("Bearer", jwt);

        var setupRequest = new HttpRequestMessage(HttpMethod.Get, "/Account/EnableTwoFactor") { Headers = { Authorization = authHeader } };
        var setupResponse = await Client.SendAsync(setupRequest);
        Assert.Equal(HttpStatusCode.OK, setupResponse.StatusCode);
        var sharedKey = (await setupResponse.Content.ReadFromJsonAsync<EnableTwoFactorResponse>())!.SharedKey;

        var enableRequest = new HttpRequestMessage(HttpMethod.Post, "/Account/EnableTwoFactor")
        {
            Headers = { Authorization = authHeader },
            Content = JsonContent.Create(new { Code = ComputeTotp(sharedKey) })
        };
        var enableResponse = await Client.SendAsync(enableRequest);
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);

        return (user.Id, sharedKey);
    }

    // Base's ActionFilter returns 400 with the ModelState serialized as { "<key>": ["<message>", ...] }.
    private static async Task AssertModelStateError(HttpResponseMessage response, string key, string message)
    {
        var json = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400 but got {(int)response.StatusCode}: {json}");
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty(key, out var errors), $"Expected ModelState key '{key}' in: {json}");
        Assert.Contains(message, errors.EnumerateArray().Select(e => e.GetString()));
    }

    #endregion

    [Fact]
    public async Task Register_Verify_Email_And_Phone()
    {
        // Arrange / Act — register and confirm email via the link in the verification email
        var email = $"test_{Guid.NewGuid()}@example.com";
        var (user, jwt) = await RegisterAndVerifyEmail(email);

        // Assert — email confirmed, phone stored
        Assert.True(user.EmailConfirmed);
        Assert.Equal("1234567890", user.PhoneNumber);

        // Act — request a phone verification code
        await DeleteMailHogMessages(PhoneInbox);
        var verifyPhoneRequest = new HttpRequestMessage(HttpMethod.Post, "/Account/VerifyPhone")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", jwt) }
        };
        var verifyPhoneResponse = await Client.SendAsync(verifyPhoneRequest);

        // Assert — SMS delivered with a code
        Assert.Equal(HttpStatusCode.OK, verifyPhoneResponse.StatusCode);
        var code = await WaitForSmsCode(@"Your verification code is: (\d+)");
        Assert.False(string.IsNullOrEmpty(code), "Did not receive phone verification SMS in MailHog");
    }

    [Fact]
    public async Task Enable_Microsoft_Authenticator()
    {
        // Arrange — register a fresh, verified account
        var email = $"test_{Guid.NewGuid()}@example.com";
        var (user, jwt) = await RegisterAndVerifyEmail(email);
        var authHeader = new AuthenticationHeaderValue("Bearer", jwt);

        // Act — fetch the TOTP setup info
        var setupRequest = new HttpRequestMessage(HttpMethod.Get, "/Account/EnableTwoFactor") { Headers = { Authorization = authHeader } };
        var setupResponse = await Client.SendAsync(setupRequest);

        // Assert — response contains a shared key and a valid otpauth URI
        Assert.Equal(HttpStatusCode.OK, setupResponse.StatusCode);
        using var setupDoc = JsonDocument.Parse(await setupResponse.Content.ReadAsStringAsync());
        var sharedKey = setupDoc.RootElement.GetProperty("sharedKey").GetString();
        var authenticatorUri = setupDoc.RootElement.GetProperty("authenticatorUri").GetString();
        Assert.False(string.IsNullOrWhiteSpace(sharedKey));
        Assert.StartsWith("otpauth://totp/Zuhid:", authenticatorUri);
        Assert.Contains(email, authenticatorUri);

        // Act — submit an invalid code
        var invalidRequest = new HttpRequestMessage(HttpMethod.Post, "/Account/EnableTwoFactor")
        {
            Headers = { Authorization = authHeader },
            Content = JsonContent.Create(new { Code = "000000" })
        };
        var invalidResponse = await Client.SendAsync(invalidRequest);

        // Assert — invalid code is rejected
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        // Act — submit a valid code computed from the shared key
        var enableRequest = new HttpRequestMessage(HttpMethod.Post, "/Account/EnableTwoFactor")
        {
            Headers = { Authorization = authHeader },
            Content = JsonContent.Create(new { Code = ComputeTotp(sharedKey!) })
        };
        var enableResponse = await Client.SendAsync(enableRequest);

        // Assert — 2FA is now enabled
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
        Assert.True((await GetUser(email)).TwoFactorEnabled);
        Assert.False(user.TwoFactorEnabled);
    }

    [Fact]
    public async Task Login_With_Two_Factor_Authenticator()
    {
        var (userId, sharedKey) = await RegisterAndEnableTwoFactor($"test_{Guid.NewGuid()}@example.com");

        // Reject invalid code
        var invalidResponse = await Client.PostAsJsonAsync("/Account/LoginTwoFactor", new { UserId = userId, Code = "000000", Provider = "Authenticator" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        // Accept valid TOTP code
        var validResponse = await Client.PostAsJsonAsync("/Account/LoginTwoFactor", new { UserId = userId, Code = ComputeTotp(sharedKey), Provider = "Authenticator" });
        Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);
        using var validDoc = JsonDocument.Parse(await validResponse.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(validDoc.RootElement.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Login_With_Two_Factor_Email()
    {
        var email = $"test_{Guid.NewGuid()}@example.com";
        var (userId, _) = await RegisterAndEnableTwoFactor(email);

        // Send 2FA token via email
        await DeleteMailHogMessages(email);
        var sendResponse = await Client.PostAsJsonAsync("/Account/SendTwoFactorToken", new { UserId = userId, Provider = "Email" });
        Assert.Equal(HttpStatusCode.OK, sendResponse.StatusCode);

        var code = await WaitForMatch(email, @"class=""code"">\s*(\d+)\s*<");
        Assert.False(string.IsNullOrEmpty(code), "Did not receive 2FA email code in MailHog");

        // Reject invalid code
        var invalidResponse = await Client.PostAsJsonAsync("/Account/LoginTwoFactor", new { UserId = userId, Code = "000000", Provider = "Email" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        // Accept valid code
        var validResponse = await Client.PostAsJsonAsync("/Account/LoginTwoFactor", new { UserId = userId, Code = code, Provider = "Email" });
        Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);
        using var validDoc = JsonDocument.Parse(await validResponse.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(validDoc.RootElement.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Login_With_Two_Factor_Phone()
    {
        var (userId, _) = await RegisterAndEnableTwoFactor($"test_{Guid.NewGuid()}@example.com");

        // Send 2FA token via phone (SMS is routed to the phone@test.com inbox)
        await DeleteMailHogMessages(PhoneInbox);
        var sendResponse = await Client.PostAsJsonAsync("/Account/SendTwoFactorToken", new { UserId = userId, Provider = "Phone" });
        Assert.Equal(HttpStatusCode.OK, sendResponse.StatusCode);

        var code = await WaitForSmsCode(@"Your two-factor authentication code is: (\d+)");
        Assert.False(string.IsNullOrEmpty(code), "Did not receive 2FA SMS code in MailHog");

        // Accept valid code
        var validResponse = await Client.PostAsJsonAsync("/Account/LoginTwoFactor", new { UserId = userId, Code = code, Provider = "Phone" });
        Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);
        using var validDoc = JsonDocument.Parse(await validResponse.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(validDoc.RootElement.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Register_Duplicate_Email_Returns_BadRequest()
    {
        // Arrange — register once successfully
        var email = $"test_{Guid.NewGuid()}@example.com";
        Assert.Equal(HttpStatusCode.OK, (await Register(email)).StatusCode);

        // Act — register again with the same email
        var second = await Register(email);

        // Assert — duplicate is rejected with validation errors
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Contains("Duplicate", await second.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task VerifyEmail_Invalid_Token_Returns_BadRequest()
    {
        // Arrange — register a fresh account
        var email = $"test_{Guid.NewGuid()}@example.com";
        await Register(email);

        // Act — verify with a bogus token
        var response = await Client.PostAsJsonAsync("/Account/VerifyEmail", new { Email = email, Token = "not-a-valid-token" });

        // Assert — rejected
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_Unknown_User_Returns_BadRequest()
    {
        // Act — verify email for a user that does not exist
        var response = await Client.PostAsJsonAsync("/Account/VerifyEmail",
            new { Email = $"nobody_{Guid.NewGuid()}@example.com", Token = "whatever" });

        // Assert — UserNotFound surfaces as a validation error
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("UserNotFound", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Confirm_Phone_With_Valid_And_Invalid_Token()
    {
        // Arrange — register and verify email
        var email = $"test_{Guid.NewGuid()}@example.com";
        var (_, jwt) = await RegisterAndVerifyEmail(email);
        var authHeader = new AuthenticationHeaderValue("Bearer", jwt);

        // Act — request a phone verification code
        await DeleteMailHogMessages(PhoneInbox);
        var verifyPhoneRequest = new HttpRequestMessage(HttpMethod.Post, "/Account/VerifyPhone") { Headers = { Authorization = authHeader } };
        var verifyPhoneResponse = await Client.SendAsync(verifyPhoneRequest);
        Assert.Equal(HttpStatusCode.OK, verifyPhoneResponse.StatusCode);

        var code = await WaitForSmsCode(@"Your verification code is: (\d+)");
        Assert.False(string.IsNullOrEmpty(code), "Did not receive phone verification code in MailHog");

        // Act — confirm with an invalid token first
        var invalidRequest = new HttpRequestMessage(HttpMethod.Post, "/Account/ConfirmPhone")
        {
            Headers = { Authorization = authHeader },
            Content = JsonContent.Create(new { Token = "000000" })
        };
        var invalidResponse = await Client.SendAsync(invalidRequest);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        // Act — confirm with the valid token
        var confirmRequest = new HttpRequestMessage(HttpMethod.Post, "/Account/ConfirmPhone")
        {
            Headers = { Authorization = authHeader },
            Content = JsonContent.Create(new { Token = code })
        };
        var confirmResponse = await Client.SendAsync(confirmRequest);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        Assert.True((await GetUser(email)).PhoneNumberConfirmed);
    }

    [Fact]
    public async Task Authenticated_Endpoints_Require_Token()
    {
        // Act — no bearer token
        var response = await Client.PostAsync("/Account/VerifyPhone", null);

        // Assert — challenged
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendTwoFactorToken_Nonexistent_User_Returns_BadRequest()
    {
        // Act — request a token for a user id that does not exist
        var response = await Client.PostAsJsonAsync("/Account/SendTwoFactorToken",
            new { UserId = Guid.NewGuid(), Provider = "Email" });

        // Assert — UserNotFound surfaces as a ModelState error
        await AssertModelStateError(response, "UserNotFound", "User not found");
    }

    [Theory]
    [InlineData("Foo")]
    [InlineData("Authenticator")]
    public async Task SendTwoFactorToken_Invalid_Provider_Returns_BadRequest(string provider)
    {
        // Arrange — a real user (no email verification or 2FA needed to reach the provider check)
        var email = $"test_{Guid.NewGuid()}@example.com";
        Assert.Equal(HttpStatusCode.OK, (await Register(email)).StatusCode);
        var user = await GetUser(email);

        // Act — ask for a token over a provider that cannot deliver one
        var response = await Client.PostAsJsonAsync("/Account/SendTwoFactorToken",
            new { UserId = user.Id, Provider = provider });

        // Assert — rejected cleanly under the Provider key, not a 500
        await AssertModelStateError(response, "Provider", "Provider must be Email or Phone");
    }

    [Fact]
    public async Task LoginTwoFactor_Nonexistent_User_Returns_BadRequest()
    {
        // Act — complete 2FA for a user id that does not exist
        var response = await Client.PostAsJsonAsync("/Account/LoginTwoFactor",
            new { UserId = Guid.NewGuid(), Code = "123456", Provider = "Authenticator" });

        // Assert — UserNotFound surfaces as a ModelState error
        await AssertModelStateError(response, "UserNotFound", "User not found");
    }

    [Fact]
    public async Task EnableTwoFactor_Get_Nonexistent_User_Returns_BadRequest()
    {
        // Arrange — a validly signed JWT whose subject is not in the database
        var jwt = await GenerateJwt(new UserEntity { Id = Guid.NewGuid(), Email = $"ghost_{Guid.NewGuid()}@example.com" });
        var request = new HttpRequestMessage(HttpMethod.Get, "/Account/EnableTwoFactor")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", jwt) }
        };

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        await AssertModelStateError(response, "UserNotFound", "User not found");
    }

    [Fact]
    public async Task EnableTwoFactor_Post_Nonexistent_User_Returns_BadRequest()
    {
        // Arrange — a validly signed JWT whose subject is not in the database
        var jwt = await GenerateJwt(new UserEntity { Id = Guid.NewGuid(), Email = $"ghost_{Guid.NewGuid()}@example.com" });
        var request = new HttpRequestMessage(HttpMethod.Post, "/Account/EnableTwoFactor")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", jwt) },
            Content = JsonContent.Create(new { Code = "123456" })
        };

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        await AssertModelStateError(response, "UserNotFound", "User not found");
    }

    [Fact]
    public async Task Login_Without_Two_Factor_Returns_Token_And_Omits_Null_Theme()
    {
        // Arrange — a verified account with 2FA off and no theme set
        var email = $"test_{Guid.NewGuid()}@example.com";
        await RegisterAndVerifyEmail(email);

        // Act
        var response = await Client.PostAsJsonAsync("/Account/Login", new { Email = email, Password });

        // Assert — token returned; null fields are left out of the body entirely
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("token").GetString()));
        Assert.False(doc.RootElement.TryGetProperty("theme", out _), $"Expected no theme property: {json}");
        Assert.False(doc.RootElement.TryGetProperty("requiresTwoFactor", out _));
        Assert.False(doc.RootElement.TryGetProperty("userId", out _));
        Assert.False(doc.RootElement.TryGetProperty("providers", out _));

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Null(body!.Theme);
    }

    [Fact]
    public async Task Login_With_Two_Factor_Enabled_Returns_Providers()
    {
        // Arrange — 2FA on, and the registered phone makes Phone a provider
        var email = $"test_{Guid.NewGuid()}@example.com";
        var (userId, _) = await RegisterAndEnableTwoFactor(email);

        // Act
        var response = await Client.PostAsJsonAsync("/Account/Login", new { Email = email, Password });

        // Assert — 2FA shape only, no token
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("token", out _), $"Expected no token property: {json}");
        Assert.False(doc.RootElement.TryGetProperty("theme", out _));

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.True(body!.RequiresTwoFactor);
        Assert.Equal(userId, body.UserId);
        Assert.Equal(["Authenticator", "Email", "Phone"], body.Providers!);
    }

    [Fact]
    public async Task Login_Wrong_Password_Returns_BadRequest()
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@example.com";
        await RegisterAndVerifyEmail(email);

        // Act
        var response = await Client.PostAsJsonAsync("/Account/Login", new { Email = email, Password = "WrongPassword1!" });

        // Assert
        await AssertModelStateError(response, "Login", "Invalid login attempt");
    }

    [Fact]
    public async Task Login_Unknown_Email_Returns_BadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/Account/Login",
            new { Email = $"nobody_{Guid.NewGuid()}@example.com", Password });

        // Assert
        await AssertModelStateError(response, "Login", "Invalid login attempt");
    }

    [Fact]
    public async Task LoginTwoFactor_Invalid_Provider_Returns_BadRequest()
    {
        // Arrange — a real user with 2FA enabled
        var (userId, _) = await RegisterAndEnableTwoFactor($"test_{Guid.NewGuid()}@example.com");

        // Act — submit an unrecognised provider (hits the switch `_ => false` branch)
        var response = await Client.PostAsJsonAsync("/Account/LoginTwoFactor",
            new { UserId = userId, Code = "123456", Provider = "CarrierPigeon" });

        // Assert — treated as an invalid code
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpResponseMessage> UpdateTheme(string jwt, string theme) =>
        await Client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/Account/UpdateTheme")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", jwt) },
            Content = JsonContent.Create(new { Theme = theme })
        });

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public async Task UpdateTheme_Valid_Theme_Persists_And_Is_Returned_On_Login(string theme)
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@example.com";
        var (_, jwt) = await RegisterAndVerifyEmail(email);

        // Act
        var response = await UpdateTheme(jwt, theme);

        // Assert — stored on the user and echoed back with the next login token
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(theme, (await GetUser(email)).Theme);

        var login = await Client.PostAsJsonAsync("/Account/Login", new { Email = email, Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal(theme, body.Theme);
    }

    [Theory]
    [InlineData("material")] // removed from ValidThemes
    [InlineData("solarized")]
    public async Task UpdateTheme_Invalid_Theme_Returns_BadRequest_And_Does_Not_Persist(string theme)
    {
        // Arrange — a user who already has a valid theme
        var email = $"test_{Guid.NewGuid()}@example.com";
        var (_, jwt) = await RegisterAndVerifyEmail(email);
        Assert.Equal(HttpStatusCode.OK, (await UpdateTheme(jwt, "dark")).StatusCode);

        // Act
        var response = await UpdateTheme(jwt, theme);

        // Assert — rejected with a Theme error and the existing theme is untouched
        await AssertModelStateError(response, "Theme", "Invalid theme. Must be one of: light, dark.");
        Assert.Equal("dark", (await GetUser(email)).Theme);
    }

    [Fact]
    public async Task UpdateTheme_Unauthenticated_Returns_Unauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/Account/UpdateTheme", new { Theme = "light" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTheme_Nonexistent_User_Returns_BadRequest()
    {
        // Arrange — a validly signed JWT whose subject is not in the database
        var jwt = await GenerateJwt(new UserEntity { Id = Guid.NewGuid(), Email = $"ghost_{Guid.NewGuid()}@example.com" });

        // Act
        var response = await UpdateTheme(jwt, "light");

        // Assert
        await AssertModelStateError(response, "UserNotFound", "User not found");
    }

    // RFC 6238 TOTP — mirrors what ASP.NET Core Identity uses in AuthenticatorTokenProvider
    private static string ComputeTotp(string base32Key)
    {
        var key = Base32Decode(base32Key.ToUpperInvariant().Replace(" ", ""));
        var timestep = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        var timestepBytes = BitConverter.GetBytes(timestep);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timestepBytes);
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(timestepBytes);
        var offset = hash[^1] & 0x0F;
        var code = ((hash[offset] & 0x7F) << 24)
                 | ((hash[offset + 1] & 0xFF) << 16)
                 | ((hash[offset + 2] & 0xFF) << 8)
                 | (hash[offset + 3] & 0xFF);
        return (code % 1_000_000).ToString("D6");
    }

    private static byte[] Base32Decode(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = 0;
        var value = 0;
        var result = new List<byte>();
        foreach (var c in base32)
        {
            var idx = alphabet.IndexOf(c);
            if (idx < 0)
            {
                continue;
            }

            value = (value << 5) | idx;
            bits += 5;
            if (bits >= 8)
            {
                result.Add((byte)(value >> (bits - 8)));
                bits -= 8;
            }
        }
        return [.. result];
    }
}
