using Application.DTOs.Project;
using Application.WrapperClass;

namespace Application.ServiceInterfaces;

public interface IProjectService
{
    Task<ApiResponse<List<ProjectResponseDto>>> GetAllAsync(int userId, string? status, string? priority, string? search);
    Task<ApiResponse<ProjectResponseDto>> GetByIdAsync(int id, int userId);
    Task<ApiResponse<ProjectResponseDto>> CreateAsync(ProjectCreateDto dto, int userId);
    Task<ApiResponse<ProjectResponseDto>> UpdateAsync(int id, ProjectUpdateDto dto, int userId);
    Task<ApiResponse<object?>> DeleteAsync(int id, int userId);
}
