namespace FinancialApi.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinancialApi.Application.Common.Interfaces;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;

    public AuthController(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet("me")]
    public IActionResult GetMe()
    {
        return Ok(new
        {
            id = _currentUser.UserId,
            email = _currentUser.Email,
            name = _currentUser.Name,
            tenant = _currentUser.TenantCode,
            roles = _currentUser.Roles
        });
    }
}
