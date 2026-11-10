# Decision: §7, fixes from the code review

<!-- Section: 7 · Before 7.1 · Base: 45b7451 -->

The `code-review` skill (high, on `change/ef-core-content`) returned 10 findings. I checked them
before bringing them to Emmz.

## Emmz's answers (2026-11-10)

- **"Fix all six"** of the real, cheap findings:
  1. the unreachable pending-changes hint;
  2. the `ModelIsConfigured` gaps (index, table name, Sku length);
  3. the `decimal(18,2)` wording;
  4. the cheatsheet's self-cancelling Save block;
  5. the Testing-page pointers;
  6. the `WithContextAsync` overload.
- **Input handling: "All three changes"**: validation, 2-place money, and `GET /orders/{id}`. Her
  first answer ticked "None" as well. I asked again, and she confirmed all three.
- **§7 as drafted: "Build it as written".** That means:
  - the validation and the GET endpoint are pre-written in both Starter and Solution, not new
    TODOs;
  - a price with more than 2 decimal places is rejected with 400, not rounded.

## Rejected finding

The reviewer said Docker Desktop's Rosetta setting is on by default on recent versions. Docker's
settings reference (docs.docker.com/desktop/settings-and-maintenance/settings/, fetched
2026-11-10) lists "Use Rosetta for x86_64/amd64 emulation on Apple Silicon" as **Disabled**, so the
page stays as it is.

## Next

Task 7.1.
