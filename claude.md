# 🧠 Financial API V2 - API Modelo

## 📋 Visão Geral

API modelo baseada em **Clean Architecture** que serve como template para todos os serviços da plataforma LATAM. Esta primeira versão foca em validar a **estrutura técnica**, não as regras de negócio finais.

### O que esta API valida
- ✅ Clean Architecture (Domain, Application, Infrastructure, API)
- ✅ Multi-tenancy (Database per Country: BR, AR, CL)
- ✅ Autenticação e Autorização via Keycloak (JWT com tenant)
- ✅ EF Core configurado e migrations funcionando
- ✅ Middleware pipeline completo
- ✅ Logs estruturados
- ✅ Testes (Unit + Integration)

### O que esta API NÃO contém (ainda)
- ❌ Entidades de negócio (Transaction, Ledger, etc.)
- ❌ Regras de negócio
- ❌ Redis / Cache
- ❌ MediatR / CQRS

### Stack Tecnológica
- .NET 8.0
- ASP.NET Core Web API
- Entity Framework Core 8.0
- PostgreSQL 15 (Aurora RDS em produção)
- Keycloak 23.0
- Serilog
- xUnit + FluentAssertions

## 🎯 Critério de Sucesso

A API estará pronta quando:
- ✅ Subir localmente conectada ao PostgreSQL via Docker
- ✅ Keycloak funcionando e emitindo JWTs com tenant
- ✅ Migrations aplicáveis nos 3 bancos (BR, AR, CL)
- ✅ `GET /health` funcionando sem autenticação
- ✅ `GET /auth/me` retornando claims do JWT autenticado
- ✅ Multi-tenancy resolvendo banco correto por tenant
- ✅ Middleware pipeline executando na ordem correta
- ✅ Logs estruturados com correlation ID

## 📚 Documentação

### Setup e Arquitetura
1. **[Arquitetura](docs/01-architecture.md)** - Clean Architecture, camadas e princípios
2. **[Setup Inicial](docs/02-setup.md)** - Criar solução, projetos e configurar dependências

### Implementação por Camada
3. **[Domain Layer](docs/03-domain-layer.md)** - Base entities, Tenant value object
4. **[Application Layer](docs/04-application-layer.md)** - Interfaces (ports)
5. **[Infrastructure Layer](docs/05-infrastructure-layer.md)** - EF Core, Multi-tenancy, Auth, Logs
6. **[API Layer](docs/06-api-layer.md)** - Controllers, Middleware Pipeline, Program.cs

### Configuração e Validação
7. **[Keycloak](docs/07-keycloak.md)** - Configuração completa do Identity Provider
8. **[Testes](docs/08-testing.md)** - Unit e Integration tests
9. **[Checklist](docs/09-checklist.md)** - Validação final e troubleshooting

## 🚀 Quick Start

```bash
# 1. Subir ambiente Docker
docker-compose up -d

# 2. Criar solução .NET
# Seguir: docs/02-setup.md

# 3. Configurar Keycloak
# Seguir: docs/07-keycloak.md

# 4. Aplicar migrations
./scripts/apply-migrations.sh

# 5. Rodar API
cd src/Api
dotnet run

# 6. Testar
curl http://localhost:5000/api/health
```

## 📁 Estrutura do Projeto

```
financial-api/
├── src/
│   ├── Domain/         # Base Entity, Tenant value object
│   ├── Application/    # Interfaces (ports)
│   ├── Infrastructure/ # EF Core, Auth, Multi-tenancy, Logs
│   └── Api/            # Controllers, Middleware Pipeline
├── tests/
│   ├── Domain.Tests/
│   ├── Application.Tests/
│   └── Api.Tests/
├── docker/
│   ├── docker-compose.yml
│   └── init-databases.sql
└── docs/
```

## ⚠️ Regras Importantes

### Domain Layer
- ❌ NUNCA usar atributos do EF Core (`[Key]`, `[Required]`, etc)
- ❌ NUNCA conhecer Infrastructure ou frameworks
- ✅ Apenas .NET puro

### DTO vs Entity
- ❌ NUNCA expor Entities diretamente na API
- ✅ Sempre usar DTOs para comunicação externa

### EF Core
- ❌ NUNCA usar fora da Infrastructure layer
- ✅ Isolado via interface IApplicationDbContext

### Multi-Tenancy
- ❌ NUNCA fazer queries sem contexto de tenant
- ✅ Tenant sempre resolvido do JWT

## 📖 Próximos Passos

1. Leia [Arquitetura](docs/01-architecture.md) para entender os conceitos
2. Siga [Setup Inicial](docs/02-setup.md) para criar a estrutura
3. Implemente camada por camada seguindo os guias
4. Configure [Keycloak](docs/07-keycloak.md)
5. Execute [Checklist](docs/09-checklist.md) de validação

---

**💡 Dica**: Siga a documentação na ordem numerada. Cada documento assume que o anterior foi completado.
