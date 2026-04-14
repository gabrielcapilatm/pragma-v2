# ✅ Checklist de Validação

## 1. Docker & Infraestrutura

```bash
# Verificar containers rodando
docker ps

# Deve mostrar:
# - latam-postgres (porta 5432)
# - latam-postgres-keycloak (porta 5433)
# - latam-keycloak (porta 8080)
```

**Checklist:**
- [ ] PostgreSQL rodando na porta 5432
- [ ] PostgreSQL Keycloak rodando na porta 5433
- [ ] Keycloak rodando na porta 8080
- [ ] Todos os containers "healthy"

**Verificar bancos criados:**
```bash
docker exec -it latam-postgres psql -U postgres -l

# Deve listar:
# - latam_br
# - latam_ar
# - latam_cl
```

- [ ] Banco `latam_br` existe
- [ ] Banco `latam_ar` existe
- [ ] Banco `latam_cl` existe

## 2. Keycloak

**Acessar:** http://localhost:8080

- [ ] Admin console acessível
- [ ] Login com admin/admin123 funciona
- [ ] Realm `latam-platform` criado
- [ ] Client `latam-api` criado
- [ ] Roles `admin` e `user` criados
- [ ] Usuário `admin.br` existe com tenant BR
- [ ] Usuário `user.ar` existe com tenant AR
- [ ] Usuário `user.cl` existe com tenant CL
- [ ] Mapper `tenant-mapper` configurado

**Testar token:**
```bash
TOKEN=$(curl -s -X POST 'http://localhost:8080/realms/latam-platform/protocol/openid-connect/token' \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  -d 'client_id=latam-api' \
  -d 'username=admin.br' \
  -d 'password=admin123' \
  -d 'grant_type=password' | jq -r '.access_token')

echo $TOKEN | cut -d'.' -f2 | base64 -d 2>/dev/null | jq .
```

- [ ] Token gerado com sucesso
- [ ] Token contém claim `tenant: "BR"`
- [ ] Token contém `email`
- [ ] Token contém `sub` (user ID)

## 3. Migrations

```bash
cd src/Infrastructure

# Verificar migrations criadas
ls Persistence/Migrations/

# Aplicar migrations
./scripts/apply-migrations.sh
```

- [ ] Migration `InitialCreate` existe
- [ ] Migrations aplicadas no `latam_br`
- [ ] Migrations aplicadas no `latam_ar`
- [ ] Migrations aplicadas no `latam_cl`

**Verificar schema:**
```bash
docker exec -it latam-postgres psql -U postgres -d latam_br -c "\dt"

# Deve listar:
# - users
# - __EFMigrationsHistory
```

- [ ] Tabela `users` existe em BR
- [ ] Tabela `users` existe em AR
- [ ] Tabela `users` existe em CL

## 4. Compilação

```bash
# raiz do projeto
dotnet build

# Deve terminar com: Build succeeded.
```

- [ ] Projeto compila sem erros
- [ ] Sem warnings críticos
- [ ] Todas as dependências restauradas

## 5. Testes

```bash
# Rodar todos os testes
dotnet test

# Deve mostrar: Passed!
```

- [ ] Testes de Domain passam
- [ ] Testes de Application passam
- [ ] Testes de Integration passam
- [ ] Sem testes falhando

## 6. API

```bash
cd src/Api
dotnet run

# Deve subir em: http://localhost:5000
```

- [ ] API sobe sem erros
- [ ] Swagger acessível em `/swagger`
- [ ] Logs mostram "Now listening on: http://localhost:5000"

## 7. Endpoints

### Health (sem autenticação)

```bash
curl http://localhost:5000/api/health
```

**Resposta esperada:**
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

- [ ] `GET /api/health` retorna 200
- [ ] Response contém `status: "healthy"`

### Me (sem token)

```bash
curl http://localhost:5000/api/auth/me
```

**Resposta esperada:** 401 Unauthorized

- [ ] `GET /api/auth/me` sem token retorna 401

### Me (com token BR)

```bash
# Obter token BR
TOKEN=$(curl -s -X POST 'http://localhost:8080/realms/latam-platform/protocol/openid-connect/token' \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  -d 'client_id=latam-api' \
  -d 'username=admin.br' \
  -d 'password=admin123' \
  -d 'grant_type=password' | jq -r '.access_token')

# Chamar API
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/auth/me
```

**Resposta esperada:**
```json
{
  "id": "...",
  "email": "admin@latam.br",
  "name": "Admin Brasil",
  "tenant": "BR",
  "isActive": true
}
```

- [ ] `GET /api/auth/me` com token BR funciona
- [ ] Response contém `tenant: "BR"`
- [ ] Dados do usuário corretos

### Me (com token AR)

```bash
# Obter token AR
TOKEN=$(curl -s -X POST 'http://localhost:8080/realms/latam-platform/protocol/openid-connect/token' \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  -d 'client_id=latam-api' \
  -d 'username=user.ar' \
  -d 'password=user123' \
  -d 'grant_type=password' | jq -r '.access_token')

# Chamar API
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/auth/me
```

- [ ] `GET /api/auth/me` com token AR funciona
- [ ] Response contém `tenant: "AR"`

### Me (com token CL)

```bash
# Obter token CL
TOKEN=$(curl -s -X POST 'http://localhost:8080/realms/latam-platform/protocol/openid-connect/token' \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  -d 'client_id=latam-api' \
  -d 'username=user.cl' \
  -d 'password=user123' \
  -d 'grant_type=password' | jq -r '.access_token')

# Chamar API
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/auth/me
```

- [ ] `GET /api/auth/me` com token CL funciona
- [ ] Response contém `tenant: "CL"`

## 8. Multi-Tenancy Validation

**Objetivo:** Garantir que cada tenant acessa apenas seu banco.

### Criar usuário no banco BR

```bash
docker exec -it latam-postgres psql -U postgres -d latam_br << 'EOF'
INSERT INTO users (id, email, name, is_active, created_at)
VALUES (
  'a0000000-0000-0000-0000-000000000001',
  'test@br.com',
  'Test BR',
  true,
  NOW()
);
EOF
```

### Criar usuário no banco AR

```bash
docker exec -it latam-postgres psql -U postgres -d latam_ar << 'EOF'
INSERT INTO users (id, email, name, is_active, created_at)
VALUES (
  'a0000000-0000-0000-0000-000000000002',
  'test@ar.com',
  'Test AR',
  true,
  NOW()
);
EOF
```

### Testar isolamento

```bash
# Token BR
TOKEN_BR=$(curl -s -X POST '...' -d 'username=admin.br' ... | jq -r '.access_token')

# Token AR
TOKEN_AR=$(curl -s -X POST '...' -d 'username=user.ar' ... | jq -r '.access_token')

# BR deve ver apenas dados do BR
curl -H "Authorization: Bearer $TOKEN_BR" http://localhost:5000/api/auth/me

# AR deve ver apenas dados do AR
curl -H "Authorization: Bearer $TOKEN_AR" http://localhost:5000/api/auth/me
```

- [ ] Usuário BR vê apenas dados do banco BR
- [ ] Usuário AR vê apenas dados do banco AR
- [ ] Usuário CL vê apenas dados do banco CL
- [ ] Sem vazamento de dados entre tenants

## 9. Validação Final

- [ ] Todos os itens acima passaram
- [ ] Sem erros nos logs
- [ ] Documentação está clara
- [ ] README atualizado

## 🎉 Critérios de Sucesso

O projeto está **COMPLETO** quando:

✅ Docker containers rodando (PostgreSQL + Keycloak)  
✅ Keycloak configurado com realm, client, users, mapper  
✅ Migrations aplicadas nos 3 bancos  
✅ API compila e sobe sem erros  
✅ `GET /health` retorna 200  
✅ `GET /auth/me` sem token retorna 401  
✅ `GET /auth/me` com token BR retorna dados do BR  
✅ `GET /auth/me` com token AR retorna dados do AR  
✅ `GET /auth/me` com token CL retorna dados do CL  
✅ Multi-tenancy funciona (isolamento entre bancos)  
✅ Testes passam  

## Troubleshooting

### API não conecta no banco

**Sintoma:** Erro ao chamar `/api/auth/me`

**Verificar:**
```bash
# PostgreSQL está rodando?
docker ps | grep postgres

# Connection strings no appsettings.json estão corretas?
cat src/Api/appsettings.json
```

### Token não contém tenant

**Sintoma:** API retorna erro "Tenant not found in JWT"

**Solução:**
1. Verificar mapper no Keycloak
2. Verificar atributo do usuário
3. Gerar novo token

### 401 sempre

**Sintoma:** Mesmo com token, retorna 401

**Verificar:**
```bash
# Keycloak está rodando?
curl http://localhost:8080

# Authority está correto no appsettings.json?
# Deve ser: http://localhost:8080
```

---

**🎉 Parabéns! Se todos os checkboxes estão marcados, o projeto está validado!**
