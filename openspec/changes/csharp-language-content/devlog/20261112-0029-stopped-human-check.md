# Stopped: §6 human check of the C# section

<!-- Section: 6 · Task: 6.1 · Base: 1248fa8 -->

## What I ran myself

Exercise 14 from a clean clone of the local repo (branch `change/csharp-language-content` at
`1248fa8`, cloned into the scratchpad):

- `dotnet test --project samples/exercises/14-modern-csharp/Starter.Tests`: exit 2, `total: 18`,
  `failed: 14`, as the page says. Each failure names the problem, the TODO and the page, for example
  `OrderLine.Sku has a set accessor, so a line can change after it's created. Use init (TODO 2: see /csharp/types/)`
  and `CustomerDirectory.Find's return is Unknown, expected Nullable: … (TODO 1: see /csharp/null-safety/)`.
- `dotnet test --project samples/exercises/14-modern-csharp/Solution.Tests`: exit 0, `total: 18`,
  `failed: 0`, `succeeded: 18`.

## The question for Emmz

Do the C# pages pass the visual check? Recipe:

```
! cd ~/github/emmz/dotnet-primer && npm --prefix site run build && npm --prefix site run preview
```

Then open http://localhost:4321 and check:

1. `/csharp/`, `/csharp/null-safety/`, `/csharp/types/`, `/csharp/patterns/` and
   `/cheatsheets/csharp/`, in light and dark (the theme switch, top right). On `/csharp/`, look at
   the side-by-side C# 3 / C# 14 class. On the cheatsheet, look at the tables, now with links.
2. The C# group's Previous and Next links: index → Null safety → Types → Patterns, and back.
3. Search (top bar) for `record`: it finds the Types page.
4. The copy button on one C# code block: paste it somewhere, and it's the whole block.

Stop the preview with Ctrl+C.

- **On PASS:** tick 6.1, then the workflow follow-up (whole-change self-review, change-done note,
  offer `code-review`).
- **On FAIL:** add a `## 7. Review fixes: <theme>` section with the fixes, agree it with Emmz, and
  build it with the section loop.

## Next

- Waiting for Emmz's PASS or FAIL on the recipe above. 6.1 is unticked on purpose.
- 3 commits unpushed (§3–§5) on `change/csharp-language-content`.
