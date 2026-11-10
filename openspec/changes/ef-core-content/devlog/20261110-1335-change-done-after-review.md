# Change done, after the code review

<!-- Whole change · Base: 1a84a4d · Head: b910bcd + this note -->

This adds to `20261110-1300-change-done.md`. §7 (the code-review fixes and the exercise's input
handling) has landed as `b910bcd`, and every task in `tasks.md` is ticked.

## 7.4

Emmz ran both test projects from a clean clone: "first lot failed, second lot passed". The Starter
was red and the Solution green, which matches my own run (Starter 0/17, Solution 17/17).

She also said: "there's no technical reason you can't run these tests yourself". From now on I run
clean-clone test checks myself, and keep human checks to what needs her eyes or her accounts. That's
saved as the local memory `run-clean-clone-tests-myself`.

## Whole-change review, the §7 part

§7's own diff was reviewed before its commit (see the stop note). Against the rest of the change:
- `Orders.cs` is still identical in Starter and Solution;
- the page's `OrderConfiguration` and registration snippets still match the Solution;
- the new spec scenarios each have a test (`InvalidOrdersAreRejected`, `PlacedOrderCanBeReadBack`);
- the page's Exercise 07 Goal mentions the pre-written parts.

Nothing in the main specs is affected.

## Still not verified

**CI hasn't run since §2.** `change/ef-core-content` has never been pushed.

## Next

The workflow follow-up, which needs Emmz's go-ahead:
1. merge `change/ef-core-content` to `main`, and push (this deploys the site, and is the first CI run
   of the whole change);
2. watch CI and the Pages deploy, then check the live EF Core page and cheatsheet;
3. offer `/opsx:archive`.
