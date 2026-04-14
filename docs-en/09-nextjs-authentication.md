# Next.js Authentication — Implementation Guide

> **Audience:** Frontend developers implementing the Next.js frontend for LATAM Platform V2.
> This document covers the full Auth.js + Keycloak integration, token forwarding to the API, and the security model.
> Read [Keycloak](08-keycloak.md) first if you are unfamiliar with the authentication concepts.

---

## Why This Approach

The Next.js frontend uses a **BFF (Backend for Frontend)** pattern. The Next.js server acts as a secure intermediary between the browser and both Keycloak and the `financial-api`:

- The browser **never sees the JWT** — it only holds a session cookie
- The client secret stays on the server — enables a confidential Keycloak client
- Refresh tokens are handled automatically — no manual expiry management
- XSS cannot steal tokens — the session cookie is `HttpOnly`

---

## What Changes vs. a Pure Browser Flow

| Concern | Pure browser (old HTML prototype) | Next.js BFF |
|---------|----------------------------------|-------------|
| PKCE generation | Browser JavaScript | Next.js server |
| Token storage | `sessionStorage` (visible to JS) | Server session (never in browser) |
| Client type | Public (no secret) | Confidential (secret on server) |
| Refresh token | Not implemented | Handled automatically by Auth.js |
| XSS impact | Token theft possible | Cookie is `HttpOnly` — no token to steal |
| `financial-api` calls | Browser sends Bearer directly | Server Component forwards Bearer |

---

## Stack

| Package | Purpose |
|---------|---------|
| `next-auth` (Auth.js v5) | Authentication framework — handles the full OAuth2 flow |
| `next` | App Router (Server Components, Route Handlers) |

---

## Environment Variables

Create a `.env.local` file at the root of the Next.js project:

```env
# Auth.js secret — generate with: openssl rand -base64 32
AUTH_SECRET=your-random-secret-here

# Keycloak
AUTH_KEYCLOAK_ID=latam-api
AUTH_KEYCLOAK_SECRET=<client-secret-from-keycloak-admin>
AUTH_KEYCLOAK_ISSUER=http://localhost:8080/realms/latam-platform

# API
FINANCIAL_API_URL=http://localhost:5288
```

> `AUTH_KEYCLOAK_SECRET` comes from Keycloak Admin Console:
> `Clients → latam-api → Credentials → Client Secret`

---

## Installation

```bash
npx create-next-app@latest financial-front --typescript --app
cd financial-front
npm install next-auth@beta
```

---

## Auth.js Configuration

```typescript
// auth.ts  (root of the project)
import NextAuth from "next-auth";
import Keycloak from "next-auth/providers/keycloak";

export const { handlers, auth, signIn, signOut } = NextAuth({
  providers: [
    Keycloak({
      clientId: process.env.AUTH_KEYCLOAK_ID!,
      clientSecret: process.env.AUTH_KEYCLOAK_SECRET!,
      issuer: process.env.AUTH_KEYCLOAK_ISSUER!,
    }),
  ],

  callbacks: {
    // Runs when the JWT session token is created or refreshed
    async jwt({ token, account }) {
      // On first login, persist the Keycloak access token and refresh token
      if (account) {
        token.accessToken = account.access_token;
        token.refreshToken = account.refresh_token;
        token.expiresAt = account.expires_at;
      }

      // Token still valid — return as-is
      if (Date.now() < (token.expiresAt as number) * 1000) {
        return token;
      }

      // Token expired — refresh it
      return refreshAccessToken(token);
    },

    // Runs when the session is accessed from the browser
    async session({ session, token }) {
      // Expose the access token to Server Components only
      // (never serialized into the browser response)
      session.accessToken = token.accessToken as string;
      session.error = token.error as string | undefined;
      return session;
    },
  },
});

async function refreshAccessToken(token: Record<string, unknown>) {
  try {
    const url = `${process.env.AUTH_KEYCLOAK_ISSUER}/protocol/openid-connect/token`;

    const response = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({
        grant_type: "refresh_token",
        client_id: process.env.AUTH_KEYCLOAK_ID!,
        client_secret: process.env.AUTH_KEYCLOAK_SECRET!,
        refresh_token: token.refreshToken as string,
      }),
    });

    const refreshed = await response.json();
    if (!response.ok) throw refreshed;

    return {
      ...token,
      accessToken: refreshed.access_token,
      refreshToken: refreshed.refresh_token ?? token.refreshToken,
      expiresAt: Math.floor(Date.now() / 1000) + refreshed.expires_in,
      error: undefined,
    };
  } catch {
    return { ...token, error: "RefreshAccessTokenError" };
  }
}
```

---

## Route Handler (Auth.js endpoint)

```typescript
// app/api/auth/[...nextauth]/route.ts
import { handlers } from "@/auth";

export const { GET, POST } = handlers;
```

This single file handles all Auth.js routes:
- `GET /api/auth/signin` — initiates login
- `GET /api/auth/callback/keycloak` — Keycloak redirect target
- `GET /api/auth/signout` — logout
- `GET /api/auth/session` — session info for client components

---

## TypeScript: Extending Session Types

```typescript
// types/next-auth.d.ts
import "next-auth";

declare module "next-auth" {
  interface Session {
    accessToken?: string;
    error?: string;
  }
}
```

---

## Protecting Pages

### Server Component (recommended)

```typescript
// app/dashboard/page.tsx
import { auth } from "@/auth";
import { redirect } from "next/navigation";

export default async function DashboardPage() {
  const session = await auth();

  if (!session) redirect("/api/auth/signin");

  return <div>Welcome, {session.user?.name}</div>;
}
```

### Middleware (protect entire routes at once)

```typescript
// middleware.ts  (root of the project)
import { auth } from "@/auth";

export default auth((req) => {
  if (!req.auth) {
    return Response.redirect(new URL("/api/auth/signin", req.url));
  }
});

export const config = {
  matcher: ["/dashboard/:path*", "/products/:path*"],
};
```

---

## Calling financial-api

The access token is available **only on the server**. Use it from Server Components or Route Handlers:

### From a Server Component

```typescript
// app/products/page.tsx
import { auth } from "@/auth";
import { redirect } from "next/navigation";

export default async function ProductsPage() {
  const session = await auth();

  if (!session) redirect("/api/auth/signin");

  // Token refresh error — force re-login
  if (session.error === "RefreshAccessTokenError") {
    redirect("/api/auth/signin");
  }

  const res = await fetch(`${process.env.FINANCIAL_API_URL}/api/products`, {
    headers: {
      Authorization: `Bearer ${session.accessToken}`,
    },
    cache: "no-store", // always fresh data
  });

  if (!res.ok) throw new Error(`API error: ${res.status}`);

  const products = await res.json();

  return (
    <main>
      <h1>Products</h1>
      <ul>
        {products.map((p: { id: string; name: string; price: number }) => (
          <li key={p.id}>{p.name} — {p.price}</li>
        ))}
      </ul>
    </main>
  );
}
```

### From a Route Handler (API proxy pattern)

Useful when Client Components need to fetch data:

```typescript
// app/api/products/route.ts
import { auth } from "@/auth";

export async function GET() {
  const session = await auth();

  if (!session) {
    return Response.json({ error: "Unauthorized" }, { status: 401 });
  }

  const res = await fetch(`${process.env.FINANCIAL_API_URL}/api/products`, {
    headers: {
      Authorization: `Bearer ${session.accessToken}`,
    },
  });

  const data = await res.json();
  return Response.json(data, { status: res.status });
}
```

Client Component calls `/api/products` (same origin) — the server proxies to `financial-api` with the Bearer token. The browser never interacts with `financial-api` directly.

---

## Tenant in the Session

The `tenant` claim is inside the JWT. To expose it to the frontend, extend the session callback:

```typescript
// auth.ts — extend the jwt and session callbacks
async jwt({ token, account, profile }) {
  if (account) {
    token.accessToken = account.access_token;
    token.tenant = (profile as Record<string, string>)?.tenant;
    // ...
  }
  return token;
},

async session({ session, token }) {
  session.accessToken = token.accessToken as string;
  session.tenant = token.tenant as string;
  return session;
},
```

```typescript
// types/next-auth.d.ts
declare module "next-auth" {
  interface Session {
    accessToken?: string;
    tenant?: string;
    error?: string;
  }
}
```

Usage in a Server Component:

```typescript
const session = await auth();
const tenant = session?.tenant; // "BR" | "AR" | "CL"
```

---

## Logout

```typescript
// app/components/LogoutButton.tsx
"use client";
import { signOut } from "next-auth/react";

export function LogoutButton() {
  return (
    <button onClick={() => signOut({ callbackUrl: "/" })}>
      Sign out
    </button>
  );
}
```

Auth.js handles the Keycloak logout automatically — it calls the `end_session_endpoint` from Keycloak's discovery document, ending the Keycloak session as well.

---

## Keycloak Configuration for Next.js

Changes required in Keycloak Admin Console compared to the HTML prototype:

| Setting | Old (HTML) | New (Next.js) |
|---------|-----------|--------------|
| Client authentication | OFF (public) | **ON (confidential)** |
| Valid redirect URIs | `http://localhost:3000` | `http://localhost:3000/api/auth/callback/keycloak` |
| Post logout redirect URI | `http://localhost:3000` | `http://localhost:3000` |

After enabling client authentication, copy the **Client Secret** from:
`Clients → latam-api → Credentials → Client Secret`

Paste it into `.env.local` as `AUTH_KEYCLOAK_SECRET`.

---

## Full Request Flow (Runtime)

```
Browser                    Next.js Server              financial-api / Keycloak
   │                             │                              │
   │── GET /products ───────────►│                              │
   │   (sends session cookie)    │── auth() reads session       │
   │                             │   gets stored JWT            │
   │                             │── GET /api/products ────────►│
   │                             │   Authorization: Bearer JWT  │
   │                             │◄── 200 products ─────────────│
   │◄── rendered HTML ───────────│                              │
   │   (no JWT in response)      │                              │
```

The browser sends a session cookie. The server exchanges it for the JWT internally and calls `financial-api`. The JWT never appears in the browser.

---

## Summary

| Concern | Implementation |
|---------|---------------|
| Auth framework | Auth.js v5 (`next-auth`) |
| Keycloak provider | Built-in `next-auth/providers/keycloak` |
| Token storage | Server session — never in the browser |
| Browser storage | `HttpOnly` session cookie only |
| Token refresh | Automatic — `refreshAccessToken` in `jwt` callback |
| API calls | Server Components / Route Handlers forward Bearer |
| Tenant access | Extended via `jwt` + `session` callbacks |
| Route protection | Middleware or per-page `auth()` check |
