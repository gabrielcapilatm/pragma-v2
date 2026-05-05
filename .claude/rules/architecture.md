# Clean Architecture — financial-api

Regras de arquitetura para o projeto `financial-api`. Violações devem ser apontadas e corrigidas.

## Namespace Pattern

Todos os namespaces seguem o padrão `FinancialApi.<Layer>[.<SubFolder>]`:
- `FinancialApi.Domain`
- `FinancialApi.Application`
- `FinancialApi.Infrastructure`
- `FinancialApi.Api`

## Restrições por Camada

### Domain
- NUNCA referenciar `Infrastructure`, `Application`, ou qualquer framework externo.
- Apenas .NET puro — sem atributos do EF Core (`[Key]`, `[Required]`, `[Column]`, etc.).
- Entidades herdam de `BaseEntity`. Value objects são imutáveis (record ou struct).
- Sem dependências externas via NuGet (exceto pacotes de abstração puros).

### Application
- NUNCA referenciar `Infrastructure` diretamente.
- Depende apenas de `Domain` e abstrações (interfaces/ports).
- Define as interfaces que `Infrastructure` implementa (ex: `IApplicationDbContext`).
- Sem acesso direto a EF Core, banco de dados, ou serviços de infraestrutura.

### Infrastructure
- Único lugar onde EF Core pode ser usado.
- Implementa as interfaces definidas em `Application`.
- Multi-tenancy: o contexto de tenant **sempre** é resolvido do JWT — nunca hardcoded ou por query string.
- Migrations ficam em `Infrastructure/Persistence/Migrations/`.

### Api
- Nunca expor entidades do `Domain` diretamente em responses — sempre usar DTOs.
- Controllers são finos: sem lógica de negócio.
- Middleware pipeline segue a ordem definida em `WebApplicationExtensions`.
- Tenant resolvido via claim do JWT antes de qualquer query.

## Multi-Tenancy
- Bancos por país: `BR`, `AR`, `CL`.
- O tenant é sempre derivado do JWT claim — nunca de outra fonte.
- Queries sem contexto de tenant são proibidas.
