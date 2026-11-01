using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Zuhid.Auth.Account;
using Zuhid.Auth.Entities;
using Zuhid.Base;

namespace Zuhid.Auth;

public partial class Program
{
  private static void Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);
    var appSetting = new AppSetting(builder.Configuration);
    builder.Services
        .AddIdentity<User, Role>()
        .AddEntityFrameworkStores<AuthContext>()
        .AddDefaultTokenProviders();
    builder.AddServices(appSetting);
    builder.AddScopedByConvention(Assembly.GetAssembly(typeof(AuthContext))!, [
        "Mapper",
            "Repository",
            "ApiClient",
        ]);
    builder.Services.AddScoped<JwtService>();
    builder.AddPostgres<AuthContext>(appSetting.ConnectionStrings.Auth);

    var app = builder.BuildServices(appSetting);
    app.Run();
  }
}
