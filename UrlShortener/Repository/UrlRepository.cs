using Dapper;
using UrlShortener.Models;
using UrlShortener.Database;

namespace UrlShortener.Repository;

public interface IUrlRepository
{
    
}

public class UrlRepository: IUrlRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public UrlRepository (IConnectionFactory connectionFactory) {
        this._connectionFactory = connectionFactory;
    }

    public async Task<Urls> CreateUrlsAsync(Urls urls)
    {
        string sql = @"
        INSERT into urls (id, user_id, original_url, code, created_at, expires_at, is_active) 
        VALUES (@Id, @UserId, @OriginalUrl, @Code, @CreatedAt, @ExpiresAt, @IsActive)
        RETURNING id, user_id, original_url, code, created_at, expires_at, is_active;
        ";

        using var connection = this._connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<Urls>(sql, new
        {
            Id = urls.Id,
            UserId = urls.UserId,
            OriginalUrl = urls.OriginalUrl,
            Code = urls.Code,
            CreatedAt = urls.CreatedAt,
            ExpiresAt = urls.ExpiresAt,
            IsActive = urls.IsActive
        });
    }
}