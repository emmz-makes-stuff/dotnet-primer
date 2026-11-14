# Stopped: human check (6.1)

Last commit: `81bfd05` (§5). Task 6.1 is unticked; this note is uncommitted.

## Done myself

From a clean clone of `change/coming-from-3-5` at `81bfd05` (scratchpad `clean-clone/`):
- `dotnet test --project samples/exercises/15-session-state/Starter.Tests`: 5 failed, 2 succeeded,
  the failures being `BasketIsKeptInSession`, `BasketIsStoredInSessionAsJson`,
  `BasketFollowsTheShopperToTheOtherServer`, `OrderOutlivesTheSession` and
  `CheckoutEmptiesTheBasket`, each with its TODO and the page path;
- `dotnet test --project samples/exercises/15-session-state/Solution.Tests`: 0 failed, 7 succeeded.

## Waiting on Emmz

A visual check in the browser preview (recipe given in the turn):
1. `/coming-from-3-5/` in light and dark, then at phone width: the three-column tables.
2. `/cheatsheets/coming-from-3-5/`, `/aspnet-core/session-and-state/` and
   `/cheatsheets/session-and-state/`, in light and dark.
3. On the hub, follow a Read link (such as Middleware) to its back-filled section.
4. Search for "Session": the Session and state page is in the results.
5. From Middleware, Next → Session and state, then Next → Logging, and Previous back again.

## On PASS

Tick 6.1, commit this note with the tick, then the whole-change review (Workflow follow-up):
self-review `git diff 4741ed2..HEAD`, the change-done note, and offer `code-review`.

## On FAIL

Add `## 7. Review fixes: <theme>` to `tasks.md` with what Emmz reports, agree it with her, and
build it with the section loop.

## Next

Wait for Emmz's result on the recipe above.
