# Keycloak — Authentication & Identity on LATAM Platform V2

> **Audience:** Developers joining the team who are new to Keycloak.
> This document explains what Keycloak is, its core concepts, and exactly how we use it on this platform.

---

## What is Keycloak?

Keycloak is an open-source **Identity Provider (IdP)** — a dedicated service responsible for everything related to authentication and identity. Instead of building login, user management, token issuance, and session handling inside each application, you delegate all of that to Keycloak.

Think of it as the single source of truth for "who is this user and what are they allowed to do."

**What Keycloak handles for us:**
- Login page (username/password, future SSO/social)
- Token issuance (JWT)
- User management (create, deactivate, assign roles)
- Session management (logout, token expiry)
- Public key exposure (so APIs can validate tokens without calling Keycloak)

---

## Core Concepts

### Realm

A realm is an **isolated authentication space**. It has its own users, clients, roles, and settings — completely independent from other realms on the same Keycloak instance.

**Our setup:**
```
Keycloak instance (localhost:8080)
└── latam-platform  ← our realm
    ├── Users
    ├── Clients
    └── Roles
```

**Analogy:** Think of a realm like a separate tenant in Keycloak itself. If you had a staging environment and a production environment, you'd use two realms: `latam-platform-staging` and `latam-platform`.

> Do not confuse Keycloak realms with our platform's multi-tenancy. Our "tenant" concept (BR, AR, CL) is a **custom claim inside the JWT** — it is not a Keycloak realm. We use a single Keycloak realm for all countries.

---

### Client

A client is an **application registered in Keycloak** that is allowed to request authentication. Every application that needs to authenticate users must be registered as a client.

**Our client:**

| Setting | Value | Meaning |
|---------|-------|---------|
| Client ID | `latam-api` | Identifier used by apps when redirecting to Keycloak |
| Client type | **Confidential** (Next.js) | Has a client secret — kept safe on the Next.js server |
| Valid redirect URIs | `http://localhost:3000/api/auth/callback/keycloak` | Where Keycloak sends the user back after login |
| Web origins | `http://localhost:3000` | Allowed CORS origins |

**Public vs. Confidential clients:**
- **Public** — no secret, used when the app runs entirely in the browser (our previous HTML prototype)
- **Confidential** — has a secret, used when there is a server that can keep it safe (Next.js BFF)

> With Next.js, the client secret lives only on the server and never reaches the browser. This is why we can upgrade from Public to Confidential.

---

### User

A user is an **identity registered in Keycloak**. Unlike traditional systems where users live in the application database, here users live in Keycloak.

**Key point for our platform:** A user exists once, in Keycloak, and works across all countries. There is no "user per country database" — the country (tenant) is just a claim on the token.

**Our test users (local development):**

| Username | Password | Tenant attribute |
|----------|----------|-----------------|
| `admin.br` | `admin123` | `BR` |
| `admin.ar` | `admin123` | `AR` |
| `admin.cl` | `admin123` | `CL` |

---

### Role

Roles define **what a user is allowed to do**. Keycloak supports two types:

- **Realm roles** — available across all clients in the realm (e.g., `admin`, `operator`)
- **Client roles** — scoped to a specific client (not used by us currently)

Roles are included in the JWT token inside the `realm_access` claim:

```json
{
  "realm_access": {
    "roles": ["admin", "default-roles-latam-platform"]
  }
}
```

---

### JWT Token (JSON Web Token)

When a user successfully authenticates, Keycloak issues a **JWT access token** — a signed, self-contained object that carries the user's identity and claims.

**Structure:** Three Base64-encoded parts separated by dots:
```
header.payload.signature
```

**Our token payload looks like this:**
```json
{
  "sub": "a1b2c3d4-...",
  "preferred_username": "admin.br",
  "name": "Admin Brazil",
  "email": "admin.br@latam.com",
  "tenant": "BR",
  "realm_access": {
    "roles": ["admin"]
  },
  "iss": "http://localhost:8080/realms/latam-platform",
  "exp": 1744200000,
  "iat": 1744196400
}
```

| Claim | Meaning |
|-------|---------|
| `sub` | Unique user ID (UUID) |
| `preferred_username` | Login name |
| `name` | Full name |
| `email` | Email address |
| `tenant` | **Our custom claim** — the country code (BR, AR, CL) |
| `realm_access.roles` | Roles assigned to this user |
| `iss` | Issuer — which Keycloak realm issued this token |
| `exp` | Expiry timestamp |
| `iat` | Issued at timestamp |

The `tenant` claim is **not a standard JWT claim** — we added it via a **Protocol Mapper** in Keycloak, which reads from the user's attributes and injects it into every token.

**Why JWT is self-contained:** The API does not need to call Keycloak on every request. It validates the token's signature using Keycloak's public key (fetched once at startup from the JWKS endpoint), then reads the claims directly from the token.

---

### JWKS (JSON Web Key Set)

Keycloak exposes a public endpoint with the cryptographic keys used to sign tokens:

```
GET http://localhost:8080/realms/latam-platform/protocol/openid-connect/certs
```

The API fetches these keys at startup and caches them. When a request arrives with a JWT, the API verifies the signature against these keys — confirming the token was genuinely issued by our Keycloak and not forged.

---

### Protocol Mapper

A Protocol Mapper is a **Keycloak configuration** that injects additional data into the token. This is how we get the `tenant` claim into the JWT.

**Our mapper setup:**
- Type: `User Attribute`
- User attribute name: `tenant`
- Token claim name: `tenant`
- Claim JSON type: `String`
- Added to: Access Token

When a user has `tenant = BR` in their Keycloak attributes, every token issued for that user will carry `"tenant": "BR"`.

---

## Authentication Flow: Authorization Code + PKCE

We use the **Authorization Code flow with PKCE** (Proof Key for Code Exchange) — the current industry standard for browser-based applications (OAuth 2.1).

### Why not username/password (ROPC)?

The Resource Owner Password Credentials flow is deprecated in OAuth 2.1 because:
- The frontend handles the user's raw credentials
- Bypasses Keycloak's login page (no MFA, no SSO possible)
- Not supported in some corporate environments

### Why Authorization Code + PKCE?

- The user's credentials never touch our frontend code
- The browser talks directly to Keycloak's login page
- PKCE prevents authorization code interception attacks
- Enables MFA and SSO in the future without changing the frontend

### Full flow step by step (Next.js + Auth.js)

```
1. User clicks "Sign in"
        │
        ▼
2. Next.js SERVER generates:
   - code_verifier  = random 32-byte string (kept on the server)
   - code_challenge = SHA-256(code_verifier), base64url encoded
   - state          = random string (CSRF protection, kept on the server)
   Nothing is stored in the browser at this point.
        │
        ▼
3. Browser redirects to Keycloak:
   GET /realms/latam-platform/protocol/openid-connect/auth
     ?response_type=code
     &client_id=latam-api
     &redirect_uri=http://localhost:3000/api/auth/callback/keycloak
     &scope=openid profile
     &code_challenge=<hash>
     &code_challenge_method=S256
     &state=<random>
        │
        ▼
4. Keycloak shows its login page
   User enters username + password
        │
        ▼
5. Keycloak redirects back to Next.js callback route:
   GET http://localhost:3000/api/auth/callback/keycloak
     ?code=<auth_code>&state=<same_random>
        │
        ▼
6. Next.js SERVER validates state (CSRF check)
        │
        ▼
7. Next.js SERVER exchanges the code for a token:
   POST /realms/latam-platform/protocol/openid-connect/token
     grant_type=authorization_code
     &client_id=latam-api
     &client_secret=<secret>             ← confidential client
     &redirect_uri=.../api/auth/callback/keycloak
     &code=<auth_code>
     &code_verifier=<original_verifier>
        │
        ▼
8. Keycloak returns JWT access token + refresh token
        │
        ▼
9. Next.js SERVER stores tokens in the session (server-side)
   Browser receives only: Set-Cookie: next-auth.session=<opaque> (HttpOnly, Secure)
   The JWT never reaches the browser.
        │
        ▼
10. Every page/API call from the browser sends the session cookie automatically.
    Next.js server reads the session, retrieves the JWT, and forwards it
    as "Authorization: Bearer <token>" to financial-api.
```

**Why this is more secure than a pure browser flow:**
- `code_verifier`, `client_secret`, and the JWT itself never exist in the browser
- The session cookie is `HttpOnly` — JavaScript cannot read it, even if XSS occurs
- Refresh tokens are handled automatically by Auth.js on the server

> See [Next.js Authentication Implementation](09-nextjs-authentication.md) for the full implementation guide.

---

## How the API Validates Tokens

The API never asks Keycloak "is this token valid?". Instead:

```
Request arrives with Bearer token
        │
        ▼
JwtBearer middleware
  - Fetches JWKS from Keycloak (cached, refreshed periodically)
  - Validates signature
  - Validates issuer matches our realm
  - Validates token has not expired
        │
        ▼
KeycloakClaimsTransformation
  - Reads realm_access.roles (JSON string in the token)
  - Extracts each role and adds it as individual "roles" claim
  - Reason: ASP.NET's IsInRole() and [Authorize(Roles=...)]
    work with flat claims, not nested JSON
        │
        ▼
Request proceeds — User identity available via ICurrentUserService
```

### Configuration in `appsettings.json`

```json
{
  "Keycloak": {
    "Authority": "http://localhost:8080",
    "Realm": "latam-platform",
    "ClientId": "latam-api"
  }
}
```

### Registration in `DependencyInjection.cs`

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{authority}/realms/{realm}";
        options.RequireHttpsMetadata = false; // true in production
        options.MapInboundClaims = false;     // prevents .NET from remapping claim names
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = true,
            ValidateLifetime = true,
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
        };
    });
```

**`MapInboundClaims = false`** is critical. Without it, the .NET middleware remaps standard JWT claim names to long URIs (e.g., `sub` becomes `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`), breaking our claim reading code.

---

## Accessing User Identity in Code

Any service that needs to know who the current user is injects `ICurrentUserService`:

```csharp
public class SomeService
{
    private readonly ICurrentUserService _currentUser;

    public SomeService(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public void DoSomething()
    {
        var userId   = _currentUser.UserId;      // from "sub" claim
        var name     = _currentUser.Name;        // from "name" claim
        var email    = _currentUser.Email;       // from "email" claim
        var tenant   = _currentUser.TenantCode;  // from "tenant" claim → routes to correct DB
        var roles    = _currentUser.Roles;       // from "roles" claims (after transformation)
        var isAuthed = _currentUser.IsAuthenticated;
    }
}
```

The `TenantCode` is the most important — it is what `TenantResolver` uses to pick the correct country database for every request.

---

## Protecting Endpoints

```csharp
// Requires any authenticated user
[Authorize]
public IActionResult GetProducts() { ... }

// Requires a specific role
[Authorize(Roles = "admin")]
public IActionResult AdminAction() { ... }

// No authentication required
[AllowAnonymous]
public IActionResult HealthCheck() { ... }

// Using a named policy
[Authorize(Policy = "AdminOnly")]
public IActionResult AdminPolicy() { ... }
```

Policies are defined in `DependencyInjection.cs`:

```csharp
services.AddAuthorizationBuilder()
    .AddPolicy("RequireAuthenticated", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
```

---

## Local Keycloak Setup (Quick Reference)

After running `docker-compose up -d`, Keycloak is available at `http://localhost:8080`.

**Admin console:** `http://localhost:8080/admin` (admin / admin)

**Steps to configure from scratch:**

1. Create realm `latam-platform`
2. Create client `latam-api`
   - Client authentication: **ON** (confidential client)
   - Valid redirect URIs: `http://localhost:3000/api/auth/callback/keycloak`
   - Web origins: `http://localhost:3000`
   - Copy the generated **Client Secret** — needed for the Next.js `.env`
3. Create Protocol Mapper on the client
   - Mapper type: User Attribute
   - User attribute: `tenant`
   - Token claim name: `tenant`
   - Claim JSON type: String
   - Add to access token: ON
4. Create realm roles: `admin`, `operator`
5. Create users and set their `tenant` attribute:
   - `admin.br` → tenant: `BR` → assign role: `admin`
   - `admin.ar` → tenant: `AR` → assign role: `admin`
   - `admin.cl` → tenant: `CL` → assign role: `admin`

**Useful endpoints (all under `http://localhost:8080/realms/latam-platform`):**

| Endpoint | Purpose |
|----------|---------|
| `/protocol/openid-connect/auth` | Authorization (login redirect) |
| `/protocol/openid-connect/token` | Token exchange |
| `/protocol/openid-connect/logout` | Logout |
| `/protocol/openid-connect/certs` | JWKS — public keys for token validation |
| `/.well-known/openid-configuration` | Discovery document — all endpoints listed |

---

## Summary

| Concept | What it is in our context |
|---------|--------------------------|
| Realm | `latam-platform` — our isolated auth space |
| Client | `latam-api` — the application registered in Keycloak |
| User | Single account, works across all countries |
| Tenant | Custom JWT claim (`BR`, `AR`, `CL`) — not a Keycloak concept |
| Role | `admin`, `operator` — defined in the realm, assigned per user |
| JWT | Signed token carrying identity + tenant + roles |
| PKCE | Security extension preventing code interception in the browser |
| JWKS | Public keys the API uses to validate tokens locally |
| Protocol Mapper | Injects the `tenant` attribute into the JWT |
