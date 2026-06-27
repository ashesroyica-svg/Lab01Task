using Application.DTOs.Todo;
using Application.RepositoryInterfaces;
using Application.ServiceInterfaces;
using Application.WrapperClass;
using Domain.Entities;

namespace Application.Services;

public class TodoService : ITodoService
{
    private readonly ITodoRepository _todoRepository;
    private readonly IProjectRepository _projectRepository;

    public TodoService(ITodoRepository todoRepository, IProjectRepository projectRepository)
    {
        _todoRepository = todoRepository;
        _projectRepository = projectRepository;
    }

    public async Task<ApiResponse<List<TodoResponseDto>>> GetAllAsync(int userId, int? projectId, string? status, string? priority, string? search)
    {
        var tasks = await _todoRepository.GetAllAsync(userId, projectId, status, priority, search);
        var result = tasks.Select(MapToDto).ToList();
        return ApiResponse<List<TodoResponseDto>>.Ok(result, "Tasks retrieved.");
    }

    public async Task<ApiResponse<TodoResponseDto>> GetByIdAsync(int id, int userId)
    {
        var task = await _todoRepository.GetByIdAsync(id, userId);
        if (task == null)
            return ApiResponse<TodoResponseDto>.Fail("Task not found.");
        return ApiResponse<TodoResponseDto>.Ok(MapToDto(task));
    }

    public async Task<ApiResponse<TodoResponseDto>> CreateAsync(TodoCreateDto dto, int userId)
    {
        if (!await _projectRepository.ExistsAsync(dto.ProjectId, userId))
            return ApiResponse<TodoResponseDto>.Fail("Project not found or access denied.");

        var task = new TodoTask
        {
            ProjectId = dto.ProjectId,
            UserId = userId,
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            Status = dto.Status,
            DueDate = dto.DueDate,
            IsCompleted = dto.Status == "Completed",
            CreatedDate = DateTime.UtcNow
        };

        var created = await _todoRepository.AddAsync(task);
        var withProject = await _todoRepository.GetByIdAsync(created.Id, userId);
        return ApiResponse<TodoResponseDto>.Ok(MapToDto(withProject!), "Task created.");
    }

    public async Task<ApiResponse<TodoResponseDto>> UpdateAsync(int id, TodoUpdateDto dto, int userId)
    {
        var task = await _todoRepository.GetByIdAsync(id, userId);
        if (task == null)
            return ApiResponse<TodoResponseDto>.Fail("Task not found.");

        if (!await _projectRepository.ExistsAsync(dto.ProjectId, userId))
            return ApiResponse<TodoResponseDto>.Fail("Project not found or access denied.");

        task.ProjectId = dto.ProjectId;
        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Priority = dto.Priority;
        task.Status = dto.Status;
        task.DueDate = dto.DueDate;
        task.IsCompleted = dto.Status == "Completed";
        task.UpdatedDate = DateTime.UtcNow;

        var updated = await _todoRepository.UpdateAsync(task);
        var withProject = await _todoRepository.GetByIdAsync(updated.Id, userId);
        return ApiResponse<TodoResponseDto>.Ok(MapToDto(withProject!), "Task updated.");
    }

    public async Task<ApiResponse<TodoResponseDto>> UpdateStatusAsync(int id, string status, int userId)
    {
        var task = await _todoRepository.GetByIdAsync(id, userId);
        if (task == null)
            return ApiResponse<TodoResponseDto>.Fail("Task not found.");

        task.Status = status;
        task.IsCompleted = status == "Completed";
        task.UpdatedDate = DateTime.UtcNow;

        var updated = await _todoRepository.UpdateAsync(task);
        var withProject = await _todoRepository.GetByIdAsync(updated.Id, userId);
        return ApiResponse<TodoResponseDto>.Ok(MapToDto(withProject!), "Status updated.");
    }

    public async Task<ApiResponse<object?>> DeleteAsync(int id, int userId)
    {
        var task = await _todoRepository.GetByIdAsync(id, userId);
        if (task == null)
            return ApiResponse<object?>.Fail("Task not found.");

        await _todoRepository.SoftDeleteAsync(task);
        return ApiResponse<object?>.Ok(null, "Task deleted.");
    }

    private static TodoResponseDto MapToDto(TodoTask t) => new()
    {
        Id = t.Id,
        ProjectId = t.ProjectId,
        ProjectName = t.Project?.Name ?? string.Empty,
        Title = t.Title,
        Description = t.Description,
        Priority = t.Priority,
        Status = t.Status,
        DueDate = t.DueDate,
        IsCompleted = t.IsCompleted,
        CreatedDate = t.CreatedDate,
        UpdatedDate = t.UpdatedDate
    };
}
