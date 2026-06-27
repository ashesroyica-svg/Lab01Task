using Application.DTOs.Auth;
using Application.WrapperClass;

namespace Application.ServiceInterfaces;

public interface IAuthService
{
    Task<ApiResponse<object?>> RegisterAsync(RegisterDto dto);
    Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginDto dto);
}
