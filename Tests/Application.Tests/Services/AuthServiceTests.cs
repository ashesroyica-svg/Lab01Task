using Application.DTOs.Auth;
using Application.RepositoryInterfaces;
using Application.ServiceInterfaces;
using Application.Services;
using Domain.Entities;
using Moq;

namespace Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_userRepo.Object, _jwtService.Object, _passwordHasher.Object);
    }

    // --- RegisterAsync ---

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ReturnsFailure()
    {
        _userRepo.Setup(r => r.EmailExistsAsync("test@example.com")).ReturnsAsync(true);

        var result = await _sut.RegisterAsync(new RegisterDto
        {
            Username = "Alice",
            Email = "test@example.com",
            Password = "pass123",
            ConfirmPassword = "pass123"
        });

        Assert.False(result.Success);
        Assert.Equal("Email already registered.", result.Message);
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_CreatesUserAndReturnsSuccess()
    {
        _userRepo.Setup(r => r.EmailExistsAsync("new@example.com")).ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash("pass123")).Returns("hashed_password");
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _sut.RegisterAsync(new RegisterDto
        {
            Username = "Bob",
            Email = "new@example.com",
            Password = "pass123",
            ConfirmPassword = "pass123"
        });

        Assert.True(result.Success);
        Assert.Equal("Registration successful.", result.Message);
        _userRepo.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Username == "Bob" &&
            u.Email == "new@example.com" &&
            u.PasswordHash == "hashed_password")), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_EmailStoredAsLowercase()
    {
        _userRepo.Setup(r => r.EmailExistsAsync("upper@example.com")).ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        await _sut.RegisterAsync(new RegisterDto
        {
            Username = "Carol",
            Email = "UPPER@example.com",
            Password = "pass123",
            ConfirmPassword = "pass123"
        });

        _userRepo.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == "upper@example.com")), Times.Once);
    }

    // --- LoginAsync ---

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsFailure()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("unknown@example.com")).ReturnsAsync((User?)null);

        var result = await _sut.LoginAsync(new LoginDto
        {
            Email = "unknown@example.com",
            Password = "pass123"
        });

        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.Message);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsFailure()
    {
        var user = new User { Id = 1, Email = "test@example.com", PasswordHash = "hash" };
        _userRepo.Setup(r => r.GetByEmailAsync("test@example.com")).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("wrongpass", "hash")).Returns(false);

        var result = await _sut.LoginAsync(new LoginDto
        {
            Email = "test@example.com",
            Password = "wrongpass"
        });

        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.Message);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenResponse()
    {
        var user = new User { Id = 1, Username = "Alice", Email = "alice@example.com", PasswordHash = "hash" };
        _userRepo.Setup(r => r.GetByEmailAsync("alice@example.com")).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("pass123", "hash")).Returns(true);
        _jwtService.Setup(j => j.GenerateToken(1, "alice@example.com", "Alice", It.IsAny<DateTime>()))
                   .Returns("jwt_token_value");

        var result = await _sut.LoginAsync(new LoginDto
        {
            Email = "alice@example.com",
            Password = "pass123"
        });

        Assert.True(result.Success);
        Assert.Equal("Login successful.", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal("jwt_token_value", result.Data!.Token);
        Assert.Equal("Alice", result.Data.Username);
        Assert.Equal("alice@example.com", result.Data.Email);
    }

    [Fact]
    public async Task LoginAsync_EmailNormalisedToLowercase()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("alice@example.com")).ReturnsAsync((User?)null);

        await _sut.LoginAsync(new LoginDto { Email = "ALICE@example.com", Password = "pass" });

        _userRepo.Verify(r => r.GetByEmailAsync("alice@example.com"), Times.Once);
    }
}
