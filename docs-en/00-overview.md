# LATAM Platform V2 — Architecture & Engineering Overview

> **Audience:** Engineering leads, product managers, new team members, and technical stakeholders.
> This document consolidates all architectural decisions, engineering standards, and infrastructure choices made for the first version of the LATAM Platform V2.

---

## 1. Context and Motivation

LATAM Platform V2 is being built from scratch with a clear mandate: **get the technical foundation right before adding business logic**. The first deliverable is a reference API template that all future services on the platform will be based on.

### What this first version validates

| Area | Status |
|------|--------|
| Clean Architecture (layered, dependency-inverted) | Validated |
| Multi-tenancy — database per country (BR, AR, CL, ...) | Validated |
| Centralized authentication via Keycloak (JWT + tenant claim) | Validated |
| EF Core with migrations across all country databases | Validated |
| Middleware pipeline (correlation ID, error handling, structured logging) | Validated |
| Minimum test suite (unit + integration skeleton) | Validated |

### What is explicitly out of scope (for now)

- Business domain entities (Transaction, Ledger, Settlement, etc.)
- Business rules
- Redis / distributed cache
- MediatR / CQRS pattern
- Read/write separation

### Strategic rationale

Given the team size and the need to validate the technical foundation quickly, the platform starts with **a single modular API** rather than distributed microservices. This reduces operational complexity, avoids premature coupling between services, and makes it easier to standardize the cross-cutting concerns (auth, persistence, observability) before splitting.

The modular approach respects domain boundaries (Auth, Conciliation, Settlement) within the same codebase, allowing each module to evolve independently — and to be extracted into its own service if and when scale demands it.

---

## 2. System Architecture

### High-level topology

The system has two distinct flows: the authentication flow (login) and the API request flow (subsequent calls).

```
── AUTHENTICATION FLOW (login) ──────────────────────────────────

  [Browser]  ──── redirect ────►  [Keycloak]
                                       │  issues JWT with
                                       │  tenant claim
  [Browser]  ◄─── JWT token ──────────┘


── API REQUEST FLOW (after login) ───────────────────────────────

  [Browser]
      │  Authorization: Bearer <JWT>
      ▼
  [Reverse Proxy / Load Balancer]
      │
      ▼
  [financial-api]
      │  1. validates JWT signature locally
      │     (public key fetched once from Keycloak JWKS endpoint)
      │  2. extracts tenant claim from token
      │
      ▼
  [Aurora RDS — PostgreSQL per country]
      ├── DB_BR  (Brazil)
      ├── DB_AR  (Argentina)
      └── DB_CL  (Chile)
```

> The API does **not** call Keycloak on every request. It fetches Keycloak's public keys (JWKS) once at startup and validates JWT signatures locally. Keycloak is only contacted directly by the browser during the login flow.

Authentication is **centralized** and completely separate from the transactional databases. A single Keycloak realm (`latam-platform`) issues JWT tokens that carry a `tenant` claim identifying the user's country. The API reads that claim on every request to route data access to the correct country database.

### Clean Architecture layers

```
┌─────────────────────────────────────┐
│             API Layer               │  Controllers, Middleware, DTOs, OpenAPI
├─────────────────────────────────────┤
│         Application Layer           │  Use cases, interfaces (ports)
├─────────────────────────────────────┤
│           Domain Layer              │  Entities, Value Objects, domain rules
├─────────────────────────────────────┤
│       Infrastructure Layer          │  EF Core, Keycloak auth, Serilog, DB
└─────────────────────────────────────┘
```

**Dependency rule:** arrows always point inward. Infrastructure implements interfaces defined by Application. Domain has zero dependencies on any framework.

---

## 3. Multi-Tenancy

### Strategy: Database per Country

Each country gets its own isolated PostgreSQL database. There is no shared schema between countries.

| Aspect | Decision |
|--------|----------|
| Isolation level | Full database isolation per country |
| Tenant identifier | Country code extracted from JWT claim `tenant` |
| Tenant resolution | `TenantResolver` reads JWT → `ConnectionResolver` maps to connection string → `DbContext` created dynamically |
| Tenant in headers | Not used — JWT is the single source of truth |
| Adding a new country | Add connection string to config + create database + run migrations |

### Tenant resolution flow

```
HTTP Request
    │
    ▼
JWT Bearer Token
    │  contains claim: tenant = "BR"
    ▼
TenantResolver
    │  extracts tenant code
    ▼
ConnectionResolver
    │  reads ConnectionStrings:BR from appsettings
    ▼
ApplicationDbContext (scoped, per-request)
    │  connected to the correct country DB
    ▼
Query executed
```

**Rule:** No database operation may occur without a resolved tenant context. Any attempt to resolve a connection string for an unknown tenant throws an explicit exception.

---

## 4. Authentication & Authorization

### Keycloak as the Identity Provider

| Setting | Value |
|---------|-------|
| Realm | `latam-platform` |
| Client | `latam-api` (public, no client secret) |
| Flow | Authorization Code + PKCE (frontend) |
| Token format | JWT (RS256) |
| Tenant claim | Custom claim `tenant` on JWT payload |

### How it works end-to-end

1. The user opens the frontend and clicks **Sign in**.
2. The browser is redirected to Keycloak's login page via Authorization Code + PKCE.
3. After successful authentication, Keycloak redirects back with an authorization code.
4. The frontend exchanges the code for an access token (never exposes the code verifier to the server).
5. Every API request includes the JWT as `Authorization: Bearer <token>`.
6. The API validates the signature, expiry, and issuer, then extracts `tenant` and `roles` claims.

### Why Authorization Code + PKCE (not username/password flow)

- ROPC (Resource Owner Password Credentials) is deprecated in OAuth 2.1.
- Authorization Code + PKCE is the current industry standard for browser-based apps.
- The user's credentials never touch the frontend code.
- Keycloak manages session, MFA, and SSO transparently.

### JWT claim mapping

| JWT claim | Mapped to |
|-----------|-----------|
| `sub` | User ID |
| `preferred_username` | Username |
| `tenant` | Tenant code (country) |
| `realm_access.roles` | Roles (extracted via `IClaimsTransformation`) |

---

## 5. Persistence

### Technology choices

| Concern | Choice | Rationale |
|---------|--------|-----------|
| Primary database | Aurora RDS (PostgreSQL) | Managed, scalable, ACID-compliant |
| ORM | Entity Framework Core | .NET standard, migration support, Clean Arch compatible |
| Schema evolution | EF Migrations | Code-first, version-controlled, deterministic |
| NoSQL | TBD | Only when there is a clear technical or business need (logs, notifications, immutable documents) |

### EF Core usage rules

- EF Core is **only allowed in the Infrastructure layer**. It must never leak into Domain or Application.
- `DbContext` must be simple: no business logic, only configuration and access.
- Entity mappings use `IEntityTypeConfiguration<T>` classes, never data annotations on domain entities.
- All schema changes go through migrations. Manual schema edits are prohibited.
- Applied migrations must never be modified; create a new migration to correct mistakes.
- `AsNoTracking()` must be used on all read-only queries.
- N+1 queries must be avoided by using `Include()` or projections.

### Multi-tenant DbContext

The `ApplicationDbContext` is created per-request with the connection string dynamically resolved from the JWT's `tenant` claim. A `DesignTimeDbContextFactory` exists for running `dotnet ef` commands at design time (uses a default connection string, not a JWT).

---

## 6. Infrastructure & Hosting

### Cloud: AWS

| Resource | Service |
|----------|---------|
| Container orchestration | ECS (Elastic Container Service) |
| Container registry | ECR (Elastic Container Registry) |
| Database | Aurora RDS (PostgreSQL) |
| Secrets | AWS Secrets Manager |
| Configuration | AWS Parameter Store |
| Encryption | AWS KMS |

### Local development

Docker Compose brings up the full local environment:
- PostgreSQL instances (one per country)
- Keycloak
- A minimal frontend served by nginx

```bash
docker-compose up -d
dotnet run --project src/Api
```

### Secrets management

| Context | Storage |
|---------|---------|
| Local development | `appsettings.Development.json` (not committed) |
| CI/CD pipeline | GitHub Actions secrets |
| Staging / Production | AWS Secrets Manager + Parameter Store |

---

## 7. CI/CD

### Pipeline strategy

Each repository does **not** maintain an independent pipeline definition. Instead, repositories reference a **shared pipeline template**. This ensures:
- Consistent build, test, and deploy steps across all services
- Centralized maintenance of pipeline standards
- Easier rollout of pipeline improvements

### Deployment flow

```
feat/PRAG-xxxx  ──PR──►  staging  ──PR──►  master (production)
```

| Stage | Trigger | Approval |
|-------|---------|----------|
| Staging | Automatic on PR merge | None (automated) |
| Production | PR merge from staging → master | None at this stage (to be revisited) |

### Pipeline steps

1. Restore dependencies
2. Build
3. Run unit tests (visibility only — pipeline does not fail on test failure initially)
4. Publish test coverage report
5. Build Docker image
6. Push to ECR
7. Deploy to ECS

### Test coverage

A minimum coverage threshold is being established from day one to build a quality culture. The exact percentage will be refined, but coverage will be tracked from the initial phase even before it becomes a hard gate.

---

## 8. Repository Structure

### Strategy: Multi-repo

One repository per application/product. This gives teams autonomy, avoids dependency conflicts between products evolving in parallel, and matches the team's current experience level.

### Repository naming

| Type | Pattern | Examples |
|------|---------|---------|
| API | `{product}-api` | `financial-api`, `conciliation-api`, `settlement-api` |
| Frontend | `{product}-front` | `financial-front`, `conciliation-front` |
| Shared lib | Descriptive name | `core` |
| Infrastructure | `infrastructure` | Docker Compose, Terraform/CDK, DB configs |

### Current repositories

```
github.com/latam-platform/
├── financial-api/       ← API template (.NET)
├── financial-front/     ← Frontend (Next.js, future)
└── infrastructure/      ← Docker Compose + IaC
```

### Branching model

```
master   (production)
  ▲
  │ PR
staging  (QA / staging)
  ▲
  │ PR
feat/PRAG-1234  (development)
```

| Branch | Direct commits | PR required | Approvers |
|--------|----------------|-------------|-----------|
| `master` | Blocked | Yes | 1 (any) |
| `staging` | Blocked | Yes | 1 (any) |
| `feat/*` | Allowed | — | — |
| `hotfix/*` | Allowed | — | — |

### Commit convention

Format: `<type>(<scope>): <description>`

| Type | Purpose |
|------|---------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `refactor` | Code change with no behavior change |
| `test` | Test additions or modifications |
| `chore` | Maintenance tasks |
| `perf` | Performance improvement |
| `ci` | CI/CD changes |

Branch names must always include the Jira/Azure DevOps card ID: `feat/PRAG-1234`.

---

## 9. Observability

### Structured logging with Serilog

All API logs are structured (JSON) and enriched with:
- `CorrelationId` — injected by `CorrelationIdMiddleware`, propagated through the request lifecycle
- `RequestPath`, `StatusCode`, `Elapsed` — from `UseSerilogRequestLogging()`
- Log level overrides configurable per namespace in `appsettings.json`

### Monitoring tooling

The observability stack (metrics, tracing, alerting) has not been finalized yet. The choice will be made at a more appropriate stage of the platform's evolution.

---

## 10. Code Standards & Non-Negotiable Rules

### Domain Layer
- No EF Core attributes (`[Key]`, `[Required]`, etc.) on domain entities
- No dependency on any external framework
- Only pure .NET

### API Layer
- Never expose domain entities directly; always use DTOs
- All protected endpoints require `[Authorize]`

### EF Core / Infrastructure
- EF Core confined to the Infrastructure project
- All DB access goes through `IApplicationDbContext`

### Multi-tenancy
- No query without a tenant context
- Tenant always comes from the JWT — never from headers, query strings, or body

### Pull Request template

```markdown
## Description
<!-- Brief description of the change -->

## Type of change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation

## Checklist
- [ ] Code follows project standards
- [ ] Unit tests added/updated
- [ ] No build warnings
- [ ] Migration created (if applicable)

## Related card
PRAG-XXXX
```

---

## 11. Technology Stack Summary

| Layer | Technology | Version |
|-------|-----------|---------|
| Runtime | .NET | 10 |
| Web framework | ASP.NET Core Web API | 10 |
| ORM | Entity Framework Core | 9+ |
| Database | PostgreSQL (Aurora RDS) | 15 |
| Identity Provider | Keycloak | 23 |
| Logging | Serilog | latest |
| API docs | OpenAPI + Scalar | latest |
| Containers | Docker / ECS | — |
| CI/CD | GitHub Actions | — |
| Cloud | AWS | — |
| Testing | xUnit + FluentAssertions | latest |
