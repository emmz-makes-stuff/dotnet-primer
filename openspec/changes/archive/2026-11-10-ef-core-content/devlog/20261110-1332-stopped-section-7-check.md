# Stopped: §7 built, waiting for the 7.4 check

<!-- Section: 7 · Tasks: 7.1–7.4 · Base: 45b7451 -->

7.1–7.3 are done and committed, and 7.4's `make gates` is green. Its short human check is still
open, so 7.4 is unticked. Emmz's choices are in `20261110-1315-review-fixes-decision.md`.

## What landed

- **7.1, tests:**
  - `OrdersApp.MigrateAsync` asserts `HasPendingModelChanges()` before `MigrateAsync`, with the
    shared `OrdersApp.PendingModelChangesHint`. That hint is also used by
    `MigrationsMatchTheModel`.
  - `ModelIsConfigured` now checks the `CustomerEmail` index, the `OrderLines` table name and
    `Sku`'s length of 32.
  - A non-generic `WithContextAsync(Func<OrdersDbContext, Task>)` overload removes the dummy
    `return true` / `Task.FromResult(true)`.
  - **Checked by breaking the Solution:**
    - with the index, the table name and the Sku length removed, `ModelIsConfigured` shows each hint
      in turn (index first, then table, then Sku);
    - the four database tests show the friendly pending-changes hint, not EF's raw exception;
    - the Solution was restored from a scratchpad backup.
- **7.2, input handling,** pre-written in **both** projects:
  - `PlaceOrderRequest.Validate()` and `OrderItem.Problems()` are in `Orders.cs`, which is still
    identical in Starter and Solution. They handle JSON nulls.
  - `PlaceOrderRequest.MaxMoney` is 99,999,999.99. A price over that is rejected before the sum, so
    it can't overflow.
  - Prices with more than 2 decimal places are **rejected** (`UnitPrice != decimal.Round(UnitPrice, 2)`).
  - `GET /orders/{id}` is `AsNoTracking().Include(o => o.Lines)`, or 404. The Starter's version has
    `[FromServices]`, like its other endpoints.
  - **New tests:**
    - `InvalidOrdersAreRejected`: a theory with 9 raw-JSON cases, expecting 400 and no save;
    - `PlacedOrderCanBeReadBack`: follows `Location`, expects the total of 20.48 and 2 lines, and
      404 for an unknown id.
  - **With the validation turned off** in the Solution, all 9 cases fail (restored afterwards).
  - **Spec:** `sample-exercises` gains the paragraph and the scenarios "Invalid order is rejected"
    and "Placed order can be read back". The proposal's exercise bullet and the page's Exercise 07
    Goal ("already written, so you don't need to change them") are updated.
- **7.3, wording:**
  - "EF Core … maps it to `decimal(18,2)`", on the page and in the Solution comment. The page's
    `OrderConfiguration` still matches the Solution exactly.
  - The cheatsheet's Save block uses `newOrder`, `placed` and `old`.
  - The Testing pointers now say those pages "will cover" fixtures and Testcontainers. The Testing
    page is still "Coming soon".
  - The snippets compile: `0 Warning(s)`, `0 Error(s)`.

## Results

- **Solution:** 17/17 (7 facts, plus 9 theory cases, plus 1 fact). **Starter:** 17/17 fail before
  TODO 1, each with the "isn't registered" hint first.

```
SITE_BUILD_EXIT:0
SITE_LINT_EXIT:0
SITE_FORMAT_EXIT:0
SITE_TEST_EXIT:0
SAMPLES_BUILD_EXIT:0
SAMPLES_FORMAT_EXIT:0
SAMPLES_TEST_EXIT:0
VALIDATE_EXIT:0
GATES_EXIT:0
```

## The question (7.4)

From a fresh clean clone, does exercise 07 still behave? The Starter should be red (17 failing,
with guiding hints), and the Solution green (17 passing).

```sh
git clone --branch change/ef-core-content "$PWD" "$TMPDIR/primer-check" && cd "$TMPDIR/primer-check"
dotnet test --project samples/exercises/07-ef-core/Starter.Tests
dotnet test --project samples/exercises/07-ef-core/Solution.Tests
```

Delete any earlier clone first. Cleanup, separately: `rm -rf "$TMPDIR/primer-check"`.

- **On PASS:** tick 7.4. Rerun the whole-change review for §7, update the change-done note, then
  the follow-up (merge to `main`, push, check the live page, offer `/opsx:archive`). Ask before
  running `code-review` again.
- **On FAIL:** add `## 8. Review fixes: …`.

## Next

Wait for Emmz's 7.4 result.
