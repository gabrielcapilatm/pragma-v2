# 🔐 Keycloak Setup

## Objetivo

Configurar Keycloak como Identity Provider com suporte a multi-tenancy via JWT claims.

## 1. Acessar Admin Console

```
URL: http://localhost:8080
Username: admin
Password: admin123
```

## 2. Criar Realm

1. Clique no dropdown "master" (canto superior esquerdo)
2. Clique em "Create Realm"
3. **Realm name**: `latam-platform`
4. **Enabled**: ON
5. Clique em "Create"

## 3. Criar Client

1. Na barra lateral, clique em "Clients"
2. Clique em "Create client"

### General Settings
- **Client ID**: `latam-api`
- **Name**: `LATAM Platform API`
- **Description**: `API Client for LATAM Platform`
- Clique em "Next"

### Capability config
- **Client authentication**: OFF (public client)
- **Authorization**: OFF
- **Standard flow**: ON
- **Direct access grants**: ON
- **Implicit flow**: OFF
- **Service accounts roles**: OFF
- Clique em "Next"

### Login settings
- **Root URL**: `http://localhost:5000`
- **Home URL**: `http://localhost:5000`
- **Valid redirect URIs**: `http://localhost:5000/*`
- **Web origins**: `http://localhost:5000`
- Clique em "Save"

## 4. Criar Roles

1. Na barra lateral, clique em "Realm roles"
2. Clique em "Create role"

### Criar role: admin
- **Role name**: `admin`
- **Description**: `Administrator role`
- Clique em "Save"

### Criar role: user
- **Role name**: `user`
- **Description**: `Standard user role`
- Clique em "Save"

## 5. Criar Usuários de Teste

### Usuário 1: Admin Brasil

1. Na barra lateral, clique em "Users"
2. Clique em "Create new user"

**Details:**
- **Username**: `admin.br`
- **Email**: `admin@latam.br`
- **Email verified**: ON
- **First name**: `Admin`
- **Last name**: `Brasil`
- Clique em "Create"

**Credentials:**
1. Clique na aba "Credentials"
2. Clique em "Set password"
3. **Password**: `admin123`
4. **Password confirmation**: `admin123`
5. **Temporary**: OFF
6. Clique em "Save"

**Role Mapping:**
1. Clique na aba "Role mapping"
2. Clique em "Assign role"
3. Selecione `admin`
4. Clique em "Assign"

**Attributes:**
1. Clique na aba "Attributes"
2. Clique em "Add an attribute"
3. **Key**: `tenant`
4. **Value**: `BR`
5. Clique em "Save"

### Usuário 2: User Argentina

Repita o processo:
- **Username**: `user.ar`
- **Email**: `user@latam.ar`
- **Password**: `user123`
- **Role**: `user`
- **Attribute**: `tenant` = `AR`

### Usuário 3: User Chile

Repita o processo:
- **Username**: `user.cl`
- **Email**: `user@latam.cl`
- **Password**: `user123`
- **Role**: `user`
- **Attribute**: `tenant` = `CL`

## 6. Configurar Mapper de Tenant

Este é o passo **CRÍTICO** para incluir o tenant no JWT.

1. Na barra lateral, clique em "Clients"
2. Clique em `latam-api`
3. Clique na aba "Client scopes"
4. Clique em `latam-api-dedicated`
5. Clique na aba "Mappers"
6. Clique em "Add mapper" → "By configuration"
7. Selecione "User Attribute"

**Configuração do Mapper:**
- **Name**: `tenant-mapper`
- **User Attribute**: `tenant`
- **Token Claim Name**: `tenant`
- **Claim JSON Type**: `String`
- **Add to ID token**: ON
- **Add to access token**: ON
- **Add to userinfo**: ON
- **Multivalued**: OFF
- Clique em "Save"

## 7. Testar Token

### Obter Token

```bash
curl -X POST 'http://localhost:8080/realms/latam-platform/protocol/openid-connect/token' \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  -d 'client_id=latam-api' \
  -d 'username=admin.br' \
  -d 'password=admin123' \
  -d 'grant_type=password'
```

### Response Example

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICJfVG...",
  "expires_in": 300,
  "refresh_expires_in": 1800,
  "refresh_token": "eyJhbGciOiJIUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICJh...",
  "token_type": "Bearer",
  "not-before-policy": 0,
  "session_state": "c3e4d5f6-7890-1234-5678-90abcdef1234",
  "scope": "profile email"
}
```

### Decodificar Token

1. Copie o `access_token`
2. Vá para https://jwt.io
3. Cole o token
4. Verifique o payload:

```json
{
  "exp": 1234567890,
  "iat": 1234567890,
  "jti": "...",
  "iss": "http://localhost:8080/realms/latam-platform",
  "sub": "...",
  "email": "admin@latam.br",
  "tenant": "BR",  // ✅ Deve estar presente!
  "realm_access": {
    "roles": ["admin"]
  }
}
```

## 8. Testar com API

```bash
# 1. Obter token
TOKEN=$(curl -s -X POST 'http://localhost:8080/realms/latam-platform/protocol/openid-connect/token' \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  -d 'client_id=latam-api' \
  -d 'username=admin.br' \
  -d 'password=admin123' \
  -d 'grant_type=password' | jq -r '.access_token')

# 2. Chamar API
curl -H "Authorization: Bearer $TOKEN" \
     http://localhost:5000/api/auth/me
```

### Resposta Esperada

```json
{
  "id": "...",
  "email": "admin@latam.br",
  "name": "Admin Brasil",
  "tenant": "BR",
  "isActive": true
}
```

## Troubleshooting

### Token não contém tenant

**Problema**: JWT não tem o claim `tenant`

**Solução**:
1. Verifique se o mapper foi criado corretamente
2. Confirme que o usuário tem o atributo `tenant`
3. Gere um novo token (os tokens antigos não são atualizados)

### Erro 401 na API

**Problema**: API retorna 401 Unauthorized

**Causas possíveis**:
1. Token expirado (válido por 5 minutos)
2. Keycloak não está rodando
3. Configuração errada no appsettings.json

**Solução**:
```bash
# Verificar se Keycloak está rodando
docker ps | grep keycloak

# Gerar novo token
# Verificar appsettings.json:
# - Authority deve bater com URL do Keycloak
# - Realm deve ser "latam-platform"
```

---

**Próximo:** [Testes](08-testing.md)
