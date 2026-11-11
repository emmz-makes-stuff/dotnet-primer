# Stopped: 5.1 needs Emmz's visual check

<!-- Section: 5 · Task: 5.1 (unticked) · Base: 2c5dd86 -->

The stamp runs ahead of the clock (it's 16:45), so it sorts after `1650-section-4-done`. §1–§4 are
committed, and §3 (`71e4985`) and §4 (`2c5dd86`) aren't pushed yet. Nothing is uncommitted apart
from this note.

## Done myself: exercise 12 from a clean clone

A fresh `git clone --branch change/containers-content` of the local repo at `2c5dd86`, with SDK
10.0.404, on OrbStack:

- **Starter:** `dotnet test --project samples/exercises/12-containers/Starter.Tests` gives 9 total,
  9 failed. Each failure names its TODO and image:
  - `In the SDK-published image, the app listens on http://[::]:5000, not on port 8080, which the
    .NET images set with ASPNETCORE_HTTP_PORTS. Something in the code is choosing the port instead
    (TODO 1).` The same for the Dockerfile image.
  - `… the app used the warehouse name "Default warehouse", although Warehouse__Name was set to …
    (TODO 2)`, for both images.
  - `The SDK-published image runs the app as root. … (TODO 3)`, and the Dockerfile's `(TODO 4)`.
  - `The SDK-published image has a shell (/bin/sh), so it isn't on a chiseled base. … (TODO 3)`, and
    the Dockerfile's `(TODO 4)`.
  - `The Dockerfile image still holds the .NET SDK, … (TODO 4).`
- **Solution:** 9/9, in 11 s.
- No stray containers or images afterwards. The base images were already cached, so this didn't
  re-measure the first-run download.

## The question

Do the Containers page and its cheatsheet look right in both light and dark schemes, including the
size table? Does search find "chiseled", and does the copy button work on a Dockerfile block?

## Recipe

The site preview, from the repo root:

```sh
npm --prefix site run build && npm --prefix site run preview
```

Open <http://localhost:4321/deploy/containers/> and <http://localhost:4321/cheatsheets/containers/>.
- Toggle the theme (top right).
- Search for `chiseled`.
- Press the copy button on the `Dockerfile (from the exercise)` block under "Multi-stage
  Dockerfiles", then paste it somewhere.

The widest things to look at:
- the size table (3 columns, with long notes) under "Smaller images";
- the `Container…` properties table (3 columns) under "Publishing without a Dockerfile";
- the tag table under "The .NET images", whose `10.0-noble-chiseled` row is long;
- the Coming from .NET 3.5 table;
- the Trivy summary line under "Scanning images", which is wide box-drawing text.

If `localhost:4321` answers with something else, another process holds the port. The preview then
prints the port it used instead.

- **On PASS:** tick 5.1 and push. Then the whole-change review (`git diff ea386cb..HEAD`, from §1's
  base), the change-done note, and offer `code-review`.
- **On FAIL:** add `## 6. Review fixes: …` to `tasks.md` with what she found, and agree it with
  her.

## Next

Waiting on Emmz's answer to the question above.
