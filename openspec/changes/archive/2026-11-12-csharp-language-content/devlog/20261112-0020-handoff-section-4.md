# Handoff: §4 part-built — the three themed pages are written (4.1–4.3), 4.4 is next

<!-- Section: 4 · Tasks: 4.1–4.3 done, 4.4 to do · Base: 9030bb2 -->

The context reached 40%, so this is a handoff mid-section. **§4 is uncommitted on purpose:** the
section loop commits once, after 4.4 and `make gates`.

## Uncommitted work (all of it is §4's)

- New pages: `site/src/content/docs/csharp/null-safety.md`, `types.md` (sidebar label "Types") and
  `patterns.md`.
- `site/astro.config.mjs`: the C# language group now lists `csharp`, `csharp/null-safety`,
  `csharp/types` and `csharp/patterns`.
- `site/tests/dist.test.ts`: the three slugs are in `EXPECTED_SLUGS` and `FULL_CONTENT_SLUGS`, and
  `CSHARP_SLUGS` holds all four in order. (A perl `/g` once doubled `csharp/patterns` in
  `CSHARP_SLUGS`. It's fixed now: `grep -c csharp/patterns` gives 3.)
- `site/src/content/docs/index.mdx`: Standalone sections has new cards "C# null safety", "C# types" and
  "C# patterns" after "C# language".
- Pagination chain: index `next` → Null safety; Null safety `prev` → index and `next` → Types; Types
  `prev` → Null safety and `next` → Patterns; Patterns `prev` → Types and `next: false`.
- `csharp/index.mdx`: the null-check bullet fix (see the fix-null-check-bullet note), and `next`.
- `tasks.md`: 4.1–4.3 ticked.
- The fix-null-check-bullet devlog note (untracked).

State at handoff: `SITE_BUILD_EXIT:0` and `SITE_TEST_EXIT:0` (`Tests  17 passed (17)`). `make gates`
hasn't been run on §4 yet.

## Verified so far (4.1–4.3)

- **Every ```` ```csharp ```` block** on the three pages is a verbatim copy of a file that built
  clean in the modern harness. `blocks-in-sources.mjs` found 0 missing on each page. Blocks shown as
  failing were built and fail with the exact text shown.
- **Every output and message was measured.** The highlights:
  - Null safety:
    - the four-stage `Invoice.cs` walk-through (6 errors at step 1, then 5 at step 2, then clean);
    - CS8600, CS8625, CS8602 and CS1059 (`?.Visits++`);
    - CS8602 after `?.` assignment, even on a fresh local;
    - `!` → NRE, and `new string[3]` → NRE with no warning;
    - a careless `==` gives `sku == null` → `True`, while `is null` gives `False`;
    - removing `[NotNullWhen]` or `[MemberNotNull]` gives CS8602;
    - `Dictionary.TryGetValue` uses `[MaybeNullWhen(false)]` (by reflection);
    - `ThrowIfNull` → `Value cannot be null. (Parameter 'skus')`, and it's .NET 6 (Learn's API page
      starts at `net-6.0`).
  - Types:
    - record `ToString` is `OrderLine { Sku = SKU-1, Quantity = 2, UnitPrice = 9.99, Total = 19.98 }`;
    - a plain class prints `Shop.PlainLine`;
    - CS9035, CS8852, CS1061 (`pricing.goldRate`) and CS9124 (double capture);
    - a `with` copy shares the list (count 2 after the original's add);
    - `Basket` records holding lists aren't `==`;
    - assigning to a captured parameter compiles.
  - Patterns:
    - `CostOld` and `Cost` agree on all 7 rows of the page's table;
    - CS8509 (`'100.00000000000000000000000000M' is not covered`), and at run time (with a pragma)
      `SwitchExpressionException … Unmatched value was 150.`;
    - CS8510 for an unreachable arm;
    - `.. var rest` on `IReadOnlyList<Line>` gives `CS1503: Argument 1: cannot convert from
      'System.Range' to 'int'`, but works on `Line[]`;
    - `{ }` is false for null;
    - `{ Customer.Country: "GB" }` is false when `Customer` is null;
    - the dotted form is C# 10 (CS8773 at LangVersion 9), and the nested form builds at C# 8.
- **Every anchor link** on the four C# pages and the cheatsheet resolves against `site/dist`
  (`anchors.mjs`; it flags only external Learn fragments, which were fetched).
- **Learn URLs:** all 9 in the three pages' frontmatter return 200, and the `#primary-constructors`
  anchor exists.
- **Version tags:** they follow the §1 table, and every heading has one.
- **The Patterns page uses shipping rules, not discount rules,** so that it doesn't solve exercise
  14's TODO 4.

## 4.4: what's left

1. **Link the themed pages, now that they exist:**
   - the index's "What changed" bullets (records and the primary constructor → `/csharp/types/#records-c-9`
     and `#primary-constructors-c-12`, `IReadOnlyList` → Types, the null check →
     `/csharp/null-safety/#checking-arguments-in-a-public-api`);
   - the index's Where next (three bullets);
   - Exercise 14's four steps ("the Null safety page" etc.);
   - **the cheatsheet's Null safety, Types and Patterns rows** (D4), to their headings' anchors;
   - `null-safety.md`'s "on the Types page" mention of `required`, to `/csharp/types/#init-c-9-and-required-c-11`.
2. Verify all four pages and the index's new links as 3.4 did: blocks, outputs, tags, anchors
   and URLs. Then `make gates` until it shows `GATES_EXIT:0`. Then the section-done note, and one
   commit for §4 (4.1–4.4), whose body mentions the null-check bullet fix.

## Verification tooling (scratchpad, likely gone after /clear)

The scratchpad is `/private/tmp/claude-501/-Users-rendle-github-emmz-dotnet-primer/<session>/scratchpad/verify/`.
If the new session's scratchpad differs, rebuild it:
- `modern/`: an Exe csproj under a copy of the repo's `global.json`, `.editorconfig` and
  `samples/Directory.Build.props`;
- `old/`: `LangVersion` 3, `Nullable`, `ImplicitUsings` and `EnforceCodeStyleInBuild` off, and
  warnings as errors kept;
- `check.sh <old|modern> <statements-file> <defs…>`: copies the defs, splices the statements into
  `Harness.cs.txt` at `//BLOCK`, builds, and runs;
- `blocks-in-sources.mjs <pages…>`: every csharp block must be contiguous lines of some `blocks/*.cs`;
- `anchors.mjs <dist> <paths…>`: every `#fragment` link's id exists.

If the block sources are gone, 4.4's re-verification of the themed pages can rely on this note for
what was measured, and re-run only what 4.4 changes (links, which don't touch code blocks).

## Gotchas this session

- A perl `s///g` over `"csharp/types",\n` also matched inside the freshly rewritten `CSHARP_SLUGS`.
  Check lists after a bulk edit.
- The `# ` in "C#" breaks a perl `s#…#…#` substitution. Use another delimiter, or the Edit tool.
- With `ImplicitUsings`, a type named `Directory` is ambiguous with `System.IO.Directory` (CS0104).
- zsh: `echo =====` fails ("==== not found"), because `=word` is command lookup. Use `'----'`.

## Next

- §4, task 4.4: link the themed pages (list above), verify, run `make gates`, write the section-done
  note, and commit §4.
- Then §5, task 5.1: links from the topic pages (CLI, DI, EF Core and any found), and then
  `samples/README.md`.
- Emmz's style: short replies, one step at a time. Ask before changing anything the design or the
  specs don't cover.
- 1 commit unpushed (`9030bb2`, §3) on `change/csharp-language-content`. Push it when the section loop
  next needs CI, or when Emmz asks.
