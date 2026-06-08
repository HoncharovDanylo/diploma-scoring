using Identity.Application.Abstractions;
using Identity.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[ApiController]
[Route("internal/v1/users")]
public sealed class InternalUsersController : ControllerBase
{
    private readonly IUserAccountService _users;

    public InternalUsersController(IUserAccountService users) => _users = users;

    [HttpGet("{id:guid}/risk-profile")]
    [ProducesResponseType(typeof(UserRiskProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserRiskProfileDto>> GetRiskProfile(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var profile = await _users.GetRiskProfileAsync(id, cancellationToken);
        if (profile is null) return NotFound();
        return Ok(profile);
    }
}
