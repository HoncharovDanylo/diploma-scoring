using System.Security.Claims;
using Identity.Api.Services;
using Identity.Application.Abstractions;
using Identity.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserAccountService _accounts;
    private readonly JwtIssuer _jwt;

    public AuthController(IUserAccountService accounts, JwtIssuer jwt)
    {
        _accounts = accounts;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] RegisterUserRequest dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await _accounts.RegisterAsync(dto, cancellationToken);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors });
        var token = _jwt.CreateToken(result.UserId!.Value, dto.Email, new[] { "Customer" });
        return Ok(new { accessToken = token, userId = result.UserId });
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginRequest dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await _accounts.LoginAsync(dto, cancellationToken);
        if (result is null) return Unauthorized();
        var token = _jwt.CreateToken(result.UserId, dto.Email, result.Roles);
        return Ok(new { accessToken = token, userId = result.UserId });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult> Me(CancellationToken cancellationToken)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _accounts.GetPublicProfileAsync(id, cancellationToken);
        if (user is null) return NotFound();
        return Ok(new
        {
            user.Id,
            user.Email,
            user.DisplayName,
            user.DateOfBirth,
            user.EmploymentStatus,
            user.PhoneNumber,
            user.MonthlyIncome
        });
    }
}
