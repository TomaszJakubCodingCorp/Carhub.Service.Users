using Carhub.Service.Users.Core.DAL;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CarHub.Users.Tests.Integration.Helpers;

public sealed class UnauthorizedApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<UsersDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<UsersDbContext>(options => { options.UseInMemoryDatabase("UnauthDbInMemory"); });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<UsersDbContext>();
            db.Database.EnsureCreated();
        });
    }
}