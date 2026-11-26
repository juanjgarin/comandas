# Phase 1 — Orders.API as a well-built monolith

**Goal:** one service with a complete, well-tested domain and real persistence, built layer by layer before adding any distributed complexity.

**Result:** 17 steps (one commit each, plus the initial plan), **187 tests passing** (0 warnings, warnings treated as errors), and an API that runs end to end against SQL Server.

| Test project | Methods | What it covers |
|---|---|---|
| `Comandas.ArchitectureTests` | 7 | Clean Architecture dependency rules, as executable tests |
| `Comandas.SharedKernel.Tests` | 21 | `Money`, `Entity`, `AggregateRoot` |
| `Orders.Domain.Tests` | 73 | Value objects, menu Composite, `Order` aggregate, State pattern matrix, domain events |
| `Orders.Application.Tests` | 13 | Use cases with hand-written fakes (no database, no mocking library) |
| `Orders.IntegrationTests` | 17 | EF Core model + real SQL Server via Testcontainers |

> Theories expand into many cases, so the 131 methods above run as 187 tests.

---

## 1. What was built

```text
src/
├── BuildingBlocks/Comandas.SharedKernel   Entity, AggregateRoot, ValueObject, DomainException, IDomainEvent, Money
└── Services/Orders/
    ├── Orders.Domain          Quantity · MenuItem → Dish / Combo · Order + OrderLine · OrderState × 6 · 6 domain events
    ├── Orders.Application     IOrderRepository, IMenuItemRepository, IUnitOfWork · application services · DTOs
    ├── Orders.Infrastructure  OrdersDbContext · 1 configuration per entity · repositories · Unit of Work · migration · seed
    └── Orders.API             Controllers · OpenAPI + Swagger UI · composition root
```

The request path: `HTTP → Controller → OrderApplicationService → Order (domain) → IUnitOfWork → EF Core → SQL Server`.
Every arrow points inwards at compile time; only the API (composition root) knows every layer.

---

## 2. Concepts practiced, and where to find them

### Object-oriented design

| Concept | Where | Key idea |
|---|---|---|
| **Encapsulation** | [`Order`](../src/Services/Orders/Orders.Domain/Orders/Order.cs), [`Money`](../src/BuildingBlocks/Comandas.SharedKernel/ValueObjects/Money.cs) | No public setters. Private constructors + validating factories (`Of`, `Create`) make invalid objects impossible to create. Collections are exposed read-only over a private list. |
| **Inheritance / polymorphism** | [`MenuItem`](../src/Services/Orders/Orders.Domain/Menu/MenuItem.cs) → [`Dish`](../src/Services/Orders/Orders.Domain/Menu/Dish.cs), [`Combo`](../src/Services/Orders/Orders.Domain/Menu/Combo.cs) | Callers ask `GetPrice()` without knowing the concrete type; no `if (item is Combo)` anywhere. |
| **Value Objects** | `Money`, [`Quantity`](../src/Services/Orders/Orders.Domain/Orders/Quantity.cs) | Immutable, self-validating, compared by value through an explicit `ValueObject` base class. They remove "primitive obsession": a `Quantity` of 0 cannot exist. |
| **Entities vs value objects** | [`Entity`](../src/BuildingBlocks/Comandas.SharedKernel/Domain/Entity.cs) vs [`ValueObject`](../src/BuildingBlocks/Comandas.SharedKernel/Domain/ValueObject.cs) | Same data, different identity: two orders with identical lines are still different orders. |
| **Aggregate** | `Order` + `OrderLine` | The root is the only entry point; lines are changed through `internal` methods only the aggregate can call. |

### SOLID

| Principle | Example in this phase |
|---|---|
| **S** — Single Responsibility | One `IEntityTypeConfiguration<T>` per entity; controllers only translate HTTP. *Counter-example kept on purpose:* `OrderApplicationService` has 10 use cases and is split in phase 2. |
| **O** — Open/Closed | A new order status is a new `OrderState` subclass; nothing in `Order` changes. A new kind of menu item is a new `MenuItem` subclass. |
| **L** — Liskov Substitution | `Dish` and `Combo` are interchangeable wherever a `MenuItem` is expected (tested in `GetPrice_CalledThroughBaseType_WorksLikeAnyMenuItem`). |
| **I** — Interface Segregation | `IOrderRepository` gets and adds; `IUnitOfWork` saves. No "god repository". |
| **D** — Dependency Inversion | Application declares the interfaces; Infrastructure implements them. `AddInfrastructure()` is where `IOrderRepository → OrderRepository` is wired. Proof: the whole Application layer is tested without a database. |

### Design patterns

| Pattern | Where | Notes |
|---|---|---|
| **Composite** | `Combo` / `ComboComponent` | Price, availability (`CanBeOrdered`) and `Contains` recurse through combos nested to any depth. Cycle guard prevents infinite recursion. |
| **State** | [`States/`](../src/Services/Orders/Orders.Domain/Orders/States) | One stateless singleton per status; each returns the next state. The base class rejects everything by default, so final states override nothing. |
| **Observer (domain events)** | [`Events/`](../src/Services/Orders/Orders.Domain/Orders/Events) | Raised only after a successful transition; collected, not yet dispatched (phase 2). |
| **Repository + Unit of Work** | [`Repositories/`](../src/Services/Orders/Orders.Infrastructure/Persistence/Repositories), [`EfUnitOfWork`](../src/Services/Orders/Orders.Infrastructure/Persistence/EfUnitOfWork.cs) | EF's `DbContext` already *is* a unit of work; our class exposes it through the interface Application asked for. |
| **Factory method** | `Order.Create`, `Dish.Create`, `Combo.Create`, `Money.Of` | The only way to obtain a valid instance. |

### Clean Architecture

- Four layers per service, with references pointing inwards only.
- **Enforced by tests**, not just by convention: [ArchUnitNET rules](../tests/Comandas.ArchitectureTests/OrdersLayerDependencyTests.cs) fail the build if, say, the Domain references EF Core. Verified by deliberately leaking EF Core into the Domain and watching the test turn red.

### EF Core

| Feature | Where |
|---|---|
| Fluent API, one configuration per entity | [`Configurations/`](../src/Services/Orders/Orders.Infrastructure/Persistence/Configurations) |
| Owned type (`Money`) vs value converter (`Quantity`) | [`ValueObjectMappings`](../src/Services/Orders/Orders.Infrastructure/Persistence/Configurations/ValueObjectMappings.cs) |
| TPH inheritance with a `Kind` discriminator | `MenuItemConfiguration` |
| Enum stored as text | `Status` column (`"Confirmed"`) |
| Optimistic concurrency, `RowVersion` as a **shadow property** | `OrderConfiguration` — the domain class knows nothing about it |
| Backing fields for read-only collections | `_lines`, `_components` |
| Migrations applied on startup | `ApplyMigrationsAsync()` + `InitialCreate` |
| Seeding through the domain (`UseSeeding` / `UseAsyncSeeding`) | [`MenuSeeder`](../src/Services/Orders/Orders.Infrastructure/Persistence/Seeding/MenuSeeder.cs) |
| No-tracking reads with identity resolution | `MenuItemRepository` |

---

## 3. Decisions taken

| # | Decision | Chosen | Why |
|---|---|---|---|
| D1 | Package licensing | AwesomeAssertions · MediatR 12.5.0 · MassTransit 8.x | Free and permissive for a public portfolio; same APIs as the commercial versions. |
| D2 | Shared base classes | `Comandas.SharedKernel` | One small, stable model; `Money` lives there because Menu and Orders both need it. |
| — | Value object style | Explicit `ValueObject` base class | Shows how value equality works instead of hiding it in `record`. |
| D11 | Entity ids | `Guid.CreateVersion7()` generated by the domain | The id exists before saving, so events can carry it. |
| — | Combo price | Sum of components − combo % | Keeps the Composite visible: the price is recursive and follows dish price changes. |
| — | State pattern variant | Each state returns the next state | States stay independent of `Order`; `Order` persists a plain enum. |
| D4 | Inheritance mapping | TPH | No joins, simplest polymorphic queries; the cost is NULL columns. |
| D3 | API style | Controllers + Swagger UI | The style found in most existing enterprise code. |
| — | Dev secrets | User Secrets | The repository is public: no password in any file (verified). |
| — | Ryuk (Testcontainers) | Disabled, containers labelled `comandas.tests` | No extra image download; orphans are easy to find and remove. |

---

## 4. Problems found and lessons learned

1. **`ValueGeneratedNever()` matters with domain-generated keys.** Without it, EF treats a new line with a pre-set Guid as existing and issues an `UPDATE` that affects 0 rows — reported, confusingly, as a `DbUpdateConcurrencyException`. A Testcontainers test now guards it; removing the line made that test fail exactly as predicted.
2. **Read the SQL that EF generates.** Generating the create script (without a database) revealed that shadow foreign keys are optional by default: `OrderLines.OrderId` was nullable, allowing orphan lines. Fixed with `.IsRequired()` and protected by a test.
3. **Never verify a write with the same `DbContext`.** EF returns the object it already holds in memory, so the test would pass even if nothing was saved. Integration tests always write with one context and read with another.
4. **`Include` is not recursive.** Nested combos are loaded by reading the whole (small) menu in one query with identity resolution, so every combo instance arrives with its components.
5. **A failed operation must leave the object unchanged** — validate first, then assign. Several tests assert the "before" state after an exception.
6. **`ReadOnlyCollection<T>` implements `IList<T>`.** It protects itself by throwing on mutation, not by hiding the interface — a test that assumed otherwise was wrong and was corrected.
7. **User Secrets load only in `Development`.** The startup error message written in `Program.cs` explained the fix on the first failure.
8. **Environment hiccups:** a background installer replaced the .NET 10 SDK with .NET 11 mid-phase (fixed by reinstalling SDK 10; `global.json` pins 10.0.x); Git Bash rewrites Linux paths in `docker exec` (`MSYS_NO_PATHCONV=1`); stopping `dotnet run` can leave the `Orders.API` process alive and holding the port.

---

## 5. Known limitations (carried into later phases)

| Limitation | Planned in |
|---|---|
| Domain and not-found errors return HTTP 500 | 2.11 — global error handling with ProblemDetails |
| No input validation before the domain | 2.9 — FluentValidation pipeline |
| Domain events are collected but never dispatched | 2.10 |
| `OrderApplicationService` mixes many use cases | 2.6 — one CQRS handler per use case |
| `RowVersion` only guards the `Orders` row: concurrent edits to *lines only* go undetected | 2.12 — mark the aggregate root as modified when a child changes |
| `MenuItemRepository.GetByIdAsync` loads the whole menu | Acceptable for a small menu; the menu moves to its own service with a Redis cache in phase 4 |
| The menu lives inside the Orders service | 4.4 — Menu.API |
| Local SQL Server is started by hand with `docker run` | 3.5 — docker-compose |

---

## 6. How to run what exists today

```powershell
# Once: SQL Server + connection string (see CLAUDE.md → Commands)
docker start comandas-sqlserver

# All tests (Docker must be running for the integration tests)
dotnet test Comandas.slnx

# The API, then open http://localhost:5102/swagger or run src/Services/Orders/Orders.API/Orders.API.http
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/Services/Orders/Orders.API
```
