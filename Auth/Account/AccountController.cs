using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zuhid.Auth.Entities;
using Zuhid.Auth.Notification;

namespace Zuhid.Auth.Account;

[ApiController]
[Route("[controller]")]
public class AccountController(AccountRepository accountRepository, AccountMapper accountMapper,
    NotificationApiClient notificationApiClient, LoginValidator loginValidator,
    JwtService jwtService, AppSetting appSetting) : ControllerBase
{
    private static readonly HashSet<string> ValidThemes = ["light", "dark"];

    [HttpGet()]
    public async Task<UserEntity?> Get()
    {
        return await accountRepository.GetById(UserId);
    }

    [HttpPost("Register")]
    [AllowAnonymous]
    public async Task Register([FromBody] RegisterRequest request)
    {
        var (user, errors) = await accountRepository.Add(accountMapper.Map(request), request.Password);
        if (user != null)
        {
            var token = await accountRepository.GenerateEmailConfirmationTokenAsync(user);
            await notificationApiClient.VerifyEmail(new VerifyEmailMessage
            {
                Email = user.Email!,
                AppUrl = appSetting.AppUrl,
                Token = token
            });
        }
        else
        {
            errors?.ForEach(e => ModelState.AddModelError(e.Key, e.Value));
        }
    }

    [HttpPost("VerifyEmail")]
    [AllowAnonymous]
    public async Task VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var errors = await accountRepository.ConfirmEmailAsync(request.Email, request.Token);
        errors?.ForEach(e => ModelState.AddModelError(e.Key, e.Value));
    }

    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<LoginResponse?> Login([FromBody] LoginRequest request)
    {
        var (result, user) = await accountRepository.Login(request.Email, request.Password);
        if (result.RequiresTwoFactor)
        {
            var providers = new List<string> { "Authenticator", "Email" };
            if (!string.IsNullOrEmpty(user!.PhoneNumber))
            {
                providers.Add("Phone");
            }
            return new LoginResponse { RequiresTwoFactor = true, UserId = user.Id, Providers = providers };
        }

        loginValidator.Validate(result, ModelState);
        if (!ModelState.IsValid)
        {
            return null;
        }

        var token = await jwtService.GenerateToken(user!);
        return new LoginResponse { Token = token, Theme = user!.Theme };
    }

    [HttpPost("VerifyPhone")]
    public async Task VerifyPhone()
    {
        var (user, token) = await accountRepository.GenerateChangePhoneNumberTokenAsync(UserId);
        if (user != null)
        {
            await notificationApiClient.VerifyPhone(new VerifyPhoneMessage
            {
                Phone = user.PhoneNumber!,
                Token = token
            });
        }
    }

    [HttpPost("ConfirmPhone")]
    public async Task ConfirmPhone([FromBody] ConfirmPhoneRequest request)
    {
        var errors = await accountRepository.ConfirmPhoneAsync(UserId, request.Token);
        errors?.ForEach(e => ModelState.AddModelError(e.Key, e.Value));
    }

    [HttpGet("EnableTwoFactor")]
    public async Task<EnableTwoFactorResponse?> EnableTwoFactor()
    {
        var user = await accountRepository.GetById(UserId);
        if (user == null)
        {
            AddUserNotFound();
            return null;
        }

        var key = await accountRepository.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await accountRepository.ResetAuthenticatorKeyAsync(user);
            key = await accountRepository.GetAuthenticatorKeyAsync(user);
        }

        var uri = $"otpauth://totp/Zuhid:{user.Email}?secret={key}&issuer=Zuhid&digits=6";
        return new EnableTwoFactorResponse { SharedKey = key!, AuthenticatorUri = uri };
    }

    [HttpPost("EnableTwoFactor")]
    public async Task EnableTwoFactor([FromBody] EnableTwoFactorRequest request)
    {
        var user = await accountRepository.GetById(UserId);
        if (user == null)
        {
            AddUserNotFound();
            return;
        }

        var isValid = await accountRepository.VerifyTwoFactorTokenAsync(user, request.Code);
        if (!isValid)
        {
            ModelState.AddModelError("Code", "Verification code is invalid.");
            return;
        }

        await accountRepository.SetTwoFactorEnabledAsync(user, true);
    }

    [HttpPost("SendTwoFactorToken")]
    [AllowAnonymous]
    public async Task SendTwoFactorToken([FromBody] SendTwoFactorTokenRequest request)
    {
        var user = await accountRepository.GetById(request.UserId);
        if (user == null)
        {
            AddUserNotFound();
            return;
        }

        // Validate before generating: UserManager throws for an unknown provider name.
        if (request.Provider != "Email" && request.Provider != "Phone")
        {
            ModelState.AddModelError("Provider", "Provider must be Email or Phone");
            return;
        }

        var code = await accountRepository.GenerateTwoFactorTokenAsync(user, request.Provider);

        if (request.Provider == "Email")
        {
            await notificationApiClient.SendTwoFactorEmail(new TwoFactorEmailMessage
            {
                Email = user.Email!,
                Token = code
            });
        }
        else
        {
            await notificationApiClient.SendTwoFactorSms(new TwoFactorSmsMessage
            {
                Phone = user.PhoneNumber!,
                Token = code
            });
        }
    }

    [HttpPost("LoginTwoFactor")]
    [AllowAnonymous]
    public async Task<LoginResponse?> LoginTwoFactor([FromBody] TwoFactorLoginRequest request)
    {
        var user = await accountRepository.GetById(request.UserId);
        if (user == null)
        {
            AddUserNotFound();
            return null;
        }

        var isValid = request.Provider switch
        {
            "Authenticator" => await accountRepository.VerifyTwoFactorTokenAsync(user, request.Code),
            "Email" or "Phone" => await accountRepository.VerifyTwoFactorTokenAsync(user, request.Provider, request.Code),
            _ => false
        };

        if (!isValid)
        {
            ModelState.AddModelError("Code", "Verification code is invalid.");
            return null;
        }

        var token = await jwtService.GenerateToken(user);
        return new LoginResponse { Token = token, Theme = user.Theme };
    }

    [HttpPost("UpdateTheme")]
    public async Task UpdateTheme([FromBody] UpdateThemeRequest request)
    {
        if (!ValidThemes.Contains(request.Theme))
        {
            ModelState.AddModelError("Theme", "Invalid theme. Must be one of: light, dark.");
            return;
        }
        var errors = await accountRepository.UpdateTheme(UserId, request.Theme);
        errors?.ForEach(e => ModelState.AddModelError(e.Key, e.Value));
    }

    private void AddUserNotFound() => ModelState.AddModelError("UserNotFound", "User not found");

    private Guid UserId
    {
        get
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userId!);
        }
    }
}

