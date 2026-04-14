# 🎯 Domain Layer

## Objetivo

Camada mais interna da Clean Architecture. Neste momento contém apenas a base estrutural — sem entidades de negócio. Entidades de negócio (Transaction, Ledger, etc.) serão adicionadas futuramente.

## Estrutura de Pastas

```
src/Domain/
├── Common/
│   └── Entity.cs
├── ValueObjects/
│   └── Tenant.cs
├── Exceptions/
│   └── DomainException.cs
└── Domain.csproj
```

## 1. Base Entity

```csharp
// src/Domain/Common/Entity.cs
namespace FinancialApi.Domain.Common;

public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;

    protected Entity() { }

    protected Entity(TId id)
    {
        Id = id;
    }
}
```

## 2. Tenant Value Object

```csharp
// src/Domain/ValueObjects/Tenant.cs
namespace FinancialApi.Domain.ValueObjects;

public sealed class Tenant
{
    public string Code { get; }

    private static readonly string[] ValidTenants = ["BR", "AR", "CL"];

    private Tenant(string code)
    {
        Code = code;
    }

    public static Tenant Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Tenant code cannot be empty", nameof(code));

        var upperCode = code.ToUpperInvariant();

        if (!ValidTenants.Contains(upperCode))
            throw new ArgumentException($"Invalid tenant: {code}", nameof(code));

        return new Tenant(upperCode);
    }

    public override string ToString() => Code;

    public static Tenant BR => Create("BR");
    public static Tenant AR => Create("AR");
    public static Tenant CL => Create("CL");
}
```

## 3. Domain Exception

```csharp
// src/Domain/Exceptions/DomainException.cs
namespace FinancialApi.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
```

## 4. Princípios

- ❌ NUNCA atributos de framework (`[Key]`, `[Required]`, etc.)
- ❌ NUNCA dependências externas — apenas .NET puro
- ✅ Setters privados/protected
- ✅ Factory Methods para criação com validação

---

**Próximo:** [Application Layer](04-application-layer.md)
