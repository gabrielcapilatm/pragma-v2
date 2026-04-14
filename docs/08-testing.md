# 🧪 Testes

## Objetivo

Testes unitários e de integração para validar a estrutura técnica.

## Estrutura de Testes

```
tests/
├── Domain.Tests/
│   └── ValueObjects/
│       └── TenantTests.cs
├── Application.Tests/
│   └── (a ser expandido com casos de uso futuros)
└── Api.Tests/
    └── Integration/
        ├── CustomWebApplicationFactory.cs
        ├── HealthControllerTests.cs
        └── AuthControllerTests.cs
```

## 1. Unit Tests - Domain

### TenantTests.cs

```csharp
// tests/Domain.Tests/ValueObjects/TenantTests.cs
namespace FinancialApi.Domain.Tests.ValueObjects;

using FluentAssertions;
using FinancialApi.Domain.ValueObjects;
using Xunit;

public class TenantTests
{
    [Theory]
    [InlineData("BR")]
    [InlineData("AR")]
    [InlineData("CL")]
    public void Create_WithValidCode_ShouldCreateTenant(string code)
    {
        var tenant = Tenant.Create(code);
        tenant.Code.Should().Be(code.ToUpperInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("XX")]
    [InlineData("US")]
    public void Create_WithInvalidCode_ShouldThrowException(string code)
    {
        Action act = () => Tenant.Create(code);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithLowerCase_ShouldConvertToUpperCase()
    {
        var tenant = Tenant.Create("br");
        tenant.Code.Should().Be("BR");
    }

    [Fact]
    public void StaticHelpers_ShouldReturnCorrectTenants()
    {
        Tenant.BR.Code.Should().Be("BR");
        Tenant.AR.Code.Should().Be("AR");
        Tenant.CL.Code.Should().Be("CL");
    }
}
```

## 2. Integration Tests - API

### CustomWebApplicationFactory.cs

```csharp
// tests/Api.Tests/Integration/CustomWebApplicationFactory.cs
namespace FinancialApi.Api.Tests.Integration;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using FinancialApi.Infrastructure.Persistence;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("financial_test")
        .WithUsername("postgres")
        .WithPassword("test123")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));
        });
    }

    public async Task InitializeAsync() => await _dbContainer.StartAsync();
    public new async Task DisposeAsync() => await _dbContainer.DisposeAsync();
}
```

### HealthControllerTests.cs

```csharp
// tests/Api.Tests/Integration/HealthControllerTests.cs
namespace FinancialApi.Api.Tests.Integration;

using System.Net;
using FluentAssertions;
using Xunit;

public class HealthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ShouldReturnHealthy()
    {
        var response = await _client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }
}
```

### AuthControllerTests.cs

```csharp
// tests/Api.Tests/Integration/AuthControllerTests.cs
namespace FinancialApi.Api.Tests.Integration;

using System.Net;
using FluentAssertions;
using Xunit;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMe_WithoutToken_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

## 3. Rodar Testes

```bash
# Todos
dotnet test

# Por projeto
dotnet test tests/Domain.Tests
dotnet test tests/Application.Tests
dotnet test tests/Api.Tests

# Verbose
dotnet test --verbosity detailed
```

## 4. Boas Práticas

```csharp
// Naming: [Method]_[Scenario]_[ExpectedResult]
public void Create_WithValidCode_ShouldCreateTenant() { }

// AAA Pattern
[Fact]
public void Test()
{
    // Arrange
    var code = "BR";

    // Act
    var tenant = Tenant.Create(code);

    // Assert
    tenant.Code.Should().Be("BR");
}
```

---

**Próximo:** [Checklist de Validação](09-checklist.md)
