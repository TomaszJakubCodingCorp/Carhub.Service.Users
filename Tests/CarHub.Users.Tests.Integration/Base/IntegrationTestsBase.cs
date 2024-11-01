using Carhub.Service.Users.Core.DAL;
using CarHub.Users.Tests.Integration.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace CarHub.Users.Tests.Integration.Base;

public abstract class IntegrationTestsBase : IClassFixture<TestApplicationFactory>, IDisposable
{
    private readonly IServiceScope _scope;
    protected readonly UsersDbContext DbContext;
    protected readonly HttpClient Client;

    protected IntegrationTestsBase(TestApplicationFactory factory)
    {
        _scope = factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        Client = factory.CreateClient();
    }

    public void Dispose()
    {
        _scope?.Dispose();
        DbContext?.Dispose();
        Client?.Dispose();
    }
}