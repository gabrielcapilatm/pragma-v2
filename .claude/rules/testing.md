---
paths:
  - "tests/**/*.cs"
---

# Testing Conventions — financial-api

Aplica apenas a arquivos em `tests/**/*.cs`.

## Stack

- **xUnit** para estrutura de testes.
- **FluentAssertions** para assertivas — nunca `Assert.True(x == y)` ou `Assert.Equal` quando FluentAssertions oferece alternativa legível.
- **Moq** disponível em `Application.Tests` (apenas para mocks de interfaces de Application).
- **Testcontainers.PostgreSql** disponível em `Api.Tests` para integration tests com banco real.

## Nomenclatura

Padrão obrigatório para nomes de métodos de teste:

```
MethodName_Scenario_ExpectedResult
```

Exemplos:
- `GetById_WhenIdNotFound_ReturnsNull`
- `Resolve_WhenTenantClaimMissing_ThrowsUnauthorizedException`
- `ApplyMigrations_WhenDatabaseEmpty_CreatesAllTables`

## Proibições

- **Domain.Tests**: sem Moq — testes de domínio testam lógica pura sem mocks.
- Nunca usar `Assert.True(x == y)` — preferir `x.Should().Be(y)`.
- Nunca mockar banco de dados em `Api.Tests` — usar Testcontainers com PostgreSQL real.
- Sem lógica de negócio nos testes — setup via builders ou fixtures.

## Estrutura

Cada projeto de teste espelha a estrutura do projeto testado:
```
tests/Domain.Tests/
  Entities/
  ValueObjects/
tests/Application.Tests/
  UseCases/
tests/Api.Tests/
  Controllers/
  Integration/
```
