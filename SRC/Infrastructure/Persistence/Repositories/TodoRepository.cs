using Application.RepositoryInterfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories;

public class TodoRepository : ITodoRepository
{
    private readonly AppDbContext _context;

    public TodoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TodoTask>> GetAllAsync(int userId, int? projectId, string? status, string? priority, string? search)
    {
        var query = _context.Tasks
            .Include(t => t.Project)
            .Where(t => t.UserId == userId);

        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(t => t.Priority == priority);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => EF.Functions.Like(t.Title, $"%{search}%") ||
                                     EF.Functions.Like(t.Description ?? "", $"%{search}%"));

        return await query.OrderByDescending(t => t.CreatedDate).ToListAsync();
    }

    public async Task<TodoTask?> GetByIdAsync(int id, int userId) =>
        await _context.Tasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

    public async Task<TodoTask> AddAsync(TodoTask task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<TodoTask> UpdateAsync(TodoTask task)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task SoftDeleteAsync(TodoTask task)
    {
        task.IsDeleted = true;
        task.UpdatedDate = DateTime.UtcNow;
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountByStatusAsync(int userId, string status) =>
        await _context.Tasks.CountAsync(t => t.UserId == userId && t.Status == status);

    public async Task<int> CountByPriorityAsync(int userId, string priority) =>
        await _context.Tasks.CountAsync(t => t.UserId == userId && t.Priority == priority);

    public async Task<List<(string ProjectName, int Count)>> GetTaskCountPerProjectAsync(int userId)
    {
        var result = await _context.Tasks
            .Where(t => t.UserId == userId)
            .GroupBy(t => t.Project.Name)
            .Select(g => new { ProjectName = g.Key, Count = g.Count() })
            .ToListAsync();

        return result.Select(x => (x.ProjectName, x.Count)).ToList();
    }

    public async Task<List<(string Status, int Count)>> GetTaskStatusCountsAsync(int userId)
    {
        var result = await _context.Tasks
            .Where(t => t.UserId == userId)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return result.Select(x => (x.Status, x.Count)).ToList();
    }
}
