# Stopped: §8 human check (task 8.2)

<!-- Section: 8 · Tasks: 8.1 done, 8.2 blocked on Emmz · Base: 4d04ac0 -->

**8.1 is done and ticked, but uncommitted** (it lands in the §8 commit with 8.2):

- `samples/README.md`:
  - the Docker note says 01–06 don't need Docker for their tests (04–06 capture telemetry in
    memory), and that the pages use it for the optional dashboard;
  - the Layout tree lists 04–06;
  - the table has 04–06, linked to the Logging, Tracing and Metrics pages.
- Checked against `samples/exercises/` (all six have Solution, Solution.Tests, Starter, Starter.Tests
  and tests). `make gates`: `SITE_BUILD_EXIT:0`, `SITE_LINT_EXIT:0`, `SITE_FORMAT_EXIT:0`,
  `SITE_TEST_EXIT:0`, `SAMPLES_BUILD_EXIT:0`, `SAMPLES_FORMAT_EXIT:0`, `SAMPLES_TEST_EXIT:0`,
  `VALIDATE_EXIT:0`, `GATES_EXIT:0`.

**Blocked:** 8.2 needs Emmz to look at the site and the dashboard. There are also two open
questions.

## Questions for Emmz

1. **The human check** (recipe below). Do the pages, the cheatsheets, the copy buttons, the
   searches and the dashboard pass?
2. **Keep the bounded `payment.method` in Exercise 06?** It's in `20261109-1731-section-6-done.md`.
   `OrderMetrics.PaymentMethodTag` maps the client's payment method to `card`/`voucher`/`other`. It's
   in Solution and Starter (provided), with a fourth test. The Metrics page uses it as its "values
   that look bounded but aren't" example. The plan didn't ask for it.

## Follow-up agreed with Emmz (not in this change)

While reading the copy, Emmz asked for a **Docker section on Getting started**
(`site/src/content/docs/platform/dotnet-10-and-cli.md`): how to get Docker on Windows with WSL 2
(and the Mac/Linux options). She chose to give it **its own change, proposed after this one is
finished and archived**. Don't add it to this change's `tasks.md`. Offer `/opsx:propose` for it
after `/opsx:archive`.

## Recipe

**A. The site** (from the repo root):

```sh
npm --prefix site run build && npm --prefix site run preview
```

Open the URL it prints (normally `http://localhost:4321`). Check each page in **light and dark**:

- `/observability/logging/`, `/observability/tracing/`, `/observability/metrics/`
- `/cheatsheets/logging/`, `/cheatsheets/tracing/`, `/cheatsheets/metrics/`

On each: the tables (several are wide), the code blocks' **copy buttons**, the cheatsheet tip, and
the Exercise section's GitHub link. Then use the site search for **cardinality** (expect Metrics
and its cheatsheet) and **ActivitySource** (expect Tracing and its cheatsheet). Also check that the
intro page's Cheatsheets cards and the sidebar list all seven.

**B. The dashboard**, with the scratch app `MetricSnip`: the Logging page's setup, plus Exercise 06's
metrics and endpoint. In a terminal:

```sh
docker run --rm -d --name aspire-dashboard \
  -p 18888:18888 -p 4317:18889 -p 4318:18890 \
  mcr.microsoft.com/dotnet/aspire-dashboard:13.6
docker logs aspire-dashboard 2>&1 | grep login
OTEL_METRIC_EXPORT_INTERVAL=5000 ASPNETCORE_URLS=http://localhost:5299 \
  dotnet run --no-launch-profile --project "/private/tmp/claude-501/-Users-rendle-github-emmz-dotnet-primer/03e6f410-461f-443f-8724-8c331e02b8b5/scratchpad/metricsnip/MetricSnip"
```

In a second terminal, place three orders:

```sh
for m in card card voucher; do curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5299/orders \
  -H 'content-type: application/json' \
  -d "{\"customerEmail\":\"a@example.test\",\"paymentMethod\":\"$m\",\"items\":[{\"sku\":\"A\",\"quantity\":1}],\"total\":42.5}"; done
```

That should print `201 201 402`. Open the login link from `docker logs`, then check:

- **Traces:** three `POST /orders` traces. One is a waterfall of the request span.
- **Structured** logs: the framework's request entries, linked to their traces.
- **Metrics**, resource `orders`:
  - `orders.placed`, split by `order.outcome`/`payment.method` (2 accepted card, 1 declined voucher);
  - `orders.value`, with its `{GBP}` unit;
  - `http.server.request.duration`.

Stop it all with Ctrl+C, then `docker stop aspire-dashboard`.

## On PASS

Commit §8 as `feat(observability-content): samples README and human check (§8)`, with 8.1 and 8.2 in
the body, and Emmz's answer recorded in the section-done note. Then follow "Finishing a change":
the whole-change review from `6ba7677` (the first section's base is `6ba7677`, the plan commit), the
change-done note, and the offer of `code-review`.

If she drops the bounded `payment.method`, add a `## 9. Review fixes` section for it. Its scope:
remove `PaymentMethodTag` and its test from 06, and the Metrics page example.

## On FAIL

Record exactly what failed (page, theme, element). Add the fixes to `tasks.md` as a new
`## 9. Review fixes: <theme>` section, agree it with her, and build it with the section loop.
