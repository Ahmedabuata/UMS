\# Security Policy



\*\*Project:\*\* UMS IAM — Identity \& Access Management System  

\*\*Repository:\*\* \[Ahmedabuata/UMS](https://github.com/Ahmedabuata/UMS)  

\*\*Maintainer:\*\* Ahmed Almahallawi  

\*\*Last Updated:\*\* 2026-09-15



\---



\## Table of Contents



1\. Supported Versions

2\. Reporting a Vulnerability

3\. Secrets Policy

4\. Protection Layers

5\. Incident Response

6\. Incident Report SEC-2026-001

7\. Setup Instructions

8\. Security Best Practices



\---



\## Supported Versions



| Version | Supported | Status |

|---------|-----------|--------|

| 13.8.x  | Yes       | Current production |

| Less than 13.8 | No | Upgrade required |



\---



\## Reporting a Vulnerability



Please DO NOT open public issues for security vulnerabilities.



To report a vulnerability:

\- Use GitHub's private vulnerability reporting

\- Response Time: Within 48 hours

\- Disclosure: 90 days (responsible disclosure)



Include:

\- Description of the vulnerability

\- Steps to reproduce

\- Potential impact

\- Suggested fix (if any)



\---



\## Secrets Policy



\### Development (Local)



Use .NET User Secrets:



&#x20;   cd Identity.Microservice/src/Identity.Api

&#x20;   dotnet user-secrets init

&#x20;   dotnet user-secrets set "ConnectionStrings:IdentityDb" "Host=localhost;..."

&#x20;   dotnet user-secrets set "Jwt:Key" "your-dev-key-min-64-chars"



Storage: %APPDATA%\\Microsoft\\UserSecrets\\UserSecretsId\\secrets.json



Git Status: NEVER committed (outside repo)



\### Docker



Use .env file (excluded from Git):



&#x20;   cp .env.example .env

&#x20;   Edit with real values

&#x20;   docker-compose up -d



Template: .env.example (committed)



Real file: .env (excluded)



\### Production



Use one of:

\- Azure Key Vault (recommended)

\- HashiCorp Vault

\- AWS Secrets Manager

\- Environment variables (minimum)



Never:

\- Hardcode secrets in code

\- Commit appsettings.Development.json

\- Commit .env files

\- Share secrets via email/chat



\---



\## Protection Layers



This project uses 5 layers of protection:



\### Layer 1: .gitignore (Prevents Staging)



Blocks sensitive files:

\- appsettings.Development.json

\- appsettings.Production.json

\- .env

\- .env.\*

\- .env.example (exception)

\- \*.pfx, \*.key, \*.pem

\- secrets.json



\### Layer 2: pre-commit Hook (Prevents Commit)



Rejects commits with sensitive files (even with git add -f).



Location: .git/hooks/pre-commit



\### Layer 3: Dependabot (Dependency Updates)



\- Security alerts enabled

\- Malware alerts enabled

\- Security updates enabled

\- Weekly version updates



Config: .github/dependabot.yml



\### Layer 4: TruffleHog (Secret Scanning)



Runs on every push/PR:

\- Config: .github/workflows/secret-scan.yml

\- Scans Git history

\- Weekly + on every push



\### Layer 5: Trivy (Vulnerability Scanning)



Scans filesystem:

\- Config: .github/workflows/dependency-check.yml

\- SARIF upload to GitHub Security

\- Weekly scheduled



\---



\## Incident Response



\### Immediate (First 15 minutes)



1\. Rotate the exposed secret immediately

&#x20;  - JWT Key: invalidate all tokens

&#x20;  - DB password: ALTER USER

&#x20;  - API keys: revoke + regenerate



2\. Check Audit Logs



3\. Assess Exposure:

&#x20;  - Was the repository public?

&#x20;  - Who had access?

&#x20;  - How long was it exposed?



\### Short-Term (Within 24 hours)



4\. Remove from Git tracking:

&#x20;   git rm --cached sensitive-file



5\. Clean Git history (if public exposure):

&#x20;   java -jar bfg.jar --delete-files file

&#x20;   git reflog expire --expire=now --all

&#x20;   git gc --prune=now --aggressive

&#x20;   git push --force



6\. Document the incident



\### Long-Term (Within 1 week)



7\. Review all secrets

8\. Update policies

9\. Train team

10\. Add monitoring



\---



\## Incident Report SEC-2026-001



\### Summary



Date: 2026-09-15

Severity: Low (Private repo, dev-only values)

Status: Resolved



\### What Happened



Development credentials were committed to Git History in appsettings files within the deprecated ums-api project directory.



\### Exposed Items



| Item | Type | Severity | Since |

|------|------|----------|-------|

| PostgreSQL password | Credential | Low | 2026-09-01 |

| JWT signing key | Secret | Medium | 2026-09-01 |

| Mailtrap SMTP | Credential | Low | 2026-09-01 |

| RabbitMQ credentials | Credential | Low | 2026-09-01 |



\### Why It Was Low Severity



1\. Repository was Private (no external access)

2\. No Collaborators were added

3\. Values were development-only (production differs)

4\. System had no production deployment



\### Remediation Actions



\- Removed ums-api, frontend, database, \_DUMP from Git (473 files)

\- Removed appsettings with secrets

\- Moved all secrets to .NET User Secrets

\- Created appsettings.example.json (placeholders)

\- Updated .gitignore with comprehensive rules

\- Installed pre-commit hook

\- Enabled Dependabot + Secret Scanning workflows

\- Created .env.example for Docker

\- Patched 3 High NuGet vulnerabilities



\### Verification Commands



&#x20;   git ls-files | grep -E "appsettings.(Development|Production)|.env$"

&#x20;   gitleaks detect --source . --verbose

&#x20;   dotnet list package --vulnerable



\### Prevention



\- 3-layer protection implemented

\- Weekly scans scheduled

\- Automated dependency updates

\- Pre-commit hooks installed



\---



\## Setup Instructions



\### For New Developers



Step 1: Clone the repository



&#x20;   git clone https://github.com/Ahmedabuata/UMS.git

&#x20;   cd UMS



Step 2: Configure User Secrets for Identity.Api



&#x20;   cd Identity.Microservice/src/Identity.Api

&#x20;   dotnet user-secrets init

&#x20;   dotnet user-secrets set "ConnectionStrings:IdentityDb" "Host=localhost;Port=5432;Database=identity\_db;Username=ums\_user;Password=YOUR\_PASSWORD"

&#x20;   dotnet user-secrets set "Jwt:Key" "your-dev-key-min-64-chars"

&#x20;   dotnet user-secrets set "Jwt:RefreshTokenPepper" "your-pepper-min-32-chars"

&#x20;   dotnet user-secrets set "EmailSettings:SmtpUser" "your-mailtrap-user"

&#x20;   dotnet user-secrets set "EmailSettings:SmtpPass" "your-mailtrap-pass"



Step 3: Configure User Secrets for HR.Api



&#x20;   cd ../../../HR.Microservice/HR.Api

&#x20;   dotnet user-secrets init

&#x20;   dotnet user-secrets set "ConnectionStrings:HrConnection" "Host=localhost;Port=5432;Database=hr\_microservice\_db;Username=ums\_user;Password=YOUR\_PASSWORD"

&#x20;   dotnet user-secrets set "ConnectionStrings:UmsConnection" "Host=localhost;Port=5432;Database=ums\_db;Username=ums\_user;Password=YOUR\_PASSWORD"

&#x20;   dotnet user-secrets set "JWT:Key" "your-dev-key-min-64-chars"



Step 4: Configure Frontend



&#x20;   cd identity-web

&#x20;   cp .env.example .env

&#x20;   Edit .env with your values



Step 5: Configure Docker (optional)



&#x20;   cp .env.example .env

&#x20;   Edit .env with real values



Step 6: Run the services



Terminal 1 - Identity.Api:

&#x20;   cd Identity.Microservice/src/Identity.Api

&#x20;   dotnet run



Terminal 2 - HR.Api:

&#x20;   cd HR.Microservice/HR.Api

&#x20;   dotnet run



Terminal 3 - Frontend:

&#x20;   cd identity-web

&#x20;   npm install

&#x20;   npm run dev



\---



\## Security Best Practices



\### DO



\- Use dotnet user-secrets for development

\- Use environment variables for Docker

\- Use Azure Key Vault for production

\- Enable 2FA on GitHub

\- Review Dependabot PRs weekly

\- Run security scans before deployment

\- Rotate secrets quarterly

\- Use different secrets per environment



\### DO NOT



\- Never commit appsettings.Development.json

\- Never commit .env files

\- Never hardcode credentials

\- Never share secrets in chat/email

\- Never use production secrets locally

\- Never disable pre-commit hooks

\- Never ignore Dependabot alerts

\- Never use the same secrets across environments



\---



\## Contact



Author: Ahmed Almahallawi

GitHub: @Ahmedabuata

Repository: https://github.com/Ahmedabuata/UMS



\---



Last Review: 2026-09-15

Next Review: 2026-12-15 (Quarterly)



Stay Secure

