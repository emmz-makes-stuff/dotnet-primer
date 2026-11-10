# Design

## Context

- **The stub** `data/ef-core.md` has a "What this covers" paragraph, two Learn links and the
  "Coming soon" note. It's in `EXPECTED_SLUGS` and in the Learning path, after Metrics and before
  gRPC. Neither it nor a cheatsheet is in `FULL_CONTENT_SLUGS` or `CHEATSHEETS`.
- **The conventions are set** in `site/README.md` and `samples/README.md`:
  - the topic skeleton: outcome, cheatsheet tip, concepts, Pitfalls, exercise;
  - the cheatsheet shape;
  - the exercise layout: `<Name>.Starter`, `<Name>.Solution`, a shared `tests/` compiled into
    `Starter.Tests` and `Solution.Tests`, and both `.slnx` files;
  - central package management in `samples/Directory.Packages.props`;
  - xUnit v3 (ADR-0003), and warnings as errors.
- **Decisions Emmz has made** (2026-11-10): SQL Server; Testcontainers for the exercise tests; a
  page, a cheatsheet and an exercise.
- **Current versions** (NuGet and MCR, 2026-11-10):
  - `Microsoft.EntityFrameworkCore.SqlServer`, `.Design` and the `dotnet-ef` tool at 10.0.14 (11.0 is
    still an RC);
  - `Testcontainers.MsSql` at 4.16.0;
  - `Microsoft.AspNetCore.Mvc.Testing` is already pinned at 10.0.14.
  - **The SQL Server image:** `mcr.microsoft.com/mssql/server:2025-latest`. The newest CU tag is
    `2025-CU9-ubuntu-24.04`. Both `2025-latest` and `2022-latest` are **single-architecture
    (amd64) manifests**.
- **Machines:**
  - this Mac is Apple silicon with OrbStack, which runs amd64 images under Rosetta;
  - CI's `samples` job runs on `ubuntu-latest` (x64), which has Docker;
  - `make samples-test` already runs unsandboxed when it needs the network, and Docker needs it too.

## Goals / Non-Goals

**Goals:**
- An engineer finishes the page able to:
  - register a context correctly;
  - model and migrate a schema;
  - write queries whose SQL they've looked at;
  - save changes deliberately.
- Every claim about generated SQL is checked against SQL that EF Core actually logged, not written
  from memory.
- Exercise 07 tests against the real engine, and fails with messages that guide.
- The samples test gate stays green on CI and on Apple silicon.

**Non-Goals:**
- Teaching SQL Server administration. The container is a dev database only.
- A repository or unit-of-work abstraction over `DbContext`. The page says `DbContext` already is
  one.
- A reusable test-infrastructure project shared across exercises. 07 is self-contained like 01–06.

## Decisions

### D1. Page shape and order

The page follows the topic skeleton, with sections in this order:
1. **Outcome.**
2. **Cheatsheet tip.**
3. **"Coming from Entity Framework 3.5/4"**, first, as a short table. The readers' existing mental
   model is the obstacle, so it goes before the details.
4. **A local SQL Server:** one `docker run` command and a connection string in user secrets.
5. **`DbContext` and its lifetime.**
6. **Modelling.**
7. **Migrations,** including "In production".
8. **Querying.**
9. **Change tracking and saving.**
10. **Pitfalls,** then **Exercise 07**.

Every LINQ example is followed by the SQL EF Core logged for it, trimmed only of its parameter
declarations.

- **Rejected: putting the EF 3.5/4 mapping at the end.** Readers would hit `Include` and wonder where
  lazy loading went before the mapping explained it.

### D2. The example domain

The page and the exercise share the orders domain from 04–06:
- **`Order`:** `Id`, `CustomerEmail`, `Status` (an enum stored as a string), `Total`, `PlacedAt`,
  `Lines`;
- **`OrderLine`:** `Id`, `OrderId`, `Sku`, `Quantity`, `UnitPrice`.

Readers already know the endpoints from 04–06, so only the persistence is new.

The fluent configuration lives in `IEntityTypeConfiguration<T>` classes applied with
`ApplyConfigurationsFromAssembly`. Data annotations are shown once, as the alternative. **Reason:**
the fluent API covers everything, and it keeps the entity classes plain.

### D3. Packages and the `dotnet ef` tool (agreed with Emmz, 2026-11-10)

**`PackageVersion` entries** for `samples/Directory.Packages.props`:
- `Microsoft.EntityFrameworkCore.SqlServer` at 10.0.14;
- `Microsoft.EntityFrameworkCore.Design` at 10.0.14, which the tool needs in the startup project, as
  `PrivateAssets="all"`;
- `Testcontainers.MsSql` at 4.16.0;
- **added after the 1.1 probe (Emmz, 2026-11-10):** `System.Security.Cryptography.Pkcs` at 10.0.14,
  with `<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>`.
  - The SqlServer provider pulls in 9.0.11 through `Microsoft.Data.SqlClient` 6.1.7, and that
    version has a high (NU1903) and a moderate (NU1902) advisory, which the samples'
    warnings-as-errors turns into a failed build. The transitive pin lifts it to a patched version.
  - **Rejected:** referencing SqlClient 7.x directly (a major jump under EF Core 10), and
    suppressing the warnings (the vulnerable package would stay).
  - The page's Pitfalls tell readers what to do if they see NU1903 after adding the provider.

**The tool is run with `dnx dotnet-ef@10.0.14 …`**, with no install. That is the same pattern as
`dotnet-counters` on the Metrics page, which ran fine under auto mode.
- **Fallback, only if `dnx` fails in 1.1 (ask first):** a local tool manifest (`dotnet new tool-manifest` and `dotnet tool install
  dotnet-ef`, which `dotnet tool restore` then restores).
- **Rejected: a global install.** The pages avoid global tools, and the auto-mode classifier blocks
  `dotnet tool install -g`.
- **Probed in task 1.1, and confirmed:** `dnx -y dotnet-ef@10.0.14` runs `migrations add`,
  `database update`, `migrations script --idempotent` and `migrations bundle`. The fallback isn't
  needed.

**Neither the packages nor the tool is in `docs/dependencies.md` yet.** The packages are NuGet
references, not machine dependencies, so they need no row. `dotnet-ef` through `dnx` needs only the
SDK, so it needs no row either. The `docker` row's "Used for" column gains "exercise 07's tests". The fallback
manifest, if it were needed, is a repo file and still needs no new row.

### D4. Exercise 07 shape

**The endpoints** (minimal APIs, in `Program.cs`, like 04–06):
- **`POST /orders`:** `{ customerEmail, items: [{ sku, quantity, unitPrice }] }`. It saves an
  `Order` with its lines in one `SaveChangesAsync`, and returns 201 with `{ orderId }` (an `int`
  identity).
  - **Revised in §2:** this is the 04–06 request, minus `paymentMethod` and the client-sent `total`,
    and with a `unitPrice` on each item. The server computes the total from the lines, so the stored
    total and prices come from one source.
  - The payment fake is dropped. Payment was the subject of 04–06; this exercise is about
    persistence. Every order is stored as `Placed`.
- **`GET /customers/{email}/orders`:** returns `[{ id, status, total, lineCount }]`, newest first.
  The Solution uses one projection with `Select` and `o.Lines.Count`, plus `AsNoTracking` implied by
  the projection.
  - **The Starter's version works but is N+1:** it loads the orders, then counts each order's lines
    in a loop. So the test fails on the command count, not on the answer. This repeats the 04
    pattern: "it works, but badly".
- **`POST /orders/{id}/cancel`:** loads the tracked order with `FindAsync`, sets `Status`, and calls
  `SaveChangesAsync`. Returns 204, or 404 for an unknown ID.

**The Starter's TODOs:**
1. Register `OrdersDbContext` from `ConnectionStrings:Orders`.
2. Complete the two `IEntityTypeConfiguration` classes (max length, `decimal(10,2)`, the enum as a
   string, the relationship).
3. Create the initial migration with `dnx dotnet-ef migrations add InitialCreate`. The Starter ships
   with no `Migrations/` folder.
4. Make the save a single `SaveChanges`.
5. Fix the N+1 list.
6. Implement cancel.

The Solution ships its migration, generated by the tool and reviewed, not written by hand.
Reviewing it caught a convention worth teaching: with no `DbSet<OrderLine>`, the lines table was
named `OrderLine`, next to `Orders`. The configuration now names it `OrderLines`.

**Added in §2:**
- **The Starter's endpoints take `[FromServices] OrdersDbContext`.** Until TODO 1 registers the
  context, minimal APIs would infer an unregistered type as the request body, and the app would
  fail at startup. The tests could then give no guiding message. The Solution doesn't need it.
- **`.editorconfig` marks `**/Migrations/*.cs` as `generated_code = true`.** EF's generated
  migration uses a block-scoped namespace (IDE0161), which `EnforceCodeStyleInBuild` turned into a
  build error. So a reader's own `migrations add` in the Starter would have broken their build.
- **`migrations remove` connects to the database,** to check whether the migration was applied. With
  no connection string it fails with "The ConnectionString property has not been initialized".
  `migrations remove --force` skips the check. The test message and the page's Pitfalls say so.

**The tests** (in shared `tests/`):
- **An xUnit v3 assembly fixture** starts one `MsSqlContainer` for the run. A
  `WebApplicationFactory<Program>` subclass sets `ConnectionStrings:Orders` to a **fresh database per
  test class** (a unique database name on the same server), and calls `Database.MigrateAsync()` in
  the fixture. One container per run keeps the run to one SQL Server start, which is the slow part.
- **Migration matches the model:** `MigrateAsync` succeeds on an empty database, `GetMigrations()`
  isn't empty, and `Database.HasPendingModelChanges()` (EF Core 9+) is false. The message says to run
  `migrations add`.
- **Model is configured:** reads `context.Model` metadata (`GetMaxLength()`, `GetColumnType()`, the
  navigation's cardinality). These are in-process checks, with clear messages.
- **Saved together:**
  - a `SaveChangesInterceptor` registered by the test factory counts `SavedChanges` calls during the
    request, and the test asserts exactly 1. That is sturdier than counting SQL batches, which EF
    may split;
  - the test reads the order and its 3 lines back through a fresh context, and checks the stored
    total and prices.
- **N+1:** a `DbCommandInterceptor` counts `SELECT` commands during the list request. Five
  orders are seeded, and the request must issue exactly 1.
- **Cancel:** the stored status is read back. An unknown ID returns 404.
- **Docker missing:** Testcontainers throws `DockerUnavailableException` from `Build()`, not from
  `StartAsync()` (seen in 1.1). So the fixture builds the container inside `InitializeAsync`, not in
  a field initialiser or constructor. It catches that exception and rethrows with "exercise 07's
  tests need a running Docker engine: see https://dotnet-primer.emmz.dev/platform/docker/".

**The container image is pinned by tag** in the fixture (`2025-CU9-ubuntu-24.04`, or the newest CU
tag at task 1.1), not `latest`. Same reason as the dashboard pin in observability-content: stable
behaviour.

### D5. Apple silicon and the Docker page

The SQL Server image is amd64-only (Context). Running it on Apple silicon needs x64 emulation:
Checked in 1.1 (see the probe devlog note):
- **OrbStack:** works with no settings. Seen on this Mac, where it was ready in 13 s.
- **Rancher Desktop:** Preferences → Virtual Machine → Emulation. VZ is the default; tick **Enable
  Rosetta support**, which appears only on Apple silicon. Rancher Desktop isn't installed here (Emmz's
  2026-11-10 decision), so this stays untested, and the macOS note already says so.
- **Colima:** `colima start --vm-type=vz --vz-rosetta`.
- **Docker Desktop:** turn on "Use Rosetta for x86_64/amd64 emulation on Apple Silicon". **It is off
  by default.** (The first version of this design said on. 1.1 corrected it.)
- **Microsoft's own statement:** SQL Server images are supported only on x86-64, and emulation such
  as Rosetta 2 is "not tested or supported". The note says so plainly: fine for practice, not a
  supported setup.

The note is a short paragraph under `## macOS`, with the per-tool switch. The page's opening list of
pages that need Docker gains EF Core.

- **Rejected: `mcr.microsoft.com/azure-sql-edge`.** It was retired on 30 September 2025, and it had
  already dropped ARM64 (confirmed in 1.1).
- **Rejected: SQLite for Apple silicon.** That contradicts Emmz's choice of engine.

### D6. Cheatsheet

It follows the cheatsheet shape (back link, `##` groups of copyable blocks, minimal prose):
- registration (`AddDbContext`, the factory, pooling);
- an `IEntityTypeConfiguration` skeleton with the common fluent calls;
- the `dnx dotnet-ef` commands (`migrations add`/`remove`/`list`/`script --idempotent`,
  `database update`, `migrations bundle`);
- the query patterns (`AsNoTracking`, projection, `Include`/`ThenInclude`, `AsSplitQuery`,
  `ToQueryString`);
- saving (`Add`/`Update`/`Remove`, `ExecuteUpdate`/`ExecuteDelete`, the concurrency token);
- turning on SQL logging;
- the SQL Server `docker run` command.

### D7. Code accuracy and verification

As in earlier changes:
- every snippet is compiled in a scratch project, net10.0, with warnings as errors;
- "from the exercise" snippets are copies of the real files;
- Learn links are checked with the link script.

Also:
- **Every SQL listing on the page** is pasted from EF Core's own log output, captured by running the
  snippet against the SQL Server container in a scratch app. The devlog records the commands.
- **The concurrency example** is run, two contexts saving the same row, to show the real exception
  and message.
- **The production migration commands** (`script --idempotent`, `migrations bundle`) are run in the
  scratch project, and their output is noted.

### D8. CI and the samples gate

No workflow change is planned. `dotnet test --solution samples/Primer.Solutions.slnx` on
`ubuntu-latest` has Docker, and Testcontainers uses it. Task 1.1 confirms this with a throwaway
branch push, or by checking the first CI run after §2 lands.

- **If image pulls make CI flaky:** add one retry in the fixture before failing. Don't cache images.
- **Local:** `make samples-test` runs unsandboxed (the Docker socket), as already noted in
  CLAUDE.md's sandbox list.

### D9. Section order

1. Probe and package versions (D3, D4, D5, D8).
2. Exercise 07.
3. The cheatsheet, then the EF Core page.
4. The Docker page's Apple silicon note, the samples README, and the dependencies row.
5. The human check.

As in observability-content, the exercise comes before the page, so the page's "from the exercise"
snippets are real.

## Risks / Trade-offs

- **SQL Server under Rosetta is slow to start** (tens of seconds), and Microsoft doesn't support it
  on arm64.
  - Mitigation: one container per test run (D4).
  - The page says the first run pulls about 1.5 GB and starts slowly.
  - A note says it's a development workaround, not a supported configuration.
- **Testcontainers needs Docker in every gate run.** A missing daemon used to fail only `make deps`.
  Now it fails `make samples-test` too, and blocks a samples-only change on a machine without Docker.
  Mitigation: the fixture's message (D4). Docker is already a listed dependency.
- **Counting SQL commands is brittle across EF versions.** Mitigation: count only `SELECT`s during the
  one request, assert exactly 1, and pin EF Core at 10.0.14.
- **`2025-latest` moves.** Mitigation: pin a CU tag in the fixture and on the page, and say "at the
  time of writing".
- **The SA password in samples.** Testcontainers generates it, and the page's `docker run` uses a
  placeholder (`Your_strong_password1`) with a line saying it's local only. Connection strings go in
  user secrets on the page, never in `appsettings.json`, matching the Configuration page.
- **The exercise skips Testing-page concepts** (fixtures, `WebApplicationFactory` customisation).
  Mitigation: the exercise README says the Testing page explains the test setup, and the engineer
  doesn't need to change it.

## Open Questions

- **Should the page show `FromSql`?** One example is planned, and dropping it changes nothing else.
  Decided while writing the page in §3.
