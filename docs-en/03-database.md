# Database Strategy

**Objective:** Define the base persistence structure for the reference API, including the authentication strategy, use of relational and NoSQL databases, and the data access pattern with Entity Framework Core.

---

## Decisions

### Authentication Database

- There will be a **dedicated authentication database**, separate from the per-country transactional databases.
- This database will centralize user information, authentication state, authorization claims, and related data.
- The goal is to avoid multiple logins per country and keep authentication decoupled from the transactional layer.

### Relational Database

- **Database:** Aurora RDS with PostgreSQL
- The relational database is the **primary persistence source** for the application.
- It is used for transactional data and structures that require consistency, relationships, and strict control.

### ORM

- **Entity Framework Core** is the standard ORM, given that all backend applications are built in .NET.
- See [Entity Framework](04-entity-framework.md) for full usage guidelines.

### NoSQL

- NoSQL databases **will** be used on the platform.
- Usage will be driven by **clear business or technical need**, not convenience.
- Exact use cases will be defined as user stories are refined.

### MVP Scope

At this stage, the focus is on supporting a **functional login flow in the API**, usable via Scalar, Postman, or similar tools. The initial database setup must be sufficient to support this basic flow.

---

## Initial Guidelines

### Entity Framework

| Guideline | Detail |
|-----------|--------|
| EF Core as standard | Default data access pattern for the reference API |
| Migrations versioned | Maintained alongside source code |
| Domain independence | Domain layer must not depend directly on infrastructure details |
| Layer separation | DB access must respect the Clean Architecture layering |

### When to Use a Relational Database

- Transactional data
- Data with relationships between entities
- Structures that require consistency
- Core operational information

### When to Consider NoSQL

- Logs and audit trails
- Notifications
- Immutable documents
- Structures requiring fast reads or flexible schema

### When NOT to Use NoSQL

- Do not use just for convenience
- Do not use without a clearly defined responsibility boundary
- Do not replace the primary relational database without a clear technical justification

---

## Open Questions

- Which tables will be created for the MVP?
- How will the connection strategy between the authentication database and the transactional databases work?
- In which scenarios will NoSQL be officially adopted?
- Will there be a future read/write separation (CQRS)?
