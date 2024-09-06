using Carhub.Service.Users.Core.Entities;
using Carhub.Service.Users.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Carhub.Service.Users.Core.DAL;

internal sealed class DatabaseInitializer(
    IServiceProvider serviceProvider,
    TimeProvider timeProvider,
    IPasswordManager passwordManager,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            dbContext.Database.Migrate();
            logger.LogInformation("Database migrated");

            var users = dbContext.Users.ToList();
            if (users.Count is not 0)
                return Task.CompletedTask;

            passwordManager.CreatePasswordHash("SuperSecretAdminPassword123!", out var hash, out var salt);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "carhub.app2024@gmail.com",
                PasswordHash = hash,
                PasswordSalt = salt,
                Firstname = "CarHub",
                Lastname = "Admin",
                Role = "Admin",
                IsActive = true,
                CreatedAt = timeProvider.GetLocalNow().DateTime,
                Claims = []
            };

            users = [user];

            dbContext.Users.AddRange(users);
            dbContext.SaveChanges();
            logger.LogInformation("Database initialized");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}