# Deploy confirmed: site live on dotnet-primer.emmz.dev

`main` was fast-forwarded to `1a2612b` and pushed, at Emmz's request.

## CI on the first push

| Workflow | Result |
|---|---|
| `samples` | Success. I dispatched it by hand, because its path filter didn't fire on the first push to the new branch. |
| `site`, gates job | Success. |
| `site`, deploy job (attempt 1) | Failed with "404 … Ensure GitHub Pages has been enabled", as expected before setup. |

## Emmz's setup

Emmz did the setup:

- made the repo public;
- set Pages to deploy from GitHub Actions;
- set the custom domain;
- added the Cloudflare CNAME;
- enforced HTTPS.

The deploy re-run (attempt 2) succeeded.

## Verified live

| Check | Result |
|---|---|
| `http://dotnet-primer.emmz.dev/` | 301 to `https://` |
| `https://dotnet-primer.emmz.dev/` | 200: the introduction, with the Report link and the Learn block |
| `/aspnet-core/configuration/`, `/cheatsheets/dotnet-cli/`, `/pagefind/pagefind.js` | 200 |
| `/no-such-page/` | 404 |
| Pages API | `cname: dotnet-primer.emmz.dev`, `https_enforced: true`, `build_type: workflow` |
| Repo | `PUBLIC`. The exercise folder link returns 200. |

All the `tasks.md` workflow follow-ups are done, apart from archiving.

**Nit for the first content change:** the home page's `<title>` is "dotnet-primer | dotnet-primer",
because the page title equals the site title. Give `index.mdx` a distinct `title`.

## Next

- Waiting on Emmz's yes before running `/opsx:archive`. This note is uncommitted, and the working
  tree is on `main`.
- Parked for later changes:
  - the provisional order of the learning path after Middleware;
  - how diagrams are done;
  - external link checking;
  - Aspire docs are now on aspire.dev, not Learn;
  - optionally, a `/` keyboard shortcut for search;
  - the duplicate home-page title.
