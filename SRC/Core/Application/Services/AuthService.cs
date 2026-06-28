using Application.DTOs.Auth;
using Application.RepositoryInterfaces;
using Application.ServiceInterfaces;
using Application.Settings;
using Application.WrapperClass;
using Domain.Entities;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtSettings _jwtSettings;

    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    public AuthService(IUserRepository userRepository, IJwtService jwtService,
        IPasswordHasher passwordHasher, IOptions<JwtSettings> jwtSettings)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<ApiResponse<object?>> RegisterAsync(RegisterDto dto)
    {
        if (await _userRepository.EmailExistsAsync(dto.Email.ToLower()))
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

        if (user == null)
            return ApiResponse<LoginResponseDto>.Fail("Invalid email or password.");

        // Check lockout
        if (user.LockoutUntil.HasValue && user.LockoutUntil.Value > DateTime.UtcNow)
            return ApiResponse<LoginResponseDto>.Fail(
                "Account locked due to too many failed attempts. Please try again in 15 minutes.");

        // Wrong password — increment failure counter
        if (!_passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                user.FailedLoginAttempts = 0;
            }
            await _userRepository.UpdateAsync(user);
            return ApiResponse<LoginResponseDto>.Fail("Invalid email or password.");
        }

        // Success — reset lockout state
        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;
        await _userRepository.UpdateAsync(user);

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes);
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
