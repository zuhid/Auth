using System.Reflection;
using Microsoft.AspNetCore.Identity;
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
        ]);
        builder.AddPostgres<AuthContext>(appSetting.ConnectionStrings.Auth);

        var app = builder.BuildServices(appSetting);
        app.Run();
    }
}
