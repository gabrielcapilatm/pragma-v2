namespace FinancialApi.Infrastructure;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using FinancialApi.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication;
using FinancialApi.Infrastructure.Auth;
using FinancialApi.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<IClaimsTransformation, KeycloakClaimsTransformation>();

        // Multi-tenancy
        services.AddScoped<ITenantResolver, TenantResolver>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<ConnectionStringResolver>();
        services.AddScoped<DbContextFactory>();

        // Database
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<DbContextFactory>().CreateDbContext());

        services.AddScoped<ApplicationDbContext>(provider =>
            provider.GetRequiredService<DbContextFactory>().CreateDbContext());

        // Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var authority = configuration["Keycloak:Authority"];
                var realm = configuration["Keycloak:Realm"];

                options.Authority = $"{authority}/realms/{realm}";
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles"
                };
            });

        // Authorization
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAuthenticated", policy => policy.RequireAuthenticatedUser())
            .AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));

        return services;
    }
}
