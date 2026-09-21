using System.Net;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using UrlShortener.Services;
using UrlShortener.DTO.Requests;
using UrlShortener.DTO.Response;
using UrlShortener.Models;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;

namespace UrlShortener.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly string refreshCookieName = "refresh_token";

    public AuthController(IAuthService authService)
    {
        this._authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestData request)
    {
        var result = await this._authService.RegisterAsync(request.Username, request.Email, request.Password);

        if (!result.Success)
        {
            return StatusCode(result.ErrorCode ?? (int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = result.Error ?? "an error occured."
            });
        }
        return StatusCode((int)HttpStatusCode.Created, new SuccessResponse<string>
        {
            Data = result.Data
        });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmailAsync([FromBody] EmailVerificationRequestData request)
    {
        if (request.VerificationCode is null)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = "please provide a verification"
            });
        }
        var result = await this._authService.VerifyEmailAsync(request.EmailAddress, request.VerificationCode);
        if (!result.Success)
        {
            return StatusCode(result.ErrorCode ?? (int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = result.Error ?? "an error occured."
            });
        }
        this.SetRefreshCookie(result.Data!.RefreshToken);
        return StatusCode((int)HttpStatusCode.Created, new SuccessResponse<string>
        {
            Data = result.Data.AccessToken
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequestData request)
    {
        var result = await this._authService.LoginAsync(request.Email, request.Password);
        if (!result.Success)
        {
            return StatusCode(result.ErrorCode ?? (int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = result.Error ?? "an error occured."
            });
        }
        this.SetRefreshCookie(result.Data!.RefreshToken);
        return StatusCode((int)HttpStatusCode.Created, new SuccessResponse<string>
        {
            Data = result.Data.AccessToken
        });
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleAuthAsync([FromBody] GoogleAuthRequestData request)
    {
        var result = await this._authService.GoogleAuthAsync(request.IdToken);
        if (!result.Success)
        {
            return StatusCode(result.ErrorCode ?? (int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = result.Error ?? "an error occured."
            });
        }

        SetRefreshCookie(result.Data!.RefreshToken);
        return StatusCode((int)HttpStatusCode.Created, new SuccessResponse<string>
        {
            Data = result.Data.AccessToken
        });
    }


    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordData request)
    {
        var result = await this._authService.ForgotPasswordAsync(request.EmailAddress);
        if (!result.Success)
        {
            return StatusCode(result.ErrorCode ?? (int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = result.Error ?? "an error occured."
            });
        }
        return StatusCode((int)HttpStatusCode.Created, new SuccessResponse<string>
        {
            Data = result.Data
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordData request)
    {
        var result = await this._authService.ResetPasswordAsync(request.EmailAddress, request.ResetToken, request.NewPassword, request.ConfirmPassword);
        if (!result.Success)
        {
            return StatusCode(result.ErrorCode ?? (int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = result.Error ?? "an error occured."
            });
        }
        return StatusCode((int)HttpStatusCode.Created, new SuccessResponse<string>
        {
            Data = result.Data
        });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenData request)
    {
        var result = await this._authService.RefreshTokenAsync(request.RefreshToken);

        if (!result.Success)
        {
            this.ClearRefreshCookie();
            return StatusCode(result.ErrorCode ?? (int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = result.Error ?? "an error occured."
            });
        }
        this.SetRefreshCookie(result.Data!.RefreshToken);
        return StatusCode((int)HttpStatusCode.Created, new SuccessResponse<string>
        {
            Data = result.Data.AccessToken
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync([FromBody] RefreshTokenData request)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }


        var result = await this._authService.LogoutAsync(userId, request.RefreshToken);
        this.ClearRefreshCookie();
        if (!result.Success)
        {
            return StatusCode(result.ErrorCode ?? (int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = result.Error ?? "an error occured."
            });
        }
        return StatusCode((int)HttpStatusCode.Created, new SuccessResponse<string>
        {
            Data = result.Data
        });
    }

    private void SetRefreshCookie(string refreshToken)
    {
        HttpContext.Response.Cookies.Append(this.refreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/api/auth"
        });
    }

    private void ClearRefreshCookie()
    {
        Response.Cookies.Delete(this.refreshCookieName, new CookieOptions { Path = "/api/auth" });
    }
}