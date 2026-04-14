# 🔧 Application Layer

## Objetivo

Define as interfaces (ports) que a Infrastructure implementa. Neste momento não contém casos de uso de negócio — apenas os contratos necessários para autenticação, multi-tenancy e acesso ao banco.

## Estrutura de Pastas

```
src/Application/
├── Common/
│   └── Interfaces/
│       ├── IApplicationDbContext.cs
│       ├── ITenantResolver.cs
│       └── ICurrentUserService.cs
├── DependencyInjection.cs
└── Application.csproj
```

## 1. Interfaces

### IApplicationDbContext.cs

```csharp
// src/Application/Common/Interfaces/IApplicationDbContext.cs
namespace FinancialApi.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### ITenantResolver.cs

```csharp
// src/Application/Common/Interfaces/ITenantResolver.cs
namespace FinancialApi.Application.Common.Interfaces;

using FinancialApi.Domain.ValueObjects;

public interface ITenantResolver
{
    Tenant GetCurrentTenant();
}
```

### ICurrentUserService.cs

```csharp
// src/Application/Common/Interfaces/ICurrentUserService.cs
namespace FinancialApi.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    string? Name { get; }
    string? TenantCode { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
}
```

## 2. Dependency Injection

```csharp
// src/Application/DependencyInjection.cs
namespace FinancialApi.Application;

using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
```

## Princípios

- ✅ Apenas interfaces — sem implementações
- ❌ Sem referência a EF Core, HttpContext ou qualquer framework
- ❌ Sem regras de negócio nesta fase

---

**Próximo:** [Infrastructure Layer](05-infrastructure-layer.md)
