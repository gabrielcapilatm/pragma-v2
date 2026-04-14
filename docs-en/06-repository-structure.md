# Repository Structure

**Context:** Define and prepare the repository structure for starting the development of the first LATAM Platform V2 API, establishing versioning, branching, and code quality standards.

---

## Decisions

### 1. Repository Strategy: Multi-repo

**Decision:** Use a multi-repository approach — one repository per application/product.

**Rationale:**
- Greater autonomy between teams and products
- Products evolve in parallel without dependency conflicts
- Avoids monorepo management complexity at this stage
- The team lacks practical monorepo experience

### 2. Repository Structure

```
github.com/latam-platform/
├── financial-api/       ← Conciliation & Settlement API (.NET)
├── financial-front/     ← Frontend (Next.js)
└── infrastructure/      ← Docker Compose + IaC (Terraform/CDK) + DB configs
```

**Naming convention:**

| Type | Pattern | Examples |
|------|---------|---------|
| APIs | `{product}-api` | `conciliation-api`, `settlement-api` |
| Frontends | `{product}-front` | `conciliation-front`, `settlement-front` |
| Shared libraries | Descriptive name | `core` |
| Infrastructure | `infrastructure` | — |

**Separation by responsibility:**

| Type | Technology | Examples | Purpose |
|------|-----------|---------|---------|
| API | .NET | `conciliation-api`, `settlement-api` | Backend and business logic |
| Frontend | Next.js | `conciliation-front`, `settlement-front` | User interface |
| Core | .NET | `core` | Shared library (DB connections, helpers) |
| Infra | Docker/Terraform | `infrastructure` | IaC, compose, local configurations |

### 3. Branching Strategy

**Model:** Simplified with `master` + `staging` + feature branches

```
master   (production)
  ▲
  │ PR
staging  (QA / staging)
  ▲
  │ PR
feat/PRAG-1234  (development)
```

#### Feature workflow

```bash
# 1. Create branch from master
git checkout master
git pull
git checkout -b feat/PRAG-1234

# 2. Develop and commit
git commit -m "feat(conciliation): add transaction validation"

# 3. Push and open PR to staging
git push origin feat/PRAG-1234
# → Open PR: feat/PRAG-1234 → staging

# 4. After approval and merge to staging, open PR to master
# → Open PR: feat/PRAG-1234 → master

# 5. After merge to master, delete the branch
git branch -d feat/PRAG-1234
git push origin --delete feat/PRAG-1234
```

#### Hotfix workflow

```bash
# 1. Create branch from master
git checkout master
git pull
git checkout -b hotfix/PRAG-5678

# 2. Fix and commit
git commit -m "fix(settlement): correct payment calculation"

# 3. Direct PR to master (urgent)
git push origin hotfix/PRAG-5678
# → Open PR: hotfix/PRAG-5678 → master

# 4. After merge, delete the branch
```

#### Branch protections

| Branch | Protection | Direct commits | PR required | Approvers |
|--------|-----------|----------------|-------------|-----------|
| `master` | Protected | Blocked | Required | 1 (any) |
| `staging` | Protected | Blocked | Required | 1 (any) |
| `feat/*` | Not protected | Allowed | — | — |
| `hotfix/*` | Not protected | Allowed | — | — |

### 4. Branch Naming

**Pattern:** `<type>/[CARD-ID]`

```
feat/PRAG-1234   → Normal features
hotfix/PRAG-5678 → Urgent fixes
```

**Rules:**
- Always use the Jira/Azure DevOps card ID
- Keep lowercase
- Use hyphens as separators

### 5. Commit Convention: Conventional Commits

**Format:** `<type>(<scope>): <description>`

```
[optional body]

[optional footer]
```

**Allowed types:**

| Type | Purpose | Example |
|------|---------|---------|
| `feat` | New feature | `feat(conciliation): add transaction import` |
| `fix` | Bug fix | `fix(settlement): correct payment calculation` |
| `docs` | Documentation | `docs(readme): update setup instructions` |
| `refactor` | Refactoring without behavior change | `refactor(core): simplify connection factory` |
| `test` | Test additions or modifications | `test(settlement): add unit tests for payment service` |
| `chore` | Maintenance tasks | `chore(deps): update entity framework to 8.0` |
| `perf` | Performance improvement | `perf(conciliation): optimize query for large datasets` |
| `ci` | CI/CD changes | `ci(pipeline): add sonarqube analysis` |

**Suggested scopes:**

| Scope | Module |
|-------|--------|
| `conciliation` | Conciliation module |
| `settlement` | Settlement module |
| `core` | Core library |
| `infra` | Infrastructure |
| `api` | API layer |
| `domain` | Domain layer |
| `db` | Database/migrations |

**Complete examples:**

```bash
# Feature
git commit -m "feat(conciliation): add CSV file upload endpoint"

# Bug fix
git commit -m "fix(settlement): prevent duplicate payment processing"

# Refactoring
git commit -m "refactor(core): extract connection string resolver to separate class"

# Documentation
git commit -m "docs(api): add swagger documentation for transaction endpoints"

# Breaking change
git commit -m "feat(settlement)!: change payment status enum values

BREAKING CHANGE: payment status values changed from numeric to string"
```

### 6. Code Review Policy

| Aspect | Decision |
|--------|----------|
| PR required? | Yes, for `master` and `staging` |
| Required approvers | 1 (any) |
| Specific approver? | No (at this stage) |
| Auto-merge? | No |
| Delete branch after merge? | Yes (automatic) |

**Full PR checklist:**

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
- [ ] Documentation updated
- [ ] No build warnings
- [ ] Migration created (if applicable)

## Related card
PRAG-XXXX
```

### 7. Core Repository (Shared Library)

**Purpose:** Code shared between multiple APIs

**Contents:**
- Database connection utilities
- Shared domain models (if applicable)
- Common helpers and extensions

**Repository structure:**

```
core/
└── src/
    └── LatamPlatform.Core/
        ├── Database/
        │   ├── DbConnectionFactory.cs
        │   └── TenantConnectionResolver.cs
        ├── Extensions/
        │   └── StringExtensions.cs
        ├── Models/
        │   └── BaseEntity.cs
        └── Helpers/
            └── DateTimeHelper.cs
```

**Distribution:**
- Internal NuGet package (recommended for production)
- Or direct project reference (during local development)

**Usage example:**

```csharp
// conciliation-api/Program.cs
using LatamPlatform.Core.Database;

builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
```
