# Stopped: 6.1 human check of the Testing page, cheatsheet and checker wording

<!-- Section: 6 · Task: 6.1 (unticked) · Base: 68cd639 · Nothing uncommitted -->

## The part I ran myself

This is from a clean clone of `change/testing-content` at `68cd639`, in this session's scratchpad,
unsandboxed.
- **Starter:** `dotnet run --project samples/exercises/09-testing/Checker` printed "no tests yet."
  and each of the four bugs as missed, each with its TODO hint. It took `real 3.40` s.
- **Solution:** the same with `-- Solution.Tests` printed "all your tests pass." and then
  `4 of 4 seeded bugs caught.` It took `real 12.64` s.
  - Bug 1 was caught by `PricingTests.TotalTakesTenPercentOffAtOrAboveTheThreshold(10000, 9000)`.
  - Bug 2 by `CancelReleasesStockTests.CancellingReleasesTheOrdersLines`.
  - Bug 3 by that test, plus two `CancelWindowTests`.
  - Bug 4 by `OrdersApiTests.CancelAfterTheWindowIsAConflict` and `PlacedOrderCanBeReadBack`.

## The question for Emmz

Do these look right? The recipe is in the turn's closing message.
- The Testing page and its cheatsheet, in light and dark.
- Search finds "WebApplicationFactory".
- The copy button works on a C# block.
- The checker's output and hint wording on the Starter.

## Next

- **On PASS:** tick 6.1 and commit it with a short note. Then do the workflow follow-up:
  - self-review the whole change (`git diff 98b6c89..HEAD`);
  - write the change-done note;
  - offer `code-review`.
- **On FAIL:** add `## 7. Review fixes: <theme>` to `tasks.md` with her findings, agree them with
  her, and build them with the section loop.
