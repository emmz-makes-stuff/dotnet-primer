# Handoff: §3 started: 3.1 (cheatsheet) done and ticked, uncommitted; 3.2 (the page) next

<!-- Section: 3 · Tasks: 3.1 ticked, 3.2–3.4 open · Base: af71c82 -->

The context passed 40% after 3.1, so I'm handing off before the page, the largest task.
**Nothing in §3 is committed**, and neither are 2.5's tick and `1626-section-2-done`. All of it
lands in §3's single commit.

## Uncommitted files

- `site/src/content/docs/cheatsheets/containers.md` (new, Prettier-formatted): the images and tags,
  publish without a Dockerfile (the CLI and `.csproj` properties, plus a defaults table), a
  multi-stage Dockerfile and `.dockerignore`, run and look inside (`docker top`, `inspect`, `cp`,
  the `--pid=container:` BusyBox sidecar), the environment-variable table, a compact
  `compose.yaml` and the Compose commands, a local `registry:3` on 5001 and the SDK push, ECR
  (marked not run), and Trivy (with the CI form `--severity HIGH,CRITICAL --exit-code 1`).
- `site/astro.config.mjs`: `cheatsheets/containers` is last in the Cheatsheets group.
- `site/src/content/docs/index.mdx`: a "Containers" `LinkCard`, last.
- `site/tests/dist.test.ts`: `cheatsheets/containers` in `EXPECTED_SLUGS` only. **`FULL_CONTENT_SLUGS`
  and `CHEATSHEETS` wait for 3.4**, because the page doesn't link to the cheatsheet yet.
- `tasks.md`: 2.5 and 3.1 ticked. `devlog/20261111-1626-section-2-done.md` (CI run 43057802074).

Verified: `SITE_BUILD_EXIT:0` and `SITE_FORMAT_EXIT:0` after the cheatsheet. `site-test` is still
red only on `12-containers` (D9).

## Unverified cheatsheet items, for 3.3 to build or run

Probes 1.1–1.3 didn't run these. 3.3 must, or they come out:
- the `<ContainerEnvironmentVariable Include="Warehouse__Name" Value="Leeds" />` item, and the
  defaults table's `ContainerPort` row (it says 8080/tcp from the base image; 1.1 saw `ExposedPorts`
  `8080/tcp`);
- `docker inspect orders-api --format '{{.Config.User}} {{json .Config.ExposedPorts}}'`, and
  `docker cp orders:/app/appsettings.json -` (1.1 ran `docker cp` only on `/proc` and absent paths);
- the cheatsheet's shortened `compose.yaml`: no `timeout` or `start_period`, and no volume, unlike
  1.3's;
- "`ASPNETCORE_ENVIRONMENT`: `Production` unless set", and that environment variables override
  `appsettings.{Environment}.json` (the documented ASP.NET Core order);
- the ECR commands use example values (`eu-west-2`, `123456789012`) and are syntax-only, as D7 says.

## For 3.2 (the page), the facts and where they're recorded

- The size table (1.1): 262, 170, 132 and 60.5 MB, on `linux/arm64`, OrbStack, 2026-11-11. The
  exercise's Starter (one `sdk` stage) is **977 MB** against the Solution's **132 MB** (`1622`).
- The SDK's base picks (1.1's table), and why `InvariantGlobalization` matters. Default users:
  `aspnet:10.0` runs as **root** with a Dockerfile, while SDK publish sets 1654.
- The Dockerfile rebuild output, with `#10 CACHED` (1.1).
- Compose (1.3): the `up` output, the 201 and `GET` bodies, the `down -v` output. **SqlClient's
  `Globalization Invariant Mode is not supported`** on plain chiseled is both a Pitfall and the
  reason the Compose example uses `-extra`. Mention the `localhost:8080` clash, and the
  `"5080:8080"` advice.
- Registry and Trivy (1.3): the push digest and the SDK push output. 22 against 0 vulnerabilities,
  with 97 against 8 packages, and the date and DB time.
- No `docker debug` (Docker Desktop only, inferred for Rancher). Inspect with `cp`, `top` and the
  sidecar (1.1).
- Exercise 12 section: the exercise-07 format. Its tests need Docker (link `/platform/docker/`),
  the tree link
  `https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/12-containers`,
  the four TODOs (D4), and that the tests build both images and run them (first run pulls the bases).
- "From exercise 12" blocks must be line-for-line copies (D11). The 4.3 harness from the async
  change is gone; `1440` and `1521` in the archived async change describe how to rebuild it. The
  scratch probes (`c1`, `c2`, `c3`, `l1o`) were in the session scratchpad, and are gone after a
  clear.

## Gotchas this session

- Scratch projects outside the repo must copy in `global.json`, or they build on the 11.0 RC.
- In zsh, `$f:latest` is read as a `:l` modifier, so write `"${f}:latest"`. `set -- $p` doesn't
  split words. BSD `sed` has no `\b`, so use `perl -pi`.
- The `rm -rf "$VAR"` safety check blocks the whole command.
- Docker commands need the sandbox off. A sandboxed `docker image ls` says "permission denied",
  and a count of 0 from it means nothing.
- The site's Prettier must run from `site/` (`cd site && npx prettier --write <file>`).
- `make samples-test` takes 29 s warm, both before and after exercise 12.

## Next

- **3.2:** rewrite `site/src/content/docs/deploy/containers.md` per D1 and D6–D8, covering all
  eight Containers requirements in `specs/topic-page-content/spec.md`, plus the cheatsheet tip, the
  Pitfalls list in 3.2, and the Exercise 12 section. Then 3.3 (verify every snippet and number, and
  the cheatsheet items above), then 3.4 (the `FULL_CONTENT_SLUGS` and `CHEATSHEETS` pair, fetch
  every URL, `GATES_EXIT:0`). Then §3's one commit, with 2.5's tick in it.
- Nothing is pushed beyond `af71c82`. The branch tracks `origin/change/containers-content`.
