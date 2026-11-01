using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Zuhid.Auth.Entities;

namespace Zuhid.Auth.Account;

public class AccountRepository(UserManager<User> userManager)
{
    public async Task<string> GenerateEmailConfirmationTokenAsync(User user)
    {
        return await userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<(User? User, List<KeyValuePair<string, string>>? errors)> Add(User user, string password)
    {
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new KeyValuePair<string, string>(e.Code, e.Description)).ToList();
            return (null, errors);
        }
        var foundUser = await userManager.FindByEmailAsync(user.Email!);
        return (foundUser, null);
    }
}

