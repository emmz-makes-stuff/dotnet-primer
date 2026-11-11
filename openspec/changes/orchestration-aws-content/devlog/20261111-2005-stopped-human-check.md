# Stopped: 5.1 human check of the Orchestration & AWS page and cheatsheet

<!-- Section: 5 · Task: 5.1 · Base: 3303596 -->

5.1 is unticked, and nothing is uncommitted except this note.

## Done for 5.1 already (not a human check: see the memory note on clean-clone tests)

From a clean clone (`git clone --branch change/orchestration-aws-content`) at `3303596`:
- `Starter.Tests`: total 5, failed 4, succeeded 1. Each failure names what's wrong and its TODO
  (TODO 1: liveness 503 with the store offline; TODO 2: readiness 404; TODO 2 then 3: no readiness
  to fail during shutdown; TODO 4: the order cut off at shutdown).
- `Solution.Tests`: total 5, failed 0, succeeded 5.

## Question for Emmz

Do the page and the cheatsheet look right, in both light and dark, and do search and the copy
button work?

## Recipe

The site preview, from the repo root. **Port 4321 is held by another Node process on this Mac**, so
it's pinned to 4380:

```sh
npm --prefix site run build && npm --prefix site run preview -- --port 4380
```

Open <http://localhost:4380/deploy/orchestration-and-aws/> and
<http://localhost:4380/cheatsheets/orchestration-and-aws/>.
- Toggle the theme (top right).
- Search for `readiness`.
- Press the copy button on the `task-definition.json` block under "Amazon ECS on Fargate", then
  paste it somewhere.

The widest things to look at:
- the "ECS, EKS or Lambda?" table (both pages);
- the EKS table, whose right column is long;
- the timeouts table under "When the timeouts are wrong";
- the `aspire describe` box-drawing output under "Aspire on your machine";
- the `docker stop` client output, and the Coming from .NET 3.5 table.

- **On PASS:** tick 5.1. Then the workflow follow-up: the whole-change review (`git diff
  384453b..HEAD`, from §1's base), the main-specs check, the change-done note, and offer
  `code-review`.
- **On FAIL:** add `## 6. Review fixes: …` to `tasks.md` with what she found, and agree it with
  her.

## Next

- Waiting on Emmz's answer to the human check above.
