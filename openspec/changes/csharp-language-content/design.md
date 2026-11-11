# Design

## Context

- **The stub** `csharp/index.md` has a "What this covers" paragraph, two Learn links ("What's new in
  C# 14", "C# documentation") and the "Coming soon" note, with `prev: false` and `next: false`. It's
  in `EXPECTED_SLUGS`, and the sidebar's "C# language" group holds only it. The introduction links
  it under "Standalone sections" ("From C# 3.0 to C# 14."). It isn't in `FULL_CONTENT_SLUGS`, and
  has no cheatsheet.
- **Starlight's default pagination follows the sidebar across groups,** so without frontmatter the
  first C# page would offer the Orchestration & AWS page as "Previous". Learning-path pages rely on
  that order; the standalone pages turn it off.
- **The site tests:**
  - every `EXPECTED_SLUGS` entry has to be linked from the introduction ("the introduction links
    every area and standalone section");
  - `FULL_CONTENT_SLUGS` pages mustn't show the placeholder;
  - `CHEATSHEETS` pairs have to link both ways;
  - every exercise folder has to be linked from some page.
- **Topic pages that explain a C# feature in passing** (found by search, 2026-11-11):
  - `platform/dotnet-10-and-cli.md`, on the `Nullable` property;
  - `aspnet-core/dependency-injection.md`, on primary constructors, and its cheatsheet;
  - `data/ef-core.md`, on nullable reference types and required columns.

  The section's search (§5) may find more.
- **The repo's style rules bind the exercise.** `.editorconfig` sets
  `csharp_style_namespace_declarations = file_scoped:warning` and `csharp_prefer_braces = true:warning`.
  `samples/Directory.Build.props` has `EnforceCodeStyleInBuild` and `TreatWarningsAsErrors`. So a
  Starter with block namespaces, or `if` without braces, fails `make samples-build`. Primary
  constructors, `var`, expression bodies and target-typed `new` are only `suggestion`, so a Starter
  may use their C# 3 forms.
- **Shared tests compile against both projects.** Each exercise's `tests/` is compiled into
  `Starter.Tests` and `Solution.Tests`, so the test code can only use the API and syntax that compile
  against the Starter's types and the Solution's.
- **The SDK** is pinned at 10.0.400 with `rollForward: latestFeature`, and this Mac has 10.0.404. Its
  default language version for net10.0 should be C# 14; the probe confirms it (D5).
- **Decisions Emmz made** (2026-11-11, explore session): see proposal.md. In short:
  - four themed pages (shape B), each feature tagged with its version, and no "what's new" page;
  - one cheatsheet;
  - one exercise, 14, described in full on the index;
  - async, `Span`, LINQ, lambdas, generic math and source generators out.

## Goals / Non-Goals

**Goals:**
- An engineer who last wrote C# 3 can read any file in the primer's samples, and knows where each
  unfamiliar feature is explained.
- Every example compiles on the pinned SDK with the repo's settings, and every warning and output
  shown is from a real build or run.
- Exercise 14's tests are deterministic, need no Docker and no web host, and check what the
  engineer was asked to do as far as a test can see it.

**Non-Goals:**
- A complete C# reference. The pages cover what this team will meet in modern .NET code, and link
  to Learn for the rest.
- Checking style in the exercise. Tests can't see whether a rule was written as a switch
  expression; the exercise says so (D7).
- Changing the samples' style rules to suit the exercise.
- The Coming from .NET 3.5 section.

## Decisions

### D1. Section shape and navigation

The sidebar's "C# language" group lists four pages, in order:

| Slug | Title | Sidebar label |
| --- | --- | --- |
| `csharp` | Modern C# at a glance | Modern C# at a glance |
| `csharp/null-safety` | Null safety | Null safety |
| `csharp/types` | Types: records, constructors and members | Types |
| `csharp/patterns` | Patterns | Patterns |

- **Pagination stays inside the group.** Each page sets `prev` and `next` in its frontmatter to its
  neighbours in the group (Starlight's `{ link, label }` form), and the first page's `prev` and the
  last page's `next` are `false`. A site test checks that no C# page's pagination leaves the group,
  which is the "C# language section pages" requirement.
- **The introduction** links all four under "Standalone sections", and adds the C# cheatsheet card
  under "Cheatsheets". The four slugs go into `EXPECTED_SLUGS`, as each page lands.

**Alternative considered:** nesting the three themed pages under the index in a collapsible
sub-group. The other areas list their pages flat, so this one does too.

### D2. Version tags

Each feature's heading carries its version: `## Records (C# 9)`, `### List patterns (C# 11)`. When
one heading covers several versions, the text says which part came when. Headings are searchable, need
no MDX component, and show in the page's table of contents.

The versions come from Learn's "What's new in C# 7.0 … 14" pages and the language version history,
fetched in §1 (D5), not from memory. The probe note keeps a table of feature, version and source
URL, which the pages and the cheatsheet use.

### D3. The pages

**`csharp/` (Modern C# at a glance),** in the topic order the spec gives:
1. The outcome, and the cheatsheet tip.
2. **One class, C# 3 and C# 14:** the same small order type, side by side. Each difference is named
   and linked to the section or page that covers it.
3. **Less ceremony:** file-scoped namespaces (10), implicit and global usings (10), top-level
   statements (9), target-typed `new()` (9), collection expressions (12), `params` collections
   (13), raw string literals (11), string interpolation (6, with interpolated raw strings in 11),
   expression-bodied members (6, 7), using declarations (8), and local functions (7).
4. **Not here:** async, `IAsyncEnumerable` and `Span<T>`, with a link to the Async & performance
   page. Lambdas, LINQ and `var` are named as unchanged from C# 3.
5. **Pitfalls,** then **Exercise 14** (D6), then **Where next**, which links the three themed pages.

**`csharp/null-safety`:**
- what `<Nullable>enable</Nullable>` changes, and that every new project has it;
- `string` against `string?`, and the warnings;
- `?.`, `??` (2.0/6), `??=` (8);
- `is null` and `is not null` (7, 9);
- the null-forgiving `!`, and when it's justified;
- the attributes: `[NotNullWhen]`, `[MaybeNullWhen]`, `[MemberNotNull]`, `[AllowNull]`;
- null-conditional assignment, `a?.B = c` (14);
- **turning it on in old code:** `#nullable enable` a file at a time, the warnings a real build
  gives for a C# 3-style file, and how each is fixed. The example is shaped like exercise 14's
  Starter, but isn't copied from it.
- Then "Coming from C# 3", Pitfalls, and a pointer to Exercise 14.

**`csharp/types`:**
- records (9) and record structs (10): value equality, `ToString`, deconstruction;
- `init` (9) and `required` (11), and object initializers;
- `with` (9; on structs and anonymous types, 10);
- primary constructors (12): on a record they become properties, on a class they're captured
  parameters, and the DI page uses them;
- tuples and deconstruction (7);
- the `field` keyword (14);
- extension members (14), against C# 3's extension methods.
- Then "Coming from C# 3", Pitfalls (for example, a record holding a `List<T>` isn't deep-equal),
  and a pointer to Exercise 14.

**`csharp/patterns`:**
- `is` with a type pattern (7);
- switch expressions (8);
- property patterns (8, extended in 10);
- relational and logical patterns (9);
- list and slice patterns (11);
- `when` guards;
- an if/else chain rewritten as a switch expression, with the same results;
- the non-exhaustive warning, and what happens at run time when no arm matches. Both are measured,
  not named from memory.
- Then "Coming from C# 3", Pitfalls, and a pointer to Exercise 14.

The pages use the order domain the rest of the primer uses (orders, lines, SKUs, customers), so the
examples look like the samples.

### D4. Cheatsheet

`cheatsheets/csharp`, linked both ways with `csharp/`, with a back link and Learn links:
- "the C# 3 way → the modern way", one table per theme (less ceremony, null safety, types,
  patterns), each row tagged with its version and linking to the section;
- short code blocks only where a table cell can't hold the form.

It goes in the sidebar's Cheatsheets group, on the introduction, in `EXPECTED_SLUGS` and
`FULL_CONTENT_SLUGS`, and in `CHEATSHEETS` as `"csharp": "cheatsheets/csharp"`.

### D5. Measured, not remembered

The probe (§1), in scratch projects with the repo's `global.json`, `.editorconfig` and
`samples/Directory.Build.props`, records:
- **the default language version** on the pinned SDK for net10.0 (with `#error version`, which
  prints the compiler and language versions);
- **that each C# 13 and 14 feature the pages show compiles:** `params` collections, `field`,
  extension members, null-conditional assignment;
- **the reflection the exercise's tests depend on** (D7), against both a C# 3-style type and a
  modern one:
  - `RequiredMemberAttribute` on a `required` property;
  - `IsExternalInit` as a required custom modifier on an `init` setter;
  - `NullabilityInfoContext`'s states for annotated, unannotated and `#nullable disable` members;
- **that a C# 3-style file builds under the repo's rules** with `#nullable disable` (D8), and the
  exact warnings it gives when that line is deleted;
- **the version table** (D2), from fetched Learn pages.

The page work in §3 and §4 also measures each output it shows (record `ToString`, the
non-exhaustive warning, `SwitchExpressionException`).

### D6. Exercise 14 shape

A class library, `ModernCSharp.Starter` and `ModernCSharp.Solution`, with:
- `Customer` (name, email, loyalty tier) and a `CustomerDirectory` whose lookup returns null for an
  unknown customer;
- `OrderLine` (SKU, quantity, unit price);
- `Order` (customer id, lines, discount), with `ApplyDiscount`;
- `Pricing`, which works out an order's discount rate and total.

The Starter is C# 3 in its types, its null handling and its branching, and follows the repo's
enforced style (file-scoped namespaces, braces). Its TODOs, each naming the page to read:
1. **Null safety** (Null safety page): the Starter's file opens with `#nullable disable`. Delete it,
   annotate the lookup as returning `Customer?` and the email as `string?`, fix the warnings, and
   price an order whose customer is missing without an exception. The Starter dereferences the
   lookup's result, so a missing customer throws `NullReferenceException`.
2. **Records** (Types page): `OrderLine` is a class with public setters. Make it a record with
   `required`, `init` members.
3. **Immutability** (Types page): `ApplyDiscount` changes the order in place and returns it. Make
   `Order` a record and return a copy (`with`), leaving the original unchanged.
4. **Patterns** (Patterns page): the discount rules are an if/else chain that checks them in the
   wrong order and misses a case. Rewrite them as a switch expression. The rules (final values are
   fixed in §2):

   | Order | Discount |
   | --- | --- |
   | its only line is a gift card (a list pattern) | none |
   | a Gold customer | 10% |
   | a total of 100 or more | 5% |
   | anything else, or no customer found | none, apart from the total rule |

   The Starter checks the total before the tier, so a Gold customer's large order gets 5%, and it
   discounts a single gift-card line.

### D7. Exercise 14's tests

- **The same API on both sides.** The Starter and the Solution have the same public members, apart
  from what the TODOs change: nullability, record-ness, `required` and `init`. The tests use only
  syntax that compiles against both: object initializers, collection expressions, `Equals`, `==`
  and reflection. They never use `with` or positional record syntax.
- **What each test checks:**
  - TODO 1: `NullabilityInfoContext` gives `Nullable` for the lookup's return and the email, and
    `NotNull` for the name. Pricing an order with an unknown customer returns its total with no
    tier discount, and throws nothing.
  - TODO 2: two `OrderLine`s with the same values are `Equals` and `==`. Each property carries
    `RequiredMemberAttribute`, and its setter has the `IsExternalInit` modifier.
  - TODO 3: after `ApplyDiscount`, the returned order has the discount, and the original's discount
    is unchanged. They're different instances.
  - TODO 4: one test per rule in D6's table, named for its case.
- **The messages** name what's wrong, the TODO, and the page's path (for example
  `/csharp/null-safety/`), in the style of exercise 13's `Expect` helper.
- **No timing, no host, no Docker:** plain unit tests, so they're deterministic on any runner.
- The exercise says that TODO 4's tests check the rules' results, and can't see whether a switch
  expression produced them.

**Alternative considered:** a Roslyn analyzer test that checks the syntax. That's heavy machinery for
an exercise that's practice, not assessment, so it's left out.

### D8. The Starter's `#nullable disable`

The Starter turns nullable off in its source file, not in its project file, so that TODO 1 is the
"a file at a time" step the Null safety page teaches. With it, the Starter builds clean under
warnings as errors. Without it, it fails with nullable warnings, which is what the engineer then
fixes. The probe (D5) records those warnings, so the exercise's description can say what to expect.

### D9. Links from the topic pages

Each in-passing explanation found in §5's search gets a link to the C# section that covers the
feature. The explanation itself stays: the link adds to it, and doesn't replace it. At least these:
- the CLI page's `Nullable` bullet → `/csharp/null-safety/`;
- the DI page's primary constructor paragraph → `/csharp/types/#primary-constructors-c-12` (or the
  heading's real anchor);
- the EF Core page's nullable note → `/csharp/null-safety/`.

### D10. Section order

1. **Probe** (D5).
2. **Exercise 14:** scaffold, Solution, tests, Starter, CI. **This lands with `SITE_TEST_EXIT:1` on
   purpose,** because "Undescribed exercise is caught" fails for `14-modern-csharp` until §3
   describes it. The section note and the commit body record it.
3. **The index page and the cheatsheet:** `csharp/` rewritten (with Exercise 14), the cheatsheet, the
   sidebar, the introduction's cards, and the site tests' lists, plus the pagination test (D1).
4. **The three themed pages:** null safety, types and patterns, each added to the sidebar,
   `EXPECTED_SLUGS`, `FULL_CONTENT_SLUGS` and the introduction as it lands. Then they're verified
   (D11).
5. **Links and the samples README:** the topic pages' links (D9), and exercise 14 in
   `samples/README.md`.
6. **The human check.**

Each page's sidebar entry lands with the page, because a sidebar entry for a missing page breaks
the build.

### D11. Code accuracy and verification

As in earlier changes:
- every C# block is compiled in a scratch project, net10.0, with the repo's `.editorconfig` and
  warnings as errors. A block shown as not compiling is compiled too, and must fail with the message
  the page shows;
- every warning and output shown is from a real build or run;
- "from the exercise" blocks are copies of the real files, checked by script;
- each version tag matches the §1 table;
- every URL on the pages is fetched.

## Risks / Trade-offs

- **The C# 3 Starter can't look fully C# 3,** because the repo enforces file-scoped namespaces and
  braces. → The Starter is C# 3 in what the exercise practises (types, nulls, branching), and the
  exercise's description says its layout is already modern. An `.editorconfig` override in the
  exercise folder was considered, and rejected: it would make the exercise the one place the
  samples' style differs.
- **Reflection on emitted metadata can differ from what the docs describe.** → §1 measures it
  before any test relies on it. If a check doesn't work, the probe stops, and asks before the test
  design changes.
- **A test can't see syntax,** so an engineer can pass TODO 4 by reordering the if/else. → It's
  practice, not assessment, and the exercise says so (D7).
- **Version tags from memory are easy to get wrong** (for example, `??=` is C# 8, and `is not null`
  is C# 9). → Every tag comes from the §1 table, from fetched Learn pages.
- **Four pages at once are a lot of content to verify.** → They're split across §3 and §4, each with
  its own verification and gates, so a section can hand off cleanly.
- **C# 14 is new,** so search results and blog posts may show preview syntax (for example, early
  extension-member forms). → The pages show only what compiles on the pinned SDK, and link to Learn.
