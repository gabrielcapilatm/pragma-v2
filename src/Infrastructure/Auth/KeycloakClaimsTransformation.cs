namespace FinancialApi.Infrastructure.Auth;

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

public class KeycloakClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var realmAccess = principal.FindFirstValue("realm_access");
        if (realmAccess is null)
            return Task.FromResult(principal);

        var identity = (ClaimsIdentity)principal.Identity!;

        using var doc = JsonDocument.Parse(realmAccess);
        if (!doc.RootElement.TryGetProperty("roles", out var rolesElement))
            return Task.FromResult(principal);

        foreach (var role in rolesElement.EnumerateArray())
        {
            var roleName = role.GetString();
            if (roleName is not null && !principal.HasClaim("roles", roleName))
                identity.AddClaim(new Claim("roles", roleName));
        }

        return Task.FromResult(principal);
    }
}
