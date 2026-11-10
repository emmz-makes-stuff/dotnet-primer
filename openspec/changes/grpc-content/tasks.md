# Tasks

## 1. Probe and package versions

- [x] 1.1 Probe the setup (design D3, D5, D8, D9). In scratch projects on 10.0.4xx:
  - a `dotnet new web` server with `Grpc.AspNetCore` 2.85.0 and a two-RPC `.proto`, run on Kestrel
    with the page's h2c setting, and called from a scratch console client (`Grpc.Net.Client`);
  - the same client against an HTTP/1.1-only endpoint, recording the exact error;
  - an xUnit v3 test project that generates a client from a copy of the `.proto` with a different
    `csharp_namespace`, references the server project, and calls it through
    `factory.Server.CreateHandler()`, checking that the two sets of generated types don't clash;
  - what the client gets when nothing is mapped (`Unimplemented`, or another status), and when one
    RPC is missing from the server's contract;
  - a deadline on a server stream, checking `DeadlineExceeded` and that the server's token is
    cancelled.

  Write a devlog note with the exact settings, error texts and statuses.
- [x] 1.2 Add the four `PackageVersion` entries from design D8 to `samples/Directory.Packages.props`.
  Verify that `make samples-build` and `make samples-format` pass (unsandboxed, for NuGet). **If
  restore reports a vulnerable transitive package, stop and ask.**

## 2. Exercise 08: gRPC

- [x] 2.1 Scaffold `samples/exercises/08-grpc/` like 07 (design D4):
  - `Grpc.Starter` and `Grpc.Solution` web projects, with `Protos/orders.proto`;
  - the shared `tests/`, with `tests/Protos/orders.proto`;
  - `Starter.Tests` and `Solution.Tests`.

  Add them to `Primer.slnx`. Add only the Solution and its tests to `Primer.Solutions.slnx`. Verify
  with `make samples-build`.
- [x] 2.2 Implement the Solution: the full contract, the pre-written `OrderStore`, validation and
  `IOrderTracker`, and `OrdersService` with the three RPCs. Check that the Solution's `.proto` and
  `tests/Protos/orders.proto` differ only in `option csharp_namespace` (design D3). Verify that
  `make samples-build` passes.
- [x] 2.3 Write the shared tests (D4): the factory with the gRPC channel and the test tracker, and
  the guiding messages for "not mapped" and "RPC missing from the contract".
  - Cover every scenario in the two exercise-08 requirements, with assertion messages that name the
    missing behaviour.
  - Verify that `make samples-test` passes. Run exercise 08's tests three more times to check the
    deadline test is stable.
- [x] 2.4 Write the Starter: the unary-only `.proto` with a `TODO` for `TrackOrder`, the pre-written
  parts, and `TODO`s 1–5. Verify that the Starter tests fail with the guiding messages. Complete a
  scratch copy, confirm that its tests go green, and discard it. Then complete a second scratch copy
  with `CancellationToken.None` passed to the tracker, and confirm that only the deadline test fails.
- [x] 2.5 Push the branch, and check that the `samples` workflow passes on `ubuntu-latest` with
  exercise 08's tests run. Record the run ID in the section-done note.

## 3. gRPC cheatsheet and page

- [ ] 3.1 Create `site/src/content/docs/cheatsheets/grpc.md` (design D6), with a back link and Learn
  links. Add it to the sidebar's Cheatsheets group, the introduction's cheatsheet cards and
  `EXPECTED_SLUGS`. Verify that `make site-build` passes.
- [ ] 3.2 Rewrite `site/src/content/docs/services/grpc.md` per D1, D2, D5 and D7. Cover every item in
  the six gRPC page requirements. Add the cheatsheet tip, the Pitfalls (including the HTTP/1.1-only
  endpoint and `double` for money), and an "Exercise 08" section in the exercise-01 format. That
  section says it needs no Docker and gives the exact GitHub tree link.
- [ ] 3.3 Verify the page (D9):
  - compile every snippet in a scratch project (warnings as errors);
  - run the deadline, error, health-check and HTTP/1.1 examples, and paste their real output;
  - confirm that the "from the exercise" snippets match the real files.

  Record all of it in the section-done note.
- [ ] 3.4 Add `services/grpc` and `cheatsheets/grpc` to `FULL_CONTENT_SLUGS`, and the pair to
  `CHEATSHEETS`. Check every Learn link with the link script. Verify that `make gates` shows
  `GATES_EXIT:0`.

## 4. Samples README

- [ ] 4.1 Update `samples/README.md`: add 08 to the layout and the exercise table, and make the
  Docker prerequisite say that only exercise 07 needs it. Verify with `make gates`.

## 5. Human check

- [ ] 5.1 Run exercise 08 from a clean clone myself (Starter fails with guiding messages, Solution
  passes), and report the result. Then stop for Emmz to check, with a copy-paste recipe:
  - the gRPC page and its cheatsheet, in light and dark schemes;
  - search finds "proto";
  - the code-block copy button on a `.proto` block.

  On PASS, tick and continue. On FAIL, add a review-fix section.

## Workflow follow-up

- Self-review the whole change, write the change-done note, and offer the `code-review` skill.
- Merge to `main` and push, with Emmz's go-ahead, then check the live page and cheatsheet.
- Offer `/opsx:archive`.
