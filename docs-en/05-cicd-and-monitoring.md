# CI/CD and Monitoring

**Objective:** Define the initial CI/CD structure and monitoring principles to support the development of the first reference API, ensuring standardization across repositories, delivery flow automation, and a minimum quality baseline for future evolution.

---

## Decisions

### Pipeline Strategy

Each repository will **not** maintain a fully independent pipeline. The adopted strategy is a **shared pipeline template** referenced by each repository.

**Goals:**
- Ensure standardization across all services
- Reduce duplication
- Facilitate centralized maintenance of the CI/CD process

### Pull Request Template

A standard PR template is defined for all repositories, focused on change clarity, traceability, and a minimum checklist.

```markdown
## Description
<!-- Brief description of the change -->
<!-- If this is a large change, provide a more technical breakdown -->

## Type of change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation

## Checklist
- [ ] Migration created (if applicable)

## Related card
PRAG-XXXX
```

### Deploy to Staging

- Deploy to staging is **automatic**.
- The goal is to accelerate technical validation and reduce manual effort in the initial flow.

### Deploy to Production

- At this stage, there is **no formal approval gate for production deployments**.
- The approval definition will be revisited when the environment is more stable and the delivery process is more mature.

### Tests in the Pipeline

- The pipeline will run **unit tests**.
- At this stage, the pipeline will **not fail automatically** on test failures or coverage below a threshold.
- The initial intention is to provide visibility and encourage quality without blocking platform evolution while the template is still being consolidated.

### Test Coverage

- Defining a minimum coverage from the beginning helps build a quality culture.
- Even without a hard gate at this stage, it makes sense to start with a reference number for tracking.
- The exact percentage can be refined, but coverage will be observed from the initial phase.

### Monitoring

- The monitoring tool has **not yet been defined**.
- The observability stack will be chosen at a more appropriate stage of the platform's evolution.

---

## Pipeline Steps (Reference)

```
┌─────────────────────────────────────────────────┐
│  Trigger: PR merged to staging or master         │
└────────────────────┬────────────────────────────┘
                     │
          ┌──────────▼──────────┐
          │  1. Restore & Build  │
          └──────────┬──────────┘
                     │
          ┌──────────▼──────────┐
          │  2. Unit Tests       │  (visibility only — no hard fail initially)
          └──────────┬──────────┘
                     │
          ┌──────────▼──────────┐
          │  3. Coverage Report  │
          └──────────┬──────────┘
                     │
          ┌──────────▼──────────┐
          │  4. Docker Build     │
          └──────────┬──────────┘
                     │
          ┌──────────▼──────────┐
          │  5. Push to ECR      │
          └──────────┬──────────┘
                     │
          ┌──────────▼──────────┐
          │  6. Deploy to ECS    │
          └─────────────────────┘
```

---

## Deployment Flow

```
feat/PRAG-xxxx
      │
      │ PR
      ▼
  staging  ──── automatic deploy
      │
      │ PR
      ▼
  master   ──── production deploy (no approval gate, for now)
```

---

## Future Considerations

- Introduce a manual approval gate for production once the environment stabilizes
- Define and enforce a minimum test coverage threshold
- Choose and integrate a monitoring/observability stack (metrics, tracing, alerting)
- Consider adding static analysis / security scanning to the pipeline
