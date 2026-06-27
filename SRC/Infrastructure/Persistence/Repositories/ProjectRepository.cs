using Application.RepositoryInterfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Project>> GetAllAsync(int userId, string? status, string? priority, string? search)
    {
        var query = _context.Projects
            .Include(p => p.Tasks)
            .Where(p => p.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(p => p.Priority == priority);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.Like(p.Name, $"%{search}%") ||
                                     EF.Functions.Like(p.Description ?? "", $"%{search}%"));

        return await query.OrderByDescending(p => p.CreatedDate).ToListAsync();
    }

    public async Task<Project?> GetByIdAsync(int id, int userId) =>
        await _context.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

    public async Task<Project> AddAsync(Project project)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async Task<Project> UpdateAsync(Project project)
    {
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async Task SoftDeleteAsync(Project project)
    {
        project.IsDeleted = true;
        project.UpdatedDate = DateTime.UtcNow;
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id, int userId) =>
        await _context.Projects.AnyAsync(p => p.Id == id && p.UserId == userId);
}
