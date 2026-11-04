using Microsoft.AspNetCore.Mvc.Testing;

namespace Zuhid.Auth.Tests.Integration;

public abstract class BaseIntegrationTest(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    protected WebApplicationFactory<Program> Factory { get; } = factory;
    protected HttpClient Client { get; } = factory.CreateClient();
    protected IServiceProvider Services { get; } = factory.Services;
}
