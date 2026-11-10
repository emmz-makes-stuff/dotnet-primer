# Decision: generated classes in a `.Protos` namespace

<!-- Section: 6 · During the 5.1 stop · Head: 66b2268 -->

Emmz, 2026-11-10, after the 5.1 stop: "change `option csharp_namespace = "GrpcOrders.Contract"` to
`option csharp_namespace = "GrpcOrders.Protos"`, i prefer that as a convention for the generated
classes".

Asked how far it goes, she chose **everywhere**:
- `GrpcOrders.Contract` → `GrpcOrders.Protos` (the Solution's and Starter's contracts);
- `GrpcOrders.Tests.Contract` → `GrpcOrders.Tests.Protos` (the published contract the tests build
  their client from; it must still differ from the server's);
- the cheatsheet's `Shop.Orders` → `Shop.Orders.Protos`;
- the page recommends the convention where it explains `option csharp_namespace`.

The visual parts of 5.1 weren't reported on, so 5.1 stays open; §6's last task re-asks them.

## Next

6.1: the rename (see `tasks.md` §6).
