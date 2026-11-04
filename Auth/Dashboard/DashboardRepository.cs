using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuhid.Auth.Entities;

namespace Zuhid.Auth.Dashboard;

public class DashboardRepository(UserManager<UserEntity> userManager)
{
    public async Task<StatsModel> GetStats()
    {
        var now = DateTimeOffset.UtcNow;
        var total = await userManager.Users.CountAsync();
        var active = await userManager.Users.CountAsync(u => u.LockoutEnd == null || u.LockoutEnd < now);
        return new StatsModel { Active = active, Inactive = total - active };
    }
}
