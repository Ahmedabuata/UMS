# UMS Docker Setup

This project is developed on Windows but deploys to Linux containers via Docker Compose.

## Prerequisites

- Docker Desktop (with WSL 2 backend) on Windows
- Docker Compose v2 (`docker compose`)

## Services

| Service   | Image                       | Port(s)  | Purpose                       |
|-----------|-----------------------------|----------|-------------------------------|
| ums-db    | postgres:15-alpine          | 5432     | PostgreSQL database           |
| ums-redis | redis:7-alpine              | 6379     | Redis cache / token storage   |
| ums-api   | local build (Dockerfile.api)| 8080     | .NET 8 UMS REST API           |
| ums-pgadmin | dpage/pgadmin4:latest     | 5050     | Database admin web UI         |

## Getting Started

> Use the legacy v2 command `docker-compose` or the v2 command `docker compose` (both are supported).

```bash
# Validate the compose file
docker-compose config

# Build and start all containers
docker-compose up --build

# Run in detached mode, then tail logs
docker-compose up -d --build
docker-compose logs -f ums-api
```

## Endpoints / Credentials

- **UMS API**: http://localhost:8080  (Swagger at `/swagger` when not in Docker environment)
- **pgAdmin**: http://localhost:5050
  - Email: `admin@ums.com`
  - Password: `Admin@123`
- **Database**:
  - Host: `localhost:5432` (from host) or `ums-db:5432` (inside the network)
  - Database: `ums_db`
  - User: `ums_user`
  - Password: `Ums_Pass_2024!`

## Initialization

On first startup, `ums-db` runs the init SQL script mounted from
`./database/DB-FINAL-CORRECT.sql` (mounted read-only into
`/docker-entrypoint-initdb.d/01-init.sql`). This creates the full 30-table schema.

The API then applies any additional EF Core migrations/`EnsureCreated`, and seeds an
admin user when the database is empty (`admin@ums.com` / `Admin@123`).

## Configuration

Connection strings and JWT settings are environment-driven. The API uses
`appsettings.Docker.json` (selected via `ASPNETCORE_ENVIRONMENT=Docker`), and the
compose file overrides values via `ConnectionStrings__*` / `JWT__*` environment
variables so no secrets are baked into images.

## Useful Commands

```bash
# Stop and remove containers (keeps named volume ums_pgdata)
docker-compose down

# Stop and remove containers AND the database volume
docker-compose down -v

# Rebuild a single service
docker-compose up --build ums-api

# View logs
docker-compose logs -f
```

## Notes

- The `Dockerfile.api` uses forward-slash paths and LF line endings so it builds
  correctly both on Windows (with BuildKit) and on Linux CI.
- A named volume `ums_pgdata` persists database data across container restarts.
- Remove `-v` from `down` if you want to keep data; use `down -v` to fully reset.
