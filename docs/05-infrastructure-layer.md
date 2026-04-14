# 🔌 Infrastructure Layer

## Objetivo

Implementa as interfaces definidas no Application: EF Core, autenticação JWT, multi-tenancy e logs.

## Estrutura de Pastas

```
src/Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── DbContextFactory.cs
│   ├── ConnectionStringResolver.cs
│   └── Migrations/
├── Auth/
│   ├── CurrentUserService.cs
│   └── TenantResolver.cs
├── Logging/
│   └── LoggingConfiguration.cs
├── DependencyInjection.cs
└── Infrastructure.csproj
```

## 1. Multi-Tenancy

### TenantResolver.cs

```csharp
// src/Infrastructure/Auth/TenantResolver.cs
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
```

### CurrentUserService.cs

```csharp
// src/Infrastructure/Auth/CurrentUserService.cs
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
        User?.FindAll("realm_access/roles")?.Select(c => c.Value)
        ?? Enumerable.Empty<string>();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
```

## 2. Persistence

### ConnectionStringResolver.cs

```csharp
// src/Infrastructure/Persistence/ConnectionStringResolver.cs
namespace FinancialApi.Infrastructure.Persistence;

using Microsoft.Extensions.Configuration;
using FinancialApi.Domain.ValueObjects;

public class ConnectionStringResolver
{
    private readonly IConfiguration _configuration;

    public ConnectionStringResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString(Tenant tenant)
    {
        var connectionString = _configuration.GetConnectionString(tenant.Code);

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException(
                $"Connection string not found for tenant: {tenant.Code}");

        return connectionString;
    }
}
```

### DbContextFactory.cs

```csharp
// src/Infrastructure/Persistence/DbContextFactory.cs
namespace FinancialApi.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using FinancialApi.Application.Common.Interfaces;

public class DbContextFactory
{
    private readonly ConnectionStringResolver _connectionStringResolver;
    private readonly ITenantResolver _tenantResolver;

    public DbContextFactory(
        ConnectionStringResolver connectionStringResolver,
        ITenantResolver tenantResolver)
    {
        _connectionStringResolver = connectionStringResolver;
        _tenantResolver = tenantResolver;
    }

    public ApplicationDbContext CreateDbContext()
    {
        var tenant = _tenantResolver.GetCurrentTenant();
        var connectionString = _connectionStringResolver.GetConnectionString(tenant);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
```

### ApplicationDbContext.cs

```csharp
// src/Infrastructure/Persistence/ApplicationDbContext.cs
namespace FinancialApi.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using FinancialApi.Application.Common.Interfaces;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

## 3. Logging

### LoggingConfiguration.cs

```csharp
// src/Infrastructure/Logging/LoggingConfiguration.cs
namespace FinancialApi.Infrastructure.Logging;

using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;

public static class LoggingConfiguration
{
    public static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "FinancialApi")
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Host.UseSerilog();
    }
}
```

## 4. Dependency Injection

```csharp
// src/Infrastructure/DependencyInjection.cs
namespace FinancialApi.Infrastructure;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using FinancialApi.Application.Common.Interfaces;
using FinancialApi.Infrastructure.Auth;
using FinancialApi.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

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
                options.RequireHttpsMetadata = false; // Dev only
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidateLifetime = true
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAuthenticated", policy => policy.RequireAuthenticatedUser())
            .AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));

        return services;
    }
}
```

## 5. Migrations

### Criar Migration

```bash
cd src/Infrastructure

dotnet ef migrations add InitialCreate \
    --startup-project ../Api \
    --output-dir Persistence/Migrations
```

### Aplicar em Todos os Bancos

```bash
# Brasil
dotnet ef database update --startup-project ../Api \
    --connection "Host=localhost;Port=5432;Database=latam_br;Username=postgres;Password=dev123"

# Argentina
dotnet ef database update --startup-project ../Api \
    --connection "Host=localhost;Port=5432;Database=latam_ar;Username=postgres;Password=dev123"

# Chile
dotnet ef database update --startup-project ../Api \
    --connection "Host=localhost;Port=5432;Database=latam_cl;Username=postgres;Password=dev123"
```

---

**Próximo:** [API Layer](06-api-layer.md)
