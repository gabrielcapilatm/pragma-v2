namespace FinancialApi.Api.OpenApi;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;

public class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private readonly IConfiguration _configuration;

    public BearerSecuritySchemeTransformer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authority = _configuration["Keycloak:Authority"];
        var realm = _configuration["Keycloak:Realm"];
        var tokenUrl = new Uri($"{authority}/realms/{realm}/protocol/openid-connect/token");

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                Password = new OpenApiOAuthFlow
                {
                    TokenUrl = tokenUrl,
                    Scopes = new Dictionary<string, string>
                    {
                        { "openid", "OpenID Connect" },
                        { "profile", "Profile" }
                    }
                }
            }
        };

        return Task.CompletedTask;
    }
}
