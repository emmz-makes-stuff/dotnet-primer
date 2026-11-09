# dotnet-primer site

The primer's static site, built with [Astro](https://astro.build) and
[Starlight](https://starlight.astro.build). The browser gets plain HTML; any page scripts are vanilla
TypeScript.

```sh
npm ci            # once
npm run dev       # live-reloading local server
npm run build     # static build into dist/
npm run preview   # serve dist/ locally (after a build)
```

The gate commands (`build`, `lint`, `format:check`, `test`) are listed in the root `README.md`.

## Adding a page

1. Create a Markdown (`.md`) or MDX (`.mdx`) file under `src/content/docs/`. The file path is the URL:
   `src/content/docs/aspnet-core/configuration.md` is served at `/aspnet-core/configuration/`.
2. Give it frontmatter. `title` and `learn` are **required**:

   ```yaml
   ---
   title: Configuration
   description: One sentence for search results and link previews.
   learn:
     - title: Configuration in ASP.NET Core
       url: https://learn.microsoft.com/aspnet/core/fundamentals/configuration/
   ---
   ```

   `learn` lists at least one page on Microsoft Learn. The links are shown in an **"On Microsoft
   Learn"** block at the end of the page. The build **fails** if `learn` is missing or empty, or if a
   URL doesn't start with `https://learn.microsoft.com/`. Prefer locale-free URLs, without `/en-us/`.

3. Add it to the sidebar in `astro.config.mjs` (see below). Pages are not added automatically.
4. Every page automatically gets an **Edit page** link and a **Report a problem** link, which opens a
   GitHub issue prefilled with the page title and URL. You don't add anything for these.

## Sidebar and the learning path

The sidebar in `astro.config.mjs` is an explicit, ordered list, and **its order is the learning path**.
ASP.NET Core is the spine, and it leads with Configuration → Dependency Injection → Middleware. The
**Previous / Next** links at the bottom of each page follow the sidebar order.

Groups:

- _Getting started_: the platform and the `dotnet` CLI.
- _Learning path_: the ordered path.
- _C# language_ and _Coming from .NET 3.5_: standalone, outside the path.
- _Cheatsheets_.

To reorder the path, move entries within the _Learning path_ group. URLs don't change.

Pages outside the path (standalone sections and cheatsheets) turn off Previous / Next, so readers
aren't walked out of the path by accident:

```yaml
prev: false
next: false
```

## Code samples

Use fenced code blocks with a language. Every block gets syntax highlighting and a copy button.
Supported languages include `csharp`, `sh`, `json`, `xml`, `yaml` and `dockerfile`.

````md
```csharp title="Program.cs"
var builder = WebApplication.CreateBuilder(args);
```
````

`title="…"` adds a file-name tab. Expressive Code also supports line markers (`{2-3}`), and inserted
(`ins={4}`) or deleted (`del={5}`) lines, which are handy for showing an old way and a new way.

## Topic pages

A finished learning-path page follows this skeleton:

1. **What you'll be able to do**: an opening paragraph. If the topic has a cheatsheet, put a
   one-line link to it straight after, as a `:::tip`.
2. **Concepts**: one `##` section per subtopic, each with a short code example. Push variations to
   the cheatsheet.
3. **`## Pitfalls`**: the common mistakes, each with its symptom and its fix.
4. **`## Exercise NN: …`**, if the topic has one (see below).

Teach modern .NET on its own terms. A one-line "you may know this as …" is fine, but old-to-new
mappings belong on the _Coming from .NET 3.5_ page.

**C# samples must be right for .NET 10.** Compile any sample shown as a complete file in a scratch
`dotnet new web` project, with warnings as errors, before publishing. Give fragments a `title="…"`
naming the file they belong in.

**Stub pages** carry a `:::note[Coming soon]` aside. The site tests detect the placeholder by that
exact title. When a page is finished:

- remove the aside;
- add its slug to `FULL_CONTENT_SLUGS` in `tests/dist.test.ts`, so the placeholder can't come back.

## Cheatsheets

A cheatsheet lives in `src/content/docs/cheatsheets/`. It has `prev: false` and `next: false`, its
own `learn` links, a "Back to …" link to its topic page first, then `##` groups of copyable code
blocks with as little prose as possible. To add one:

1. List it in the sidebar's _Cheatsheets_ group and in the intro page's _Cheatsheets_ cards.
2. Add it to `EXPECTED_SLUGS` in `tests/dist.test.ts`.
3. Add the topic → cheatsheet pair to `CHEATSHEETS` in `tests/dist.test.ts`. The tests then require
   a link each way.

## Diagrams

Diagrams are hand-written inline SVG in Astro components under `src/components/diagrams/`. The
pattern is set by `RequestPipeline.astro`:

- **Colours:** use Starlight tokens (`var(--sl-color-gray-6)`, `var(--sl-color-accent)`, …), never
  hex values. The diagram then follows the light/dark toggle.
- **Accessibility:** give the `<svg>` `role="img"` and `aria-labelledby` pointing to a `<title>` and
  a `<desc>`. The `<desc>` says in words what the diagram shows. The site tests fail any visible
  content `<svg>` without an accessible name.
- **Text:** use real `<text>`, not outlined paths. Keep labels at 14px or more, with a `viewBox` and
  `width: 100%`, so the diagram stays readable at phone width.
- **Use:** the page must be `.mdx`, so it can import the component:
  `import RequestPipeline from "../../../components/diagrams/RequestPipeline.astro";`.

## Exercises

An exercise lives in `samples/exercises/NN-topic/` (see `samples/README.md`). Describe it on the
page for the subject area it practises: the goal, the steps, and how to run its tests. Link to the
exercise folder on GitHub:
`https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/NN-topic`.
The site tests fail if any folder under `samples/exercises/` has no page linking to it like this.

## Theme

Colours, fonts and widths are CSS custom properties in `src/styles/theme.css`. Starlight's components
are replaced in `src/components/`:

- `MarkdownContent.astro` renders the "On Microsoft Learn" block.
- `EditLink.astro` renders the "Report a problem" link.
