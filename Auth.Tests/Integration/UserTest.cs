using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Zuhid.Auth.Account;
using Zuhid.Auth.Entities;
using Zuhid.Auth.User;

namespace Zuhid.Auth.Tests.Integration;

// Admin-only User API: GET /User (UserRepository.Get) and PUT /User/{id:guid} (UserRepository.Update).
public class UserTest(WebApplicationFactory<Program> factory) : BaseIntegrationTest(factory)
{
    private const string AdminEmail = "admin@example.com";

    #region Helpers

    private static string NewEmail() => $"test_{Guid.NewGuid()}@example.com";

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
        var email = NewEmail();
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

    // Create a user through POST /User and return its id.
    private async Task<Guid> AddUser(AuthenticationHeaderValue auth, UserModel model)
    {
        var response = await Send(HttpMethod.Post, "/User", auth, model);
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 from POST /User but got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        var user = await FindUser(model.Email);
        Assert.NotNull(user);
        return user!.Id;
    }

    private async Task<List<UserModel>> GetUsers(AuthenticationHeaderValue auth)
    {
        var response = await Send(HttpMethod.Get, "/User", auth);
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 from GET /User but got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<List<UserModel>>())!;
    }

    private async Task<UserModel> GetListedUser(AuthenticationHeaderValue auth, Guid id)
    {
        var listed = (await GetUsers(auth)).SingleOrDefault(u => u.Id == id);
        Assert.NotNull(listed);
        return listed!;
    }

    // Base's ActionFilter returns 400 with the ModelState serialized as { "<key>": ["<message>", ...] }.
    private static async Task AssertModelStateError(HttpResponseMessage response, string key, string message)
    {
        var json = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400 but got {(int)response.StatusCode}: {json}");
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty(key, out var errors), $"Expected ModelState key '{key}' in: {json}");
        Assert.Contains(message, errors.EnumerateArray().Select(e => e.GetString()));
    }

    // A bad body is rejected by model validation before the action runs. Depending on which filter wins,
    // the errors are either at the root (Base's ActionFilter) or under "errors" (ApiController ProblemDetails).
    private static async Task AssertValidationError(HttpResponseMessage response, string key)
    {
        var json = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400 but got {(int)response.StatusCode}: {json}");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var container = root.TryGetProperty("errors", out var nested) ? nested : root;
        Assert.True(container.EnumerateObject().Any(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase)),
            $"Expected validation error for '{key}' in: {json}");
    }

    #endregion

    #region GET /User

    [Fact]
    public async Task Get_As_Admin_Returns_New_User_With_Correct_Fields()
    {
        // Arrange
        var auth = await AdminAuth();
        var email = NewEmail();
        var id = await AddUser(auth, new UserModel { Email = email, FirstName = "Ada", LastName = "Lovelace", PhoneNumber = "5551234567" });

        // Act
        var users = await GetUsers(auth);

        // Assert
        var listed = users.SingleOrDefault(u => u.Id == id);
        Assert.NotNull(listed);
        Assert.Equal(email, listed!.Email);
        Assert.Equal("Ada", listed.FirstName);
        Assert.Equal("Lovelace", listed.LastName);
        Assert.Equal("5551234567", listed.PhoneNumber);
        Assert.True(listed.IsActive);
        Assert.Contains(users, u => u.Email == AdminEmail);
    }

    [Fact]
    public async Task Get_Returns_Json_In_CamelCase()
    {
        // Act
        var response = await Send(HttpMethod.Get, "/User", await AdminAuth());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        var first = doc.RootElement.EnumerateArray().First();
        foreach (var name in new[] { "id", "email", "firstName", "lastName", "phoneNumber", "isActive" })
        {
            Assert.True(first.TryGetProperty(name, out _), $"Missing '{name}' in: {first}");
        }
    }

    [Fact]
    public async Task Get_Reflects_Deactivate_And_Activate()
    {
        // Arrange
        var auth = await AdminAuth();
        var id = await AddUser(auth, new UserModel { Email = NewEmail(), FirstName = "Test", LastName = "User" });

        // Act / Assert — deactivated
        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Post, $"/User/{id}/Deactivate", auth)).StatusCode);
        Assert.False((await GetListedUser(auth, id)).IsActive);

        // Act / Assert — reactivated
        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Post, $"/User/{id}/Activate", auth)).StatusCode);
        Assert.True((await GetListedUser(auth, id)).IsActive);
    }

    [Fact]
    public async Task Get_Without_Token_Returns_Unauthorized()
    {
        var response = await Send(HttpMethod.Get, "/User", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_As_NonAdmin_Returns_Forbidden()
    {
        var response = await Send(HttpMethod.Get, "/User", await NonAdminAuth());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region PUT /User/{id}

    [Fact]
    public async Task Put_Updates_Fields_Visible_Via_Get()
    {
        // Arrange
        var auth = await AdminAuth();
        var email = NewEmail();
        var id = await AddUser(auth, new UserModel { Email = email, FirstName = "Before", LastName = "Name", PhoneNumber = "1111111111" });

        // Act
        var response = await Send(HttpMethod.Put, $"/User/{id}", auth,
            new UserModel { Email = email, FirstName = "After", LastName = "Changed", PhoneNumber = "2222222222" });

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 but got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        var listed = await GetListedUser(auth, id);
        Assert.Equal("After", listed.FirstName);
        Assert.Equal("Changed", listed.LastName);
        Assert.Equal("2222222222", listed.PhoneNumber);
        Assert.Equal(email, listed.Email);
        Assert.True(listed.IsActive);
    }

    [Fact]
    public async Task Put_Can_Clear_Optional_Fields()
    {
        // Arrange
        var auth = await AdminAuth();
        var email = NewEmail();
        var id = await AddUser(auth, new UserModel { Email = email, FirstName = "First", LastName = "Last", PhoneNumber = "3333333333" });

        // Act — null optional fields overwrite the stored values
        var response = await Send(HttpMethod.Put, $"/User/{id}", auth, new UserModel { Email = email });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var listed = await GetListedUser(auth, id);
        Assert.Null(listed.FirstName);
        Assert.Null(listed.LastName);
        Assert.Null(listed.PhoneNumber);
    }

    [Fact]
    public async Task Put_Does_Not_Change_Active_State()
    {
        // Arrange — a deactivated user
        var auth = await AdminAuth();
        var email = NewEmail();
        var id = await AddUser(auth, new UserModel { Email = email, FirstName = "Test", LastName = "User" });
        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Post, $"/User/{id}/Deactivate", auth)).StatusCode);

        // Act — IsActive in the body is ignored by Update; only Activate/Deactivate change it
        var response = await Send(HttpMethod.Put, $"/User/{id}", auth, new UserModel { Email = email, FirstName = "Renamed", IsActive = true });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var listed = await GetListedUser(auth, id);
        Assert.Equal("Renamed", listed.FirstName);
        Assert.False(listed.IsActive);
    }

    // Current contract: UserRepository.Update only copies FirstName/LastName/PhoneNumber, so the Email in
    // the body is ignored (even though it is [Required]). Changing to another user's email is therefore
    // neither applied nor rejected. See the report: this is a suspected bug / unclear contract.
    [Fact]
    public async Task Put_Ignores_Email_Change_Including_Duplicate_Of_Another_User()
    {
        // Arrange
        var auth = await AdminAuth();
        var email = NewEmail();
        var id = await AddUser(auth, new UserModel { Email = email, FirstName = "Test", LastName = "User" });

        // Act — try to take the admin's email
        var response = await Send(HttpMethod.Put, $"/User/{id}", auth, new UserModel { Email = AdminEmail, FirstName = "Test", LastName = "User" });

        // Assert — accepted, but the stored email is unchanged
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(email, (await GetListedUser(auth, id)).Email);
        Assert.Equal(email, (await FindUser(email))!.UserName);
    }

    [Fact]
    public async Task Put_Nonexistent_User_Returns_BadRequest()
    {
        // Act
        var response = await Send(HttpMethod.Put, $"/User/{Guid.NewGuid()}", await AdminAuth(),
            new UserModel { Email = NewEmail(), FirstName = "Ghost" });

        // Assert
        await AssertModelStateError(response, "UserNotFound", "User not found");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public async Task Put_Invalid_Email_Returns_BadRequest(string email)
    {
        // Arrange
        var auth = await AdminAuth();
        var original = NewEmail();
        var id = await AddUser(auth, new UserModel { Email = original, FirstName = "Keep", LastName = "Me" });

        // Act
        var response = await Send(HttpMethod.Put, $"/User/{id}", auth, new UserModel { Email = email, FirstName = "Changed" });

        // Assert — rejected by model validation and nothing persisted
        await AssertValidationError(response, "Email");
        Assert.Equal("Keep", (await GetListedUser(auth, id)).FirstName);
    }

    [Fact]
    public async Task Put_Non_Guid_Id_Returns_NotFound_Or_MethodNotAllowed()
    {
        // The {id:guid} constraint means no PUT endpoint matches; ASP.NET Core answers 405 because the
        // HTTP-method policy is evaluated before the route constraint.
        var response = await Send(HttpMethod.Put, "/User/not-a-guid", await AdminAuth(), new UserModel { Email = NewEmail() });
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
    }

    [Fact]
    public async Task Put_Without_Token_Returns_Unauthorized()
    {
        var response = await Send(HttpMethod.Put, $"/User/{Guid.NewGuid()}", null, new UserModel { Email = NewEmail() });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Put_As_NonAdmin_Returns_Forbidden()
    {
        // Arrange
        var adminAuth = await AdminAuth();
        var email = NewEmail();
        var id = await AddUser(adminAuth, new UserModel { Email = email, FirstName = "Original" });

        // Act
        var response = await Send(HttpMethod.Put, $"/User/{id}", await NonAdminAuth(), new UserModel { Email = email, FirstName = "Hacked" });

        // Assert — forbidden and unchanged
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Original", (await GetListedUser(adminAuth, id)).FirstName);
    }

    #endregion
}
