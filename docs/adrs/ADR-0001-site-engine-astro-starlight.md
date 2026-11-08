# ADR-0001: Build the site with Astro + Starlight

- **Status:** Accepted
- **Date:** 2026-11-08
- **Deciders:** Emmz

## Context

`docs/DISCOVERY.md` asks for an engaging **static** site (hard constraint) for engineers coming from
.NET 3.5. It is used both for self-paced learning and as a desk reference, and needs:

- strong, distinctive visual design;
- syntax-highlighted code with one-click copy;
- an ordered learning path;
- per-area cheatsheets;
- links to Microsoft docs;
- a "report a problem" GitHub issue link on every page;
- site-wide search, "if possible".

Emmz's house default for frontend work is vanilla TypeScript and CSS, with no frameworks unless asked.

## Decision

Use **Astro 7 with the Starlight docs theme** (verified 2026-11-08: astro 7.4.7, needs Node ≥22.23;
@astrojs/starlight 0.42.10, peer astro ^7.2.10).

- Content is Markdown/MDX under `site/src/content/docs/`.
- Any client-side behaviour is **vanilla TypeScript** in `<script>` tags. No UI framework (React, Vue,
  etc.) is added.
- The distinctive look comes from `customCss` and Starlight **component overrides**.
- Search is Starlight's built-in **Pagefind** index, built at build time, so the search requirement is
  met.
- Code blocks use Starlight's built-in **Expressive Code**, which includes copy buttons.
- The learning path is the **sidebar order**: ASP.NET Core is the spine, leading with Configuration →
  Dependency Injection → Middleware. The C# language area and "coming from .NET 3.5" are standalone
  sections.
- A "Report a problem" link comes from a component override. It links to
  `https://github.com/emmz-makes-stuff/dotnet-primer/issues/new`, prefilled with the page URL and title.

Emmz chose this knowing it departs from her vanilla default. Astro works only at build time, the
output is plain HTML, and the page scripts stay vanilla TypeScript.

## Alternatives rejected

- **Plain Astro with custom layouts**: most design freedom, but we would rebuild nav, sidebar, ToC,
  prev/next and the search wiring that Starlight already provides.
- **Vanilla Vite + TS with a hand-written Markdown pipeline**: the house default taken literally, but
  it means writing and maintaining our own static-site generator.
- **DocFX**: native to .NET, but dated themes, weak on "strong visual design", and clunkier to
  customise.

## Consequences

- The site will look like Starlight until it is restyled, so design work is an explicit task.
- Starlight is pre-1.0 (0.x). Upgrades may break things, so versions are pinned in
  `site/package-lock.json`.
- Diagrams are **deferred**. The lean is committed SVG files, because Mermaid needs client JS or a
  headless browser at build time.
