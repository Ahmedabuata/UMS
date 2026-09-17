# Golden Rules Update — Rules 69-80

**Date:** 2026-09-15  
**Context:** Added after resolving security incidents SEC-2026-001 & SEC-2026-002  
**Status:** Active

---

## Overview

This document adds **12 new Golden Rules** (69-80) to the UMS IAM project, derived from two security incidents and their resolution. These rules complement the original 60 rules documented in \UMS-IAM-GOLDEN-V13.8.html\.

| Category | Rules | Focus |
|----------|-------|-------|
| Secrets Management | 69-71 | How to handle credentials |
| Git Hygiene | 72-75 | Keeping Git clean |
| Protection Layers | 76-77 | Defense in depth |
| Incident Response | 78-79 | Reacting to leaks |
| Continuous Improvement | 80 | Ongoing security |

---

## Category 1: Secrets Management (Rules 69-71)

### Rule 69 — Never Commit Secrets to Git

**Rule:** Never put a secret in a file that goes to Git. Not even once. Not even in a private repo. Not even "for testing".

**Rationale:**
- Git History is permanent
- Anyone with access (past, present, future) can retrieve secrets
- Private repos can become public
- Collaborators can be added later

**Do:**
- ✅ Use \dotnet user-secrets\ for local development
- ✅ Use \.env\ files for Docker (git-ignored)
- ✅ Use Azure Key Vault / HashiCorp Vault for production
- ✅ Use GitHub Secrets for CI/CD

**Don't:**
- ❌ Hardcode secrets in code
- ❌ Commit \ppsettings.Development.json\
- ❌ Commit \.env\ files
- ❌ Share secrets via email/chat

**Practical Example:**

\\\powershell
# ❌ WRONG — appsettings.Development.json committed to Git
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Password=RealPassword123!"
  }
}

# ✅ CORRECT — Use User Secrets
cd Identity.Microservice/src/Identity.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Password=RealPassword123!"
\\\

---

### Rule 70 — Use the Right Tool for Each Environment

**Rule:** Match the secrets tool to the environment. User Secrets ≠ Docker. Environment Variables ≠ Vault.

**The Right Tool Matrix:**

| Environment | Tool | Storage Location | Git Status |
|-------------|------|------------------|------------|
| **Local Development** | \dotnet user-secrets\ | \%APPDATA%\\Microsoft\\UserSecrets\\\ | ❌ Outside repo |
| **Docker** | \.env\ file | Project root | ❌ Git-ignored |
| **CI/CD** | GitHub Secrets | Repository Settings | ❌ Encrypted |
| **Production** | Azure Key Vault / HashiCorp Vault | Cloud | ❌ Never in code |

**Why It Matters:**

| Problem | Solution |
|---------|----------|
| User Secrets don't work in Docker | Use \.env\ for Docker |
| \.env\ doesn't work locally without setup | Use User Secrets for local |
| Environment Variables are visible in process list | Use Vault for production |

**Practical Example — Docker:**

\\\yaml
# docker-compose.yml
services:
  identity-api:
    env_file:
      - .env
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
\\\

\\\nv
# .env (git-ignored)
ConnectionStrings__IdentityDb=Host=identity-db;Password=RealPassword123!
Jwt__Key=RealJwtKeyHere
\\\

**Note:** Use \__\ (double underscore) instead of \:\ in environment variables.

---

### Rule 71 — Document Secrets Without Exposing Them

**Rule:** In documentation, use \[REDACTED]\ or placeholders. Never write actual secret values — even if the secret has been rotated.

**Rationale:**
- Documentation can be shared, published, or leaked
- Even old secrets provide patterns (length, format, origin)
- Attackers use patterns to guess other secrets
- Compliance (GDPR, SOC 2) requires no secret exposure in written records

**Do:**
\\\markdown
| Item | File | Value |
|------|------|-------|
| Database Password | appsettings.json | [REDACTED] |
| JWT Signing Key | appsettings.json | [REDACTED] |
\\\

**Don't:**
\\\markdown
| Item | File | Value |
|------|------|-------|
| Database Password | appsettings.json | ActualPass123! |
| JWT Signing Key | appsettings.json | SuperSecret2024 |
\\\

**Incident Report Template:**
\\\markdown
### Exposed Items
| Type | File | Value |
|------|------|-------|
| Database Password | appsettings.json | [REDACTED] |
| JWT Key | appsettings.json | [REDACTED] |
| SMTP Credentials | appsettings.json | [REDACTED] |
\\\

---

## Category 2: Git Hygiene (Rules 72-75)

### Rule 72 — .gitignore Before First Commit

**Rule:** Create \.gitignore\ before any \git init\. Have it ready with comprehensive rules for your tech stack.

**Why:** Files added to Git are hard to remove. Prevention is 100x easier than cleanup.

**Baseline .gitignore for .NET Projects:**

\\\gitignore
# Secrets
appsettings.Development.json
appsettings.Production.json
appsettings.Staging.json
appsettings.Local.json
secrets.json
.env
.env.*
!.env.example

# Certificates & Keys
*.pfx
*.p12
*.key
*.pem
*.crt
*.cer
*.snk

# Build artifacts
bin/
obj/
[Dd]ebug/
[Rr]elease/
*.dll
*.exe
*.pdb

# IDE
.vs/
.idea/
.vscode/*
!.vscode/extensions.json

# User-specific
*.user
*.suo
*.userosscache

# Logs
*.log
logs/

# OS
.DS_Store
Thumbs.db
\\\

**Baseline .gitignore for Node.js Projects:**

\\\gitignore
# Dependencies
node_modules/

# Environment
.env
.env.*
!.env.example

# Build
dist/
build/
.next/
out/

# Logs
*.log
npm-debug.log*

# IDE
.vscode/
.idea/

# OS
.DS_Store
Thumbs.db
\\\

---

### Rule 73 — .gitignore Does NOT Protect Already-Tracked Files

**Rule:** If a file is already committed, adding it to \.gitignore\ does nothing. You must first run:
\\\ash
git rm --cached <file>
\\\

**Why:** \.gitignore\ only prevents **untracked** files from being staged. Already-tracked files continue to be tracked.

**Correct Workflow:**

\\\powershell
# Step 1: Remove from tracking (keeps on disk)
git rm --cached appsettings.Development.json

# Step 2: Add to .gitignore
Add-Content .gitignore "appsettings.Development.json"

# Step 3: Verify
git status
# Should show: deleted: appsettings.Development.json

# Step 4: Commit
git commit -m "security: stop tracking appsettings.Development.json"

# Step 5: If already pushed — MUST also:
#   a) Rotate the exposed secret
#   b) Clean Git History (BFG / filter-repo)
\\\

**Common Mistake:**
\\\ash
# ❌ Does nothing if file is tracked
echo "appsettings.Development.json" >> .gitignore
git commit -m "ignored file"
# Result: file STILL in Git!
\\\

---

### Rule 74 — Review Files Before Every Commit

**Rule:** Never use \git add .\ without review. Read every file name. Search for words like \password\, \secret\, \key\, \	oken\.

**Daily Workflow:**

\\\powershell
# 1. Check what changed
git status

# 2. Review each file
git diff

# 3. Search for suspicious patterns
git diff --name-only | Select-String "password|secret|key|token|\.env|appsettings"

# 4. Add specifically (not "git add .")
git add src/Controllers/UsersController.cs
git add src/Services/UserService.cs

# 5. Final check
git diff --cached

# 6. Commit
git commit -m "..."
\\\

**What to Look For:**
- ❌ \ppsettings.Development.json\
- ❌ \.env\ files (only \.env.example\ is OK)
- ❌ \*.pfx\, \*.key\, \*.pem\
- ❌ \secrets.json\
- ❌ Files with "backup", "old", "test" in name
- ❌ Large files (>1 MB usually a mistake)

---

### Rule 75 — Clean Git History When Secrets Are Leaked

**Rule:** Removing a file from Git is not enough. If it was pushed, it lives in Git History forever — until you rewrite history.

**Tools:**

| Tool | Best For | Speed |
|------|----------|-------|
| **BFG Repo-Cleaner** | Simple cases, fast | Very fast |
| **git filter-repo** | Complex cases | Fast |

**When to Use:**
- ✅ Secret was pushed (even briefly)
- ✅ Repository is public
- ✅ You have no collaborators (or coordinated)

**BFG Workflow:**

\\\powershell
# 1. Rotate the secret FIRST (before any cleanup)
#    - Database: ALTER USER
#    - JWT: Generate new key
#    - API: Revoke + regenerate

# 2. Clone repository as mirror
cd C:\Temp
git clone --mirror https://github.com/USER/REPO.git repo-clean.git

# 3. Install Java + BFG
choco install openjdk bfg-repo-cleaner -y

# 4. Run BFG
java -jar bfg.jar --delete-files "secret-file.json" repo-clean.git

# 5. Clean Git
cd repo-clean.git
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# 6. Force push
git push --force --mirror

# 7. Sync local repository
cd C:\Projects\YourProject
git fetch origin --prune
git reset --hard origin/main

# 8. Verify
git log --all --full-history --oneline -- secret-file.json
# Should be empty
\\\

**Warning:**
- Force push rewrites history
- All collaborators must re-clone
- Backup the repository before starting

---

## Category 3: Protection Layers (Rules 76-77)

### Rule 76 — Use Defense in Depth (5 Layers Minimum)

**Rule:** Never rely on a single protection layer. Use at least 5 independent layers.

**The 5 Standard Layers:**

| # | Layer | Technology | Triggers On | Action |
|---|-------|------------|-------------|--------|
| 1 | Pre-Staging | \.gitignore\ | \git add\ | Blocks staging |
| 2 | Pre-Commit | \pre-commit\ hook | \git commit\ | Rejects commit |
| 3 | Dependencies | Dependabot | Daily/Weekly | Alerts + PRs |
| 4 | Secret Scan | TruffleHog | push/PR | Detects leaks |
| 5 | Vuln Scan | Trivy | Weekly + push | Detects CVEs |

**Why Multiple Layers:**

Any single layer can be bypassed:
- \.gitignore\ — bypassed with \git add -f\
- \pre-commit\ — bypassed with \git commit --no-verify\
- Dependabot — only checks dependencies
- TruffleHog — only checks what's pushed

**With 5 layers, all must fail for a secret to leak.**

**Configuration Files:**

\\\
.gitignore                                        ← Layer 1
.git/hooks/pre-commit                             ← Layer 2
.github/dependabot.yml                            ← Layer 3
.github/workflows/secret-scan.yml                 ← Layer 4
.github/workflows/dependency-check.yml            ← Layer 5
\\\

---

### Rule 77 — Automate Security Scanning from Day One

**Rule:** Enable Dependabot, secret scanning, and vulnerability scanning on day one. Not "later". Not "after launch".

**Day-One Checklist:**

\\\powershell
# 1. Enable Dependabot on GitHub
#    Settings → Security → Dependabot
#    ✅ Dependabot alerts
#    ✅ Dependabot security updates
#    ✅ Dependabot malware alerts

# 2. Add .github/dependabot.yml
# 3. Add .github/workflows/secret-scan.yml (TruffleHog)
# 4. Add .github/workflows/dependency-check.yml (Trivy)
# 5. Install pre-commit hook
# 6. Verify .gitignore coverage
\\\

**Why It Matters:**
- Manual reviews miss things
- Dependabot detected 6 vulnerabilities in seconds
- Weekly scans catch new CVEs automatically
- Alerts notify you before problems grow

**Example — Dependabot immediately detected:**

| Package | Severity | Fixed In |
|---------|----------|----------|
| Microsoft.Extensions.Caching.Memory | High | 8.0.1 |
| System.Text.Json | High | 8.0.5 |
| Newtonsoft.Json | High | 13.0.3 |

**Without automation:** Manual review might miss all 3.
**With automation:** Detected + fixed in under 30 minutes.

---

## Category 4: Incident Response (Rules 78-79)

### Rule 78 — Rotate Secrets Immediately on Leak

**Rule:** When a secret is exposed, assume it's compromised. Rotate it within minutes — not hours, not days.

**Rotation Guide:**

| Secret Type | Action | Side Effects |
|-------------|--------|--------------|
| **Database Password** | \ALTER USER user WITH PASSWORD 'new'\ | All apps need restart |
| **JWT Key** | Generate new (64+ chars) | All existing tokens invalidated |
| **API Key** | Revoke in provider dashboard + regenerate | All API calls need new key |
| **SMTP Credentials** | Change in provider dashboard | Emails may be delayed |
| **Encryption Keys** | Re-encrypt affected data | Data migration needed |

**Rotation Workflow:**

\\\powershell
# 1. Generate new secret (strong)
 = -join ((65..90) + (97..122) + (48..57) + (33,64,35,36,37,94,38,42) | Get-Random -Count 64 | ForEach-Object {[char]})

# 2. Update in primary system
#    - Database: ALTER USER
#    - JWT: Add to User Secrets
#    - API: Provider dashboard

# 3. Update in all consuming services
dotnet user-secrets set "Jwt:Key" 

# 4. Restart services
docker-compose down
docker-compose up -d

# 5. Verify services work
curl http://localhost:5000/health

# 6. Document the incident
\\\

**What NOT to Do:**
- ❌ Wait "to see if anyone noticed"
- ❌ Assume "it was old, no one cares"
- ❌ Rotate only the most obvious secret
- ❌ Skip rotation because "it's just a private repo"

---

### Rule 79 — Document Every Security Incident

**Rule:** After resolving a security incident, write a formal report. Include: what happened, impact, actions taken, prevention.

**Incident Report Template:**

\\\markdown
# Incident Report: SEC-YYYY-XXX

**Date:** YYYY-MM-DD  
**Severity:** Low / Medium / High / Critical  
**Status:** Resolved / In Progress / Open  
**Author:** [Name]

## Summary
[1-2 sentences describing the incident]

## Timeline
- YYYY-MM-DD HH:MM — Discovery
- YYYY-MM-DD HH:MM — Initial response
- YYYY-MM-DD HH:MM — Remediation
- YYYY-MM-DD HH:MM — Verification

## What Happened
[Detailed technical description]

## Exposed Items
| Type | File/Location | Value |
|------|---------------|-------|
| Database Password | appsettings.json | [REDACTED] |
| JWT Key | appsettings.json | [REDACTED] |

## Impact Assessment
- Who could have seen it?
- How long was it exposed?
- What could an attacker do?
- Was there any evidence of misuse?

## Root Cause
[Why did this happen?]

## Remediation Actions
- [ ] Rotate secrets
- [ ] Remove from Git tracking
- [ ] Clean Git History
- [ ] Update .gitignore
- [ ] Install protection layers
- [ ] Add monitoring

## Prevention
[What changed to prevent recurrence]

## Lessons Learned
[What we learned]
\\\

**Why It Matters:**
- Future developers understand what happened
- Justifies security decisions
- Provides reference for similar incidents
- Shows compliance (SOC 2, ISO 27001)

---

## Category 5: Continuous Improvement (Rule 80)

### Rule 80 — Review Security Posture Regularly

**Rule:** Security is not a one-time task. Schedule regular reviews.

**Recommended Schedule:**

| Frequency | Task | Duration |
|-----------|------|----------|
| **Weekly** | Review Dependabot PRs | 15 min |
| **Weekly** | Check for new alerts | 5 min |
| **Monthly** | Review access permissions | 30 min |
| **Monthly** | Update dependencies | 1 hour |
| **Quarterly** | Rotate secrets | 2 hours |
| **Quarterly** | Review policies | 2 hours |
| **Yearly** | Full security audit | 1 day |

**Continuous Monitoring (Automatic):**
- ✅ Dependabot alerts (real-time)
- ✅ Secret scanning (every push)
- ✅ Vulnerability scanning (weekly)
- ✅ Audit logs (real-time)
- ✅ Failed login attempts (real-time)

**Manual Checks:**

\\\powershell
# Weekly — Check for vulnerabilities
cd C:\Projects\YourProject
dotnet list package --vulnerable
cd identity-web
npm audit
cd ..

# Monthly — Check access permissions
# GitHub → Settings → Collaborators

# Quarterly — Rotate secrets
dotnet user-secrets list
# Review each, decide if rotation needed
\\\

---

## Summary Table — Rules 69-80

| # | Rule | Category |
|---|------|----------|
| 69 | Never commit secrets to Git | Secrets Management |
| 70 | Use the right tool per environment | Secrets Management |
| 71 | Document without exposing | Secrets Management |
| 72 | .gitignore before first commit | Git Hygiene |
| 73 | .gitignore doesn't protect tracked files | Git Hygiene |
| 74 | Review files before every commit | Git Hygiene |
| 75 | Clean Git History when leaked | Git Hygiene |
| 76 | Defense in depth (5 layers minimum) | Protection |
| 77 | Automate security scanning day one | Protection |
| 78 | Rotate secrets immediately on leak | Incident Response |
| 79 | Document every incident | Incident Response |
| 80 | Review security posture regularly | Continuous Improvement |

---

## Integration with Existing Rules

**Original 60 Rules (UMS-IAM-GOLDEN-V13.8.html):**
- Rules 1-10: Auth & JWT
- Rules 11-25: Backend API
- Rules 26-35: Frontend State
- Rules 36-45: UI/UX
- Rules 46-55: Debugging
- Rules 56-60: Final

**New 12 Rules (69-80):**
- Rules 69-71: Secrets Management
- Rules 72-75: Git Hygiene
- Rules 76-77: Protection Layers
- Rules 78-79: Incident Response
- Rule 80: Continuous Improvement

**Total: 80 Golden Rules**

---

## Context: Why These Rules Were Added

**Incidents:**
- **SEC-2026-001:** Development credentials in \ppsettings.json\ committed to Git
- **SEC-2026-002:** \docker-compose.yml.backup\ with hardcoded secrets

**Outcome:**
- 473+ files removed from Git tracking
- 55 commits cleaned with BFG Repo-Cleaner
- 3 High NuGet vulnerabilities fixed
- 5 protection layers installed
- Comprehensive documentation created

**Result:** Rules 69-80 codify the lessons learned, ensuring future projects avoid similar issues.

---

**Last Updated:** 2026-09-15  
**Total Golden Rules:** 80  
**Status:** Production-Ready
