using Domain.Entities;

namespace Application.RepositoryInterfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<User> AddAsync(User user);
    Task<User> UpdateAsync(User user);
}
