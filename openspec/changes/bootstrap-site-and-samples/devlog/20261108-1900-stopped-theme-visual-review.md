# Stopped: theme needs Emmz's visual review (6.1)

<!-- Section: 6 · Tasks: 6.1 · Base: fdb7c7e -->

Task 6.1 is built and passes every automated check. It still needs a person to judge the look, and
`CLAUDE.md` says to stop and ask for that. I've left the work **uncommitted** and 6.1 **unticked**.

**Typefaces:** all self-hosted through `@fontsource-variable/*` ^5.3.0, which `customCss` loads
before `theme.css`.

- **Body:** Atkinson Hyperlegible Next. It was designed for legibility, which suits long reading.
- **Headings** (`h1#_top`, `.hero h1`, and markdown h1–h3): Fraunces at weight 600/700, with
  optical sizing. It's a characterful serif, chosen to give the site its distinctive look.
- **Code:** JetBrains Mono.
- **Rejected:** Inter, which is too generic for "distinctive".

**Palette:**

- Dark mode: ink-blue base (`#15122a`), violet accent `#8b6cf6`, accent-high `#ddd3ff`.
- Light mode: warm paper base (`#fcfaf6`), violet accent `#5b34d6`.
- Every text and link pair checked passes WCAG AA. The lowest are dark-mode accent on black (4.84)
  and light-mode meta text (5.39).
- Content width: 46rem. Line height: 1.7.

**Checks passed:**

- `SITE_BUILD_EXIT:0`, `SITE_LINT_EXIT:0`, `SITE_FORMAT_EXIT:0`, `SITE_TEST_EXIT:0`.
- `dist/_astro` contains 10 `.woff2` files, and nothing references fonts.googleapis.

**Preview:** `astro preview` is running as a daemon at http://127.0.0.1:4321 (pid 46318). To stop it,
run `npx --prefix site astro preview stop`.

**Question for Emmz:** is the look right in both light and dark, as a starting point? Fine-tuning
belongs to the later design change, not this one. While the preview is open, she could also do the
9.2 checks: search finds "Dependency Injection", and the cheatsheet's copy button copies only the
code.

## Next

- **If she approves:**
  1. tick 6.1;
  2. write a section-6-done note quoting the gates;
  3. commit §6;
  4. stop the preview;
  5. carry on with §7 (samples), task 7.1.
- **If she asks for changes:** adjust `site/src/styles/theme.css` or the font choices, re-run the
  site gates, and ask again.
- **If she also did the 9.2 checks:** record the results in a note, so 9.2 only needs the 9.1 gate
  re-run at the end.
