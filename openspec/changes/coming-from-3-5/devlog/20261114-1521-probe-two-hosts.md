# Probe: session across two in-process hosts (1.1)

Base commit for §1: `4741ed2`.

Scratch tree:
`/private/tmp/claude-501/-Users-rendle-github-emmz-dotnet-primer/5ab1b23a-e532-455e-917b-fb971dc0855b/scratchpad/probe15`.
It has the repo's `global.json`, `.editorconfig`, `Directory.Build.props` and
`Directory.Packages.props`, a minimal session app, and xUnit v3 tests. The tests run two
`WebApplicationFactory<Program>` hosts that share one recording `MemoryDistributedCache`, with
`HandleCookies = false`, and copy the cookie by hand. The run was unsandboxed, so `~/.aspnet` was
writable (as on CI). SDK 10.0.404, macOS 26.6.2, arm64.

## Results

| Hosts | Data Protection | B reads A's basket |
| --- | --- | --- |
| distinct content roots | default | `[]`: not shared |
| **same** content root | default | `[{"sku":"SKU-1","quantity":2}]`: shared |
| distinct content roots | shared keys dir + `SetApplicationName("probe-shop")` | `[{"sku":"SKU-1","quantity":2}]`: shared |

- Both hosts report `applicationName: "App"` from `IHostEnvironment`. Data Protection's default
  isolation comes from the **content root path**, not from `ApplicationName`. With the same root,
  both hosts use the user-profile key ring (`~/.aspnet/DataProtection-Keys`, which already has keys
  on this Mac), so they read each other's cookie even without TODO 3.
- **So D7's distinct content roots are needed, and they work.** Without them, the cross-instance
  test would pass against a Starter that never configures Data Protection.
- When B can't unprotect the cookie, it says nothing: it starts a new, empty session. That's a good
  Pitfall for the page ("the basket vanishes on the other replica, with no error").
- **JSON in session:** the recording cache's write that contains `SKU-1` is session's own format,
  then the key and the value's bytes as they are:
  `\x02\0\0\x01…\0\x06basket\0\0\0\x1e[{"Sku":"SKU-1","Quantity":2}]`. So a test can find the
  basket's JSON inside a cache write (D7).
- The session cookie is `.AspNetCore.Session=CfDJ8…` (Data Protection's prefix).

## Lost write (for 1.2, already run)

After one item (SKU-1) sets up the session, two concurrent `POST /basket/items?delay=200` (SKU-2 and
SKU-3, each loading, waiting 200 ms, then writing) responded:

```
[{"sku":"SKU-1","quantity":1},{"sku":"SKU-2","quantity":1}]
[{"sku":"SKU-1","quantity":1},{"sku":"SKU-3","quantity":1}]
```

and the final basket was `[{"sku":"SKU-1","quantity":1},{"sku":"SKU-2","quantity":1}]`: SKU-3 was
lost. The 200 ms delay between load and write is what makes it repeatable.

## Next

1.2: compile the Redis example against `Microsoft.Extensions.Caching.StackExchangeRedis` in the
scratch tree, then fetch Learn's session, distributed caching and Data Protection pages and list
the URLs the page will cite.
