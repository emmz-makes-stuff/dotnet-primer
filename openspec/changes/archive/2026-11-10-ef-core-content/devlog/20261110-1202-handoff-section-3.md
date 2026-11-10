# Handoff: §3 is mid-section (3.1 and 3.2 done, uncommitted; 3.3 and 3.4 left)

<!-- Section: 3 · Tasks: 3.1–3.4 · Base: be7c211 -->

Context passed 40% after 3.2, so this hands off before 3.3. **§3 is uncommitted on purpose.** The
section loop commits it once, after 3.4.

## Done in §3 so far

- **The EF6 decision** is in `20261110-1156-ef6-mapping.md`. The spec, design D1, the proposal and
  `site/README.md` are updated, and `openspec validate --strict` passes.
- **3.1:** `site/src/content/docs/cheatsheets/ef-core.md`, wired into the sidebar
  (`astro.config.mjs`), the introduction's cards (`index.mdx`) and `EXPECTED_SLUGS`.
- **3.2:** `site/src/content/docs/data/ef-core.md`, written in the D1 order (EF6 table after Change
  tracking, before Pitfalls), with the Exercise 07 section and its GitHub tree link.
  `SITE_BUILD_EXIT:0`, `SITE_LINT_EXIT:0` and `SITE_FORMAT_EXIT:0`.
- **Not done yet:** `FULL_CONTENT_SLUGS` and `CHEATSHEETS` (task 3.4). So `make gates` hasn't run
  since the page was written, and the `07-ef-core` link check should now pass.

## Uncommitted files

- `openspec/changes/ef-core-content/{design.md,proposal.md,specs/topic-page-content/spec.md,tasks.md}`;
- `openspec/changes/ef-core-content/devlog/20261110-1156-ef6-mapping.md`, and this note;
- `site/README.md`, `site/astro.config.mjs`, `site/src/content/docs/index.mdx` and
  `site/tests/dist.test.ts`;
- `site/src/content/docs/data/ef-core.md`;
- `site/src/content/docs/cheatsheets/ef-core.md`, which is new.

## Where the page's SQL came from, and what 3.3 must still check

I captured SQL with a scratch console app,
`<scratchpad>/efprobe/Sql/Program.cs`, running against a container on port 14331. Its log is
`<scratchpad>/efprobe/Sql/bin/Debug/net10.0/sql.log`. The scratchpad may not survive a `/clear`.
If it's gone, rebuild the app from the notes below. **The container is removed.**

- **That app's model had a `Version` rowversion column, and the exercise's doesn't.** On the page I
  dropped `[o].[Version]` from the SELECT listings, and the `OUTPUT INSERTED.[Version]` and
  `AND [Version] = @p2` parts from the **cancel** UPDATE listing, so they match the exercise's model.
  The **concurrency** listing keeps them, correctly. **3.3 must re-capture with the exercise's
  model, with no `Version`, and paste those listings exactly.**
- **The N+1 listing on the page squeezes each `SELECT COUNT(*) …` onto one line.** The log has it on
  three. Either restore the three-line form, or note in the section-done note that it's reformatted
  (D7 says "trimmed only of parameter declarations").
- **Captured and quoted verbatim:**
  - the `where` query, parameterized `@email`;
  - `ToQueryString`;
  - `include`: a LEFT JOIN on `[OrderLines]`, `ORDER BY [o].[Id]`;
  - the projection: a correlated `COUNT(*)` subquery;
  - `ExecuteUpdate` (`UPDATE [o] SET [o].[Status] = @p FROM [Orders] AS [o] WHERE … < @cutoff`);
  - the translation error for `Customers.IsVip(…)`, an `InvalidOperationException` whose full text
    is in the log, abridged with "…" on the page;
  - `DbUpdateConcurrencyException`: "The database operation was expected to affect 1 row(s), but
    actually affected 0 row(s); data may have been modified or deleted since entities were loaded."
  - The insert is one INSERT with `OUTPUT INSERTED.[Id]`, then one `MERGE [OrderLines]` for the
    lines. The page says "inserts the lines in one batch".
- **Claims on the page that I wrote without running them (check each in 3.3, and fix or cut):**
  - "`AddDbContextFactory` also registers the context itself as scoped";
  - the message "A second operation was started on this context instance before a previous operation
    completed";
  - "a `decimal` gets no precision (EF Core warns…)": check the warning really appears;
  - the pitfall "a model change with no migration" (queries fail with "Invalid column name", and
    `database update` reports nothing to apply). EF Core 9+ may instead **throw a
    `PendingModelChangesWarning`** on `Migrate`/`database update`. Check, and reword to what
    actually happens;
  - `[MaxLength(256)]` and `[Precision(10, 2)]` as data annotations;
  - `dotnet user-secrets init` then `set` (also in the cheatsheet);
  - `./efbundle --connection "<connection string>"` (run `./efbundle --help`; the probe built a
    bundle, but didn't run it with `--connection`);
  - "The first run downloads about 1.8 GB" (the image is 1.76 GB uncompressed; the download may be
    smaller), and "about 15 seconds" (13 s, seen on OrbStack);
  - the `docker run` with `--platform linux/amd64`. **Gotcha:** without it, Docker on Apple silicon
    prints "WARNING: The requested image's platform (linux/amd64) does not match the detected host
    platform (linux/arm64/v8)…". The cheatsheet's `docker run` **lacks `--platform`**: add it, to
    match the page.
  - **The cheatsheet's fragments:** it uses `db.Customers` and `o.Payments` in the Include and
    ThenInclude examples, and `order.Version`. Compile them in a scratch project that defines those
    types, or simplify them.
- **Snippets "from the exercise"** (Program.cs registration, the list projection, the save, cancel,
  `OrderConfiguration`) were copied from `Solution/`. Diff them again in 3.3. The save snippet is
  marked "abridged".

## Gotchas this session

- **Run Prettier from `site/`** (`cd site && npx prettier --write …`). From the repo root it can't
  find `prettier-plugin-astro`.
- **Every `docker`, `dotnet test` (samples) and NuGet command needs `dangerouslyDisableSandbox`.**
  A sandboxed `dotnet test` of exercise 07 is the way to see the no-Docker message.
- **Panopticon works now:** report at each section start and after each tick.

## Next

- **Task 3.3:** verify the page as above (D7), then record the results in the §3 section-done note.
- **Then 3.4:** add `data/ef-core` and `cheatsheets/ef-core` to `FULL_CONTENT_SLUGS`, and the pair to
  `CHEATSHEETS`. Run the link script over the Learn links. Then `make gates` to `GATES_EXIT:0`,
  which also clears §2's deliberate red, the `07-ef-core` link check.
- **Then:** self-review §3 against the five EF Core page requirements, the Docker requirement (that
  one is §4), and the **main** specs (see the memory "self-review checks main specs"). Write the
  section-done note, and make one commit:
  `feat(ef-core-content): EF Core cheatsheet and page (§3)`.
- **Emmz's standing decisions:** SQL Server; Testcontainers; a cheatsheet; `dnx` for `dotnet-ef`;
  the four pins (with Pkcs 10.0.14); "Coming from Entity Framework 6", after the concepts.
- **`change/ef-core-content` has never been pushed.** The temporary `ci/ef-core-content-s2` branch
  was deleted. Nothing is running in the background.
