# Stopped: the 5.1 human check, again, after §6

<!-- Section: 5 · Task: 5.1 · Base: 9e0e657 -->

§6 (context pooling) is committed as `9e0e657`, with `GATES_EXIT:0`. Task 5.1 is unticked, and the
working tree is clean.

## The question

Do the four 5.1 checks pass now? Look especially at the rewritten "DbContext and its lifetime"
section (`/data/ef-core/#dbcontext-and-its-lifetime`), the new pitfall "Per-request state in a
pooled context", and the cheatsheet's Register block.

## Recipe

As in `20261110-1220-stopped-human-check.md`:

```sh
npm --prefix site run build && npm --prefix site run preview
```

```sh
git clone --branch change/ef-core-content "$PWD" "$TMPDIR/primer-check" && cd "$TMPDIR/primer-check"
dotnet test --project samples/exercises/07-ef-core/Starter.Tests
dotnet test --project samples/exercises/07-ef-core/Solution.Tests
```

The Starter should fail all 7 tests, the new `ContextIsPooled` among them. The Solution should pass
all 7. Cleanup, separately: `rm -rf "$TMPDIR/primer-check"` (delete any earlier clone first).

## On PASS / On FAIL

As in the first stop note: on PASS, tick 5.1, then the whole-change review from `1a84a4d`. On FAIL,
add `## 7. Review fixes: …`.

## Next

Wait for Emmz's result on 5.1.
