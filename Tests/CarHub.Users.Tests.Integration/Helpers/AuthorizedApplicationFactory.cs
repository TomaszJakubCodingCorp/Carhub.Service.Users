using Carhub.Service.Users.Core.DAL;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CarHub.Users.Tests.Integration.Helpers;

public class AuthorizedApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.Test.json", false);
            config.AddUserSecrets<Program>(true, true);
        });
        builder.ConfigureServices(ConfigureServices);
        return base.CreateHost(builder);
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<UsersDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<UsersDbContext>(options => { options.UseInMemoryDatabase("AuthDbInMemory"); });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<UsersDbContext>();
            db.Database.EnsureCreated();

            services.AddSingleton<IPolicyEvaluator, FakePolicyEvaluator>();
        });
    }
}