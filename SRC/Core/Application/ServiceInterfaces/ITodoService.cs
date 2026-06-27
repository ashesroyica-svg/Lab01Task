using Application.DTOs.Todo;
using Application.WrapperClass;

namespace Application.ServiceInterfaces;

public interface ITodoService
{
    Task<ApiResponse<List<TodoResponseDto>>> GetAllAsync(int userId, int? projectId, string? status, string? priority, string? search);
    Task<ApiResponse<TodoResponseDto>> GetByIdAsync(int id, int userId);
    Task<ApiResponse<TodoResponseDto>> CreateAsync(TodoCreateDto dto, int userId);
    Task<ApiResponse<TodoResponseDto>> UpdateAsync(int id, TodoUpdateDto dto, int userId);
    Task<ApiResponse<TodoResponseDto>> UpdateStatusAsync(int id, string status, int userId);
    Task<ApiResponse<object?>> DeleteAsync(int id, int userId);
}
