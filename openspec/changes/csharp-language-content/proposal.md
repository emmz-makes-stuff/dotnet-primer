# Proposal

## Why

The learning path is finished, and the C# language section is one of the two placeholders left. Emmz
chose it as the next change on 2026-11-11. The engineers last wrote C# 3, and the primer's own
samples are written in C# 14: across the 137 `.cs` files in `samples/`, file-scoped namespaces,
records, primary constructors, nullable annotations, collection expressions and patterns are on
nearly every page. The topic pages explain only three of these features, each in passing (nullable
reference types on the CLI and EF Core pages, primary constructors on the DI page), and nothing
links to `/csharp/` yet. DISCOVERY.md names "C# language" as its own area: "C# 3.0 → 14:
async/await, nullable reference types, records, pattern matching".

## What Changes

The scope, agreed with Emmz in an explore session on 2026-11-11:

- **The C# language section becomes four pages, split by theme** (Emmz: shape B, "a small group of
  themed pages"). Each feature is tagged with the C# version it arrived in, and there's no "what's
  new in C# 13/14" page: those features sit under their themes.
  - **`csharp/`, Modern C# at a glance:** one class written in C# 3 and in C# 14, side by side.
    Then the "less ceremony" syntax a reader meets at the top of every file: file-scoped
    namespaces, implicit and global usings, top-level statements, target-typed `new()`, collection
    expressions, `params` collections (13), raw string literals, interpolation, expression-bodied
    members, using declarations and local functions. It ends with Pitfalls, **Exercise 14 in full**
    (Emmz: on the index page, each step linking to the themed page it practises), and Where next.
  - **`csharp/null-safety`:** nullable reference types, `?.`, `??` and `??=`, `is null` and
    `is not null`, the null-forgiving `!`, the nullable attributes such as `[NotNullWhen]`,
    null-conditional assignment (14), and turning nullable on in an old codebase, a file at a time.
  - **`csharp/types`:** records and `record struct`, `init`, `required`, `with`, primary
    constructors (on classes and on records, and how they differ), tuples and deconstruction, the
    `field` keyword (14), and extension members (14).
  - **`csharp/patterns`:** `is` patterns, switch expressions, and property, relational, logical and
    list patterns.

  Each themed page opens with its outcome, teaches with code, has a "Coming from C# 3" table, ends
  with Pitfalls, and points to Exercise 14.
- **A C# cheatsheet** (`cheatsheets/csharp`): "the C# 3 way, the modern way", side by side. It's
  linked both ways with the section, and listed in the sidebar and on the introduction.
- **Exercise 14** (`samples/exercises/14-modern-csharp`), Emmz's choice of one exercise for the
  area: a small order-pricing class library written C# 3 style, with no web host and no Docker. Its
  TODOs cover null safety (starting by deleting a `#nullable disable`), records and immutability,
  and discount rules as patterns. The tests check behaviour and what the compiler emits (record
  equality, `required` and `init` members, nullability annotations), and each failure names the
  page to read.
- **Links from the topic pages:** where a topic page explains a C# feature in passing, it links to
  the C# page that covers it.
- **The samples README** gains exercise 14.

**Out of scope** (agreed in the explore session):
- `async`/`await`, `IAsyncEnumerable`, `Span<T>` and `ref struct`: the Async & performance page
  covers them, and the C# index points there;
- lambdas, LINQ, `var` and extension methods, which C# 3 already had (C# 14's extension *members*
  are in);
- static abstract interface members and generic math;
- source generators, which readers meet as `[LoggerMessage]` on the Logging page;
- the "Coming from .NET 3.5" section, which is the other placeholder and a separate change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- **`topic-page-content`:**
  - "Full-content pages carry no placeholder" covers the four C# pages and the C# cheatsheet.
  - New: "C# language pages share a shape", "C# examples are compiled and measured", "C# index
    covers modern C# at a glance", "C# null safety page covers nullable reference types", "C# types page covers records and constructors",
    "C# patterns page covers pattern matching", and "Topic pages link C# features to the C#
    section".
- **`site-structure`:**
  - "Cheatsheets per area" grows from fourteen cheatsheets to fifteen.
  - New: "C# language section pages", for the four pages, their order, and navigation within the
    group.
- **`sample-exercises`:**
  - "Site describes each exercise" gains a scenario for exercise 14.
  - New: "Modern C# exercise practises null safety", "Modern C# exercise practises records and
    immutability", and "Modern C# exercise practises patterns".

## Impact

- **Site:**
  - `site/src/content/docs/csharp/index.md`, rewritten, and the new `csharp/null-safety.md`,
    `csharp/types.md`, `csharp/patterns.md` and `cheatsheets/csharp.md`;
  - the sidebar's C# language and Cheatsheets groups in `site/astro.config.mjs`;
  - `site/src/content/docs/index.mdx`: the standalone-sections cards and a cheatsheet card;
  - `site/tests/dist.test.ts` (`EXPECTED_SLUGS`, `FULL_CONTENT_SLUGS`, `CHEATSHEETS`);
  - one-line links from the topic pages that explain a C# feature in passing (the CLI, DI and EF
    Core pages, and any others the section's search finds).
- **Samples:**
  - `samples/exercises/14-modern-csharp`, a class library with no web host, and both `.slnx` files;
  - `samples/README.md`.
  - No new packages: the tests use reflection from the base library (`NullabilityInfoContext`) and
    the xUnit packages already in `Directory.Packages.props`.
- **Gates:** `make samples-test` gains exercise 14's tests, which need no Docker.
- **Tooling:** nothing new. The pinned SDK (10.0.4xx) compiles C# 14 by default, which the probe
  confirms.
