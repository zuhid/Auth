using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Zuhid.Auth.Notification;

namespace Zuhid.Auth.Account;

[ApiController]
[Route("[controller]")]
public class AccountController(AccountRepository accountRepository, AccountMapper accountMapper,
    NotificationApiClient notificationApiClient, JwtService jwtService, AppSetting appSetting) : ControllerBase
{
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
  public async Task<IActionResult> EnableTwoFactor()
  {
    var user = await accountRepository.GetById(UserId);
    if (user == null) return NotFound();

    var key = await accountRepository.GetAuthenticatorKeyAsync(user);
    if (string.IsNullOrEmpty(key))
    {
      await accountRepository.ResetAuthenticatorKeyAsync(user);
      key = await accountRepository.GetAuthenticatorKeyAsync(user);
    }

    var uri = $"otpauth://totp/Zuhid:{user.Email}?secret={key}&issuer=Zuhid&digits=6";
    return Ok(new { SharedKey = key, AuthenticatorUri = uri });
  }

  [HttpPost("EnableTwoFactor")]
  public async Task<IActionResult> EnableTwoFactor([FromBody] EnableTwoFactorRequest request)
  {
    var user = await accountRepository.GetById(UserId);
    if (user == null) return NotFound();

    var isValid = await accountRepository.VerifyTwoFactorTokenAsync(user, request.Code);
    if (!isValid)
    {
      ModelState.AddModelError("Code", "Verification code is invalid.");
      return BadRequest(ModelState);
    }

    await accountRepository.SetTwoFactorEnabledAsync(user, true);
    return Ok();
  }

  [HttpPost("SendTwoFactorToken")]
  [AllowAnonymous]
  public async Task<IActionResult> SendTwoFactorToken([FromBody] SendTwoFactorTokenRequest request)
  {
    var user = await accountRepository.GetById(request.UserId);
    if (user == null) return NotFound();

    var code = await accountRepository.GenerateTwoFactorTokenAsync(user, request.Provider);

    if (request.Provider == "Email")
    {
      await notificationApiClient.SendTwoFactorEmail(new TwoFactorEmailMessage
      {
        Email = user.Email!,
        Token = code
      });
    }
    else if (request.Provider == "Phone")
    {
      await notificationApiClient.SendTwoFactorSms(new TwoFactorSmsMessage
      {
        Phone = user.PhoneNumber!,
        Token = code
      });
    }
    else
    {
      return BadRequest(new { error = "Provider must be Email or Phone" });
    }

    return Ok();
  }

  [HttpPost("LoginTwoFactor")]
  [AllowAnonymous]
  public async Task<IActionResult> LoginTwoFactor([FromBody] TwoFactorLoginRequest request)
  {
    var user = await accountRepository.GetById(request.UserId);
    if (user == null) return NotFound();

    var isValid = request.Provider switch
    {
      "Authenticator" => await accountRepository.VerifyTwoFactorTokenAsync(user, request.Code),
      "Email" or "Phone" => await accountRepository.VerifyTwoFactorTokenAsync(user, request.Provider, request.Code),
      _ => false
    };

    if (!isValid)
    {
      ModelState.AddModelError("Code", "Verification code is invalid.");
      return BadRequest(ModelState);
    }

    var token = await jwtService.GenerateToken(user);
    return Ok(new { Token = token, Theme = user.Theme });
  }

  private Guid UserId
  {
    get
    {
      var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
      return Guid.Parse(userId!);
    }
  }
}

