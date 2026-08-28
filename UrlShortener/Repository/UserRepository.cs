using Dapper;
using UrlShortener.Models;
using UrlShortener.Database;

namespace UrlShortener.Repository;

public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(Guid id);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User> CreateUserAsync(User user);
    Task<User?> UpdatePasswordByEmailAsync(string email, string password, DateTime updatedAt);
}

public class UserRepository : IUserRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public UserRepository(IConnectionFactory connectionFactory)
    {
        this._connectionFactory = connectionFactory;
    }

    public async Task<User> CreateUserAsync(User user)
    {
        string sql = @"
        INSERT INTO users (username, email, password, updated_at, is_verified)
        VALUES (@Username, @Email, @Password, @UpdatedAt, @IsVerified)
        RETURNING id, username, email, password, created_at, updated_at, is_active, is_verified;
        ";

        using var connection = this._connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<User>(sql, new
        {
            Username = user.Username,
            Email = user.Email,
            Password = user.Password,
            UpdatedAt = user.UpdatedAt,
            IsVerified = user.IsVerified
        });
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        string sql = @"
        SELECT * FROM users
        WHERE id=@Id
        LIMIT 1;
        ";

        using var connection = this._connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        string sql = @"
        SELECT * FROM users
        WHERE email=@Email
        LIMIT 1;
        ";

        using var connection = this._connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        string sql = @"
        SELECT * FROM users
        WHERE username=@Username
        LIMIT 1;
        ";

        using var connection = this._connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<User?> UpdatePasswordByEmailAsync(string email, string password, DateTime updatedAt)
    {
        string sql = @"
        UPDATE users
        SET password=@Password, updated_at=@UpdatedAt
        WHERE email=@Email
        RETURNING id, username, email, password, created_at, updated_at, is_active, is_verified;
        ";

        using var connection = this._connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<User>(sql, new
        {
            Email = email,
            Password = password,
            UpdatedAt = updatedAt
        });
    }
}