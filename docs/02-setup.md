# 🚀 Setup Inicial

## Pré-requisitos

- .NET 8.0 SDK
- Docker e Docker Compose
- IDE (VS Code, Rider ou Visual Studio)

## 1. Criar Estrutura da Solução

```bash
# Na pasta do projeto
dotnet new sln -n FinancialApi

# Criar projetos (camadas)
dotnet new classlib -n Domain -o src/Domain
dotnet new classlib -n Application -o src/Application
dotnet new classlib -n Infrastructure -o src/Infrastructure
dotnet new webapi -n Api -o src/Api

# Criar projetos de teste
dotnet new xunit -n Domain.Tests -o tests/Domain.Tests
dotnet new xunit -n Application.Tests -o tests/Application.Tests
dotnet new xunit -n Api.Tests -o tests/Api.Tests
```

## 2. Adicionar Projetos à Solução

```bash
dotnet sln add src/Domain/Domain.csproj
dotnet sln add src/Application/Application.csproj
dotnet sln add src/Infrastructure/Infrastructure.csproj
dotnet sln add src/Api/Api.csproj

dotnet sln add tests/Domain.Tests/Domain.Tests.csproj
dotnet sln add tests/Application.Tests/Application.Tests.csproj
dotnet sln add tests/Api.Tests/Api.Tests.csproj
```

## 3. Configurar Referências Entre Projetos

```bash
# Application → Domain
dotnet add src/Application reference src/Domain

# Infrastructure → Application
dotnet add src/Infrastructure reference src/Application

# Api → Application e Infrastructure
dotnet add src/Api reference src/Application
dotnet add src/Api reference src/Infrastructure

# Testes
dotnet add tests/Domain.Tests reference src/Domain
dotnet add tests/Application.Tests reference src/Application
dotnet add tests/Api.Tests reference src/Api
```

## 4. Instalar Packages NuGet

### Infrastructure
```bash
cd src/Infrastructure
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add package Serilog.AspNetCore --version 8.0.0
dotnet add package Serilog.Sinks.Console --version 5.0.0
```

### Api
```bash
cd ../Api
dotnet add package Swashbuckle.AspNetCore --version 6.5.0
```

### Testes
```bash
cd ../../tests/Domain.Tests
dotnet add package FluentAssertions --version 6.12.0

cd ../Application.Tests
dotnet add package FluentAssertions --version 6.12.0
dotnet add package Moq --version 4.20.0

cd ../Api.Tests
dotnet add package FluentAssertions --version 6.12.0
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 8.0.0
dotnet add package Testcontainers.PostgreSql --version 3.7.0
```

## 5. Estrutura Final

```
financial-api/
├── src/
│   ├── Domain/
│   ├── Application/
│   ├── Infrastructure/
│   └── Api/
├── tests/
│   ├── Domain.Tests/
│   ├── Application.Tests/
│   └── Api.Tests/
└── FinancialApi.sln
```

## 6. Verificar Setup

```bash
dotnet build
# Deve terminar com: Build succeeded.
```

## 7. Instalar Ferramentas Globais

```bash
dotnet tool install --global dotnet-ef
dotnet ef --version
```

---

**Próximo:** [Domain Layer](03-domain-layer.md)
