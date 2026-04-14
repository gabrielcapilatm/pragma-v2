# 🌐 API Layer

## Objetivo

Expor endpoints HTTP e configurar o middleware pipeline completo.

## Estrutura de Pastas

```
src/Api/
├── Controllers/
│   ├── HealthController.cs
│   └── AuthController.cs
├── Middlewares/
│   ├── CorrelationIdMiddleware.cs
│   └── ErrorHandlingMiddleware.cs
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
└── Api.csproj
```

## 1. Middleware Pipeline

O pipeline segue a ordem definida na arquitetura:

```
Request
  │
  ▼
CorrelationId     → gera/propaga ID de correlação
  │
  ▼
ErrorHandler      → captura exceções globais
  │
  ▼
Serilog           → loga request/response
  │
  ▼
Authentication    → valida JWT (Keycloak)
  │
  ▼
TenantResolver    → extrai tenant do JWT, define banco
  │
  ▼
Authorization     → verifica permissões/roles
  │
  ▼
Controller
```

## 2. Middlewares

### CorrelationIdMiddleware.cs

```csharp
// src/Api/Middlewares/CorrelationIdMiddleware.cs
namespace FinancialApi.Api.Middlewares;

using Serilog.Context;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
```

### ErrorHandlingMiddleware.cs

```csharp
// src/Api/Middlewares/ErrorHandlingMiddleware.cs
namespace FinancialApi.Api.Middlewares;

using System.Net;
using System.Text.Json;
using FinancialApi.Domain.Exceptions;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception");

        var statusCode = exception switch
        {
            DomainException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            InvalidOperationException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        var response = new
        {
            error = exception.Message,
            type = exception.GetType().Name,
            timestamp = DateTime.UtcNow
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

## 3. Controllers

### HealthController.cs

```csharp
// src/Api/Controllers/HealthController.cs
namespace FinancialApi.Api.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }
}
```

### AuthController.cs

```csharp
// src/Api/Controllers/AuthController.cs
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
```

## 4. Program.cs

```csharp
// src/Api/Program.cs
using FinancialApi.Application;
using FinancialApi.Infrastructure;
using FinancialApi.Infrastructure.Logging;
using FinancialApi.Api.Middlewares;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging
LoggingConfiguration.ConfigureSerilog(builder);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
```

## 5. Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "BR": "Host=localhost;Port=5432;Database=latam_br;Username=postgres;Password=dev123",
    "AR": "Host=localhost;Port=5432;Database=latam_ar;Username=postgres;Password=dev123",
    "CL": "Host=localhost;Port=5432;Database=latam_cl;Username=postgres;Password=dev123"
  },
  "Keycloak": {
    "Authority": "http://localhost:8080",
    "Realm": "latam-platform",
    "ClientId": "latam-api"
  }
}
```

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

## 6. Testar

```bash
# Health (sem autenticação)
curl http://localhost:5000/api/health

# Me (com token)
TOKEN="eyJhbGciOi..."
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/auth/me
```

---

**Próximo:** [Keycloak Setup](07-keycloak.md)
