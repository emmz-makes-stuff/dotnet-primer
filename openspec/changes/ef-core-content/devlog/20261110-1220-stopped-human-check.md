# Stopped: §5 human check (task 5.1)

<!-- Section: 5 · Task: 5.1 · Base: 49b70ee -->

§1–§4 are committed (`49b70ee`), with `GATES_EXIT:0`. Task 5.1 needs Emmz's eyes, so it's
unticked. The working tree is clean.

## The question

Do these pass?
1. The EF Core page (`/data/ef-core/`) and its cheatsheet (`/cheatsheets/ef-core/`) read well in
   both light and dark schemes, including the long one-line `Include` SQL listing, which scrolls
   sideways.
2. Search finds "DbContext".
3. The copy button on a `dnx dotnet-ef` block copies the command.
4. Exercise 07 from a clean clone: the Starter's tests fail with guiding messages, and the
   Solution's pass. Docker must be running.

## Recipe

The site preview, from the repo root:

```sh
npm --prefix site run build && npm --prefix site run preview
```

Open <http://localhost:4321/data/ef-core/> and <http://localhost:4321/cheatsheets/ef-core/>.
Toggle the theme (top right), search for `DbContext`, and press the copy button on the
`dnx dotnet-ef@10.0.14 migrations add InitialCreate` block, then paste it somewhere.

Exercise 07 from a clean clone of the local branch (it has never been pushed):

```sh
git clone --branch change/ef-core-content "$PWD" "$TMPDIR/primer-check" && cd "$TMPDIR/primer-check"
dotnet test --project samples/exercises/07-ef-core/Starter.Tests
dotnet test --project samples/exercises/07-ef-core/Solution.Tests
```

Cleanup, separately, afterwards: `rm -rf "$TMPDIR/primer-check"`.

## On PASS

Tick 5.1. Then follow "Finishing a change": the whole-change review
`git diff 1a84a4d..HEAD` (the first section's base is `1a84a4d`), checked against the main specs too.
Then the change-done note, and the offer of `code-review`.

## On FAIL

Add `## 6. Review fixes: <theme>` to `tasks.md`, agree it with Emmz, and build it with the section
loop.

## Next

Wait for Emmz's result on 5.1.
