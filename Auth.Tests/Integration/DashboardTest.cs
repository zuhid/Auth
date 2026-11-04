using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Zuhid.Auth.Account;
using Zuhid.Auth.Dashboard;
using Zuhid.Auth.Entities;
using Zuhid.Auth.User;

namespace Zuhid.Auth.Tests.Integration;

// The stats are global counts over auth.users, so any test that creates or (de)activates users
// concurrently would skew the deltas asserted here. This collection disables parallelization, which makes
// xUnit run these tests on their own after all parallel collections (e.g. AccountTest) have finished.
[CollectionDefinition(Name, DisableParallelization = true)]
public class DashboardCollection
{
    public const string Name = "Dashboard (non-parallel)";
}

[Collection(DashboardCollection.Name)]
public class DashboardTest(WebApplicationFactory<Program> factory) : BaseIntegrationTest(factory)
{
    private const string AdminEmail = "admin@example.com";
    private const string StatsUrl = "/Dashboard/stats";

    #region Helpers

    private async Task<UserEntity?> FindUser(string email)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();
        return await userManager.FindByEmailAsync(email);
    }

    // Mint a JWT the same way the API does after login, without going through /Account/Login.
    private async Task<string> GenerateJwt(UserEntity user)
    {
        using var scope = Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<JwtService>();
        return await jwtService.GenerateToken(user);
    }

    private async Task<AuthenticationHeaderValue> AdminAuth()
    {
        var admin = await FindUser(AdminEmail);
        Assert.NotNull(admin);
        return new AuthenticationHeaderValue("Bearer", await GenerateJwt(admin!));
    }

    // A user with no roles, created directly through UserManager (no mail/notification dependency).
    private async Task<AuthenticationHeaderValue> NonAdminAuth()
    {
        var email = $"test_{Guid.NewGuid()}@example.com";
        using (var scope = Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();
            var result = await userManager.CreateAsync(new UserEntity { UserName = email, Email = email, FirstName = "Test", LastName = "User" });
            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        }
        var user = await FindUser(email);
        return new AuthenticationHeaderValue("Bearer", await GenerateJwt(user!));
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string url, AuthenticationHeaderValue? auth, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (auth != null)
        {
            request.Headers.Authorization = auth;
        }
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }
        return await Client.SendAsync(request);
    }

    private async Task<StatsModel> GetStats(AuthenticationHeaderValue auth)
    {
        var response = await Send(HttpMethod.Get, StatsUrl, auth);
        var json = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 but got {(int)response.StatusCode}: {json}");
        return (await response.Content.ReadFromJsonAsync<StatsModel>())!;
    }

    #endregion

    [Fact]
    public async Task GetStats_As_Admin_Returns_Ok_With_NonNegative_Counts()
    {
        // Act
        var response = await Send(HttpMethod.Get, StatsUrl, await AdminAuth());

        // Assert — 200 with camelCase { active, inactive }
        var json = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 but got {(int)response.StatusCode}: {json}");
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("active", out var active), $"Missing 'active' in: {json}");
        Assert.True(doc.RootElement.TryGetProperty("inactive", out var inactive), $"Missing 'inactive' in: {json}");
        Assert.True(active.GetInt32() >= 1, "The seeded admin itself is active, so active must be at least 1");
        Assert.True(inactive.GetInt32() >= 0);
    }

    [Fact]
    public async Task GetStats_Without_Token_Returns_Unauthorized()
    {
        // Act — no bearer token
        var response = await Send(HttpMethod.Get, StatsUrl, null);

        // Assert — challenged
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStats_As_NonAdmin_Returns_Forbidden()
    {
        // Act — authenticated, but without the Admin role
        var response = await Send(HttpMethod.Get, StatsUrl, await NonAdminAuth());

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_Stats_Route_Is_Removed()
    {
        // Act — the stats endpoint moved from /User/stats to /Dashboard/stats
        var response = await Send(HttpMethod.Get, "/User/stats", await AdminAuth());

        // Assert — no GET endpoint matches. ASP.NET Core answers 405 rather than 404 here because the
        // path shape also matches PUT /User/{id:guid}, and the HTTP-method policy runs before the guid
        // route constraint is evaluated. Either status proves the old GET endpoint is gone.
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
    }

    [Fact]
    public async Task GetStats_Reflects_Add_Deactivate_And_Activate()
    {
        // Arrange
        var auth = await AdminAuth();
        var email = $"test_{Guid.NewGuid()}@example.com";
        var before = await GetStats(auth);

        // Act — add a user via the User API
        var addResponse = await Send(HttpMethod.Post, "/User", auth, new UserModel { Email = email, FirstName = "Test", LastName = "User" });
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        var afterAdd = await GetStats(auth);

        // Assert — new users are active
        Assert.Equal(before.Active + 1, afterAdd.Active);
        Assert.Equal(before.Inactive, afterAdd.Inactive);

        // Act — deactivate
        var user = await FindUser(email);
        Assert.NotNull(user);
        var deactivateResponse = await Send(HttpMethod.Post, $"/User/{user!.Id}/Deactivate", auth);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        var afterDeactivate = await GetStats(auth);

        // Assert — moves from active to inactive
        Assert.Equal(afterAdd.Active - 1, afterDeactivate.Active);
        Assert.Equal(afterAdd.Inactive + 1, afterDeactivate.Inactive);

        // Act — reactivate
        var activateResponse = await Send(HttpMethod.Post, $"/User/{user.Id}/Activate", auth);
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);
        var afterActivate = await GetStats(auth);

        // Assert — back to active
        Assert.Equal(afterDeactivate.Active + 1, afterActivate.Active);
        Assert.Equal(afterDeactivate.Inactive - 1, afterActivate.Inactive);
    }
}
