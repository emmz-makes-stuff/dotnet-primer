# Tasks

## 1. Probe, versions, and the Aspire and AWS CLI rows

- [x] 1.1 Add the Aspire CLI and AWS CLI rows to `docs/dependencies.md`, and their checks to
  `make deps` (design D9): `aspire --version` and `aws --version`, with no AWS auth check. Record
  aspire.dev's install command. Verify that `make deps` shows `DEPS_EXIT:0` (unsandboxed), and say
  why in a devlog note.
- [x] 1.2 Probe health checks and shutdown (D2, D3, D5, D6), in scratch projects on 10.0.4xx,
  unsandboxed:
  - the live and ready endpoints' real status codes and bodies, with the store up, with it down,
    and during shutdown;
  - the default `HostOptions.ShutdownTimeout` in .NET 10, and `docker stop`'s default timeout;
  - whether .NET 10's `WebApplicationFactory.UseKestrel` runs the app on a random port, and whether
    stopping it drains an in-flight request;
  - whether a `ShutdownTimeout` shorter than an order really cuts the order off (D4). **If it
    doesn't, stop and ask** before choosing another bug for TODO 4;
  - `docker stop` with an order in flight, on the SDK-published image, with and without `--init`,
    and with the short timeout: the order's response, and the app's shutdown log lines.

  Write a devlog note with the exact outputs, versions and platform.
- [x] 1.3 Probe Aspire (D7) with the Aspire CLI, unsandboxed, in a scratch folder. Run an AppHost
  with SQL Server, the orders API (`WithReference`, `WaitFor`) and a second project that calls the
  API by its resource name. Record:
  - each CLI command and its output;
  - the dashboard's address and sign-in;
  - the connection string's configuration key, and the SQL Server image Aspire chose;
  - the versions;
  - what ServiceDefaults maps for health, and in which environments.

  Write a devlog note.
- [x] 1.4 Probe the AWS content (D8), with nothing run against AWS:
  - fetch each AWS doc the page will cite, and record its URL;
  - write the ECS task definition, and check its keys against
    `aws ecs register-task-definition --generate-cli-skeleton` with `jq`;
  - `help` for each `aws` command used;
  - compile `AddAWSLambdaHosting` and `AddSystemsManager` (with Secrets Manager, through Parameter
    Store's reference or the provider the docs name) in scratch projects, with warnings as errors.

  Record the current package versions. **If restore reports a vulnerable package, stop and ask.**
  Write a devlog note.

## 2. Exercise 13: health checks and shutdown

- [x] 2.1 Scaffold `samples/exercises/13-health-and-shutdown/` (design D4): `HealthAndShutdown.Starter`
  and `HealthAndShutdown.Solution` web projects, a shared `tests/`, and `Starter.Tests` and
  `Solution.Tests`. Add them to `Primer.slnx`, and the Solution and its tests to
  `Primer.Solutions.slnx`. Verify with `make samples-build`.
- [x] 2.2 Write the Solution: the orders API, the slow `POST /orders`, a store that can be taken
  offline and signals an order in progress, `/health/live` and `/health/ready`, the drain delay
  (D3), and the shutdown timeout, with TODOs 1–4 solved. Verify by running it, and calling both endpoints with the store
  up and down.
- [x] 2.3 Write the tests (design D5): real Kestrel in process, one test per check, each naming its
  TODO, synced on the store's signal. Verify that the Solution passes, ten runs in a row, and
  record how long `make samples-test` takes, before and after.
- [x] 2.4 Write the Starter, with the four bugs and TODOs 1–4 (design D4). Verify that each test
  fails against it with its guiding message, and that completing a scratch copy with the
  Solution's code passes everything. Then complete scratch copies that each leave out one TODO,
  and confirm that only that TODO's tests fail.
- [ ] 2.5 Push the branch, and run the `samples` workflow on it
  (`gh workflow run samples.yml --ref change/orchestration-aws-content`). Check that it passes on
  `ubuntu-latest` with exercise 13's tests, and record the run ID in the section-done note.
  - `SITE_TEST_EXIT:1` is expected here (D10). Record it as a deliberate red.

## 3. Orchestration & AWS cheatsheet and page

- [ ] 3.1 Create `site/src/content/docs/cheatsheets/orchestration-and-aws.md` (design D11), with a
  back link, Learn links, and aspire.dev and AWS links. Add it to the sidebar's Cheatsheets group,
  the introduction's cheatsheet cards and `EXPECTED_SLUGS`. Verify that `make site-build` passes.
- [ ] 3.2 Rewrite `site/src/content/docs/deploy/orchestration-and-aws.md` per D1, D3 and D6–D8.
  Cover every item in the seven Orchestration page requirements. Add:
  - the cheatsheet tip;
  - the Pitfalls, including:
    - liveness that checks the database;
    - readiness that stays healthy during shutdown;
    - a `ShutdownTimeout` longer than the platform's stop timeout, or shorter than the longest
      request;
    - an ECS container health check that needs `curl` in a chiseled image;
    - AWS access keys in configuration or the image;
    - treating the Aspire AppHost as a production deployment;
  - an "Exercise 13" section in the exercise-07 format. It says that it needs no Docker, and gives
    the exact GitHub tree link.

  Retarget the Containers page's link for app health checks to `#health-checks`.
- [ ] 3.3 Verify the page (D12):
  - compile every C# snippet in a scratch project, with warnings as errors;
  - run every non-AWS command as shown, and syntax-check every AWS one as D8 says;
  - check that every number and output on the page comes from §1's real output;
  - confirm that the "from the exercise" blocks match the real files.

  Record all of it in the section-done note.
- [ ] 3.4 Add `deploy/orchestration-and-aws` and `cheatsheets/orchestration-and-aws` to
  `FULL_CONTENT_SLUGS`, and the pair to `CHEATSHEETS`. Fetch every URL on both pages. Verify that
  `make gates` shows `GATES_EXIT:0`.

## 4. Samples README

- [ ] 4.1 Update `samples/README.md`: add 13 to the layout and the exercise table, and to the list
  of exercises whose tests need no Docker. Verify with `make gates`.

## 5. Human check

- [ ] 5.1 Run exercise 13 from a clean clone myself (the Starter fails with guiding messages, and
  the Solution passes), and report the result. Then stop for Emmz to check, with a copy-paste
  recipe:
  - the Orchestration & AWS page and its cheatsheet, in light and dark schemes, including the
    "which one?" table and the task definition block;
  - search finds "readiness";
  - the code-block copy button on the ECS task definition.

  On PASS, tick and continue. On FAIL, add a review-fix section.

## Workflow follow-up

- Self-review the whole change, write the change-done note, and offer the `code-review` skill.
- Check that the main specs still hold. For example, "Its tests and exercise 12's are the only ones
  that need Docker" stays true only if exercise 13 needs none.
- Merge to `main` with Emmz's go-ahead, check the live site and the tree link, then offer
  `/opsx:archive`, after `openspec validate --specs --strict`.
