using Carhub.Service.Users.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Carhub.Service.Users.Core.DAL;

internal sealed class UsersDbContext(DbContextOptions<UsersDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}