## OpenSpec workflow

<!-- emmz-setup: 0.1.6 -->

Changes are tracked with OpenSpec. Build them with `/opsx:apply`, following this loop on top of it.
Notes go in the change's `devlog/` folder via `/emmz:devlog`.

### Gates

Run gates through the `Makefile`. A gate has passed only if you saw its `LABEL_EXIT:0`. Quote exit
lines exactly; never paraphrase them.

- `make site-build` runs `npm --prefix site run build` (Astro build).
- `make site-lint` runs `npm --prefix site run lint` (`astro check`).
- `make site-format` runs `npm --prefix site run format:check` (`prettier --check .`).
- `make site-test` runs `npm --prefix site test` (Vitest over `site/dist`, so `site-build` must run
  first).
- `make samples-build` runs `dotnet build samples/Primer.slnx`. This is also the samples lint:
  analyzer warnings are treated as errors.
- `make samples-format` runs `dotnet format samples/Primer.slnx --verify-no-changes`.
- `make samples-test` runs `dotnet test --solution samples/Primer.Solutions.slnx`, then exercise
  09's checker on its solution tests (`dotnet run --project samples/exercises/09-testing/Checker --
  Solution.Tests`), which fails unless they catch every seeded bug.
- `make validate` runs `openspec validate --strict` on every active change.
- `make gates` runs all of the above, in that order.

`make build`, `make test`, `make format` and `make lint` run the matching gate for both stacks.

`make deps` is separate: it checks that every tool and login in `docs/dependencies.md` is present.

### Starting a change

1. Ask which change to build, even if there is only one (`make changes`).
2. Find the resume point: the newest devlog note's `## Next`, and the first unticked task in
   `tasks.md`. In a fresh session, `/emmz:pickup` does this and steps 3–5.
3. Run `make deps` and look for `DEPS_EXIT:0`. If it fails, stop. Offer to install what's missing, and
   hand any login to the user as `! <cmd>`. Never try to authenticate yourself.
4. Check that the working tree is clean (unless resuming a recorded stop), that `make validate` is
   green, and that you're on branch `change/<change-name>`.
5. Read `proposal.md`, `design.md`, and the specs the change touches.

A task that needs a tool, SDK, service or login not in `docs/dependencies.md` is a stop: ask before
adding it. Once it's agreed, add its row to `docs/dependencies.md` and its check to `make deps`, in the
same section commit, and say why in a devlog note.

### Each section (`## N.` in `tasks.md`), in order

1. Note the base commit (`git rev-parse --short HEAD`). For larger sections, put it in a start note.
2. Implement the section's tasks, with tests. Write a devlog note for any non-obvious decision. After
   each task, run the **context check** (below).
3. **Self-review** `git diff <base>` plus untracked files. Review against the spec requirements, not
   just the tasks. Look for:
   - missing requirements and untested behaviour;
   - swallowed errors, and dead or stub code;
   - contracts that drifted from earlier sections;
   - violations of `design.md ## Decisions` and the ADRs in `docs/adrs/`.

   Fix what you find.
4. Run `make gates` until it is all green. A section may land red only as a deliberate, stated call
   (for example, a test that a later section turns green). Record that call in the devlog note and the
   commit body.
5. Tick the section's tasks and write the section-done devlog note. Assume the context will be cleared
   before the next section, so the note must be enough to resume from on its own.
   - Its `## Next` names the next section and that section's first task, or the whole-change review
     after the last section.
   - It records anything this session knows that the repo doesn't: the user's answers, gotchas you
     hit, deferred nits, and decisions in flight.

   Then make **one commit** for the section:
   ```
   feat(<change-name>): <section summary> (§N)

   - N.1 <task summary>
   - N.2 <task summary>
   ```
6. Run the **context check**, then carry on with the next section.

**Context check.** Call the `context_usage` tool. At 40% or under, carry straight on. Over 40%, run
`/emmz:handoff` so that a cold session can resume from the repo alone. Then call the `clear_and_pickup`
tool and end the turn: it runs `/clear`, then `/emmz:pickup`, which picks the work up where the handoff
left it. If this happens mid-section, leave the section's work uncommitted; the handoff note records
it. If either tool is missing, run `/emmz:handoff` and end the turn by asking whether to `/clear` or
restart `claude`.

### Finishing a change

1. Self-review the whole change with `git diff <first-section-base>..HEAD`. Look for duplicated
   abstractions, leftover scaffolding and cross-section drift, and check that the spec is genuinely
   satisfied.
2. Write the change-done devlog note and report to the user. Then **offer** to run the `code-review`
   skill on the change branch before archiving, and run it if they say yes.
3. Fixes from either review don't land as loose commits. Add them to `tasks.md` as one or more new
   sections (`## N+1. Review fixes: <theme>`) and agree them with the user. Build them with the section
   loop above, context checks included. Then return to step 2.
4. Once the review is done or declined and no fix sections are open, **offer** `/opsx:archive`, and
   wait for a yes.

### Stop and ask

Stop when any of these happens:
- a requirement is ambiguous, or contradicts the spec or the design;
- the scope would change;
- a task needs human verification, for example:
  - the site's look and feel, in both light and dark colour schemes;
  - search results and the code-block copy button, in the browser preview;
  - the GitHub Pages and Cloudflare DNS settings, and the live HTTPS site on `dotnet-primer.emmz.dev`;
- the gates won't go green after two honest attempts.

To stop:
1. Leave the work uncommitted and the tasks unticked.
2. Write a `stopped-…` devlog note that says exactly what blocked you, and the question.
3. Send a push notification, if the tool is available.
4. End the turn with the question. For human verification, include a recipe the user can copy and
   paste.

Never decide a product question yourself and flag it afterwards.

Before the user ends a session or clears the context at any other point, run `/emmz:handoff`. Resume
with `/emmz:pickup`.
