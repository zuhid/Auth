using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Zuhid.Auth.Entities;
using Zuhid.Base;

namespace Zuhid.Auth;

public class AuthContext(DbContextOptions<AuthContext> options) : IdentityDbContext<UserEntity, RoleEntity, Guid,
    UserClaimEntity, UserRoleEntity, UserLoginEntity, RoleClaimEntity, UserTokenEntity>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        const string schema = "auth";
        builder.ToSnakeCase();
        builder.Entity<UserEntity>(entity => entity.ToTable("users", schema));
        builder.Entity<RoleEntity>(entity => entity.ToTable("role", schema));
        builder.Entity<UserRoleEntity>(entity => entity.ToTable("user_role", schema));
        builder.Entity<UserClaimEntity>(entity => entity.ToTable("user_claim", schema));
        builder.Entity<UserLoginEntity>(entity => entity.ToTable("user_login", schema));
        builder.Entity<RoleClaimEntity>(entity => entity.ToTable("role_claim", schema));
        builder.Entity<UserTokenEntity>(entity => entity.ToTable("user_token", schema));
        builder.LoadCsvData();
    }
}

