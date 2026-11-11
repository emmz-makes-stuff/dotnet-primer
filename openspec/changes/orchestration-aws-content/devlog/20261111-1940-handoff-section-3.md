# Handoff: §3 part-built. 3.1 and 3.2 written and ticked; 3.3 (verify) next

<!-- Section: 3 · Tasks: 3.1–3.2 done, 3.3–3.4 open · Base: fb740b4 -->

Handed off at 40% context, mid-section. Nothing in §3 is committed (one commit per section). The
2.5 tick is in the uncommitted `tasks.md` too, and lands in §3's commit.

## Uncommitted files and their state

- `site/src/content/docs/cheatsheets/orchestration-and-aws.md`: new, finished, and formatted with
  prettier.
- `site/src/content/docs/deploy/orchestration-and-aws.md`: rewritten, with the placeholder gone,
  and formatted. It covers health checks, graceful shutdown (with drain delay, the `docker stop` run
  and a timeouts table), Aspire, ECS, EKS, Lambda, the "which one?" table, configuration and
  credentials, Coming from .NET 3.5, Pitfalls, and Exercise 13.
- `site/src/content/docs/deploy/containers.md`: the app health-checks link now goes to
  `/deploy/orchestration-and-aws/#health-checks`.
- `site/astro.config.mjs` (sidebar), `site/src/content/docs/index.mdx` (cheatsheet card) and
  `site/tests/dist.test.ts` (`EXPECTED_SLUGS` only. `FULL_CONTENT_SLUGS` and `CHEATSHEETS` are
  3.4's job, not done yet).
- `tasks.md`: 2.5, 3.1 and 3.2 ticked.
- Gates after these edits: `SITE_BUILD_EXIT:0`, `SITE_LINT_EXIT:0`, `SITE_FORMAT_EXIT:0`.
  `site-test` wasn't run; it should go green once 3.4 is done.

## Real runs this session, already on the page (for 3.3's "numbers come from real output")

- **The `docker stop` example,** run exactly as the page shows, on 2026-11-11:
  - built with `dotnet publish -t:PublishContainer -p:ContainerRepository=health-orders
    -p:ContainerFamily=noble-chiseled -p:InvariantGlobalization=true` from `Solution/`, on base
    `aspnet:10.0-noble-chiseled`;
  - started with `docker run -d --name ex13 -p 8480:8080 health-orders`. **8480, because 8080 is
    held by another app's Node server on `[::1]`, and 5080 by another app.** The page says why;
  - the output block on the page is verbatim, and `docker inspect ex13 --format
    '{{.State.ExitCode}}'` gave `0`;
  - the log block is from the same run (`0.0298ms`).
- **The store offline:** `-e Store__Online=false` gave live `200 Healthy`, ready `503 Unhealthy`,
  and `POST /orders` `503` with the problem JSON. The log line came from that run (`0.3716ms`), on
  host port 18082. The page doesn't name a port there.
- **`Degraded` answers `200 Degraded`:** measured with a degraded-only check on the scratch probe
  app.
- **Kubernetes** (fetched https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle/): "At
  the same time as the kubelet is starting graceful shutdown of the Pod, the control plane evaluates
  whether to remove that shutting-down Pod from EndpointSlice objects", and "The default
  terminationGracePeriodSeconds setting is 30 seconds."
- Everything else on the page is from `1906` (shutdown probes), `1907` (Aspire) and `1908` (AWS).

## 3.3: what to verify

1. **Compile every C# block on both pages**, net10.0, warnings as errors, in a scratch project.
   This session's scratchpad is gone, so make a new one: copy `global.json`, `.editorconfig` and
   `samples/Directory.Build.props`. Use the packages
   `Amazon.Lambda.AspNetCoreServer.Hosting` 2.2.1 and
   `Amazon.Extensions.Configuration.SystemsManager` 7.1.1, plus the `System.Security.Cryptography.Pkcs`
   10.0.14 pin if EF Core comes in. The Lambda and the configuration snippets go in separate
   projects. The cheatsheet's `DrainDelay` is a short form without a logger: compile it too. The
   Shopfront snippet needs ServiceDefaults (`AddServiceDefaults`): copy it from a fresh
   `aspire new aspire-starter`, or compile it against a stub.
2. **"(from the exercise)" blocks must be contiguous, verbatim excerpts of `Solution/Program.cs`
   and `Solution/Health.cs`.** The page has six: two health-check excerpts from `Program.cs`
   (lines 10–14 and 26–31), the `ShutdownTimeout` excerpt (lines 19–22), and three `Health.cs`
   classes. Check them with a script, not by eye.
3. **AWS commands:** parse-check `aws ecs register-task-definition`, `aws elbv2
   create-target-group`, `aws ecs create-service` (with `--health-check-grace-period-seconds`),
   `aws ecs update-service --cluster --service --task-definition` and
   `aws eks create-pod-identity-association`, on both pages:
   - run with `AWS_CONFIG_FILE=/dev/null AWS_SHARED_CREDENTIALS_FILE=/dev/null
     AWS_EC2_METADATA_DISABLED=true AWS_REGION=eu-west-2`, and no `AWS_PROFILE` or `AWS_*` keys;
   - expect `Unable to locate credentials`, which means the arguments parsed, and not
     `Error parsing parameter`;
   - `file://task-definition.json` needs the file: write the page's JSON to the scratch folder;
   - **if any check would reach AWS, stop.** Lambda's `dotnet lambda` commands aren't checked, and
     the page says so.
4. **Claims still unverified; fetch and confirm, or soften:**
   - the ECS container health check: a service replaces a task that fails it. Check
     https://docs.aws.amazon.com/AmazonECS/latest/developerguide/healthcheck.html. The Pitfall
     "marked unhealthy and replaced, over and over" depends on it;
   - the AWS Load Balancer Controller sets the target group's health check by annotations
     (`alb.ingress.kubernetes.io/healthcheck-path`). Check its Ingress annotations page; the page
     doesn't link it yet, so consider linking it;
   - the Parameter Store user guide URL on the page
     (`…/systems-manager/latest/userguide/systems-manager-parameter-store.html`) wasn't fetched.
5. **Commands that don't need AWS:**
   - `dotnet test --project samples/exercises/13-health-and-shutdown/Starter.Tests` from the repo
     root: it should run and fail 4 of 5;
   - `dotnet run -- --Store:Online=false` from `Starter/`, the exercise's "Try it yourself";
   - the Aspire commands are from `1907`'s run, but `aspire new` there had extra flags
     (`--localhost-tld false --non-interactive --suppress-agent-init --nologo`). The page shows the
     short form; say so, or rerun the short form;
   - the Aspire install commands are from `1842`.
6. Record everything in the section-done note.

## 3.4 and the commit

- Add `deploy/orchestration-and-aws` and `cheatsheets/orchestration-and-aws` to
  `FULL_CONTENT_SLUGS`, and `"deploy/orchestration-and-aws": "cheatsheets/orchestration-and-aws"` to
  `CHEATSHEETS`, in `site/tests/dist.test.ts`.
- Fetch every URL on both pages with a JS `fetch` in `ctx_execute`. Curl is blocked by
  context-mode. github.com and aws.amazon.com may need a browser user agent.
- `make gates` → `GATES_EXIT:0`.
- Then the section-done note, and **one commit for §3**, whose body also mentions 2.5's tick.

## Gotchas from this session

- The Aspire probe found that a port that's taken fails silently. Several ports on this Mac are
  held by other apps: 5180, 5181, 8080 and 5080. **Check with `lsof` before picking one, and never
  kill anything by port.**
- `perl -0pi -e 's#…#…#'` breaks on a `#` in an anchor. Use the Edit tool, or another delimiter.
- The repo's `panopticon` MCP server (localhost:7480) has no login. The **plugin's**
  `mcp__plugin_panopticon_panopticon__report_progress` works. Report §3 with it at the section
  commit.
- The `health-orders` and `hs-probe` images are still in local Docker. They're harmless, and can
  go with `docker image rm health-orders hs-probe` at the end.

## Next

- **3.3:** verify the page and the cheatsheet, in the order of the list above. Then 3.4, `make
  gates`, the section-done note, and §3's commit.
- After §3: §4, task 4.1 (`samples/README.md`).
