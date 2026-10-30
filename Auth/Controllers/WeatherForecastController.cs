using Microsoft.AspNetCore.Mvc;

namespace Auth.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    [HttpGet]
    public string Get()
    {
        return "ssss";
    }
}
