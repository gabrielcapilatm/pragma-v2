# 🏗️ Arquitetura

## Clean Architecture - Visão Geral

```
┌─────────────────────────────────────────┐
│           API (Controllers)             │
│  - Endpoints                            │
│  - Middlewares                          │
│  - DTOs                                 │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│        Application (Use Cases)          │
│  - Business Logic                       │
│  - Orchestration                        │
│  - Interfaces (Ports)                   │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│            Domain (Core)                │
│  - Entities                             │
│  - Value Objects                        │
│  - Domain Rules                         │
│  - NO Framework Dependencies            │
└─────────────────────────────────────────┘
                  ▲
                  │
┌─────────────────┴───────────────────────┐
│      Infrastructure (External)          │
│  - EF Core / Database                   │
│  - Keycloak Integration                 │
│  - Multi-tenancy                        │
└─────────────────────────────────────────┘
```

## Regras de Dependência

- ❌ Domain **NUNCA** depende de nada (puro .NET)
- ❌ Application **NÃO** conhece Infrastructure
- ✅ Infrastructure implementa interfaces do Application
- ✅ API depende de Application e Infrastructure
- ✅ Dependências apontam sempre para dentro

## Camadas Detalhadas

### 1. Domain (Núcleo)

**Responsabilidades:**
- Definir entidades e value objects
- Conter regras de negócio puras
- Definir exceções de domínio

**Não pode:**
- Ter dependências externas
- Conhecer banco de dados
- Conhecer frameworks

**Exemplo:**
```csharp
// ✅ BOM - Domain puro
public class User
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; }
    
    public static User Create(Email email) { }
}

// ❌ RUIM - Conhecimento de EF
public class User
{
    [Key]
    public Guid Id { get; set; }
}
```

### 2. Application (Casos de Uso)

**Responsabilidades:**
- Orquestrar fluxos de negócio
- Definir interfaces (ports)
- Implementar queries e commands (CQRS)

**Não pode:**
- Implementar detalhes de infraestrutura
- Conhecer EF Core diretamente
- Fazer HTTP calls diretamente

**Exemplo:**
```csharp
// ✅ Interface no Application
public interface IUserRepository
{
    Task<User> GetByIdAsync(Guid id);
}

// ✅ Use Case no Application
public class GetUserHandler
{
    private readonly IUserRepository _repository;
}
```

### 3. Infrastructure (Implementação)

**Responsabilidades:**
- Implementar interfaces do Application
- EF Core e banco de dados
- Integrações externas
- Multi-tenancy

**Exemplo:**
```csharp
// ✅ Implementação no Infrastructure
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    
    public async Task<User> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }
}
```

### 4. API (Apresentação)

**Responsabilidades:**
- Expor endpoints HTTP
- Validar requests
- Transformar Domain → DTO
- Middlewares

**Exemplo:**
```csharp
// ✅ Controller usando DTOs
[HttpGet]
public async Task<CurrentUserDto> GetMe()
{
    var result = await _mediator.Send(new GetCurrentUserQuery());
    return result.Value; // DTO, não Entity
}
```

## Princípios SOLID

### Single Responsibility
Cada classe tem uma única razão para mudar.

```csharp
// ❌ Múltiplas responsabilidades
public class UserService
{
    public void CreateUser() { }
    public void SendEmail() { }
    public void LogActivity() { }
}

// ✅ Responsabilidade única
public class UserService { public void CreateUser() { } }
public class EmailService { public void SendEmail() { } }
public class LogService { public void LogActivity() { } }
```

### Open/Closed
Aberto para extensão, fechado para modificação.

```csharp
// ✅ Extensível sem modificar
public interface ITenantResolver
{
    Tenant GetCurrentTenant();
}

// Diferentes implementações
public class JwtTenantResolver : ITenantResolver { }
public class HeaderTenantResolver : ITenantResolver { }
```

### Dependency Inversion
Dependa de abstrações, não de implementações.

```csharp
// ❌ Depende de implementação
public class UserService
{
    private readonly UserRepository _repo;
}

// ✅ Depende de abstração
public class UserService
{
    private readonly IUserRepository _repo;
}
```

## Multi-Tenancy Strategy

### Database per Country
Cada país possui seu próprio banco de dados isolado.

**Bancos:**
- `latam_br` - Brasil
- `latam_ar` - Argentina
- `latam_cl` - Chile

**Fluxo:**
```
Request → JWT → TenantResolver → ConnectionResolver → DbFactory → DbContext correto
```

**Componentes:**
- **TenantResolver**: Lê tenant do JWT
- **ConnectionResolver**: Resolve connection string por tenant
- **DbFactory**: Cria DbContext dinâmico

## Padrões de Projeto

### Repository Pattern
Abstrai acesso a dados.

```csharp
public interface IUserRepository
{
    Task<User> GetByIdAsync(Guid id);
    Task AddAsync(User user);
}
```

### CQRS (Query/Command Separation)
Separa leitura de escrita.

```csharp
// Query (leitura)
public record GetUserQuery(Guid Id) : IRequest<UserDto>;

// Command (escrita)
public record CreateUserCommand(string Email) : IRequest<Guid>;
```

### MediatR Pattern
Desacopla request de handler.

```csharp
// Controller
var result = await _mediator.Send(new GetUserQuery(id));

// Handler
public class GetUserHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserQuery request, ...)
    {
        // implementação
    }
}
```

## Convenções de Nomenclatura

```csharp
// Classes e Interfaces: PascalCase
public class UserService { }
public interface IUserRepository { }

// Métodos: PascalCase
public async Task<User> GetUserAsync() { }

// Variáveis e parâmetros: camelCase
var currentUser = await GetUserAsync();

// Private fields: _camelCase
private readonly IUserRepository _userRepository;

// Constantes: PascalCase
public const string DefaultTenant = "BR";
```

## DTO vs Entity

### Entity (Domain)
- Contém lógica de negócio
- Vive no Domain layer
- Nunca exposta na API

```csharp
public class User
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; }
    
    public static User Create(Email email) { }
    public void Deactivate() { }
}
```

### DTO (Data Transfer Object)
- Apenas dados
- Usado para comunicação externa
- Sem lógica de negócio

```csharp
public record UserDto(
    Guid Id,
    string Email,
    bool IsActive
);
```

### Regra de Ouro

```csharp
// ❌ NUNCA
[HttpGet]
public async Task<User> GetUser() // Entity exposta!
{
    return await _context.Users.FirstAsync();
}

// ✅ SEMPRE
[HttpGet]
public async Task<UserDto> GetUser() // DTO
{
    var user = await _context.Users.FirstAsync();
    return new UserDto(user.Id, user.Email.Value, user.IsActive);
}
```

---

**Próximo:** [Setup Inicial](02-setup.md)
