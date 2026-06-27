using Domain.Entities;

namespace Application.RepositoryInterfaces;

public interface ITodoRepository
{
    Task<List<TodoTask>> GetAllAsync(int userId, int? projectId, string? status, string? priority, string? search);
    Task<TodoTask?> GetByIdAsync(int id, int userId);
    Task<TodoTask> AddAsync(TodoTask task);
    Task<TodoTask> UpdateAsync(TodoTask task);
    Task SoftDeleteAsync(TodoTask task);
    Task<int> CountByStatusAsync(int userId, string status);
    Task<int> CountByPriorityAsync(int userId, string priority);
    Task<List<(string ProjectName, int Count)>> GetTaskCountPerProjectAsync(int userId);
    Task<List<(string Status, int Count)>> GetTaskStatusCountsAsync(int userId);
}
