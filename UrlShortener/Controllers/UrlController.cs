using System.Net;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Services;
using UrlShortener.DTO.Requests;
using UrlShortener.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using UrlShortener.Models;

namespace UrlShortener.Controllers;

[ApiController]
[Route("")]
public class UrlController : ControllerBase
{
    private readonly IUrlServices _urlServices;
    public UrlController(IUrlServices urlServices)
    {
        this._urlServices = urlServices;
    }

    [Authorize]
    [HttpPost("api/v1/urls")]
    public async Task<IActionResult> CreateUrlAsync([FromBody] CreateUrlData request)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        var result = await this._urlServices.CreateShortUrlAsync(userId, request.LongUrl, expiredAt: DateTime.Now);
        if (!result.Success)
        {
            return StatusCode(result.ErrorCode ?? (int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = result.Error ?? "an error occured."
            });
        }
        return StatusCode((int)HttpStatusCode.Created, new SuccessResponse<Urls>
        {
            Data = result.Data
        });
    }

    [HttpGet("{shortCode}")]
    public async Task<IActionResult> GetShortUrlAsync(string shortCode)
    {
        var result = await this._urlServices.GetLongUrlAsync(shortCode);
        if (!result.Success)
        {
            return StatusCode(result.ErrorCode ?? (int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Details = result.Error ?? "an error occured."
            });
        }
        return RedirectPermanent(result.Data);
        // return StatusCode((int)HttpStatusCode.Created, new SuccessResponse<string>
        // {
        //     Data = result.Data
        // });
    }
}