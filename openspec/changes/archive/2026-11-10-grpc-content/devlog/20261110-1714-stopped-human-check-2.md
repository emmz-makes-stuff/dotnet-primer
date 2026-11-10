# Stopped: 6.2, recheck after the namespace change

<!-- Section: 6 · Task: 6.2 · Head: the §6 commit -->

6.1 is committed. The generated classes are in `GrpcOrders.Protos` (server), `GrpcOrders.Tests.Protos`
(the published contract) and `Shop.Orders.Protos` (cheatsheet), and the page recommends the
convention where it explains `option csharp_namespace`. Emmz's choice is in
`20261110-1711-protos-namespace-decision.md`.

## Checks

- The two contracts still differ only in `csharp_namespace`; `Orders.cs` is identical in both.
- The "from the exercise" snippets match (`check-snippets.mjs`: 4 ok); the scratch snippet project,
  renamed to `Shop.Orders.Protos`, builds with `0 Warning(s)`.
- Starter 0/12 (12 failed), Solution 12/12.
- `make gates`: `GATES_EXIT:0` (all nine lines 0).

## The question

The 5.1 visual check, still open: do the gRPC page and cheatsheet look right in light and dark,
does search find "proto", and does the copy button work on a `.proto` block? The recipe is in
`20261110-1620-stopped-human-check.md`.

- **On PASS:** tick 5.1 and 6.2, then the whole-change review, the change-done note, and offer
  `code-review`.
- **On FAIL:** add `## 7. Review fixes: …`.

## Next

Wait for Emmz's 5.1 / 6.2 result.
