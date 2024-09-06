using Carhub.Service.Users.Core.Contexts;
using Carhub.Service.Users.Core.DTOs;
using Carhub.Service.Users.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Carhub.Service.Users.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController(IIdentityService identityService, IContext context) : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto?>> GetCurrentUserAsync()
    {
        var user = await identityService.GetAsync(context.Identity.Id);
        if (user is null)
            return NotFound();
        return Ok(user);
    }

    [HttpGet("user/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<UserDto?>> GetUserByIdAsync(Guid id)
    {
        var user = await identityService.GetAsync(id);
        if (user is null)
            return NotFound();
        return Ok(user);
    }

    [HttpPost("sign-up")]
    public async Task<ActionResult> SignUpAsync(SignUpDto dto)
    {
        await identityService.SignUpAsync(dto);
        return NoContent();
    }

    [HttpPost("sign-in")]
    public async Task<ActionResult<JwtDto>> SignInAsync(SignInDto dto)
    {
        return Ok(await identityService.SignInAsync(dto));
    }
}