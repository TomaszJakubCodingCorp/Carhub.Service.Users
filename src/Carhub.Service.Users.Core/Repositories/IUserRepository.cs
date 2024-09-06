using Carhub.Service.Users.Core.Entities;

namespace Carhub.Service.Users.Core.Repositories;

public interface IUserRepository
{
    Task<User?> GetAsync(Guid id);
    Task<User?> GetAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}