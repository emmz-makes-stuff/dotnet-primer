# Stopped: Verify requires a licence

<!-- Section 1 · Base: 98b6c89 · task 1.1, uncommitted (the probe is in the scratchpad only) -->

## What the 1.1 probe found

The probe used scratch projects on SDK 10.0.4xx, with the repo's `global.json` and
`Directory.Build.props`.

**Seeded-bug switch (D4): works.**
- `SeededBugs.targets` uses `Compile Remove` for the App file and
  `Compile Include=…/bugs/1/Pricing.cs Link=…` for the bug's copy.
- `dotnet test --project Tests -p:SeededBug=1 --artifacts-path <dir>` compiles the bug and fails
  the boundary test, with exit code 2.
- The run builds into `<dir>/{obj,bin}/{App,Tests}/debug`. Separate runs don't share `obj/`, and
  the default build afterwards still passes (exit 0).

**Exit codes (D5), from Microsoft.Testing.Platform:**

| Exit code | Meaning |
|---|---|
| 0 | every test passed |
| 2 | a test failed |
| 8 | zero tests ran ("Zero tests ran", "Exit code: 8") |
| 1 | the build failed |

**Report file (D5): CTRF.**
- xUnit v3 writes several report formats with no extra package: `--report-xunit-ctrf`, `-trx`,
  `-junit`, `-nunit`, `-xml`, `-html` and `-markdown`, each with a `…-filename` option, plus
  `--results-directory`.
- The checker should use **CTRF**, which is JSON. It has `.results.summary` (tests, passed and
  failed counts) and `.results.tests[]`, each with a `name` (for example
  `Tests.PricingTests.DiscountAtThreshold`) and a `status` (`passed` or `failed`). That's easy to
  read with `System.Text.Json`.

**Filtering (D1):** these work through `dotnet test`:
- `--filter-class Tests.OtherTests`;
- `--filter-method Tests.PricingTests.NoDiscountBelow`;
- wildcards (`--filter-method "*NoDiscount*"`);
- `--filter-namespace`, `--filter-trait`, `--filter-query`, and VSTest's `--filter`.

**Coverage (D1):** with `Microsoft.Testing.Extensions.CodeCoverage` 18.12.0, this command exits 0:

```
dotnet test --project Tests --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
```

It writes `TestResults/coverage.cobertura.xml`. There are also `--coverage-settings` and other
output formats.

**Timing (D6):** five sequential cold runs (the default plus four bug runs), each with its own
artifacts path, took **10 s** on Emmz's Mac. That's far below the 60 s I'd budgeted.

**Versions** (NuGet, 2026-11-10, newest stable):

| Package | Version |
|---|---|
| `NSubstitute` | 6.2.0 |
| `NSubstitute.Analyzers.CSharp` | 1.0.17 |
| `Microsoft.Extensions.TimeProvider.Testing` | 10.11.0 (same line as `Diagnostics.Testing`) |
| `Verify.XunitV3` | 33.3.4 |
| `Microsoft.Testing.Extensions.CodeCoverage` | 18.12.0 |

## The blocker: Verify

**Verify 33.3.4 won't build without a licence property.** Its `buildTransitive/Verify.targets` runs
a SponsorCheck, which fails the build with `error SC021: No license specified. Package 'Verify'
requires a SponsorCheck license property.` Verify has moved to a maintenance-fee model. These are
the options it accepts:

- `Verify_GitHubSponsorAccount`: the account is a GitHub sponsor (for private sponsorship, also
  `Verify_SponsorshipPrivateUntil`).
- `Verify_SponsorshipLicensedUntil`: a time-bounded licence.
- `Verify_SponsorshipExemption`:
  - `OpenSource`: an open-source project with no revenue;
  - `SmallRevenue`: under US$10,000 a year in gross revenue.

  Both need `SponsorshipExemptionUntil`, at most 12 months out.
- `Verify_SponsorshipLicenseIgnored=true`, which the message calls "in breach of the package
  license".

Emmz picked snapshot testing with Verify as an extra. But the engineers' company would need to pay
to use it at work. The primer itself could claim the OpenSource exemption for the page check, but
the page would then teach a tool the team can't use without a licence. Which way to go is Emmz's
call (CLAUDE.md "Stop and ask": a product question).

## Question

How should the Testing page handle snapshot testing, now that Verify needs a paid licence for
commercial use?

1. **Keep Verify, and say so.** Teach it, with a note saying it needs a sponsorship or licence for
   commercial use, and how the property works. The page check uses the OpenSource exemption.
2. **Snapshot testing without a library.** Serialise with `System.Text.Json`, compare against an
   approved file committed beside the test, and print a diff on mismatch. Mention Verify and its
   licence in a sentence.
3. **Drop snapshot testing.** Remove it from the proposal, the spec ("Testing page covers snapshot
   testing") and design D1/D9/D10, and keep code coverage as the only extra.

## Next

- **On 1:** update design D1, D9 and D10 (licence note, exemption property in the scratch check).
  Then tick 1.1 and do 1.2.
- **On 2:** revise the snapshot requirement's wording (no library), plus design D1 and D9. Then tick
  1.1 and do 1.2.
- **On 3:** remove the requirement and the D1/D9/D10 parts, and update the proposal. Then tick 1.1
  and do 1.2.

In every case, the rest of the probe matches D4–D6. Fold the confirmed facts above into the design
(CTRF for test names; the exit codes; 10 s).
