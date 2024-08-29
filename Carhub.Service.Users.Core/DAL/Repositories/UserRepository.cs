using Carhub.Service.Users.Core.Entities;
using Carhub.Service.Users.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Carhub.Service.Users.Core.DAL.Repositories;

internal sealed class UserRepository(UsersDbContext context) : IUserRepository
{
    private readonly DbSet<User> _users = context.Users;

    public Task<User?> GetAsync(Guid id)
        => _users.SingleOrDefaultAsync(x => x.Id == id);

    public Task<User?> GetAsync(string email)
        => _users.SingleOrDefaultAsync(x => x.Email == email);

    public async Task AddAsync(User user)
    {
        await _users.AddAsync(user);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _users.Update(user);
        await context.SaveChangesAsync();
    }
}