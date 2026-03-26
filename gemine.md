
# Multi-Tenant SaaS Platform - Instructional Context

This document serves as the primary technical reference and architectural mandate for the development of the Multi-Tenant SaaS platform. All future AI interactions must adhere to the patterns and standards defined here.

## 1. Project Overview
A modular monolith SaaS platform designed for high isolation, scalability, and maintainability.

- **Primary Goal:** Provide a multi-tenant environment with physical data isolation and dynamic RBAC.
- **Core Architecture:** Modular Monolith with MediatR for decoupled module communication.
- **Multi-Tenancy Strategy:** **Schema-per-Tenant** using PostgreSQL schemas.

### Technology Stack
| Layer | Technology | Version |
|---|---|---|
| **Backend** | .NET | 8.0 |
| **Frontend** | React + Vite | 18 / 5 |
| **Database** | PostgreSQL | 16 |
| **Caching** | Redis | 7 |
| **Messaging** | MediatR (In-Process) | 12 |
| **ORM** | Entity Framework Core | 8 |
| **Realtime** | SignalR | .NET 8 |
| **Auth** | ASP.NET Core Identity + JWT | — |
| **State Mgmt** | Zustand | 4 |

---

## 2. Architectural Mandates

### 2.1 Multi-Tenancy (Schema-per-Tenant)
- Every sub-tenant has a dedicated schema in PostgreSQL (e.g., `tenant_acme`).
- **Isolation:** Middleware must set the `search_path` dynamically (e.g., `SET search_path TO tenant_acme, public;`).
- **Resolver:** Tenants are resolved via:
    1. Subdomain (e.g., `acme.app.com`)
    2. Header (`X-Tenant-Id`)
    3. JWT Claim (`tenantId`)

### 2.2 Modular Monolith Structure
Modules must be isolated in `/src/Modules`.
- **Communication:** Strictly via MediatR `IRequest` or `INotification`. No direct service-to-service dependencies across modules.
- **Persistence:** Each module should ideally have its own `DbContext` or defined domain boundaries.

### 2.3 Event-Driven Reliability (Outbox Pattern)
Cross-module side effects must use the Outbox Pattern. Domain events are persisted to an `OutboxMessages` table and processed by a background worker to ensure eventual consistency without distributed transactions.

---

## 3. Core Modules & Features

### 3.1 Identity & Impersonation
- Supports **Impersonation**: Main tenant admins can login as any sub-tenant user.
- **JWT Claims:** Must include `tenantId`, `impersonatedBy`, and `impersonatedByTenant` for audit tracking.

### 3.2 Dynamic RBAC + Delegation
Permissions are resolved via a hierarchy:
1. Direct User Permission
2. Department Permission
3. Company Permission
4. Global Role Permission
5. Default: Forbidden

### 3.3 Localization (DB-Driven)
- Translations stored in PostgreSQL, cached in Redis by namespace.
- Frontend uses `useTranslation(namespace)` for lazy-loading translations.

### 3.4 Audit Log (Partitioned)
- Automatic tracking via MediatR `AuditBehavior` pipeline.
- Database: `audit_logs` table MUST be **partitioned by month** (e.g., `audit_logs_2025_01`).

---

## 4. Development Standards & Conventions

### Backend (.NET)
- **Patterns:** CQRS with MediatR. All business logic in Handlers.
- **Tenant Context:** Use `ICurrentTenant` and `ICurrentUser` abstractions.
- **Migrations:** Use `FluentMigrator` for schema management.
- **Interceptors:** Use EF Interceptors to automatically apply schema switching and audit fields.

### Frontend (React)
- **Structure:** Modular components with TypeScript strict mode.
- **State:** Zustand for global state; React Query/Axios for data fetching.
- **Rich Text:** TipTap for help guide and content management.

### Security
- **Isolation Verification:** All integration tests must assert that data from Tenant A is unreachable by Tenant B.
- **Sanitization:** Use `DOMPurify` for any user-generated HTML (e.g., help guides).

---

## 5. Technical Details & Blueprints

### 5.1 Multi-Tenant SQL Structure

```sql
-- Global (public) schema
CREATE TABLE tenants (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(200) NOT NULL,
    slug        VARCHAR(100) NOT NULL UNIQUE,
    parent_id   UUID REFERENCES tenants(id),   -- NULL = main tenant
    is_main     BOOLEAN DEFAULT FALSE,
    settings    JSONB DEFAULT '{}',
    is_active   BOOLEAN DEFAULT TRUE,
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE users (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id     UUID NOT NULL REFERENCES tenants(id),
    email         VARCHAR(320) NOT NULL,
    password_hash VARCHAR(500),
    company_id    UUID,
    department_id UUID,
    is_active     BOOLEAN DEFAULT TRUE,
    created_at    TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE (tenant_id, email)
);

-- Tenant-specific schema (e.g., tenant_acme)
CREATE TABLE roles (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID NOT NULL REFERENCES tenants(id),
    name        VARCHAR(100) NOT NULL,
    description TEXT,
    UNIQUE (tenant_id, name)
);
```

### 5.2 MediatR Audit Behavior

```csharp
public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var methodName = typeof(TRequest).Name;
        var shouldLog = await _auditConfig.IsEnabledAsync(_currentTenant.Id, methodName);
        
        try {
            var response = await next();
            if (shouldLog) await _auditWriter.WriteAsync(new AuditEntry { ... , Result = "success" });
            return response;
        } catch (Exception) {
            if (shouldLog) await _auditWriter.WriteAsync(new AuditEntry { ... , Result = "error" });
            throw;
        }
    }
}
```

### 5.3 RBAC Permission Hierarchy Logic
When resolving `can_read`, `can_write`, etc.:
1. Check `permission_assignments` where `assignee_type = 'user' AND assignee_id = currentUser.Id`.
2. If null, check `assignee_type = 'department' AND assignee_id = currentUser.DepartmentId`.
3. If null, check `assignee_type = 'company' AND assignee_id = currentUser.CompanyId`.
4. If null, check global `role_id` assigned to the user.

---

## 6. Implementation Roadmap

### Phase 0: Infrastructure (Weeks 1-4)
- [ ] .NET Solution & Modular registration system.
- [ ] PostgreSQL Interceptor for schema switching.
- [ ] Auth middleware & Tenant resolver.
- [ ] React Skeleton & API Client.

### Phase 1: Core Modules (Weeks 5-10)
1. **Identity:** Login, Refresh, Impersonation.
2. **RBAC:** Dynamic permission engine & Role management.
3. **Org Structure:** Company/Department/User management.
4. **Localization:** Redis-backed translation system (Namespace granularity).
5. **Help/Audit:** Guide popup & Monthly partitioned logs.

### Phase 2: Domain Modules
Supplier Management, MDM (Material Data), Purchasing, Cost Control.

---

## 7. AI Instructions (LLM Context)
When working on this project:
1. **Context First:** Always reference the specific module schema defined in the blueprint.
2. **Strict Isolation:** Ensure every query/command respect the `search_path` and `tenantId`.
3. **Decoupling:** Never suggest direct references between `Modules`. Use MediatR events.
4. **Validation:** All code changes must be accompanied by tests that verify tenant boundary enforcement.

