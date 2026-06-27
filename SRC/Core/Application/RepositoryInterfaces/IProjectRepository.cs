using Domain.Entities;

namespace Application.RepositoryInterfaces;

public interface IProjectRepository
{
    Task<List<Project>> GetAllAsync(int userId, string? status, string? priority, string? search);
    Task<Project?> GetByIdAsync(int id, int userId);
    Task<Project> AddAsync(Project project);
    Task<Project> UpdateAsync(Project project);
    Task SoftDeleteAsync(Project project);
    Task<bool> ExistsAsync(int id, int userId);
}
