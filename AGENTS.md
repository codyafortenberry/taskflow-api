# AGENTS.md

This project's AI/agent guidance lives in [CLAUDE.md](CLAUDE.md). `AGENTS.md` is
the tool-agnostic convention; the two are kept in sync, so read **CLAUDE.md** for
the architecture, conventions, commands, and quality gates.

Quick pointers for automated contributors:

- Keep the build warning-clean; Release treats warnings as errors.
- Run `dotnet format` before committing (the Husky pre-commit hook enforces it).
- Follow the layering: `Controller → Service → EF Core`. DTOs in/out, never entities.
- Add a test with every behavior change (`dotnet test`).
