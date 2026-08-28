using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UrlShortener.Entities;
using UrlShortener.Models;

namespace UrlShortener.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task<bool> StoreRefreshTokenAsync(Guid userId, string refreshToken);
    Task<Guid?> ValidateRefreshTokenAsync(string refreshToken);
}

public class JWTService : IJwtService
{
    private readonly JwtSettings _jwtSetings;
    private readonly ILogger<JWTService> _logger;
    private readonly ICacheService _cache;

    public JWTService(IOptions<JwtSettings> jwtSettings, ILogger<JWTService> logger, ICacheService cache)
    {
        this._jwtSetings = jwtSettings.Value;
        this._logger = logger;
        this._cache = cache;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this._jwtSetings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: this._jwtSetings.Issuer,
            audience: this._jwtSetings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(this._jwtSetings.AccessTokenExpirationMinutes),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public async Task<bool> StoreRefreshTokenAsync(Guid userId, string refreshToken)
    {
        string key = $"RefreshToken:{refreshToken}";
        return await this._cache.SetStringAsync(key, userId.ToString(), TimeSpan.FromDays(this._jwtSetings.RefreshTokenExpirationDays));
    }

    public async Task<Guid?> ValidateRefreshTokenAsync(string refreshToken)
    {
        string key = $"RefreshToken:{refreshToken}";
        string? userIdString = await this._cache.GetAndDeleteAsync(key);

        if (userIdString is null)
        {
            return null;
        }

        return Guid.Parse(userIdString);
    }
}