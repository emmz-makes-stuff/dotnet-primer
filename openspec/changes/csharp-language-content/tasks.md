# Tasks

## 1. Probe: language version, new features, and the exercise's reflection

- [x] 1.1 In a scratch tree with the repo's `global.json`, `.editorconfig` and
  `samples/Directory.Build.props` (design D5), record:
  - the default language version for net10.0 on the pinned SDK, from `#error version`;
  - that `params` collections, the `field` keyword, extension members and null-conditional
    assignment each compile with warnings as errors.

  Write a devlog note with the exact compiler output, versions and platform.
- [x] 1.2 Probe the reflection that exercise 14's tests will use (D7), against a C# 3-style type and
  a modern one:
  - `RequiredMemberAttribute` on `required` properties;
  - `IsExternalInit` as a required custom modifier on `init` setters;
  - `NullabilityInfoContext`'s states for annotated, unannotated and `#nullable disable` members;
  - `Equals` and `==` on a record, and on a class.

  **If any check doesn't work as D7 needs, stop and ask** before the test design changes. Write a
  devlog note.
- [x] 1.3 Build a C# 3-style file with `#nullable disable` under the repo's rules (D8), and confirm
  it builds clean. Then delete that line and record the exact warnings. Fetch Learn's "What's new in
  C#" pages (7.0 to 14) and the language version history, and write the feature → version → URL
  table that D2 needs, for every feature the pages will tag. Write a devlog note.

## 2. Exercise 14: modern C#

- [x] 2.1 Scaffold `samples/exercises/14-modern-csharp/` (D6): `ModernCSharp.Starter` and
  `ModernCSharp.Solution` class libraries, a shared `tests/`, and `Starter.Tests` and
  `Solution.Tests`. Add them to `Primer.slnx`, and the Solution and its tests to
  `Primer.Solutions.slnx`. Verify with `make samples-build`.
- [x] 2.2 Write the Solution: `Customer`, `CustomerDirectory`, `OrderLine` and `Order` as D6 describes,
  with TODOs 1–4 solved, and fix D6's discount rule table. Verify that it builds with nullable on and
  warnings as errors.
- [x] 2.3 Write the tests (D7): one test per check, using only syntax that compiles against both
  projects, each failure naming what's wrong, its TODO and the page's path. Verify that the Solution
  passes ten runs in a row.
- [x] 2.4 Write the Starter, C# 3 style within the repo's rules (D6, D8), with `#nullable disable` and
  TODOs 1–4. Verify that:
  - it builds clean;
  - each test fails against it with its guiding message;
  - a scratch copy completed with the Solution's code passes everything;
  - scratch copies that each leave out one TODO fail only that TODO's tests.
- [x] 2.5 Push the branch, and run the `samples` workflow on it
  (`gh workflow run samples.yml --ref change/csharp-language-content`). Check that it passes on
  `ubuntu-latest` with exercise 14's tests, and record the run ID in the section-done note.
  - `SITE_TEST_EXIT:1` is expected here (D10). Record it as a deliberate red.

## 3. The C# index page and the cheatsheet

- [x] 3.1 Rewrite `site/src/content/docs/csharp/index.md` per D3 ("Modern C# at a glance"): the
  outcome, the cheatsheet tip, the C# 3 and C# 14 class side by side, "less ceremony" with version
  tags (D2), the pointer to Async & performance, Pitfalls, Exercise 14 (each step linked to the page
  it practises, with "needs no Docker" and the exact GitHub tree link), and Where next. Until §4
  lands, Where next and the step links name the themed pages without linking them. Set its
  pagination per D1. Verify that `make site-build` passes.
- [x] 3.2 Create `site/src/content/docs/cheatsheets/csharp.md` (D4), with the back link, Learn links,
  and the four themed tables. Add it to the sidebar's Cheatsheets group, the introduction's cheatsheet
  cards, and `EXPECTED_SLUGS`. Verify that `make site-build` passes.
- [x] 3.3 Add to `site/tests/dist.test.ts`: `csharp` and `cheatsheets/csharp` in `FULL_CONTENT_SLUGS`,
  `"csharp": "cheatsheets/csharp"` in `CHEATSHEETS`, and a test that each C# page's previous and next
  links stay inside the C# group (D1). Verify that the new test fails on a deliberately wrong
  `next`, then passes.
- [x] 3.4 Verify both pages (D11): compile every C# block, check every output against a real run,
  check every version tag against §1's table, confirm the "from the exercise" blocks by script, and
  fetch every URL. Verify that `make gates` shows `GATES_EXIT:0`, and record it all in the
  section-done note.

## 4. The null safety, types and patterns pages

- [ ] 4.1 Write `site/src/content/docs/csharp/null-safety.md` per D3, including turning nullable on
  in a C# 3-style file with the real warnings, the attributes, null-conditional assignment, "Coming
  from C# 3", Pitfalls (including `!` that hides a bug), and the pointer to Exercise 14. Add it to the
  sidebar group, `EXPECTED_SLUGS`, `FULL_CONTENT_SLUGS` and the introduction, with pagination per D1.
  Verify that `make site-build` and `make site-test` pass.
- [ ] 4.2 Write `site/src/content/docs/csharp/types.md` per D3, including record equality and
  `ToString` from a real run, class against record primary constructors, `field` and extension
  members, "Coming from C# 3", Pitfalls, and the pointer to Exercise 14. Add it as in 4.1. Verify that
  `make site-build` and `make site-test` pass.
- [ ] 4.3 Write `site/src/content/docs/csharp/patterns.md` per D3, including the if/else chain and
  its switch expression with the same results, the real non-exhaustive warning and run-time
  exception, "Coming from C# 3", Pitfalls, and the pointer to Exercise 14. Add it as in 4.1. Verify
  that `make site-build` and `make site-test` pass.
- [ ] 4.4 Link the index page's Where next and Exercise 14's steps to the three pages. Verify all
  three pages, and the index's new links, as 3.4 does (D11). Verify that `make gates` shows
  `GATES_EXIT:0`, and record it all in the section-done note.

## 5. Links from the topic pages, and the samples README

- [ ] 5.1 Search the topic pages and cheatsheets for in-passing explanations of the features the C#
  pages cover. Add a link from each to the C# section that covers it (D9), at least on the CLI, DI
  and EF Core pages. Verify that `make site-test` passes, and list every link added in the
  section-done note.
- [ ] 5.2 Update `samples/README.md`: add 14 to the layout, the exercise table, and the list of
  exercises whose tests need no Docker. Verify with `make gates`.

## 6. Human check

- [ ] 6.1 Run exercise 14 from a clean clone myself (the Starter fails with guiding messages, and the
  Solution passes), and report the result. Then stop for Emmz to check, with a copy-paste recipe:
  - the four C# pages and the C# cheatsheet, in light and dark schemes, including the side-by-side
    class and the cheatsheet's tables;
  - the C# group's previous and next links;
  - search finds "record";
  - the code-block copy button on one C# example.

  On PASS, tick and continue. On FAIL, add a review-fix section.

## Workflow follow-up

- Self-review the whole change, write the change-done note, and offer the `code-review` skill.
- Check that the main specs still hold. For example, "Exercise 07's and exercise 12's are the only
  tests that need Docker" stays true only if exercise 14 needs none.
- Merge to `main` with Emmz's go-ahead, check the live site and the tree link, then offer
  `/opsx:archive`, after `openspec validate --specs --strict`.
