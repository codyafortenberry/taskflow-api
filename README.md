# TaskFlow API

A compact, production-shaped task-management REST API — think *a much smaller
Jira*. Projects group tasks; tasks carry a title, description, status, priority,
assignee, and due date; users authenticate with JWT bearer tokens.

Built with **.NET 10**, **PostgreSQL** (EF Core), **JWT auth**, an
**OpenAPI/Swagger** UI, and a small single-page board UI.

> Built with AI assistance (Claude Code). See [CLAUDE.md](CLAUDE.md) for the
> architecture and conventions that guided the collaboration.

---

## Quick start

**Only prerequisite: Docker.** No .NET SDK or PostgreSQL install needed — the
container has everything.

One line, from clone to a running app:

```bash
git clone https://github.com/codyafortenberry/taskflow-api.git && cd taskflow-api && docker compose up --build
```

(Already cloned? Just `docker compose up --build`.)

That builds the API, starts PostgreSQL, applies migrations, and seeds demo data.
Then open:

| What | URL |
| --- | --- |
| Board UI | http://localhost:8080/ |
| Swagger UI (interactive docs) | http://localhost:8080/swagger |
| OpenAPI spec (JSON) | http://localhost:8080/swagger/v1/swagger.json |
| Health check | http://localhost:8080/health |

The database is migrated and seeded automatically on startup.

**Seeded demo accounts** (password `Password123`):

| Email | Role |
| --- | --- |
| `admin@taskflow.dev` | Admin |
| `member@taskflow.dev` | Member |

## Try it in 30 seconds

**Easiest — no terminal needed:**

1. Open the board at http://localhost:8080/ and sign in (`admin@taskflow.dev` /
   `Password123` is pre-filled).
2. Click **Copy token** in the top bar — your JWT is now on the clipboard.
3. Open http://localhost:8080/swagger, click **Authorize**, paste the token, and
   use *Try it out* on any endpoint.

**Or via the terminal:**

```bash
# 1. Log in and capture the token
TOKEN=$(curl -s http://localhost:8080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@taskflow.dev","password":"Password123"}' \
  | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')

# Optional — copy it straight to the clipboard:
#   macOS:    echo -n "$TOKEN" | pbcopy
#   Windows:  echo %TOKEN% | clip      (or `$TOKEN | clip` in PowerShell)
#   Linux:    echo -n "$TOKEN" | xclip -selection clipboard

# 2. List tasks
curl -s http://localhost:8080/api/v1/tasks -H "Authorization: Bearer $TOKEN"

# 3. Create a task (grab a projectId from GET /api/v1/projects first)
curl -s http://localhost:8080/api/v1/tasks \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"projectId":"<id>","title":"My first task","priority":"High"}'
```

## Local development

```bash
# Start only PostgreSQL in Docker
docker compose up -d db

# Run the API with hot config from appsettings.Development.json (port 5288)
dotnet run --project src/TaskFlow.Api
```

## Testing

```bash
dotnet test
```

- **Unit tests** cover password hashing, JWT generation, and validators.
- **Integration tests** exercise the real HTTP + EF Core + Npgsql stack against
  a throwaway PostgreSQL container via [Testcontainers] (needs a running Docker
  daemon). CI runs both on every push.

## API overview

All routes are under `/api/v1`. All endpoints require a bearer token except
`POST /auth/register` and `POST /auth/login`.

| Method | Route | Description |
| --- | --- | --- |
| POST | `/auth/register` | Create an account, returns a JWT |
| POST | `/auth/login` | Exchange credentials for a JWT |
| GET | `/auth/me` | Current user's profile |
| GET | `/users` | List users (for assignee pickers) |
| GET | `/users/{id}` | Get a user |
| GET | `/projects` | List projects (paged) |
| POST | `/projects` | Create a project |
| GET | `/projects/{id}` | Get a project |
| PUT | `/projects/{id}` | Update a project (owner/admin) |
| DELETE | `/projects/{id}` | Delete a project (owner/admin) |
| GET | `/tasks` | List tasks — filter, search, sort, paginate |
| POST | `/tasks` | Create a task |
| GET | `/tasks/{id}` | Get a task |
| PUT | `/tasks/{id}` | Update a task |
| PATCH | `/tasks/{id}/status` | Transition a task's status |
| DELETE | `/tasks/{id}` | Delete a task (creator/admin) |

**Task list query parameters:** `projectId`, `status`, `priority`,
`assigneeId`, `search`, `sort` (e.g. `-createdAt`, `priority`, `dueDate`),
`page`, `pageSize` (max 100).

### Response shapes

Lists are wrapped:

```json
{
  "items": [ /* ... */ ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 3,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

Errors use [RFC 9457 Problem Details], including validation errors:

```json
{
  "title": "Invalid request",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": { "Password": ["Password must be at least 8 characters."] },
  "traceId": "00-abc..."
}
```

## Security notes

- Passwords are hashed with **BCrypt** (work factor 12); plaintext is never
  stored. Login uses a uniform failure path to avoid user enumeration.
- **JWT** access tokens are validated for issuer, audience, lifetime, and
  signature. Role claims drive owner/admin authorization checks in the services.
- **Input validation** on every write via FluentValidation → `400` with
  structured errors.
- **Rate limiting** on the auth endpoints (fixed window) to blunt brute force.
- **Security headers** (`X-Content-Type-Options`, `X-Frame-Options`,
  `Referrer-Policy`) on every response.
- Secrets are supplied via configuration/environment — the keys checked in here
  are throwaway demo values. TLS is expected to terminate at the ingress/proxy
  in front of the API.

## Project structure

Every request follows one straight, predictable path — easy to trace and easy to change:

```
HTTP  →  Controller  →  Service  →  EF Core (AppDbContext)  →  PostgreSQL
         (thin)         (logic +      (data access)
                        auth checks)
```

```
src/TaskFlow.Api/
  Controllers/      Thin HTTP endpoints — bind input, call a service, return a result
  Services/         Business logic + data access (one interface + impl per feature)
  Contracts/        Request/response DTOs as records; entities never cross this boundary
  Domain/           Entities and enums (no framework concerns)
  Data/             AppDbContext, EF Core migrations, demo-data seeder
  Infrastructure/   Auth (JWT, hashing, current user), error handling, validation filter
  Validation/       FluentValidation validators (one per request DTO)
  Options/          Strongly-typed, validated configuration
  wwwroot/          Minimal single-page board UI (served at /)
tests/TaskFlow.Api.Tests/
  Unit/             Fast, no I/O (password hashing, JWT, validators)
  Integration/      Real API + PostgreSQL via Testcontainers
```

### Where to make a change

| To… | Touch (in order) |
| --- | --- |
| Add a field to a task | `Domain/Entities/TaskItem.cs` → migration → `Contracts/Tasks/` → `Validation/` → `Services/TaskService.cs` |
| Add an endpoint | `Services/*Service.cs` (logic) → `Controllers/*Controller.cs` (thin wrapper) |
| Add a new entity | `Domain/` → `AppDbContext` → migration → `Contracts/` → `Validation/` → `Services/` → `Controllers/` |
| Change a validation rule | `Validation/` |
| Change who can do what | `ICurrentUser` checks in the relevant service; `[Authorize]` on the controller |
| Change an error's status/shape | `Infrastructure/Errors/` |
| Change pagination limits | `Contracts/Common/PaginationParameters.cs` |
| Change config or secrets | `appsettings*.json` / environment variables |

Add a migration after changing an entity or the `AppDbContext`:

```bash
dotnet ef migrations add <Name> --project src/TaskFlow.Api --output-dir Data/Migrations
```

See [CLAUDE.md](CLAUDE.md) for the full conventions and extension checklist.

## Code quality & tooling

Guardrails that keep the codebase consistent:

- **`.editorconfig` + `dotnet format`** — a single formatting/style ruleset,
  enforced in CI and on commit.
- **`Directory.Build.props`** — centralized analyzer settings with
  **warnings-as-errors in Release**.
- **Husky.Net pre-commit hook** — runs `dotnet format --verify-no-changes` and a
  Release build before each commit (auto-installed on first build; no manual setup).
- **GitHub Actions CI** — format-check → build → test on every push and PR.
- **AI-assisted development** — [CLAUDE.md](CLAUDE.md) / [AGENTS.md](AGENTS.md)
  document the architecture and conventions for AI tools and humans alike.

## Configuration

| Key | Purpose |
| --- | --- |
| `ConnectionStrings__Default` | PostgreSQL connection string |
| `Jwt__SigningKey` | HS256 signing key (min 32 chars) |
| `Jwt__Issuer`, `Jwt__Audience` | Token issuer/audience |
| `Jwt__AccessTokenMinutes` | Token lifetime (default 60) |
| `Cors__AllowedOrigins__0` | Allowed CORS origins (defaults to any if unset) |

Double underscores map to nested config; set them as environment variables in
production. See [.env.example](.env.example).

## License

[MIT](LICENSE)

[Testcontainers]: https://testcontainers.com/
[RFC 9457 Problem Details]: https://www.rfc-editor.org/rfc/rfc9457
