# Initial Environment

**Objective:** Define the base infrastructure for hosting the API.

---

## Decisions

### Infrastructure

- **Cloud provider:** AWS (training account)
- **Access:** via configured IAM profile

### Orchestration

- **Container orchestration:** ECS (Elastic Container Service) with Docker containers
- Create the initial ECS cluster
- Create the ECR (Elastic Container Registry)

### Local Development Environment

- **Docker Compose** to run the database locally for development
- Possibility of accessing the staging environment for debugging purposes

### Secrets and Configuration

| Concern | Service |
|---------|---------|
| Application configuration | AWS Parameter Store |
| Secrets (credentials, keys) | AWS Secrets Manager |
| Encryption | AWS KMS |
| Build-time secrets | GitHub Actions secrets |

### Responsibilities

| Area | Owner |
|------|-------|
| AWS infrastructure | Infra team (Lima) |
| Docker + CI/CD pipelines | Dev team |

---

## Next Steps

1. Create the ECS cluster to run applications
2. Create the ECR to store and version Docker images
3. Define the required network infrastructure (VPC, subnets, security groups)
4. Configure AWS access for the team
