# CLAUDE.md

Guidance for Claude Code (and any AI assistant) working in this repository.
This project was built with AI assistance; this file keeps that collaboration
consistent by documenting the architecture, conventions, and workflows.

## What this is

TaskFlow is a compact task-management REST API (think "a much smaller Jira"):
projects contain tasks; tasks have a title, description, status, priority,
assignee, and due date; users authenticate with JWT bearer tokens. It ships
with an OpenAPI/Swagger UI and a minimal single-page board UI.

## Tech stack

- **.NET 10** Web API (controllers + a service layer)
- **PostgreSQL** via **EF Core 10** (Npgsql provider), code-first migrations
- **JWT bearer** auth; passwords hashed with **BCrypt** (work factor 12)
- **FluentValidation** for request validation
- **Swashbuckle** for the OpenAPI 3 document + Swagger UI
- **xUnit** + **Testcontainers** (real PostgreSQL) for tests
- **Docker Compose** for one-command local runs

## Architecture & layering

Request flow: `Controller → Service → EF Core DbContext → PostgreSQL`.

- **Controllers** (`Controllers/`) are thin: bind input, call a service, shape
  the HTTP result. No business logic or EF queries live here.
- **Services** (`Services/`) own business rules, authorization checks, and all
  data access. They accept/return DTOs, never leak entities.
- **Contracts** (`Contracts/`) are the request/response DTOs — immutable
  `record` types. Entities are never serialized directly.
- **Domain** (`Domain/`) holds entities and enums with no framework concerns.
- **Infrastructure** (`Infrastructure/`) holds cross-cutting concerns: auth
  (JWT, password hashing, current-user accessor), error handling, and the
  validation filter.
- **Data** (`Data/`) holds the `AppDbContext`, migrations, and the seeder.

## Conventions

- **DTOs in, DTOs out.** Never accept or return EF entities from a controller.
  Map with the extension methods in `Services/Mappings.cs`.
- **Errors are exceptions.** Throw the domain exceptions in
  `Infrastructure/Errors/AppExceptions.cs` (`NotFoundException`,
  `ConflictException`, `ForbiddenException`, `ValidationFailedException`). The
  `GlobalExceptionHandler` turns them — and FluentValidation failures — into
  RFC 9457 `ProblemDetails`. Do not return ad-hoc error objects.
- **Validation** lives in `Validation/` as FluentValidation validators and runs
  automatically via `ValidationActionFilter`. Add a validator per request DTO;
  don't hand-roll checks in controllers.
- **Authorization** is enforced in services via `ICurrentUser` (e.g. only an
  owner or admin may mutate a project). Controllers use `[Authorize]`.
- **Async everywhere** with `CancellationToken` threaded through to EF Core.
- **Read queries** use `AsNoTracking()`. List endpoints return
  `PagedResponse<T>` and cap page size at 100.
- Keep controllers RESTful: correct status codes, `CreatedAtRoute` for 201s,
  `204` for deletes.

## Common commands

```bash
# Run the whole stack (API + PostgreSQL) — API on http://localhost:8080
docker compose up --build

# Local dev loop (needs a Postgres; `docker compose up -d db` provides one)
dotnet run --project src/TaskFlow.Api        # http://localhost:5288

# Tests (unit + integration; integration needs a running Docker daemon)
dotnet test

# Format the code (also enforced by the pre-commit hook and CI)
dotnet format

# Add a migration after changing entities or the DbContext
dotnet ef migrations add <Name> --project src/TaskFlow.Api --output-dir Data/Migrations
```

## Code quality & tooling

- **`.editorconfig`** defines formatting and code-style rules; **`dotnet format`**
  applies/verifies them. CI fails on unformatted code.
- **`Directory.Build.props`** centralizes settings for all projects: nullable,
  implicit usings, Roslyn analyzers (`EnforceCodeStyleInBuild`), and
  **warnings-as-errors in Release**. Keep the build clean.
- **Husky.Net** installs a **pre-commit hook** (`.husky/`) that runs
  `dotnet format --verify-no-changes` and a Release build. It bootstraps itself on
  first local build (`Directory.Build.targets`); skipped in CI and Docker.
- **CI** (`.github/workflows/ci.yml`) runs format-check → build → test on every
  push and PR.

Docs: Swagger UI at `/swagger`, raw spec at `/swagger/v1/swagger.json`,
health check at `/health`, the board UI at `/`.

Seeded logins (development): `admin@taskflow.dev` / `Password123` (Admin),
`member@taskflow.dev` / `Password123` (Member).

## When adding a feature (checklist)

1. Add/extend the entity in `Domain/` and configure it in `AppDbContext`.
2. Create a migration.
3. Add request/response DTOs in `Contracts/`.
4. Add a FluentValidation validator in `Validation/`.
5. Put the logic in a service (interface + implementation) under `Services/`.
6. Add a thin controller action with `[ProducesResponseType]` attributes.
7. Add tests: a unit test for pure logic, an integration test for the endpoint.

## Guardrails

- Never commit real secrets. The signing keys in `appsettings.Development.json`
  and `docker-compose.yml` are throwaway demo values; production must supply
  `Jwt__SigningKey` (and the connection string) via environment/secret store.
- Keep the build warning-clean (`dotnet build` → 0 warnings).
- Configuration is read at startup from `builder.Configuration`; provide test
  config via environment variables so it is present at `CreateBuilder` time
  (see `tests/.../TaskFlowApiFactory.cs`).
