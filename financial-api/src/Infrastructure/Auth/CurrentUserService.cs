namespace FinancialApi.Infrastructure.Auth;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using FinancialApi.Application.Common.Interfaces;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue("sub");

    public string? Email => User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue("email");

    public string? Name => User?.FindFirstValue("name")
        ?? User?.FindFirstValue(ClaimTypes.Name);

    public string? TenantCode => User?.FindFirstValue("tenant");

    public IEnumerable<string> Roles =>
        User?.FindAll("roles").Select(c => c.Value)
        ?? Enumerable.Empty<string>();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
