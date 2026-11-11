# 1.1: `dotnet-counters` and `dotnet-trace` added

<!-- Section: 1 · Task: 1.1 · Base: 1f98ddf -->

- **Emmz said yes** (2026-11-11, at the start of the build) to adding both as .NET global tools.
- **Why:** the page's thread-pool starvation section (design D5) shows their real output, against
  a blocking and an async endpoint under the same load. The probe (1.3) needs them, and so does
  anyone who checks the page. Exercise 11 doesn't need them.
- **Installed** with `dotnet tool install -g` (unsandboxed, for NuGet): both are 10.0.750501, in
  `~/.dotnet/tools`, which is on the PATH.
- **`docs/dependencies.md`:** a row for each, after `dotnet`.
- **`make deps`:** a `--version` check for each, with the install command in the message. After
  the change, `DEPS_EXIT:0`.

## Next

1.2: the exercise test probe (design D3). The cancellation path through `TestServer`, the barrier
test for `/quote`, the fake logger catching the background worker's error, and the IL check for
blocking against the four handlers.
