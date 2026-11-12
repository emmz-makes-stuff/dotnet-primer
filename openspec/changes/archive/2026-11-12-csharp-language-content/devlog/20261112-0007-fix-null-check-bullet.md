# Fix: the index's "no null check" bullet now says nullable is compile-time only

<!-- Section: 3 → 4 · Base for §4: 9030bb2 -->

A side agent noticed that the index's "What changed" bullet, "No null check on `lines`", implied that
nullable reference types make the check unnecessary. The C# 3 `Order` falls back to an empty list;
the C# 14 record doesn't, and nullable only warns at compile time.

Asked; Emmz's answer (2026-11-12): **fix it now, in §4.** The bullet now says the check is
compile-time only, that code with nullable off or JSON missing a field can still pass null, that the
C# 14 version then throws in `Describe()`, and that a public API still checks its arguments with
`ArgumentNullException.ThrowIfNull`, with the Null safety page covering both. Measured (modern
harness, `glance-new.cs`):
- a caller under `#nullable disable` passing `null` for `Lines` builds with `0 Error(s)`, and then
  `Describe()` throws `System.NullReferenceException: Object reference not set to an instance of an object.`;
- `JsonSerializer.Deserialize<Order>("""{"CustomerId": "C-42"}""")` gives `Lines is null` → `True`,
  and `Describe()` throws the same.

`SITE_FORMAT_EXIT:0`. It's uncommitted, and lands in §4's first commit.

**The Null safety page must now deliver both points:**
- nullable is compile-time only, with the oblivious-caller and the JSON examples;
- how a public API checks its arguments: `ArgumentNullException.ThrowIfNull`, and on a positional
  record (a property initializer with `?? throw`, or an explicit constructor). Measure the form it
  shows.

## Next

- §4, task 4.1: write `site/src/content/docs/csharp/null-safety.md` per D3, including the two points
  above.
