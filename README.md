# UMS IAM GOLDEN V13.8 — Complete Unified Documentation

> **Author:** Ahmed Almahallawi — 2026  
> **Tech Stack:** ASP.NET Core 8 + PostgreSQL 15 + React 18 + JWT  
> **Architecture:** Microservice + Zero Trust + Defense in Depth

![Production Ready](https://img.shields.io/badge/Production-Ready-brightgreen)
![Build](https://img.shields.io/badge/Build-0%20Errors-success)
![Golden Rules](https://img.shields.io/badge/Golden%20Rules-80-yellow)
![Vulnerabilities](https://img.shields.io/badge/Vulnerabilities-0-red)

---

## 📚 Table of Contents

1. [Introduction & Objectives](#-1-introduction--objectives)
2. [General Architecture](#-2-general-architecture)
3. [Backend Architecture](#-3-backend-architecture)
4. [Authentication & Authorization](#-4-authentication--authorization)
5. [Zero Trust Security](#-45-zero-trust-security)
6. [User Management](#-5-user-management)
7. [Role Management](#-6-role-management)
8. [Permission Management](#-7-permission-management)
9. [Frontend Architecture](#-8-frontend-architecture)
10. [Database Design](#-9-database-design)
11. [Problems & Solutions](#-10-problems--solutions)
12. [UX/UI System](#-11-uxui-system)
13. [Testing Strategy](#-12-testing-strategy)
14. [Results & Achievements](#-13-results--achievements)
15. [References & Appendices](#-14-references--appendices)
16. [Extended Golden Rules 69-80](#-15-extended-golden-rules-69-80)
17. [DevSecOps Pipeline](#-16-devsecops-pipeline)
18. [Abbreviations Glossary](#-17-abbreviations-glossary)

---

## 📋 Project Summary

| Metric | Value |
|--------|-------|
| **Chapters** | 17 |
| **Golden Rules** | 80 (60 Core + 8 Security + 12 Incident) |
| **Database Tables** | 17 |
| **Database Triggers** | 11 (6 Protection + 5 UPPERCASE) |
| **Role Protections** | 9 |
| **Protection Layers** | 5 |
| **Errors** | 0 |
| **Vulnerabilities** | 0 |

---

## 🎯 1. Introduction & Objectives

**Identity and Access Management (IAM)** is the enterprise security foundation ensuring the right individuals access the right resources at the right time for the right reasons. This document covers **UMS IAM Golden V13.8 Final** — a centralized microservice built with an iterative incremental approach following 80 Golden Rules.

We adopt **Zero Trust** — *never trust, always verify* — and **Defense in Depth** with 3 layers:

1. **Frontend Gate** hides UI
2. **Backend Policy + HasQueryFilter** double-checks
3. **Database Trigger** prevents protected deletion and enforces UPPER TRIM

### 6 Main Objectives

| # | Objective | Description | Status |
|---|-----------|-------------|--------|
| 1 | Centralized IAM Microservice | Single Identity.Microservice handles auth, users, roles, permissions, groups, audit | ✅ |
| 2 | Zero Trust JWT Clean | TokenService filters IsActive, Gate hides, RevokeAll on disable | ✅ |
| 3 | RBAC — 9 Protections | Roles 9 checks, Permissions 3 checks, ProtectedRoles HashSet | ✅ |
| 4 | Hot/Cold Archiving | Transactional Idempotent batch_id UNIQUE, Location is info | ✅ |
| 5 | Frontend Enterprise UX | Gate.jsx, 4 themes, i18n ar/en | ✅ |
| 6 | Build Succeeded 0 Errors | dotnet build clean, npm run build clean | ✅ |

---

## 🏗️ 2. General Architecture

### 2.1 Project Tree

```
📁 Identity.Microservice/
├── 📁 src/Identity.Api/
│   ├── 📁 Configurations/
│   │   ├── 📄 EmailSettings.cs               — SMTP Mailtrap
│   │   └── 📄 AuditArchiveJobConfiguration.cs — batch_id UNIQUE
│   ├── 📁 Controllers/                       — 10 files
│   │   ├── 📄 AuthController.cs              — /api/auth/login
│   │   ├── 📄 UsersController.cs             — CRUD + /me IDOR-proof
│   │   ├── 📄 RolesController.cs             — 9 protections
│   │   ├── 📄 PermissionsController.cs       — IsActive/IsSensitive
│   │   ├── 📄 GroupsController.cs            — CRUD
│   │   └── 📄 AuditLogsController.cs         — Smart Routing
│   └── 📁 Services/                          — GOLDEN V13.5
│       ├── 📄 TokenService.cs                — JWT Clean
│       ├── 📄 EmailService.cs                — Mailtrap SMTP
│       └── 📄 PasswordGenerator.cs           — 12 chars
```

### 2.2 Layered Architecture — 8 Layers

| # | Layer | Technology | Description |
|---|-------|-----------|-------------|
| 1 | **Frontend** | React 18 + Vite + i18n + 4 Themes | Dashboard, Users, Roles, Permissions, Groups, AuditLogs, Settings |
| 2 | **API Client** | axiosInstance.js — Bearer Interceptor | Request interceptor adds Authorization |
| 3 | **Controllers** | 10 Controllers | Auth, Users, Roles, Permissions, Groups |
| 4 | **Validators** | FluentValidation + GlobalValidators | ValidationFilter returns `{errors:{field:[messages]}}` |
| 5 | **Services** | TokenService GOLDEN V13.5 + EmailService | GenerateAccessTokenAsync filters IsActive |
| 6 | **Middleware** | ValidationFilter + JWT Bearer + Policies | AddJwtBearer Policies permission |
| 7 | **Data** | IdentityDbContext — DbSet + HasQueryFilter | HasQueryFilter(g=>g.IsActive) |
| 8 | **Database** | PostgreSQL 15 — 17 Tables + 11 Triggers | Hot audit_logs + Cold archive |

---

## ⚙️ 3. Backend Architecture

### RolesController — 9 Protections

| # | Protection | Endpoint | Risk Mitigated |
|---|------------|----------|----------------|
| 1 | Prevent rename of protected role | `PUT {id}` | SUPER_ADMIN rename bypass |
| 2 | Prevent disabling IsSystemRole | `PUT` | System role tampering |
| 3 | Prevent disabling IsActive | `PUT / PATCH status` | Lockout all admins |
| 4 | Prevent deactivation via PATCH | `PATCH {id}/status` | Denial of admin access |
| 5 | Protected 403 on DELETE | `DELETE` | Delete SUPER_ADMIN |
| 6 | Assigned Users 400 | `DELETE` | Orphan rows in user_roles |
| 7 | Assigned Permissions 400 | `DELETE` | Orphan rows in role_permissions |
| 8 | CanManageRoles HashSet | All | Privilege escalation |
| 9 | Auto-Generate RoleName UPPER | `POST` | Inconsistent naming |

---

## 🔐 4. Authentication & Authorization

**JWT Theory:** `Header.Payload.Signature` with HS256 HMAC-SHA256 signature. Claims include `sub`, `email`, `userId`, `username`, `jti`, `role`, `permission`. Expires in **15 minutes**. Refresh Token **7 days** with rotation, theft detection, subnet matching (3 octets IPv4).

### BEFORE/AFTER Comparison — JWT Clean Zero Trust

| ❌ BEFORE V13.4 — Vulnerable | ✅ AFTER V13.5 — Secure |
|------------------------------|--------------------------|
| JWT contains USER_DELETE even if IsActive=false | TokenService `Where IsActive` excludes USER_DELETE |
| Frontend Gate finds it and shows Delete button | JWT clean without disabled permission |
| User clicks DELETE /api/users/{id} | Frontend Gate reads JWT, not found, hides button |
| Backend Policy + HasQueryFilter rejects 403 but too late | Even if DELETE called manually, Policy rejects 403 |

### Zero Trust Flow V13.5

```
1. Admin disables USER_DELETE → PATCH status { isActive: false }
2. Backend: DB is_active = false + RevokeAllUserTokens + Audit DEACTIVATE
3. User logs in again → GenerateAccessTokenAsync Where IsActive
4. USER_DELETE excluded → JWT clean without the permission
5. Frontend Gate reads JWT, not found → hides Delete button
6. Even if DELETE called manually → Policy + HasQueryFilter → 403
✓ Full consistency: JWT + Backend Filter + Frontend Gate
```

---

## 🛡️ 4.5 Zero Trust Security

> **"Never Trust, Always Verify"**

The opposite of the traditional "Castle & Moat" model. In Zero Trust, there is **no trusted internal network**. Every request — whether inside or outside — must be verified.

### The 7 Core Principles

| # | Principle | Description |
|---|-----------|-------------|
| 1 | **Verify Explicitly** | Verify identity, location, and device every time |
| 2 | **Least Privilege** | Grant minimum required permissions, only for specific duration |
| 3 | **Assume Breach** | Assume attack has occurred, minimize blast radius |
| 4 | **Micro-segmentation** | Divide network into small, isolated segments |
| 5 | **Continuous Monitoring** | Monitor everything continuously (AuditLogs) |
| 6 | **Device Trust** | Do not trust the device, even if user is trusted |
| 7 | **Context-Aware** | Access depends on context (time, location, behavior) |

### Zero Trust Mapping to Golden Rules

| Zero Trust Principle | Golden Rule | Implementation |
|---------------------|-------------|----------------|
| Verify Explicitly | #1, #10 | JWT 15min + Bearer Interceptor |
| Assume Breach | #3, #6 | Token Theft Detection + HashToken Pepper |
| Least Privilege | #19, #20 | 9 Protections + ProtectedRoles HashSet |
| Continuous Monitoring | #53, #54 | Hot/Cold Archiving + Audit Logs |
| Micro-segmentation | #12, #13 | HasQueryFilter + Microservices |
| Context-Aware | #5 | Subnet Matching 3 octets IPv4 |
| Device Trust | #37 | Current Session Badge + Refresh Token |

---

## 👥 5. User Management

**IDOR Protection:** `UsersController.GetMe` → `/me` IDOR-proof. GetCurrentUserId with Fallback Chain `/me → /profile → {id}`. UpdateMe IDOR-proof — only current user can update their profile. ToDto `DisplayName` fallback if null, use `Username`.

### SendPasswordByEmail — 5 Steps

```
1. CreateUserRequestDto.SendPasswordByEmail = true
2. PasswordGenerator — RandomNumberGenerator 12 chars excludes 0/O/1/l/I
3. MustChangePassword = true (forced on first login)
4. EmailService.SendWelcomeEmailAsync HTML via Mailtrap SMTP
5. Audit Log: CREATE_USER_WITH_EMAIL
```

---

## 🎭 6. Role Management

**Auto-Generate RoleName:** Frontend `generateRoleName trim().toUpperCase().replace(/\s+/g,'_')` — "HR Manager" → "HR_MANAGER". Backend trigger `trg_uppercase_roles_fields` `BEFORE INSERT OR UPDATE` `UPPER(TRIM(role_name))`.

| Modification | Reason | Risk Mitigated |
|--------------|--------|----------------|
| `ProtectedRoles HashSet` | Prevent delete/rename of SUPER_ADMIN/SUPER_USER | System lockout |
| `CanManageRoles includes SUPER_USER` | Managers cannot manage super roles | Privilege escalation |
| `IgnoreQueryFilters for managers` | Managers see deactivated roles | UX dead end |
| `GroupJoin avoids N+1` | Fetch roles with user counts | O(n) performance |
| `GenerateRoleName UPPER` | Naming consistency | Case-sensitive search failure |

---
## 🔑 7. Permission Management

**3 Protections:** `ProtectedPermissions HashSet` prevents delete/rename of critical permissions: `USER_READ`, `ROLE_READ`, `PERMISSION_READ`.

**Toggles:**

- `IsSensitive` — PATCH sensitivity `{isSensitive:bool}` → Audit `MARK_SENSITIVE`
- `IsActive` — PATCH status `{isActive:bool}` → On disable: `RevokeTokensForPermissionUsersAsync` → JWT invalidated
- `Format Display Name` — Frontend `formatPermissionName` `String.replace(/_/g, ' ')`

### Zero Trust Toggle Flow

```
1. Admin toggles USER_DELETE IsActive = false
2. Backend PATCH status → DB is_active = false
3. RevokeAllUserTokens for users holding that permission
4. Audit DEACTIVATE
5. Next user request → 401 → re-login
6. GenerateAccessTokenAsync Where IsActive excludes USER_DELETE
7. JWT clean → Gate hides button
```

---

## 🎨 8. Frontend Architecture

**AuthContext Session Logic:** `parseJwtPayload UTF-8` (atob with UTF-8 decode for Arabic names). `isTokenExpired` checks exp. `extractUserFromToken` preserves baseUser.

### Race Condition Fix

| ❌ Problem | ✅ Solution | 💬 Discussion |
|-----------|-------------|---------------|
| Old name after F5 | `setUser(prev => ({...prev, ...newData}))` | Guarantees latest state (Rule #4, #57) |
| Stale closure | + localStorage sync | |

### axiosInstance Interceptors

```javascript
// ✅ Request Interceptor — adds Bearer token
axiosInstance.interceptors.request.use(config => {
    const token = localStorage.getItem('accessToken');
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
});
```

---

## 🗄️ 9. Database Design

**17 Tables:** `users`, `roles`, `permissions`, `user_roles`, `role_permissions`, `groups`, `user_groups`, `refresh_tokens`, `audit_logs` (HOT), `audit_logs_archive` (COLD), `audit_archive_jobs`, `user_profiles`, `email_verification_tokens`, `password_reset_tokens`, `blocked_ips`, `trusted_devices`, `user_2fa`.

**Global Query Filters:**

```csharp
modelBuilder.Entity<Group>().HasQueryFilter(g => g.IsActive);
```

---

## 🐛 10. Problems & Solutions

### 10.1 Dependency Error Paths

| Path | Issue | Fix |
|------|-------|-----|
| 🔴 Red | 401 Unauthorized | Bearer interceptor (Rule #10) |
| 🟠 Orange | 405 Method Not Allowed | POST /users/{id}/roles expects `{roleIds:[]}` |
| 🟡 Yellow | 400 Bad Request | `[HttpGet("count")]` + Route order |
| 🔵 Blue | JWT Contaminated | `Where IsActive` + RevokeAll + Gate hides |
| 🟢 Green | Swagger Empty | `AddEndpointsApiExplorer()` + `AddApiVersioning` |

### 10.2 Diagnostic Flows

| Flow | Issue | Fix |
|------|-------|-----|
| 1 | Settings HMR Failed | Isolate AuthContext named export + ErrorBoundary |
| 2 | Dashboard 401 | Request interceptor Bearer |
| 3 | Count 400 | `[HttpGet("count")]` returns `{count}` |
| 4 | AuditLogs 403 | Seed SUPER_ADMIN with all permissions |

### 10.3 Hot/Cold Archiving

```
🔥 HOT — audit_logs — Last 3 Months
   • timestamp >= NOW() - 3 months
   • Indexed: created_at DESC, user_id, action

❄️ COLD — audit_logs_archive
   • timestamp < cutoff
   • Partitioned or separate table

📋 ARCHIVING JOB
   • status = RUNNING
   • batchId = Guid.NewGuid()
   • BEGIN TRANSACTION → INSERT → DELETE → COMMIT
   • Idempotency: batch_id UNIQUE
```

### 10.4 Triggers — 11 Total

**6 Protection DELETE Triggers:**

- `trg_prevent_protected_role_delete`
- `trg_prevent_protected_permission_delete`
- `trg_prevent_last_super_admin_unlink`

**5 UPPERCASE Triggers:**

- `trg_uppercase_roles_fields`
- `trg_uppercase_permissions_fields`
- `trg_uppercase_groups_fields`

### 10.5 Complete Problems & Solutions — 16 Items

| # | Problem | Solution |
|---|---------|----------|
| 1 | 401 Unauthorized Dashboard | Bearer interceptor + axios.defaults |
| 2 | 405 Method Not Allowed | POST /users/{id}/roles `{roleIds}` |
| 3 | 400 Bad Request /users/count | `[HttpGet("count")]` + route order |
| 4 | JWT Contaminated | IsActive filter + RevokeAll |
| 5 | HMR Failed | Isolate AuthContext + ErrorBoundary |
| 6 | Race Condition | `setUser(prev =>)` + localStorage |
| 7 | HMACSHA256 ambiguity | Fully-qualified name |
| 8 | Mixed Guid vs Guid? | Mixed handling `Where != Guid.Empty` |
| 9 | Protected role deleted | 9 checks + 3 layers |
| 10 | Permission deactivation | RevokeTokensForPermissionUsers |
| 11 | AuditLogs 403 | Seed SUPER_ADMIN |
| 12 | Email SMTP not sending | EmailSettings + Mailtrap |
| 13 | PasswordGenerator weak | RandomNumberGenerator 12 chars |
| 14 | N+1 query | GroupJoin |
| 15 | Smart Routing HOT→COLD | Location is info + Concat |
| 16 | ReferenceError id | CSS only tree + details/summary |

---

## 🎨 11. UX/UI System

**CSS Variables:** `--bg-primary`, `--bg-secondary`, `--border-color`, `--accent-color`, `--text-main`, `--text-muted`.

**4 Themes:** `dark` / `blue` / `olive` / `light`. ThemeContext persists to localStorage.

### Gate.jsx — Zero Trust UI

```jsx
export const Gate = ({ permission, permissions, requireAll = false, role, fallback = null, children }) => {
    const { hasPermission, hasPermissions, hasRole, isSuperAdmin } = usePermissions();
    if (isSuperAdmin) return <>{children}</>;
    let isAllowed = true;
    if (role && !hasRole(role)) isAllowed = false;
    if (permission && !hasPermission(permission)) isAllowed = false;
    if (permissions?.length > 0 && !hasPermissions(permissions, requireAll)) isAllowed = false;
    if (!isAllowed) return fallback;
    return <>{children}</>;
};
```

---

## 🧪 12. Testing Strategy

### Build Output

```bash
$ dotnet build
Build succeeded.
    0 Error(s)
    12 Warning(s)

$ npm run build — identity-web
✓ built in 4.23s — 0 errors
```

### Recommended Tests

| Type | Test | Priority |
|------|------|----------|
| Unit Test | TokenService IsActive filter + HashToken | 🔴 High |
| Integration | POST /api/users + PUT /roles/{id}/status | 🔴 High |
| E2E | Login → Dashboard → Users CRUD | 🟡 Medium |
| Security | Protected role deletion → 403 | 🔴 High |

---

## 🏆 13. Results & Achievements

| Component | Status | Version | Notes |
|-----------|--------|---------|-------|
| Backend Infrastructure | ✅ 100% | V13.5 | Controllers 10, JWT Bearer |
| TokenService GOLDEN | ✅ 100% | V13.5 | IsActive filter + HMACSHA256 |
| Build Succeeded | ✅ 100% | V13.8 | 0 Errors + 12 Warnings |
| RBAC Users Roles | ✅ 100% | V13.5 | 9 protections + 3 permissions |
| Database PostgreSQL | ✅ 100% | V13 | 17 Tables + 11 Triggers |
| Security Layers | ✅ 100% | V13.8 | 5 Protection layers active |

### Challenges Overcome

- ✅ HMACSHA256 ambiguity → fully-qualified name
- ✅ Mixed Guid vs Guid? → mixed handling + JWT filter
- ✅ Protected roles deletion → 9 checks + 3 layers
- ✅ ReferenceError id → CSS only tree
- ✅ Secrets in Git History → BFG cleanup + 5-layer protection

---
## 📖 14. References & Appendices — 60 Core Golden Rules

### Appendix A — 60 Core Golden Rules

#### Auth — Rules 1-10

| # | Rule |
|---|------|
| 1 | JWT 15min + Refresh 7d + Rotation |
| 2 | HMACSHA256 pepperBytes fully-qualified |
| 3 | Theft Detection — Revoked reused → RevokeAll |
| 4 | `setUser(prev=>)` avoids Race Condition |
| 5 | Subnet Matching 3 octets IPv4 |
| 6 | HashToken pepper prevents rainbow |
| 7 | Location is info (no is_archived) |
| 8 | Enrich after Pagination only 20 records |
| 9 | ExecuteUpdateAsync bulk revoke |
| 10 | Bearer interceptor + axios.defaults.headers |

#### Backend API — Rules 11-25

| # | Rule |
|---|------|
| 11 | ValidationFilter returns `{errors:{field:[messages]}}` |
| 12 | HasQueryFilter IsActive auto excludes |
| 13 | IgnoreQueryFilters for managers |
| 14 | GroupJoin avoids N+1 |
| 15 | `[HttpGet("count")]` before `{id}` |
| 16 | roleIds not roleNames POST /users/{id}/roles |
| 17 | GenerateRoleName UPPER replace spaces |
| 18 | Format Display Name replace `_` with space |
| 19 | 9 protections roles + 3 permissions |
| 20 | ProtectedRoles HashSet SUPER_ADMIN |
| 21 | SendPasswordByEmail 5 steps |
| 22 | PasswordGenerator 12 chars excludes confusing |
| 23 | Mixed Guid handling `Where != Guid.Empty` |
| 24 | ToDto DisplayName fallback Username |
| 25 | Fallback Chain /me→/profile→{id} |

#### Frontend State — Rules 26-35

| # | Rule |
|---|------|
| 26 | parseJwtPayload UTF-8 |
| 27 | isTokenExpired + extractUserFromToken |
| 28 | axiosInstance Request Bearer + Response 401→refresh |
| 29 | `t('key','Fallback')` always |
| 30 | permissionSet = new Set O(1) vs some O(n) |
| 31 | Gate returns null hides button clean |
| 32 | `disabled={isProtected}` lock icon |
| 33 | IsSensitive Toggle + IsActive Toggle |
| 34 | Add→Save→Fetch→Close UserRolesModal |
| 35 | useDebounce 300ms search |

#### UI/UX — Rules 36-45

| # | Rule |
|---|------|
| 36 | Modal does NOT close on error |
| 37 | Current Session Badge |
| 38 | Search + Add User pagination |
| 39 | Theme+Language localStorage+document |
| 40 | Toast re-login after deactivation |
| 41 | CSS Variables --bg-primary 4 themes |
| 42 | Layout uses NAVIGATION_ITEMS |
| 43 | handleApiError extractErrorMessage |
| 44 | NAVIGATION_ITEMS permission check |
| 45 | UserRolesModal Add→Save→Fetch→Close |

#### Debugging — Rules 46-55

| # | Rule |
|---|------|
| 46 | Diagnostic Flows 4 visual Boxes |
| 47 | Dependency Architecture 5 colored paths |
| 48 | Settings HMR Failed isolate AuthContext |
| 49 | Dashboard 401 Bearer interceptor |
| 50 | Count 400 `[HttpGet("count")]` |
| 51 | AuditLogs 403 Seed SUPER_ADMIN |
| 52 | HMR ErrorBoundary + check Circular |
| 53 | Hot/Cold Archiving Idempotent batch_id |
| 54 | ExecuteSqlInterpolatedAsync safe |
| 55 | On failure ROLLBACK + FAILED |

#### Final — Rules 56-60

| # | Rule |
|---|------|
| 56 | Clean HTML no complex JS |
| 57 | setUser(prev=>) + localStorage after F5 |
| 58 | CSS only Tree ul/li ::before ::after |
| 59 | details/summary native collapsible |
| 60 | Build Succeeded 0 Errors Production Ready |

### Appendix F — Glossary

#### F.1 — Authentication Protocols

| Acronym | Full Name | Meaning |
|---------|-----------|---------|
| SAML | Security Assertion Markup Language | XML-based enterprise SSO |
| OAuth | Open Authorization | Authorization protocol |
| OAuth 2.0 | Open Authorization 2.0 | Industry standard |
| OIDC | OpenID Connect | Auth layer on OAuth 2.0 |

#### F.2 — Tokens

| Acronym | Full Name | Meaning |
|---------|-----------|---------|
| JWT | JSON Web Token | Signed identity token |
| JWS | JSON Web Signature | Signed JWT portion |
| JWK | JSON Web Key | Key representation |
| JWKS | JSON Web Key Set | Public keys |
| PKCE | Proof Key for Code Exchange | OAuth extension |
| DPoP | Demonstrating Proof-of-Possession | Token holder verification |
| TOTP | Time-based One-Time Password | Time-synced OTP |

#### F.3 — Access Control

| Acronym | Full Name | Meaning |
|---------|-----------|---------|
| RBAC | Role-Based Access Control | Permissions by role |
| ABAC | Attribute-Based Access Control | Permissions by attributes |
| ReBAC | Relationship-Based Access Control | Permissions by relationships |
| IAM | Identity and Access Management | Identity system |
| JIT | Just-in-Time Access | Temporary permission |
| JML | Joiner-Mover-Leaver | Employee lifecycle |
| PoLP | Principle of Least Privilege | Minimum permissions |

#### F.4 — Vulnerabilities

| Acronym | Full Name | Meaning |
|---------|-----------|---------|
| XSS | Cross-Site Scripting | Script injection |
| CSRF | Cross-Site Request Forgery | Forced requests |
| IDOR | Insecure Direct Object Reference | Access via ID change |
| SQLi | SQL Injection | Malicious SQL |
| CVE | Common Vulnerabilities and Exposures | Vulnerability DB |
| GHSA | GitHub Security Advisory | GitHub CVE DB |

#### F.5 — Standards

| Acronym | Full Name | Meaning |
|---------|-----------|---------|
| OWASP | Open Worldwide Application Security Project | Security org |
| NIST | National Institute of Standards and Technology | US standards |
| ISO | International Organization for Standardization | Global standards |
| GDPR | General Data Protection Regulation | EU privacy law |
| CIA | Confidentiality, Integrity, Availability | Security triad |

#### F.6 — Cryptographic Algorithms

| Acronym | Full Name | Meaning |
|---------|-----------|---------|
| HS256 | HMAC with SHA-256 | Symmetric signing |
| RS256 | RSA with SHA-256 | Asymmetric signing |
| ES256 | ECDSA with SHA-256 | Lightweight asymmetric |
| HMAC | Hash-based Message Authentication Code | Symmetric signing |
| RSA | Rivest-Shamir-Adleman | Asymmetric encryption |
| SHA | Secure Hash Algorithm | Hashing function |
| AES | Advanced Encryption Standard | Symmetric encryption |

---

## 🚨 15. Extended Golden Rules 69-80

> Twelve new rules added after resolving incidents **SEC-2026-001** and **SEC-2026-002**.

### Incident Context

**Incidents:**

- **SEC-2026-001:** `appsettings.json` credentials in Git History (14 days exposed)
- **SEC-2026-002:** `docker-compose.yml.backup` with hardcoded secrets (9 days)

**Outcome:**

- 473+ files removed from Git tracking
- 55 commits cleaned with BFG Repo-Cleaner
- 3 High NuGet CVEs fixed
- 5 protection layers installed
- Comprehensive SECURITY.md created (646 lines)

### Category 1 — Secrets Management (Rules 69-71)

| # | Rule | Description |
|---|------|-------------|
| 69 | Never commit secrets to Git | Not even once. Not even in private repo |
| 70 | Use the right tool per environment | User Secrets (dev) → .env (Docker) → Vault (prod) |
| 71 | Document without exposing | Use `[REDACTED]` in docs |

### Category 2 — Git Hygiene (Rules 72-75)

| # | Rule | Description |
|---|------|-------------|
| 72 | .gitignore before first commit | Have it ready BEFORE `git init` |
| 73 | .gitignore doesn't protect tracked files | Must run `git rm --cached <file>` |
| 74 | Review files before every commit | Never `git add .` without review |
| 75 | Clean Git History when leaked | Use BFG Repo-Cleaner |

### Category 3 — Protection Layers (Rules 76-77)

| # | Rule | Description |
|---|------|-------------|
| 76 | Defense in Depth — 5 layers minimum | 1) .gitignore 2) pre-commit 3) Dependabot 4) TruffleHog 5) Trivy |
| 77 | Automate security scanning day one | Enable Dependabot + secret scanning immediately |

### Category 4 — Incident Response (Rules 78-79)

| # | Rule | Description |
|---|------|-------------|
| 78 | Rotate secrets immediately on leak | Assume compromise. Rotate within minutes |
| 79 | Document every security incident | Write formal report: what, impact, actions, prevention |

### Category 5 — Continuous Improvement (Rule 80)

| # | Rule | Description |
|---|------|-------------|
| 80 | Review security posture regularly | Weekly: Dependabot. Monthly: Access. Quarterly: Rotate |

### Summary — 12 Rules at a Glance

| # | Rule | Category | Related |
|---|------|----------|---------|
| 69 | Never commit secrets to Git | 🔴 Secrets | #61, #66 |
| 70 | Use right tool per environment | 🔴 Secrets | #62 |
| 71 | Document without exposing | 🔴 Secrets | #67 |
| 72 | .gitignore before first commit | 🟡 Git Hygiene | #64 |
| 73 | .gitignore doesn't protect tracked files | 🟡 Git Hygiene | #63 |
| 74 | Review files before every commit | 🟡 Git Hygiene | #64 |
| 75 | Clean Git History when leaked | 🟡 Git Hygiene | #65 |
| 76 | Defense in Depth — 5 layers | 🔵 Protection | #12, #68 |
| 77 | Automate security scanning day one | 🔵 Protection | #68 |
| 78 | Rotate secrets immediately on leak | 🟡 Incident | #65 |
| 79 | Document every security incident | 🟡 Incident | #67 |
| 80 | Review security posture regularly | 🟢 Improvement | #63, #68 |

---

<!-- ✂️ نهاية الجزء الثالث — أخبرني عندما تنسخه لأسلمك الجزء الرابع## 🔧 16. DevSecOps Pipeline

**DevSecOps** means integrating security into every step of the build, not just at the end.

### 16.1 Git vs GitHub

| Git | GitHub |
|-----|--------|
| Local version control | Cloud hosting platform |
| Works offline | Needs internet |
| Software tool | Web service |

> 🚗 **Analogy:** Git is the **car**, GitHub is the **garage**.

### 16.2 Glossary of Terms

| Term | Meaning | Description |
|------|---------|-------------|
| Repository | Repo | Your project folder |
| Commit | Save | Save changes locally |
| **Push** | **Upload** | Send files from your machine to GitHub ⬆️ |
| **Pull** | **Download** | Fetch changes from GitHub to your machine ⬇️ |
| Fetch | Retrieve | Fetch changes without merging |
| Branch | Fork | Separate copy of the project |
| Merge | Combine | Combine two branches |
| Clone | Copy | Clone repo from GitHub |
| Stash | Store | Temporarily save changes |

### 16.3 CI/CD Pipeline

```
1. Write Code
      ↓
2. Push to GitHub
      ↓
3. Build — automatically
      ↓
4. Test — automatically
      ↓
5. Security Scan — automatically
      ↓
6. Deploy — automatically
```

| Before CI/CD | After CI/CD |
|--------------|-------------|
| Manual testing | Automated testing |
| Forget scans | All scans mandatory |
| Errors reach production | Errors caught early |
| Slow deployment | Deploy in minutes |

### 16.4 GitHub Actions Components

| Component | Description |
|-----------|-------------|
| Workflow | YAML file defining steps |
| Event | What triggers workflow (push, schedule) |
| Job | Group of steps on same runner |
| Step | Single step (command or action) |
| Action | Ready-made tool (e.g., actions/checkout) |
| Runner | Machine running the workflow |

### 16.5 SAST vs DAST vs Container Scanning

| Type | Scans | When | Example |
|------|-------|------|---------|
| SAST | Source code | During development | CodeQL |
| DAST | Running app | After deployment | OWASP ZAP |
| Container | Docker image | After build | Trivy |

### 16.6 Tools Used in This Project

| Tool | Function | Type |
|------|----------|------|
| Gitleaks | Secret scanning | SAST |
| Trivy Config | Dockerfile & IaC scan | IaC Scanning |
| Trivy Image | Docker image scan | Container Scanning |
| Dependabot | Auto-update dependencies | SCA |
| dotnet list --vulnerable | .NET library scan | SCA |
| npm audit | NPM library scan | SCA |

### 16.7 Common Errors & Solutions

| Error | Cause | Fix |
|-------|-------|-----|
| Resource not accessible by integration | Missing permissions | Add permissions |
| Code scanning is not enabled | SARIF needs GHAS | Use upload-artifact |
| Dockerfile: no such file | Wrong path | working-directory |
| generic-api-key | Placeholders | .gitleaks.toml |
| Node 20 deprecated | Old version | Update versions |

---

## 📚 17. Abbreviations Glossary

### 17.1 Git & DevOps

| Acronym | Full Name | Meaning |
|---------|-----------|---------|
| Git | Global Information Tracker | Version control |
| CI | Continuous Integration | Auto-test code |
| CD | Continuous Delivery | Auto-prepare release |
| DevOps | Development + Operations | Dev + Ops culture |
| DevSecOps | Dev + Security + Operations | DevOps with security |
| IaC | Infrastructure as Code | Infra in code form |
| SCA | Software Composition Analysis | Dependency analysis |
| SBOM | Software Bill of Materials | Component list |

### 17.2 Security Testing

| Acronym | Full Name | Meaning |
|---------|-----------|---------|
| SAST | Static Application Security Testing | Static code scan |
| DAST | Dynamic Application Security Testing | Runtime scan |
| IAST | Interactive Application Security Testing | Hybrid scan |
| SCA | Software Composition Analysis | Dependency scan |
| CVE | Common Vulnerabilities and Exposures | Vulnerability DB |
| GHSA | GitHub Security Advisory | GitHub CVE DB |

### 17.3 IAM & Authentication

| Acronym | Full Name | Meaning |
|---------|-----------|---------|
| IAM | Identity and Access Management | Identity system |
| JWT | JSON Web Token | Signed identity token |
| RBAC | Role-Based Access Control | By role |
| ABAC | Attribute-Based Access Control | By attributes |
| MFA | Multi-Factor Authentication | Multiple factors |
| SSO | Single Sign-On | One login for all |
| SAML | Security Assertion Markup Language | Enterprise SSO |
| OAuth | Open Authorization | Authorization protocol |
| OIDC | OpenID Connect | Auth layer on OAuth |
| JML | Joiner-Mover-Leaver | Employee lifecycle |
| PoLP | Principle of Least Privilege | Minimum permissions |
| JIT | Just-in-Time Access | Temporary access |

### 17.4 Tokens & Cryptography

| Acronym | Full Name | Meaning |
|---------|-----------|---------|
| JWT | JSON Web Token | Signed token |
| JWS | JSON Web Signature | Signed JWT |
| JWK | JSON Web Key | Key representation |
| JWKS | JSON Web Key Set | Public keys |
| PKCE | Proof Key for Code Exchange | OAuth extension |
| DPoP | Demonstrating Proof-of-Possession | Token holder proof |
| TOTP | Time-based One-Time Password | Time-synced OTP |
| HMAC | Hash-based Message Authentication Code | Symmetric signing |
| HS256 | HMAC with SHA-256 | JWT signing |
| RS256 | RSA with SHA-256 | Asymmetric signing |
| ES256 | ECDSA with SHA-256 | Lightweight signing |
| AES | Advanced Encryption Standard | Symmetric encryption |
| RSA | Rivest-Shamir-Adleman | Asymmetric encryption |
| SHA | Secure Hash Algorithm | Hashing algorithm |

### 17.5 Vulnerabilities

| Acronym | Full Name | Meaning |
|---------|-----------|---------|
| XSS | Cross-Site Scripting | Script injection |
| CSRF | Cross-Site Request Forgery | Forced requests |
| IDOR | Insecure Direct Object Reference | Access via ID |
| SQLi | SQL Injection | Malicious SQL |
| RCE | Remote Code Execution | Execute code remotely |
| SSRF | Server-Side Request Forgery | Server-side requests |

### 17.6 Standards & Organizations

| Acronym | Full Name | Meaning |
|---------|-----------|---------|
| OWASP | Open Worldwide Application Security Project | Security community |
| NIST | National Institute of Standards and Technology | US standards |
| ISO | International Organization for Standardization | Global standards |
| GDPR | General Data Protection Regulation | EU privacy law |
| CIA | Confidentiality, Integrity, Availability | Security triad |

### 17.7 Technology Stack

| Acronym | Full Name | Meaning |
|---------|-----------|---------|
| API | Application Programming Interface | Software interface |
| REST | Representational State Transfer | API architecture |
| JSON | JavaScript Object Notation | Data format |
| XML | Extensible Markup Language | Data format |
| ORM | Object-Relational Mapping | DB mapping |
| SPA | Single Page Application | Web app type |
| HMR | Hot Module Replacement | Vite feature |
| SSR | Server-Side Rendering | Rendering type |
| CSR | Client-Side Rendering | Rendering type |
| UI | User Interface | User interface |
| UX | User Experience | User experience |

---

## 🏁 Final Status

<div align="center">

### ✅ PRODUCTION READY

**UMS IAM GOLDEN V13.8 FINAL**

**Ahmed Almahallawi — 2026**

![Errors](https://img.shields.io/badge/Errors-0-brightgreen)
![Production](https://img.shields.io/badge/Production-Ready-blue)
![Zero Trust](https://img.shields.io/badge/Zero%20Trust-✓-orange)
![Vulnerabilities](https://img.shields.io/badge/Vulnerabilities-0-red)

**17 Chapters — 80 Golden Rules — 17 Tables — 11 Triggers — 9 Protections — 5 Security Layers**

</div>

---

## 📝 License

MIT License

Copyright (c) 2026 Ahmed Ata Almahallawi

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

---

<!-- ✅ نهاية README.md الكامل -->
->


<!-- ✂️ نهاية الجزء الثاني — أخبرني عندما تنسخه لأسلمك الجزء الثالث -->
<!-- ✂️ نهاية الجزء الأول — أخبرني عندما تنسخه لأسلمك الجزء الثاني -->



