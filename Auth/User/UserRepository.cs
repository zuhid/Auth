using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuhid.Auth.Entities;

namespace Zuhid.Auth.User;

public class UserRepository(UserManager<UserEntity> userManager)
{
    public async Task<List<UserModel>> Get()
    {
        return await userManager.Users.Select(u => new UserModel
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email ?? "",
            PhoneNumber = u.PhoneNumber,
            IsActive = u.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.UtcNow
        })
        .ToListAsync();
    }

    public async Task<(UserEntity?, List<KeyValuePair<string, string>>?)> Add(UserEntity user)
    {
        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            return (null, result.Errors.Select(e => new KeyValuePair<string, string>(e.Code, e.Description)).ToList());
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await userManager.ConfirmEmailAsync(user, token);
        return (user, null);
    }

    public async Task<List<KeyValuePair<string, string>>?> Update(UserEntity entity)
    {
        var user = await userManager.FindByIdAsync(entity.Id.ToString());
        if (user == null)
        {
            return [new KeyValuePair<string, string>("UserNotFound", "User not found")];
        }

        user.FirstName = entity.FirstName;
        user.LastName = entity.LastName;
        user.PhoneNumber = entity.PhoneNumber;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded
            ? null
            : [.. result.Errors.Select(e => new KeyValuePair<string, string>(e.Code, e.Description))];
    }

    public async Task<List<KeyValuePair<string, string>>?> SetActive(Guid userId, bool isActive)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return [new KeyValuePair<string, string>("UserNotFound", "User not found")];
        }

        var lockoutEnd = isActive ? (DateTimeOffset?)null : DateTimeOffset.UtcNow.AddYears(100);
        var result = await userManager.SetLockoutEndDateAsync(user, lockoutEnd);
        return result.Succeeded
            ? null
            : [.. result.Errors.Select(e => new KeyValuePair<string, string>(e.Code, e.Description))];
    }
}

