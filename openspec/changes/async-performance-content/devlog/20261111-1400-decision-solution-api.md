# Decision: the Solution's API details D2 left open (2.2)

<!-- Section: 2 · Tasks: 2.1–2.2 done, uncommitted · Base: 943b2e8 -->

The stamp runs ahead of the clock (13:34), so it sorts after `1355-section-1-done`.

Also this session: on Emmz's word ("use https://panopticon.team"), reports go through the panopticon
**plugin's** server (`mcp__plugin_panopticon_panopticon__report_progress`). The localhost dev server
entry is gone from the gitignored `.mcp.json`, at her yes.

- **`IWarehouse` has a fifth method, `NotifyAsync(Order, CancellationToken)`.** D2 lists four, but
  its bug 3 (`_ = NotifyAsync(order)`) and D3's throwing fake both need a warehouse notification.
- **`NotificationWorker` passes `stoppingToken` to `NotifyAsync`, not the request's token.** The
  notification runs after the answer, so a request's token would cancel it every time.
- **Shapes:** `POST /orders` gives 201 with `Location: /orders/{id}` (like 10); `GET /orders/{id}`
  is there for the Location; the `Quote` is `(OrderId, InStock, PriceCents, ShippingCents)`; the
  namespace is `AsyncOrders` (CA1050). `SlowWarehouse` waits 500 ms per call.
- **No CA1031 suppression on the worker's `catch (Exception)`.** The build with no pragma passes
  (`latest-recommended` doesn't turn CA1031 on), so a comment says why instead.
- Run on :5311: POST 201 in 513 ms; quote 200 in 505 ms (concurrent); invalid 400 with
  `lines[0].sku`; missing quote 404; a POST aborted at 100 ms stored no order (`/orders/2` → 404),
  so the token reached `ReserveAsync`. The app log had no errors.

## Next

- **2.3:** the tests in `samples/exercises/11-async/tests/` (design D3, probe note `1330`): the test
  app with a controllable fake warehouse (`IDisposable`, CA1001, if it owns a `Barrier`), the IL
  reader, and one test per bug, each naming its TODO. Then three passing runs against the Solution.
- 2.1–2.2 are ticked but not committed. §2 commits once, after 2.5.
