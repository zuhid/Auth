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
    NotificationApiClient notificationApiClient, AppSetting appSetting) : ControllerBase
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
            // var mailMessage = await accountComposer.VerifyEmailCompose(appSetting.AppUrl, user.Email!, user.FullName, token);
            // await emailService.SendEmailAsync(mailMessage);
            Console.WriteLine($"token = {token}");
        }
        else
        {
            errors?.ForEach(e => ModelState.AddModelError(e.Key, e.Value));
        }
    }
}

