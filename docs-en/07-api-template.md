# API Template — Initial Version

**Objective:** Define the base structure for the LATAM Platform V2 reference API, ensuring the solution is born with all key technical pillars validated: authentication, multi-tenancy, persistence, and layered architecture.

> The focus of this stage is **not business logic** — it is validating the end-to-end foundation of the application.

---

## Decisions

### API Architecture

The API follows **Clean Architecture** with four layers:

| Layer | Responsibility |
|-------|---------------|
| **Domain** | Entities, value objects, pure business rules — no framework dependencies |
| **Application** | Use cases and contracts (ports/interfaces) |
| **API** | Controllers, middleware, HTTP contracts, DTOs |
| **Infrastructure** | EF Core, Keycloak auth, Serilog, external integrations |

**Key constraints:**
- Domain layer has zero dependencies on any framework
- EF Core is strictly confined to the Infrastructure layer
- DTOs are always used for external communication — entities are never exposed

### API Strategy

A **single API** with domain separation within the application:
- Modules: Auth, Conciliation, Settlement (and others as needed)
- No distributed microservices at this stage
- Module boundaries are respected within the same codebase

### Multi-tenancy

| Aspect | Decision |
|--------|----------|
| Strategy | Database per country |
| Databases | DB_BR, DB_AR, DB_CL, ... (extensible via config) |
| Tenant source | JWT token claim `tenant` |
| Tenant via header | Not used — JWT is the single source of truth |
| Adding a country | Add connection string + create DB + run migrations |

### Authentication

| Aspect | Decision |
|--------|----------|
| Type | Centralized |
| User duplication | None — a single user account works across all countries |
| Country selection | User selects country at login; JWT carries the active tenant |
| Token validation | API validates the JWT and extracts the tenant |
| Identity provider | Keycloak (realm: `latam-platform`, client: `latam-api`) |
| Auth flow | Authorization Code + PKCE (frontend) |

### Persistence

| Aspect | Decision |
|--------|----------|
| ORM | Entity Framework Core |
| Schema evolution | Migrations (mandatory, version-controlled) |
| DbContext creation | Dynamic, based on tenant extracted from JWT |
| Business logic in DbContext | Prohibited |

### Multi-tenant Components

Three components work together to route every request to the correct database:

```
HTTP Request
     │
     ▼
TenantResolver          → reads tenant claim from JWT
     │
     ▼
ConnectionResolver      → maps tenant code to connection string
     │
     ▼
ApplicationDbContext    → instantiated with the resolved connection string
```

---

## Initial Endpoints

| Endpoint | Auth required | Description |
|----------|--------------|-------------|
| `GET /api/health` | No | Returns API status |
| `GET /api/auth/me` | Yes | Returns the authenticated user's JWT claims |
| `GET /api/products` | Yes | Returns tenant-specific product data (demo) |

---

## Definition of Done (Template Functional)

The template is considered complete when all of the following pass:

- [ ] API starts locally without errors
- [ ] Database connectivity confirmed
- [ ] Authentication working — valid JWT accepted
- [ ] Tenant resolved correctly from JWT
- [ ] Dynamic database connection working per tenant
- [ ] Migrations applied successfully on all country databases (BR, AR, CL)
- [ ] `GET /api/health` returns 200 without authentication
- [ ] `GET /api/auth/me` returns claims for an authenticated user
- [ ] README documents the local setup

---

## Code Standards

| Rule | Detail |
|------|--------|
| EF Core location | Infrastructure layer only |
| Domain purity | No framework attributes or dependencies |
| Data transfer | Always use DTOs — never expose entities via API |
| Migrations | Mandatory for every schema change |
| DbContext | No business logic |
| Tenant | Always extracted from JWT — never from headers, body, or query string |

---

## Project Structure

```
financial-api/
├── src/
│   ├── Domain/
│   │   ├── Entities/
│   │   │   └── Product.cs
│   │   └── ValueObjects/
│   │       └── Tenant.cs
│   │
│   ├── Application/
│   │   └── Interfaces/
│   │       ├── IApplicationDbContext.cs
│   │       └── ICurrentUserService.cs
│   │
│   ├── Infrastructure/
│   │   ├── Auth/
│   │   │   ├── KeycloakClaimsTransformation.cs
│   │   │   └── CurrentUserService.cs
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── DesignTimeDbContextFactory.cs
│   │   │   ├── ConnectionStringResolver.cs
│   │   │   └── Configurations/
│   │   │       └── ProductConfiguration.cs
│   │   ├── Logging/
│   │   │   └── LoggingConfiguration.cs
│   │   └── DependencyInjection.cs
│   │
│   └── Api/
│       ├── Controllers/
│       │   ├── HealthController.cs
│       │   ├── AuthController.cs
│       │   └── ProductsController.cs
│       ├── Middlewares/
│       │   ├── CorrelationIdMiddleware.cs
│       │   └── ErrorHandlingMiddleware.cs
│       ├── OpenApi/
│       │   ├── BearerSecuritySchemeTransformer.cs
│       │   └── SecurityRequirementOperationTransformer.cs
│       ├── Extensions/
│       │   ├── WebApplicationBuilderExtensions.cs
│       │   └── WebApplicationExtensions.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Program.cs
│
├── tests/
│   ├── Domain.Tests/
│   ├── Application.Tests/
│   └── Api.Tests/
│
├── docker/
│   ├── docker-compose.yml
│   └── init-databases.sql
│
└── docs/
```

---

## Local Setup

```bash
# 1. Start the Docker environment (PostgreSQL + Keycloak)
docker-compose up -d

# 2. Apply migrations to all country databases
dotnet ef database update --project src/Infrastructure --startup-project src/Api \
  -- --ConnectionStrings:BR "Host=localhost;Database=financial_br;Username=postgres;Password=postgres"

# 3. Run the API
dotnet run --project src/Api

# 4. Open API docs
# http://localhost:5288/scalar/v1

# 5. Serve the frontend (optional demo)
docker run --rm -p 3000:80 -v $(pwd)/front:/usr/share/nginx/html:ro nginx:alpine
# http://localhost:3000
```

---

## Middleware Pipeline Order

```
UseCors("Frontend")
UseMiddleware<CorrelationIdMiddleware>
UseMiddleware<ErrorHandlingMiddleware>
UseSerilogRequestLogging
UseAuthentication
UseAuthorization
MapControllers
```

---

## Keycloak Configuration (Local)

| Setting | Value |
|---------|-------|
| Realm | `latam-platform` |
| Client ID | `latam-api` |
| Client type | Public (no secret) |
| Valid redirect URIs | `http://localhost:3000` |
| Web origins | `http://localhost:3000` |
| Custom claim | `tenant` — set via Protocol Mapper on user attributes |

**Test users (local):**

| Username | Password | Tenant |
|----------|----------|--------|
| `admin.br` | `admin123` | `BR` |
| `admin.ar` | `admin123` | `AR` |
| `admin.cl` | `admin123` | `CL` |
