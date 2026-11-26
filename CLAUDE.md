# CLAUDE.md — Comandas

Practice/portfolio project: a restaurant order-management system built to learn OOP, SOLID, design patterns, Clean Architecture, EF Core, microservices and Docker. **Clarity and teaching value beat cleverness.** The roadmap and progress live in [PLAN.md](PLAN.md).

## Working agreement

1. Work **one PLAN.md step at a time**. Never start the next step without the user's OK.
2. **Explain the pattern before implementing it**: before writing code for a step, explain in 3–5 lines which pattern/principle applies and why it fits here.
3. Implement → `dotnet build` → `dotnet test`. Report real results (including failures).
4. Mark the step `- [x]` in PLAN.md and suggest a **Conventional Commits** message. Do not commit unless asked.
5. When a design decision has reasonable alternatives (see PLAN.md §5), present the options with pros and cons and **let the user choose**.
6. At the end of each phase: run `dotnet test` (and from phase 3 on `docker compose up`), then write `docs/phase-N.md` summarizing what was learned.
7. **Language**: code, comments, commit messages and all documentation (PLAN.md, `docs/`, README) in **English**. Explanations and conversation with the user in **Spanish**.
8. Don't add features, packages or abstractions that the current step doesn't need (YAGNI). If something seems missing from the plan, propose it instead of adding it silently.

## Solution layout

- `src/BuildingBlocks/Comandas.SharedKernel` — base types (`Entity`, `AggregateRoot`, `ValueObject`, `IDomainEvent`, `DomainException`).
- `src/BuildingBlocks/Comandas.Contracts` — integration events only (records, no behavior).
- `src/Services/<Service>/<Service>.{Domain,Application,Infrastructure,API}` — one Clean Architecture service per bounded context.
- `src/Gateway/Comandas.Gateway` — YARP. `src/Web/comandas-web` — React frontend.
- `tests/<Project>.Tests` (unit), `tests/<Service>.IntegrationTests`, `tests/Comandas.ArchitectureTests`.

## Dependency rules (enforced by architecture tests)

| Project | May reference | Must NOT reference |
|---|---|---|
| Domain | SharedKernel | Application, Infrastructure, API, EF Core, MediatR, any framework |
| Application | Domain, SharedKernel, (Contracts) | Infrastructure, API, EF Core provider packages |
| Infrastructure | Application, Domain, SharedKernel, Contracts | API |
| API | Application, Infrastructure (composition root only) | — no business logic here |

- A service never references another service's projects. Services communicate **only through events** defined in `Comandas.Contracts`.
- Each service owns its database; no cross-database queries.
- Domain events (inside a service, in Domain) ≠ integration events (between services, in Contracts). Map one to the other explicitly in Application/Infrastructure. Naming: domain events end in `DomainEvent` (`OrderConfirmedDomainEvent`); integration events do not (`OrderConfirmed`). Both are immutable `record`s, raised only after the state change succeeded.

## C# conventions

- File-scoped namespaces; namespace = folder path (`Comandas.Orders.Domain.Orders`). One public type per file.
- `PascalCase` types/members, `_camelCase` private fields, `camelCase` locals/parameters, `I` prefix for interfaces, `Async` suffix on async methods (with a `CancellationToken` parameter last).
- Classes are `sealed` unless designed for inheritance. Prefer `record` for DTOs, commands, queries and events.
- Nullable reference types on; warnings are errors. No `!` null-forgiving operator without a comment explaining why.
- Comments explain **why** (and name the pattern where it helps learning), not what.

## Domain rules

- **No public setters.** State changes only through methods that enforce invariants; violations throw `DomainException` with a clear message.
- Construction through static factory methods (`Order.Create(...)`) or validating constructors; a private parameterless constructor only for EF Core.
- Collections exposed as `IReadOnlyCollection<T>` over a private `List<T>` backing field.
- Value objects are immutable and compared by value (`Money`, `Quantity`). Use them instead of primitives for domain concepts.
- No framework attributes (`[Required]`, `[Key]`, …) in Domain — mapping lives in Infrastructure.
- Time comes from `TimeProvider`, never `DateTime.Now`.

## Application rules

- CQRS: `VerbNounCommand` / `GetSomethingQuery` + `...Handler` (one use case per handler), `...Validator` for FluentValidation.
- Handlers depend on interfaces declared in Application (`IOrderRepository`, `IUnitOfWork`, …), not on `DbContext` (query-side exception per decision D5 in PLAN.md).
- Queries return DTOs, never entities. Commands return at most an id.

## Infrastructure rules (EF Core)

- One `IEntityTypeConfiguration<T>` per entity in `Persistence/Configurations`. No data annotations.
- Value objects as owned types; enums/states stored as strings; `RowVersion` on aggregate roots that can be edited concurrently.
- Read-only queries use `AsNoTracking()` and project to DTOs.
- Migrations in `Persistence/Migrations`, named in PascalCase describing the change (`AddDiscountToOrder`). Applied on API startup.
- Connection strings and secrets come from configuration/environment variables — never hard-coded (local dev values go in `appsettings.Development.json` or `.env`, which is git-ignored).

## API rules

- Thin endpoints: map HTTP → command/query → HTTP. No business logic.
- Errors are returned as RFC 9457 **ProblemDetails** via the global exception handler.
- Routes: `/api/<resource>` plural, kebab-case. Every API exposes `/health/live` and `/health/ready`.

## Testing conventions

- xUnit + **AwesomeAssertions** (Apache 2.0 fork of FluentAssertions, same `Should()` API — do NOT add `FluentAssertions` v8+) + Testcontainers.
- Pinned by license (decision D1): **MediatR 12.5.0** (do not upgrade to 13+), **MassTransit 8.x** (do not upgrade to 9+).
- Test names: `Method_Scenario_ExpectedResult` (e.g. `Confirm_WhenOrderIsDelivered_ThrowsDomainException`).
- Arrange / Act / Assert sections. Domain tests are pure (no I/O, no mocks needed). Integration tests use real SQL Server/RabbitMQ in containers — Docker must be running.
- Every domain rule gets a test; every bug fix gets a regression test.

## Commands

```powershell
# Build and test
dotnet build Comandas.slnx
dotnet test Comandas.slnx
dotnet test --project tests/Orders.Domain.Tests  # one project (Microsoft.Testing.Platform syntax)

# Local SQL Server (until phase 3's docker compose). One-time setup; use the SAME password in both commands.
#   The connection string is a dev secret: it lives in User Secrets, never in the repo.
docker run -d --name comandas-sqlserver -e ACCEPT_EULA=Y -e MSSQL_PID=Developer -e "MSSQL_SA_PASSWORD=<password>" -p 1433:1433 -v comandas-sqlserver-data:/var/opt/mssql mcr.microsoft.com/mssql/server:2022-latest
dotnet user-secrets set "ConnectionStrings:OrdersDb" "Server=localhost,1433;Database=OrdersDb;User Id=sa;Password=<password>;TrustServerCertificate=True" --project src/Services/Orders/Orders.API
docker start comandas-sqlserver    # later sessions

# EF Core migrations (local tool: dotnet tool restore). Creating one needs no database, only a non-empty connection string.
$env:ConnectionStrings__OrdersDb = "Server=design-time-only"
dotnet ef migrations add <Name> --project src/Services/Orders/Orders.Infrastructure --startup-project src/Services/Orders/Orders.API --output-dir Persistence/Migrations

# Run an API locally (needs SQL Server running). Migrations + seed are applied on startup.
#   User Secrets load only in the Development environment.
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/Services/Orders/Orders.API

# Docker (from phase 3)
docker compose up -d sqlserver rabbitmq redis    # infra only (final form decided in D7)
docker compose up --build                        # whole system
docker compose down                              # stop (add -v to wipe volumes)

# Frontend (phase 5)
cd src/Web/comandas-web; npm install; npm run dev
```

## Git

- Conventional Commits with a scope: `feat(orders): add State pattern for order lifecycle`, `test(orders): ...`, `refactor(menu): ...`, `build(docker): ...`, `docs: ...`, `chore: ...`.
- Scopes: `orders`, `menu`, `kitchen`, `notifications`, `gateway`, `web`, `shared`, `contracts`, `docker`, `tests`.
- Never commit `.env`, `appsettings.*.local.json`, or secrets.
