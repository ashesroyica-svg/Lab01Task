using Application.DTOs.Project;
using Application.RepositoryInterfaces;
using Application.ServiceInterfaces;
using Application.WrapperClass;
using Domain.Entities;

namespace Application.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;

    public ProjectService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<ApiResponse<List<ProjectResponseDto>>> GetAllAsync(int userId, string? status, string? priority, string? search)
    {
        var projects = await _projectRepository.GetAllAsync(userId, status, priority, search);
        var result = projects.Select(p => MapToDto(p)).ToList();
        return ApiResponse<List<ProjectResponseDto>>.Ok(result, "Projects retrieved.");
    }

    public async Task<ApiResponse<ProjectResponseDto>> GetByIdAsync(int id, int userId)
    {
        var project = await _projectRepository.GetByIdAsync(id, userId);
        if (project == null)
            return ApiResponse<ProjectResponseDto>.Fail("Project not found.");
        return ApiResponse<ProjectResponseDto>.Ok(MapToDto(project));
    }

    public async Task<ApiResponse<ProjectResponseDto>> CreateAsync(ProjectCreateDto dto, int userId)
    {
        var project = new Project
        {
            UserId = userId,
            Name = dto.Name,
            Description = dto.Description,
            Status = dto.Status,
            Priority = dto.Priority,
            DueDate = dto.DueDate,
            CreatedDate = DateTime.UtcNow
        };
        var created = await _projectRepository.AddAsync(project);
        return ApiResponse<ProjectResponseDto>.Ok(MapToDto(created), "Project created.");
    }

    public async Task<ApiResponse<ProjectResponseDto>> UpdateAsync(int id, ProjectUpdateDto dto, int userId)
    {
        var project = await _projectRepository.GetByIdAsync(id, userId);
        if (project == null)
            return ApiResponse<ProjectResponseDto>.Fail("Project not found.");

        project.Name = dto.Name;
        project.Description = dto.Description;
        project.Status = dto.Status;
        project.Priority = dto.Priority;
        project.DueDate = dto.DueDate;
        project.UpdatedDate = DateTime.UtcNow;

        var updated = await _projectRepository.UpdateAsync(project);
        return ApiResponse<ProjectResponseDto>.Ok(MapToDto(updated), "Project updated.");
    }

    public async Task<ApiResponse<object?>> DeleteAsync(int id, int userId)
    {
        var project = await _projectRepository.GetByIdAsync(id, userId);
        if (project == null)
            return ApiResponse<object?>.Fail("Project not found.");

        await _projectRepository.SoftDeleteAsync(project);
        return ApiResponse<object?>.Ok(null, "Project deleted.");
    }

    private static ProjectResponseDto MapToDto(Project p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Status = p.Status,
        Priority = p.Priority,
        DueDate = p.DueDate,
        CreatedDate = p.CreatedDate,
        UpdatedDate = p.UpdatedDate,
        TaskCount = p.Tasks?.Count(t => !t.IsDeleted) ?? 0
    };
}
