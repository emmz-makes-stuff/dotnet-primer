# Answer: all seven code-review findings become §7

<!-- Section: 7 · Base: afc582a -->

`code-review` (medium) on `change/async-performance-content` reported 7 findings. I checked each
against the code, and all 7 hold up. **Emmz chose all of them.** They're in §7 as 7.1–7.6, with 4
and 5 folded into 7.4:

1. The worker drops queued notifications at shutdown, silently → 7.1 (drain, log what's left, and a
   page note about durable queues).
2. The IL check misses `Task.WaitAll`/`WaitAny` → 7.2.
3. The quote barrier blocks three pool threads, so the test depends on free pool threads → 7.3.
4. "BenchmarkDotNet builds in Release" implies a Debug host is fine → 7.4.
5. "The true mean is within Mean ± Error" overstates a 99.9% CI → 7.4.
6. The `Concatenate` baseline adds a `","` + sku temporary each round → 7.5 (re-run, and new
   numbers on the page).
7. `make deps` doesn't enforce the 10.x that `docs/dependencies.md` states → 7.6. The tools stay
   required (Emmz's 1.1 call). Only the version check tightens.

## Next

§7, task 7.1, with the section loop: tests, self-review, gates, a section-done note, and one
commit. Then back to the finishing steps: the change-done note, then the merge with Emmz's go-ahead.
