# §9 done; the review fixes are complete

<!-- Section 9 · Base: 26d01fd · Review fixes §7–§9: d07c8df..this note's commit -->

- 9.1 The Starter `Protos/orders.proto` header now describes the Starter's own copy: it doesn't
  have `TrackOrder` yet (TODO 4), and once it's added, it must differ from the published contract
  only in `csharp_namespace`. The Solution's header is unchanged.
- 9.2 The `Hints.For` comment in `tests/OrdersApp.cs` no longer cites "the 1.1 probe". It keeps
  the three cases it explains. A grep of the exercise's sources found no other workflow
  references (`probe`, `§N`, `tasks.md`).

Gates: `GATES_EXIT:0`. Every task in `tasks.md` is ticked.

## Review of the fixes (`git diff d07c8df..HEAD`)

The changes are small and stay inside §7–§9's scope. Both copies of `Orders.cs` and of
`appsettings.json` are still identical. The page, the cheatsheet and the exercise now agree on
port 5287 and on how `GetOrder` maps. No docs state the validation limits, so none needed them
added.

## State

- `change/grpc-content` is pushed only up to §2's CI note. §3–§9 are local only.

## Next

1. Report to Emmz that the review fixes are done. Don't re-run code-review unless she asks.
2. With her go-ahead: merge to `main` and push (this deploys the site), watch CI, and check the
   live gRPC page and cheatsheet on dotnet-primer.emmz.dev.
3. Offer `/opsx:archive`.
