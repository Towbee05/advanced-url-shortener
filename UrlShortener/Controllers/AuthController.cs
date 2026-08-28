using System.Net;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
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

    public AuthController(IAuthService authService)
    {
        this._authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestData request)
    {
        if (request.Username is null)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = "please provide a username"
            });
        }
        if (request.Email is null)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = "please provide an email address"
            });
        }
        if (request.Password is null)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = "please provide a password"
            });
        }

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
        if (request.EmailAddress is null)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = "please provide an email address"
            });
        }
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
        return StatusCode((int)HttpStatusCode.Created, new SuccessResponse<AuthenticationModel>
        {
            Data = result.Data
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequestData request)
    {
        if (request.Email is null)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = "please provide an email address"
            });
        }
        if (request.Password is null)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = "please provide a password"
            });
        }
        var result = await this._authService.LoginAsync(request.Email, request.Password);
        if (!result.Success)
        {
            return StatusCode(result.ErrorCode ?? (int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = result.Error ?? "an error occured."
            });
        }
        return StatusCode((int)HttpStatusCode.Created, new SuccessResponse<AuthenticationModel>
        {
            Data = result.Data
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordData request)
    {
        if (request.EmailAddress is null)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = "please provide an email address"
            });
        }
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
        if (request.EmailAddress is null)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = "please provide an email address"
            });
        }
        if (request.ResetToken is null)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = "please provide a reset token"
            });
        }
        if (request.NewPassword is null)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = "please provide a new password"
            });
        }
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
        if (request.RefreshToken is null)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = "please provide a refresh token"
            });
        }
        var result = await this._authService.RefreshTokenAsync(request.RefreshToken);
        if (!result.Success)
        {
            return StatusCode(result.ErrorCode ?? (int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = result.Error ?? "an error occured."
            });
        }
        return StatusCode((int)HttpStatusCode.Created, new SuccessResponse<AuthenticationModel>
        {
            Data = result.Data
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> LogoutAsync([FromBody] RefreshTokenData request)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        
        if (request.RefreshToken is null)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = "please provide a refresh token"
            });
        }

        var result = await this._authService.LogoutAsync(userId, request.RefreshToken);
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
}