# Merged and live

<!-- After §7 · main at da837fa -->

With Emmz's go-ahead, `main` was fast-forwarded to `da837fa`: `git push origin HEAD:main`, then
`git branch -f main origin/main`, without checking out main. Earlier changes were merged the same
way, with no merge commits.

- The `site` and `samples` workflows on `da837fa` both passed.
- `https://dotnet-primer.emmz.dev/testing/` gives 200. It has the Exercise 09 section, the re-run
  coverage figures ("84 of 120 lines") and the repo-root filter command, and no "Coming soon".
- `https://dotnet-primer.emmz.dev/cheatsheets/testing/` gives 200. It has the back link and
  `SharedDatabase`, and no "Coming soon".
- `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/09-testing` now
  gives 200.

## Next

- Offer `/opsx:archive` and wait for a yes. After the sync, run `openspec validate --specs --strict`,
  because `make validate` fails when no change is active.
