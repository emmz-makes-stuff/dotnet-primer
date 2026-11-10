# Stopped: 5.1 human check

<!-- Section: 5 · Task: 5.1 · Base: 66b2268 -->

§1–§4 are committed (`66b2268` is the head), and `make gates` gave `GATES_EXIT:0` after §4. 5.1 is
unticked until Emmz has checked the visual parts.

## Already done (by me, from a clean clone)

`git clone --branch change/grpc-content` of the local repo into the scratchpad, at `66b2268`:
- **Starter.Tests:** 12 failed, 0 succeeded, every one with "The orders service has no endpoint.
  In Program.cs, register gRPC with builder.Services.AddGrpc(), and map the service with
  app.MapGrpcService<OrdersService>() (TODO 1)."
- **Solution.Tests:** 12 succeeded, 0 failed.

## The question

Do the gRPC page and its cheatsheet look right in light and dark? Does search find "proto"? Does
the copy button work on a `.proto` block?

## Recipe

The site preview, from the repo root:

```sh
npm --prefix site run build && npm --prefix site run preview
```

Open <http://localhost:4321/services/grpc/> and <http://localhost:4321/cheatsheets/grpc/>. Toggle
the theme (top right), search for `proto`, and press the copy button on the `Protos/orders.proto`
block, then paste it somewhere. The two tables (status codes, and Coming from WCF) are the widest
things on the page.

- **On PASS:** tick 5.1, then the whole-change review, the change-done note, and offer `code-review`.
- **On FAIL:** add `## 6. Review fixes: …` to `tasks.md` with what she found.

## Next

Wait for Emmz's 5.1 result.
