# Stopped: can't verify the global-tool lines (task 1.2)

<!-- Section: 1 · Tasks: 1.2–1.3 · Base: 5a2682c -->

## State (uncommitted)

- `cheatsheets/dotnet-cli.md` is rewritten. It has Install and select SDKs (led by `dotnetup`),
  Create, Build/run/test, Packages, Tools, User secrets, EF Core migrations, Formatting and Publish,
  and no "Coming soon".
- `cheatsheets/dotnet-cli` is added to `FULL_CONTENT_SLUGS`.
- With that state, `make gates` gave `GATES_EXIT:0`.
- Link check: all 7 Learn links OK. The method was to follow redirects, use a 15 s timeout, and accept
  only a 200 on the same path once the locale is stripped. It runs via `ctx_execute` JS.
- 1.2 and 1.3 are left **unticked**.

## What blocked

D4's Tools section asks for **global tools**, which the cheatsheet doesn't have yet.

- `dotnet tool install -g dotnetsay` (then uninstall) was denied by the auto-mode classifier.
- A scratch `--tool-path` install of the same tool was denied too.
- Only `dotnet tool install --help` could be read. It confirms `-g, --global  Install the tool for the
  current user` and `--tool-path`.

D5 says every command shown must have been run. Dropping global tools would narrow D4. So: stop and
ask.

## Verified so far (scratch, 10.0.403, unsandboxed for NuGet)

- Every other cheatsheet line ran with exit 0. The exceptions are deliberate:
  - `dotnet run` and `watch` were stopped by timeout once they were listening.
  - `dotnet format --verify-no-changes` exits **2** on the stock `webapi` template, which has a
    whitespace issue. That's a nice real example.
- **Finding:** `dotnet new xunit3` defaults to **net8.0**. Then `dotnet add Orders.Tests reference
  Orders.Api` (a net10.0 project) fails: "incompatible targeted frameworks". The cheatsheet now shows
  `-f net10.0`. The old stub line was wrong.
- **Finding:** `dotnet ef migrations script --idempotent` fails on SQLite ("not currently
  supported"). It succeeded with the SQL Server provider (script generation needs no server). The
  cheatsheet notes "not supported on SQLite". So D4's claim that the commands are
  "provider-independent" isn't quite true for that one flag.
- `dnx` has no `-y`, and its own `--version` option swallows the tool's, so the form is
  `dnx dotnet-ef -- --version`.
- `dotnet watch` ignores SIGTERM from `gtimeout`, so it hung the script. Use `gtimeout -k`, or don't
  script it.
- `dotnetup runtime install runtime@10.0` is verified in the isolated root (then uninstalled).
- `--project Orders.Data --startup-project Orders.Api` on the cheatsheet: the flags were run with
  both set to `Orders.Api`. `Orders.Data` is illustrative.
- Cleanup: the scratch user-secrets folder and the `orders-api:latest` image are removed. The
  `xunit.v3.templates` pack is left installed (harmless).

## Question for Emmz

How do we verify (or drop) the global-tool lines?

## Next

- Waiting on Emmz's answer. Then add the global-tool lines (or not), tick 1.2 and 1.3, rerun
  `make gates`, write the section-1-done note, and commit §1.
- After §1 comes §2, task 2.1: the page.
