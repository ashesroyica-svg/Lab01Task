using Application.DTOs.Auth;
using Application.RepositoryInterfaces;
using Application.ServiceInterfaces;
using Application.WrapperClass;
using Domain.Entities;

namespace Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUserRepository userRepository, IJwtService jwtService, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<object?>> RegisterAsync(RegisterDto dto)
    {
        if (await _userRepository.EmailExistsAsync(dto.Email))
            return ApiResponse<object?>.Fail("Email already registered.");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email.ToLower(),
            PasswordHash = _passwordHasher.Hash(dto.Password),
            CreatedDate = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        return ApiResponse<object?>.Ok(null, "Registration successful.");
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email.ToLower());
        if (user == null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
            return ApiResponse<LoginResponseDto>.Fail("Invalid email or password.");

        var expiresAt = DateTime.UtcNow.AddMinutes(60);
        var token = _jwtService.GenerateToken(user.Id, user.Email, user.Username, expiresAt);

        return ApiResponse<LoginResponseDto>.Ok(new LoginResponseDto
        {
            Token = token,
            Username = user.Username,
            Email = user.Email,
            ExpiresAt = expiresAt
        }, "Login successful.");
    }
}
