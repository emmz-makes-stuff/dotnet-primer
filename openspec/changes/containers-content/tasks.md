# Tasks

## 1. Probe, package version and Trivy

- [x] 1.1 Probe the image facts (design D1, D2), in scratch projects on 10.0.4xx, unsandboxed:
  - the real .NET 10 tags for `sdk`, `aspnet`, `runtime` and `runtime-deps`, including the
    `-noble-chiseled` and `-extra` variants, the AOT SDK tag, and the default `10.0` tag's OS;
  - the `ContainerFamily` value for chiseled, and `dotnet publish -t:PublishContainer`'s real
    output, with the image's default user and port from `docker inspect`;
  - the sizes of one app on the four bases (D2), with the platform;
  - a multi-stage Dockerfile rebuilt after a `.cs` change, with the restore step `CACHED`;
  - inspecting a chiseled container with no shell: whether `docker debug` works under Rancher
    Desktop, and the fallback that does.

  Write a devlog note with the exact outputs, versions and platform.
- [x] 1.2 Probe exercise 12's test approach (design D5), in a scratch copy of the shape D3 and D4
  describe:
  - building the SDK image from a test, as a child process, and the Dockerfile image with
    `ImageFromDockerfileBuilder`, with the project folder as the context (D3);
  - reading `/proc/1/status` from a running chiseled container through the Docker API's file
    copy, and an absent `/bin/sh` and `/usr/share/dotnet/sdk` failing cleanly;
  - the `Now listening on` log line naming the port, and the startup log line naming the
    warehouse.

  Each check must fail against a deliberately wrong image and pass against a right one. **If the
  file API can't read a chiseled container, stop and ask** whether to fall back to `docker inspect`
  (D5). Write a devlog note with the results and how long each build took.
- [x] 1.3 Probe the page's extras (D6, D7, D8), unsandboxed:
  - the page's Compose example: a scratch orders API with EF Core on SQL Server, the `sqlcmd`
    health check, the app starting only once the database is healthy, and `docker compose down -v`;
  - `registry:3` on `localhost:5001`: push, remove, pull by tag, then the SDK's push with
    `ContainerRegistry`;
  - Trivy: the current `aquasec/trivy` version, the socket mount under Rancher Desktop, and the
    summaries for the default and chiseled images, with the date;
  - the ECR commands, syntax-checked against the AWS docs and not run.

  Write a devlog note with the exact outputs and versions.
- [x] 1.4 Add the `Testcontainers` `PackageVersion`, at `Testcontainers.MsSql`'s version, to
  `samples/Directory.Packages.props`. Add Trivy's row to `docs/dependencies.md` (D8), with no
  `make deps` check, and say why in a devlog note. Verify that `make samples-build` and `make deps`
  pass (unsandboxed). **If restore reports a vulnerable package, stop and ask.**

## 2. Exercise 12: containers

- [x] 2.1 Scaffold `samples/exercises/12-containers/` (design D3, D4): `Containers.Starter` and
  `Containers.Solution` web projects, each standing alone for a container build, with a
  `.dockerignore`. Add a shared `tests/`, and `Starter.Tests` and `Solution.Tests`, which pass
  their app's folder as `AssemblyMetadata` and don't reference the app. Add them to `Primer.slnx`,
  and the Solution and its tests to `Primer.Solutions.slnx`. Verify with `make samples-build`, and
  `docker build .` in each app folder.
- [x] 2.2 Write the Solution: the orders API, `GET /info`, the startup log line, the `.csproj`
  container properties, and the multi-stage Dockerfile, with TODOs 1–4 solved. Verify by running
  both images and calling `GET /info` on 8080, with and without `Warehouse__Name`.
- [x] 2.3 Write the tests (design D5): the two image fixtures, with their Docker-unavailable
  message, and one test per check, each naming its TODO. Verify that the Solution passes, three
  runs in a row, and record how long `make samples-test` takes, before and after.
- [x] 2.4 Write the Starter, with the four bugs and TODOs 1–4 (design D4). Verify that each test
  fails against it with its guiding message, and that completing a scratch copy with the
  Solution's code passes everything. Then complete scratch copies that each leave out one TODO,
  and confirm that only that TODO's tests fail.
- [x] 2.5 Push the branch, and run the `samples` workflow on it
  (`gh workflow run samples.yml --ref change/containers-content`). Check that it passes on
  `ubuntu-latest` (`linux/amd64`) with exercise 12's tests, and record the run ID in the
  section-done note.
  - `SITE_TEST_EXIT:1` is expected here (D9). Record it as a deliberate red.

## 3. Containers cheatsheet and page

- [x] 3.1 Create `site/src/content/docs/cheatsheets/containers.md` (design D10), with a back link
  and Learn links. Add it to the sidebar's Cheatsheets group, the introduction's cheatsheet cards
  and `EXPECTED_SLUGS`. Verify that `make site-build` passes.
- [x] 3.2 Rewrite `site/src/content/docs/deploy/containers.md` per D1 and D6–D8. Cover every item
  in the eight Containers page requirements. Add:
  - the cheatsheet tip;
  - the Pitfalls, including:
    - running as root;
    - shipping the SDK in the runtime image;
    - copying the host's `bin/` and `obj/` into the build without a `.dockerignore`;
    - copying the source before the restore, so every change re-restores;
    - a port or setting hard-coded in the code;
    - a secret baked into an image layer;
    - a pinned base image that's never rebuilt;
    - `latest` in production;
  - an "Exercise 12" section in the exercise-07 format. It says that its tests need Docker, links
    to the Docker page, and gives the exact GitHub tree link.
- [x] 3.3 Verify the page (D11):
  - compile every C# snippet in a scratch project, with warnings as errors;
  - build every Dockerfile, and run the `compose.yaml`, as shown;
  - check that every number and output on the page comes from 1.1's or 1.3's real output;
  - confirm that the "from the exercise" blocks match the real files.

  Record all of it in the section-done note.
- [x] 3.4 Add `deploy/containers` and `cheatsheets/containers` to `FULL_CONTENT_SLUGS`, and the
  pair to `CHEATSHEETS`. Fetch every URL on both pages. Verify that `make gates` shows
  `GATES_EXIT:0`.

## 4. Samples README

- [x] 4.1 Update `samples/README.md`: add 12 to the layout and the exercise table, and make the
  Docker prerequisite say that exercises 07 and 12 need it, and why 12 does. Verify with
  `make gates`.

## 5. Human check

- [x] 5.1 Run exercise 12 from a clean clone myself (the Starter fails with guiding messages, and
  the Solution passes), and report the result. Then stop for Emmz to check, with a copy-paste
  recipe:
  - the Containers page and its cheatsheet, in light and dark schemes, including the size table;
  - search finds "chiseled";
  - the code-block copy button on a Dockerfile block.

  On PASS, tick and continue. On FAIL, add a review-fix section.

## 6. Review fixes: the Docker claim in exercise 07's requirement

- [x] 6.1 In `specs/sample-exercises/spec.md`, add a MODIFIED delta for "EF Core exercise tests use
  a real SQL Server", whose "It is the only exercise whose tests need Docker" exercise 12 makes
  false. Word it as Emmz agreed: "Its tests and exercise 12's are the only ones that need Docker."
  Keep its scenarios. Verify with `make gates`.

## Workflow follow-up

- Self-review the whole change, write the change-done note, and offer the `code-review` skill.
- Merge to `main` with Emmz's go-ahead, check the live site and both tree links, then offer
  `/opsx:archive`, after `openspec validate --specs --strict`.
