# Stopped: 6.1 needs Emmz's visual check

<!-- Section: 6 · Task: 6.1 (unticked) · Base: 1dc8d60 -->

The stamp runs ahead of the clock, so it sorts after `1445-section-5-done`. §1–§5 are committed;
§5 (`1dc8d60`) isn't pushed yet. Nothing is uncommitted apart from this note.

## Done myself: exercise 11 from a clean clone

A fresh `git clone --branch change/async-performance-content` at `1dc8d60`, with SDK 10.0.404:

- **Starter:** `dotnet test --project samples/exercises/11-async/Starter.Tests` gives 5 total,
  4 failed and 1 passed (`PlacedOrderCanBeFetched`). Each failure names its TODO:
  - `Nothing happened within 5 s: the caller cancelled POST /orders, but the warehouse's
    ReserveAsync never saw it, and kept working. Take the endpoint's CancellationToken, and pass it
    to ReserveAsync (TODO 2).`
  - `POST /orders calls TaskAwaiter.GetResult: it holds a thread while the warehouse works. Await
    it instead (TODO 1).`
  - `the warehouse notification for order 1 failed, and nothing logged it: the failure was lost.
    Hand the order to OrderNotifications, … (TODO 3).`
  - `GET /orders/1/quote failed: the quote's calls ran one after another, … await Task.WhenAll
    (TODO 4).`
- **Solution:** 5/5.
- No stray processes afterwards.

## The question

Do the Async & performance page and its cheatsheet look right in both light and dark schemes,
including the wide benchmark tables? Does search find "ValueTask", and does the copy button work on
a C# block?

## Recipe

The site preview, from the repo root:

```sh
npm --prefix site run build && npm --prefix site run preview
```

Open <http://localhost:4321/performance/async-and-performance/> and
<http://localhost:4321/cheatsheets/async-and-performance/>.
- Toggle the theme (top right).
- Search for `ValueTask`.
- Press the copy button on the `From exercise 11: Program.cs` block under "Cancellation", then
  paste it somewhere.

The widest things to look at:
- the starvation table (6 columns) under "Thread-pool starvation";
- the `Buffers` table (7 columns) under "Allocations and pooling";
- the Coming from .NET 3.5 table;
- the cheatsheet's "Blocking calls and their replacements" table;
- the `dotnet-trace` output block, whose lines are long.

- **On PASS:** tick 6.1, push, then the whole-change review, the change-done note, and offer
  `code-review`.
- **On FAIL:** add `## 7. Review fixes: …` to `tasks.md` with what she found, and agree it with her.

## Next

Waiting on Emmz's answer to the question above.
