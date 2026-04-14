# V2 Platform — Definitions & Alignments (First Version)

## Context

The goal of this first version of the reference API is to **validate the technical foundation** of the solution — not yet the business rules of the final products. The objective is to have an API that is born with the right architecture, authentication, persistence, minimum tests, and the ability to evolve as a template for all other services on the platform.

This is aligned with the pre-development roadmap, which calls for a working API base structure, validated database connectivity, health check, and basic middleware pipeline.

### Architecture principles already established

The platform's architecture follows a layered separation with Domain, Application, API/Interface, and Infrastructure layers, where:
- Infrastructure implements technical details
- Domain does not depend on any framework
- The standards documentation reinforces that every operation must consider the tenant and that no query should occur without a tenant context

### Single modular API vs. distributed microservices

Given the current stage of the project, the team size, and the need to quickly validate the technical base, the initial approach is a **single modular API** rather than multiple distributed APIs.

**Reasons for this decision:**
- Reduces operational complexity
- Avoids premature coupling between services
- Facilitates standardization of architecture, authentication, persistence, and observability

**How modularity is preserved:**
The API will be structured modularly, respecting domain boundaries (Authentication, Conciliation, Settlement), allowing independent evolution within the same codebase. This guarantees simplicity in the initial phase without sacrificing the ability to extract specific modules into independent services in the future, if and when real scale or decoupling needs arise.

This approach is aligned with the project's principles of prioritizing simplicity, maintainability, and incremental evolution, avoiding the introduction of unnecessary complexity before the right moment.

---

## What Has Been Decided

### API Architecture

The reference API follows **Clean Architecture**, with separation between:

- **Domain** — entities, value objects, and pure business rules
- **Application** — use cases and contracts (ports)
- **API** — handlers/controllers, middleware, and HTTP contracts
- **Infrastructure** — database, authentication, observability, and technical integrations

Dependencies always point inward. The infrastructure layer implements interfaces defined in the inner layers.

### Multi-tenancy

The platform is **multi-tenant by country**, with a **database-per-country** strategy, meaning each country has its own isolated database for transactional data. This decision was made for compliance, isolation, and operational security reasons.

### Authentication Separated from Transactional Data

It does not make sense to have one user per country database. Therefore, authentication must reside **outside the per-country databases**, in a centralized authentication service, avoiding duplicate users and separate logins per tenant.

### Relational Persistence

**Entity Framework Core** is the standard ORM for the API, with **migrations** as the official schema evolution strategy. Migrations are EF Core's native mechanism for versioning database changes, keeping changes in source control, and applying incremental schema evolution.

### Repository Strategy and Workflow

The repository strategy is **multi-repo**, with a branching pattern based on `master`, `staging`, and feature/hotfix branches, plus Conventional Commits. This already creates a solid base for the initial template.

---

## Related Pages

- [Initial Environment](02-initial-environment.md)
- [Database Strategy](03-database.md)
- [Repository Structure](06-repository-structure.md)
- [CI/CD and Monitoring](05-cicd-and-monitoring.md)
