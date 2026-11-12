# Merged to main, live site checked

Emmz skipped the `code-review` skill, then said go for the merge.

- Pushed `change/csharp-language-content`, then fast-forwarded `main` to `37f43da` and pushed it.
- CI on `37f43da`: the `site` and `samples` runs both concluded `success`.
- Live site, `https://dotnet-primer.emmz.dev`: `/csharp/`, `/csharp/null-safety/`, `/csharp/types/`,
  `/csharp/patterns/` and `/cheatsheets/csharp/` all return 200. None shows "Coming soon", and none
  has `side-by-side` left.
- The exercise 14 tree link
  (`https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/14-modern-csharp`)
  returns 200.
- `openspec validate --specs --strict`: `Totals: 5 passed, 0 failed (5 items)`.

## Next

- Offer `/opsx:archive`, and wait for a yes. Afterwards, `make validate` fails with no active change.
  Check the specs with `openspec validate --specs --strict` instead.
