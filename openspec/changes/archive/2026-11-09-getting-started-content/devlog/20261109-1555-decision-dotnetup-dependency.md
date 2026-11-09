# Decision: dotnetup becomes a declared dependency

<!-- Section: 3 · Tasks: 3.2 · Base: 70e2954 -->

`dotnetup` is added to `docs/dependencies.md`, and checked in `make deps` with
`command -v dotnetup && dotnetup --version`. That's required by CLAUDE.md for any new tool.

**Why:**

- The Getting started page and the cheatsheet teach `dotnetup` as the install path. Their commands
  are verified by running them (design D2, D5), so maintaining the site needs it on the machine.
- The samples README now installs the pinned SDK with it.
- Emmz chose `dotnetup` as the primer's install route in the proposal. Agreement came with the
  proposal, so no separate stop was needed.

**Also changed:**

- The `dotnet` row's install column is now `dotnetup sdk install 10.0.4xx`, from the repo root.
- The brew cask is gone from that row and from the MISSING message.
- The check itself (`^10\.0\.4`) is unchanged.
- The explicit band, not a bare `sdk install`, follows D3: a bare install tracks `10.0`, which could
  update past 4xx.

**Rejected:**

- **Checking the `dotnetup` version range** (for example `^0\.`). Preview version strings carry
  build metadata (`0.3.0-preview.1.26554.3+a98e2d0…`), and the design says "any 0.x preview". A GA 1.0
  shouldn't fail the check, so presence is enough.

**Verified:**

- `make deps` gives `DEPS_EXIT:0`.
- With the check temporarily misspelled (`dotnetupx`), it gives `MISSING dotnetup — see
  docs/dependencies.md` and `DEPS_EXIT:1`. The misspelling is reverted (`grep -c dotnetupx Makefile`
  gives 0).

## Next

- Run `make gates`, tick 3.2, write the section-3-done note, and commit §3.
- Then §4, task 4.1: the human check (stop and ask Emmz).
