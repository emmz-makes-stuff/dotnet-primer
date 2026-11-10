# Proposal

## Why

EF Core is the next page on the learning path, after Metrics, and it is still a placeholder. The
engineers know Entity Framework 3.5/4: EDMX designers, `ObjectContext`, and lazy loading everywhere.
Those habits cause the familiar EF Core mistakes:
- a context that lives too long;
- N+1 queries;
- queries that silently track everything;
- schema changes made by hand.

The Docker page has just shipped, so a real database for the exercise is now within reach.

## What Changes

- **Write the full EF Core page** (`data/ef-core`), replacing the stub. It uses SQL Server through
  `Microsoft.EntityFrameworkCore.SqlServer`, as Emmz chose on 2026-11-10.
  - **`DbContext` and its lifetime:** `AddDbContext` as scoped, one context per request, why a
    context isn't thread-safe, and `AddDbContextFactory`/pooling for singletons and background
    work.
  - **Modelling:** conventions first, then the fluent API (`OnModelCreating`,
    `IEntityTypeConfiguration<T>`) over data annotations. It covers keys, required and max-length
    strings, `decimal` precision, one-to-many relationships, and value conversions for an enum.
  - **Migrations:** `migrations add`, `database update`, reviewing the generated code, idempotent
    scripts and migration bundles for production, and why an app shouldn't migrate itself at startup
    in production.
  - **Querying:**
    - LINQ and the SQL it produces (`ToQueryString()`, and logging the SQL);
    - `Include` against projection with `Select`;
    - `AsNoTracking`;
    - N+1, and how to spot it;
    - the translation error that replaced EF 3.5's silent client evaluation;
    - async throughout.
  - **Change tracking:**
    - entity states and `SaveChanges` as the unit of work;
    - `ExecuteUpdate`/`ExecuteDelete` for set-based changes;
    - optimistic concurrency with a `rowversion` token.
  - **Coming from Entity Framework 3.5/4:** a mapping of old habits to their EF Core replacements
    (EDMX → code-first with migrations, `ObjectContext` → `DbContext`, lazy loading off by default,
    no silent client evaluation).
  - **Pitfalls,** then **Exercise 07**.
- **An EF Core cheatsheet** (`cheatsheets/ef-core`): registration, the model configuration
  snippets, the `dotnet ef` commands, query patterns, change-tracking calls, and the SQL Server
  container command. It is linked both ways with the page, listed in the sidebar and on the
  introduction, and added to the site tests' maps.
- **Exercise 07** (`samples/exercises/07-ef-core`): a starter, a solution and shared tests, like
  01–06. The engineer works on the orders app:
  - registers an `OrdersDbContext`;
  - configures the model;
  - adds the initial migration;
  - saves an order with its lines in one `SaveChanges`;
  - lists a customer's orders in a single SQL query, with no N+1;
  - cancels an order through change tracking.

  **The tests run against a real SQL Server through Testcontainers** (`Testcontainers.MsSql`), so
  this is the first exercise that needs Docker.
- **The Docker page's macOS part** says how to run x64-only images such as SQL Server on Apple
  silicon (Rancher Desktop's VZ VM type with Rosetta). Its list of pages that need Docker gains EF
  Core.
- **The samples README:** the layout and exercise table gain 07, and the Docker prerequisite says
  that 07 needs it.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- **`topic-page-content`:**
  - "Full-content pages carry no placeholder" adds the EF Core page and its cheatsheet.
  - New: "EF Core page covers DbContext and modelling", "… covers migrations", "… covers querying",
    "… covers change tracking", and "EF Core page maps Entity Framework 3.5/4 habits".
  - New: "Docker page covers x64-only images on Apple silicon".
- **`site-structure`:** "Cheatsheets per area" grows from seven cheatsheets to eight.
- **`sample-exercises`:**
  - "Site describes each exercise" gains a scenario for exercise 07.
  - New: "EF Core exercise practises the model and migrations", "… practises saving, querying and
    change tracking", and "EF Core exercise tests use a real SQL Server", the first exercise whose
    tests need Docker.

## Impact

- **Site:**
  - `site/src/content/docs/data/ef-core.md`, and a new `site/src/content/docs/cheatsheets/ef-core.md`;
  - `site/src/content/docs/platform/docker.md`;
  - the sidebar's Cheatsheets group in `site/astro.config.mjs`;
  - the cheatsheet cards on `site/src/content/docs/index.mdx`;
  - `site/tests/dist.test.ts` (`EXPECTED_SLUGS`, `FULL_CONTENT_SLUGS`, `CHEATSHEETS`).
- **Samples:**
  - `samples/exercises/07-ef-core`, and both `.slnx` files;
  - new `PackageVersion` entries in `samples/Directory.Packages.props` (agreed 2026-11-10):
    `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design` and
    `Testcontainers.MsSql`;
  - `samples/README.md`.
- **Tooling:**
  - the `dotnet-ef` tool, run through `dnx` with no install (agreed 2026-11-10);
  - `make samples-test` and CI's `dotnet test` now start a SQL Server container. `ubuntu-latest`
    runners have Docker, and locally Docker is already in `docs/dependencies.md`;
  - on Apple silicon, the container runs under x64 emulation.
- **Out of scope:**
  - other providers (PostgreSQL, SQLite, Cosmos), beyond a sentence saying they exist;
  - the in-memory provider, except a pitfall saying not to test with it;
  - raw SQL beyond `FromSql` in one example;
  - compiled models and compiled queries;
  - EF Core's OpenTelemetry instrumentation;
  - RDS or Azure SQL deployment, which belongs to the Orchestration & AWS page;
  - a Testing-page treatment of Testcontainers. The exercise uses it, and the Testing page explains
    it later.
