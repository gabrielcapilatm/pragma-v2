namespace FinancialApi.Infrastructure.Auth;

using Microsoft.AspNetCore.Http;
using FinancialApi.Application.Common.Interfaces;
using FinancialApi.Domain.ValueObjects;

public class TenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Tenant GetCurrentTenant()
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No HTTP context available");

        var tenantClaim = httpContext.User.Claims
            .FirstOrDefault(c => c.Type == "tenant")?.Value;

        if (string.IsNullOrEmpty(tenantClaim))
            throw new InvalidOperationException("Tenant not found in JWT");

        return Tenant.Create(tenantClaim);
    }
}
