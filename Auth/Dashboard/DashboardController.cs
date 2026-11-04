using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Zuhid.Auth.Dashboard;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardController(DashboardRepository dashboardRepository) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<StatsModel> GetStats()
    {
        return await dashboardRepository.GetStats();
    }
}
