namespace Application.ServiceInterfaces;

public interface IJwtService
{
    string GenerateToken(int userId, string email, string username, DateTime expiresAt);
}
