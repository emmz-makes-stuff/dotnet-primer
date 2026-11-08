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

## Exercises

An exercise lives in `samples/exercises/NN-topic/` (see `samples/README.md`). Describe it on the
page for the subject area it practises: the goal, the steps, and how to run its tests. Link to the
exercise folder on GitHub:
`https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/NN-topic`.

## Theme

Colours, fonts and widths are CSS custom properties in `src/styles/theme.css`. Starlight's components
are replaced in `src/components/`:

- `MarkdownContent.astro` renders the "On Microsoft Learn" block.
- `EditLink.astro` renders the "Report a problem" link.
