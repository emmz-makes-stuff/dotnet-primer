# Handoff: §2 tasks done, section not yet reviewed or committed

<!-- Section: 2 · Tasks: 2.1–2.3 · Base: 095d60e -->

The context passed 40% after task 2.3. All three §2 tasks are ticked. The section's self-review, its
`make gates` run, the section-done note and the commit **remain**.

## Uncommitted (all part of §2; don't revert)

- `samples/Primer.slnx`: the four `04-logging` projects, in the 03 folder style.
- `samples/Primer.Solutions.slnx`: only `04-logging/Solution` and `Solution.Tests`.
- `samples/exercises/04-logging/`, new:
  - The csprojs: `Solution/` and `Starter/` (`Logging.{Solution,Starter}.csproj`, empty
    `Sdk.Web`), and `Solution.Tests/` and `Starter.Tests/`. The test csprojs are copies of 03's, plus
    `Microsoft.Extensions.Diagnostics.Testing`.
  - `appsettings.json`: 03's, without the Maintenance section.
  - `Orders.cs` and `PaymentGateway.cs`, the shared domain, **identical** in Solution and Starter.
    `PlaceOrderRequest(CustomerEmail, PaymentMethod, Items, Total)`, `OrderItem(Sku, Quantity)`,
    `PlaceOrderResponse(OrderId, Status, Reason?)`, `IPaymentGateway.Charge(orderId, order)`,
    `PaymentResult` and `PaymentGatewayException`. `FakePaymentGateway` declines `voucher` ("vouchers
    are not accepted"), and throws for emails ending `@fail.test`. **Reuse these unchanged in 05 and
    06.**
  - `Solution/OrderLog.cs`: `[LoggerMessage]` `OrderPlaced` (EventId 1, Information, "Order {OrderId}
    placed with {ItemCount} items") and `PaymentDeclined` (EventId 2, Warning, "Payment for order
    {OrderId} was declined: {Reason}").
  - `Solution/Program.cs`: `POST /orders`, with id `ord_{guid:N}`. It returns 201 `Created` with
    `PlaceOrderResponse` for an accepted order, and a 402 JSON `PlaceOrderResponse` for a declined
    one.
  - `Starter/OrderLog.cs`: an empty partial class with TODO 1.
  - `Starter/Program.cs`: bad logging between
    `#pragma warning disable/restore CA1848, CA1873, CA2254`:
    - two `LogError` calls on decline, one with the email (TODO 2);
    - an interpolated `LogInformation` with the email (TODO 3).
  - `tests/OrderLoggingTests.cs`, namespace `Orders.Tests`:
    - a nested `App : WebApplicationFactory<Program>`, with `ConfigureLogging(l =>
      l.AddFakeLogging())` and `Services.GetFakeLogCollector()`;
    - 3 tests: placed (one Information with `OrderId` and `ItemCount=3`), declined (one Warning with
      `OrderId` and `Reason`, no Error), and no email anywhere (message, state, scopes, exception).
- `tasks.md`: 2.1, 2.2 and 2.3 ticked.

## Verified this session

- `make samples-build` gives `SAMPLES_BUILD_EXIT:0` (2.1, with a temporary Starter that copied the
  Solution).
- `make samples-format` and `samples-test` give `SAMPLES_FORMAT_EXIT:0` and `SAMPLES_TEST_EXIT:0`.
  Solution.Tests: 3/3.
- Starter.Tests fail 3/3, each with a guiding message:
  - "The customer's email address appeared in 2 log entries…";
  - "Expected exactly one Warning log with an OrderId property… found 0";
  - "Expected exactly one Information log… found 0".
- A scratch copy with the TODOs completed (Solution's `OrderLog.cs` and `Program.cs`) passed 3/3, and
  was deleted.

## Gotchas found (for the Logging page and later exercises)

- **Analyzers:** the samples' `AnalysisLevel=latest-recommended` plus `TreatWarningsAsErrors` makes
  CA1848, CA1873 and CA2254 **build errors** for plain `logger.LogX(...)` calls with interpolation or
  arguments. `[LoggerMessage]` passes.
  - This is worth a Logging-page note: the analyzers push you to source-generated logging.
  - Snippets compiled in a scratch project only get `TreatWarningsAsErrors`, so a page snippet using
    `logger.LogInformation("…{X}", x)` still compiles there.
- **`dotnet format` enforces the `.editorconfig` rule** that **all private fields, consts included,
  are `_camelCase`** (IDE1006). The build doesn't. In tests, use a primary-constructor fixture
  parameter and `internal const`, as `OrderLoggingTests` does.
- **zsh/sed scaffolding slip:** the copied Starter.Tests csproj first pointed at
  `../Solution/Logging.Starter.csproj`. It's fixed. Check `ProjectReference` paths when copying for
  05 and 06.

## Next

- **Finish §2:**
  1. Self-review `git diff 095d60e` plus the untracked `samples/exercises/04-logging/`, against the
     spec "Logging exercise practises structured, safe logging" and design D5.
  2. Run `make gates` until `GATES_EXIT:0` (unsandboxed).
  3. Write the section-2-done note, with its gate lines.
  4. Commit `feat(observability-content): exercise 04, logging (§2)`, with 2.1–2.3 in the body.
- Then §3, task 3.1: `site/src/content/docs/cheatsheets/logging.md`, plus the sidebar, the intro
  cards and `EXPECTED_SLUGS`.
- Still open from §1: Kestrel instruments weren't observed (see `20261109-1654-setup-probe.md`).
  Check again in §7.
- Report to panopticon (`report_progress`, change `observability-content`, plus the section number)
  at every section start and task tick.
