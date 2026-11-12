# Stopped: §7 recheck, the stacked C# 3 / C# 14 examples

<!-- Section: 7 · Tasks: 7.1 done, 7.2 waiting · Base: 1248fa8 -->

## Why §7 exists

Emmz's 6.1 check FAILED on its first item: on `/csharp/` the C# 3 and C# 14 blocks sat side by side
and both clipped their lines (screenshot, 2026-11-12). The `.side-by-side` grid made two columns
whenever each got 20rem, but Starlight's content column is about 48rem and the classes have 70–90
character lines. Emmz chose **stacking** (C# 3 first, then C# 14, full width) over tabs or a wider
breakout. She hadn't checked 6.1's other items yet.

## 7.1, done (uncommitted on purpose, until the recheck)

- `csharp/index.mdx`: removed both `<div class="side-by-side">` wrappers (the class, and "creating
  one"). The blocks and their titles are unchanged.
- `theme.css`: removed the `.side-by-side` rules, which nothing else used.
- `proposal.md`, `design.md` and task 3.1: "side by side" → "one after the other" (or "C# 3 first
  and C# 14 after it") for the class. The cheatsheet's "side by side" stays: its tables are.
- `make gates`: `SITE_BUILD_EXIT:0`, `SITE_LINT_EXIT:0`, `SITE_FORMAT_EXIT:0`,
  `Tests  18 passed (18)`, `SITE_TEST_EXIT:0`, `SAMPLES_BUILD_EXIT:0`, `SAMPLES_FORMAT_EXIT:0`,
  `SAMPLES_TEST_EXIT:0`, `VALIDATE_EXIT:0`, `GATES_EXIT:0`. (The first run gave `SITE_FORMAT_EXIT:1`
  on `theme.css`: a leftover blank line. Prettier fixed it.)

## The question for Emmz

```
! cd ~/github/emmz/dotnet-primer && npm --prefix site run build && npm --prefix site run preview
```

Open http://localhost:4321/csharp/ and check:

1. "One class, C# 3 and C# 14": C# 3 above C# 14, nothing clipped, in light and dark. The same for
   the "creating one" pair below it.
2. The C# group's Previous and Next links: index → Null safety → Types → Patterns, and back.
3. Search for `record`: it finds the Types page.
4. The copy button on one C# code block copies the whole block.

- **On PASS:** tick 7.2 and 6.1, write the section-done note, and commit §7 (with 6.1) as one
  commit. Then the workflow follow-up: whole-change self-review, change-done note, offer
  `code-review`.
- **On FAIL:** another review-fix section, agreed with Emmz first.

## Next

- Waiting for Emmz's PASS or FAIL on the recipe above.
- 3 commits unpushed (§3–§5) on `change/csharp-language-content`.
