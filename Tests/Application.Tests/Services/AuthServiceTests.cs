using Application.DTOs.Auth;
using Application.RepositoryInterfaces;
using Application.ServiceInterfaces;
using Application.Services;
using Application.Settings;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Moq;

namespace Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly AuthService _sut;

    private static IOptions<JwtSettings> DefaultJwtSettings =>
        Options.Create(new JwtSettings { ExpiryInMinutes = 60 });

    public AuthServiceTests()
    {
        _sut = new AuthService(_userRepo.Object, _jwtService.Object, _passwordHasher.Object, DefaultJwtSettings);
    }

    // ── RegisterAsync ────────────────────────────────────────

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

    [Fact]
    public async Task RegisterAsync_EmailNormalisedBeforeExistenceCheck()
    {
        _userRepo.Setup(r => r.EmailExistsAsync("mixed@example.com")).ReturnsAsync(true);

        var result = await _sut.RegisterAsync(new RegisterDto
        {
            Username = "Dave",
            Email = "MIXED@example.com",
            Password = "pass123",
            ConfirmPassword = "pass123"
        });

        Assert.False(result.Success);
        _userRepo.Verify(r => r.EmailExistsAsync("mixed@example.com"), Times.Once);
    }

    // ── LoginAsync ───────────────────────────────────────────

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
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _sut.LoginAsync(new LoginDto
        {
            Email = "test@example.com",
            Password = "wrongpass"
        });

        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.Message);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_IncrementsFailedAttempts()
    {
        var user = new User { Id = 1, Email = "test@example.com", PasswordHash = "hash", FailedLoginAttempts = 0 };
        _userRepo.Setup(r => r.GetByEmailAsync("test@example.com")).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("wrong", "hash")).Returns(false);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        await _sut.LoginAsync(new LoginDto { Email = "test@example.com", Password = "wrong" });

        _userRepo.Verify(r => r.UpdateAsync(It.Is<User>(u => u.FailedLoginAttempts == 1)), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_FiveFailedAttempts_LocksAccount()
    {
        var user = new User { Id = 1, Email = "test@example.com", PasswordHash = "hash", FailedLoginAttempts = 4 };
        _userRepo.Setup(r => r.GetByEmailAsync("test@example.com")).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("wrong", "hash")).Returns(false);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        await _sut.LoginAsync(new LoginDto { Email = "test@example.com", Password = "wrong" });

        _userRepo.Verify(r => r.UpdateAsync(
            It.Is<User>(u => u.LockoutUntil.HasValue && u.FailedLoginAttempts == 0)), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_AccountLocked_ReturnsFailureWithoutCheckingPassword()
    {
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            PasswordHash = "hash",
            LockoutUntil = DateTime.UtcNow.AddMinutes(10)
        };
        _userRepo.Setup(r => r.GetByEmailAsync("test@example.com")).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginDto { Email = "test@example.com", Password = "any" });

        Assert.False(result.Success);
        Assert.Contains("locked", result.Message, StringComparison.OrdinalIgnoreCase);
        _passwordHasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ExpiredLockout_AllowsLogin()
    {
        var user = new User
        {
            Id = 1, Username = "Alice", Email = "alice@example.com", PasswordHash = "hash",
            LockoutUntil = DateTime.UtcNow.AddMinutes(-1) // already expired
        };
        _userRepo.Setup(r => r.GetByEmailAsync("alice@example.com")).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("pass", "hash")).Returns(true);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _jwtService.Setup(j => j.GenerateToken(1, "alice@example.com", "Alice", It.IsAny<DateTime>()))
                   .Returns("tok");

        var result = await _sut.LoginAsync(new LoginDto { Email = "alice@example.com", Password = "pass" });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenResponse()
    {
        var user = new User { Id = 1, Username = "Alice", Email = "alice@example.com", PasswordHash = "hash" };
        _userRepo.Setup(r => r.GetByEmailAsync("alice@example.com")).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("pass123", "hash")).Returns(true);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
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
    public async Task LoginAsync_ValidCredentials_ResetsLockoutState()
    {
        var user = new User
        {
            Id = 1, Username = "Alice", Email = "alice@example.com", PasswordHash = "hash",
            FailedLoginAttempts = 3
        };
        _userRepo.Setup(r => r.GetByEmailAsync("alice@example.com")).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("pass", "hash")).Returns(true);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _jwtService.Setup(j => j.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                   .Returns("tok");

        await _sut.LoginAsync(new LoginDto { Email = "alice@example.com", Password = "pass" });

        _userRepo.Verify(r => r.UpdateAsync(
            It.Is<User>(u => u.FailedLoginAttempts == 0 && u.LockoutUntil == null)), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_EmailNormalisedToLowercase()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("alice@example.com")).ReturnsAsync((User?)null);

        await _sut.LoginAsync(new LoginDto { Email = "ALICE@example.com", Password = "pass" });

        _userRepo.Verify(r => r.GetByEmailAsync("alice@example.com"), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ExpiryUsesConfiguredMinutes()
    {
        var opts = Options.Create(new JwtSettings { ExpiryInMinutes = 30 });
        var sut = new AuthService(_userRepo.Object, _jwtService.Object, _passwordHasher.Object, opts);

        var user = new User { Id = 1, Username = "A", Email = "a@b.com", PasswordHash = "h" };
        _userRepo.Setup(r => r.GetByEmailAsync("a@b.com")).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("p", "h")).Returns(true);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _jwtService.Setup(j => j.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                   .Returns("tok");

        var result = await sut.LoginAsync(new LoginDto { Email = "a@b.com", Password = "p" });

        Assert.True(result.Success);
        // ExpiresAt should be ~30 minutes from now (within a 5-second tolerance)
        var expectedExpiry = DateTime.UtcNow.AddMinutes(30);
        Assert.True(Math.Abs((result.Data!.ExpiresAt - expectedExpiry).TotalSeconds) < 5);
    }
}
