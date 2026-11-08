# ADR-0002: Host on GitHub Pages via GitHub Actions, behind Cloudflare DNS

- **Status:** Accepted
- **Date:** 2026-11-08
- **Deciders:** Emmz

## Context

The hard constraints in `docs/DISCOVERY.md` are: a static site, hosted on **GitHub Pages**, served on
a custom domain through a **Cloudflare CNAME**, and public. The repo is
`emmz-makes-stuff/dotnet-primer`.

## Decision

- **Domain:** `dotnet-primer.emmz.dev`.
- **Deploy:** a GitHub Actions workflow on every push to `main`:
  1. install Node;
  2. `npm ci` in `site/`;
  3. build;
  4. `actions/upload-pages-artifact` with `site/dist`;
  5. `actions/deploy-pages`.

  Pages source is set to *GitHub Actions*. There is no `gh-pages` branch.
- The workflow runs the **site gates** before deploying. A failing gate blocks the deploy.
- **Custom domain:** `site/public/CNAME` contains `dotnet-primer.emmz.dev`. Astro `site` is
  `https://dotnet-primer.emmz.dev` and `base` is `/`.
- **DNS:** Cloudflare `CNAME dotnet-primer → emmz-makes-stuff.github.io`. It stays **DNS-only (grey
  cloud)** until GitHub issues the TLS certificate. After that it can be proxied, with SSL mode
  *Full (strict)*.

## Alternatives rejected

- **Deploying from a `gh-pages` branch:** the legacy model. It keeps build output in git, and gates
  can't block a deploy as cleanly.
- **Cloudflare Pages:** a good fit technically, but it breaks the GitHub Pages constraint.

## Consequences

- The deploy workflow needs `pages: write` and `id-token: write` permissions.
- Proxying through Cloudflare before the certificate is issued blocks GitHub's HTTPS provisioning.
