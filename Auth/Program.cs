using Microsoft.EntityFrameworkCore;

namespace Zuhid.Auth;

public partial class Program
{
  private static void Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    builder.Services.AddControllers();

    builder.Services.AddDbContext<AuthContext>(options => options
        .UseNpgsql("Server=localhost;Database=auth;User Id=dbuser;Password=P@ssw0rd;TrustServerCertificate=True;")
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking) // setting to no tracking to improve performance
    );

    var app = builder.Build();

    // Configure the HTTP request pipeline.

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
  }
}
