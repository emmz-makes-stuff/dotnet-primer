/**
 * Site test gate (design D7): asserts the built site in dist/ meets the
 * content-page-standards, site-structure and site-deployment specs.
 * Run `npm run build` first; this suite reads build output, not sources.
 */
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { join, relative, sep } from "node:path";
import { fileURLToPath } from "node:url";
import { parse, type HTMLElement } from "node-html-parser";
import { describe, expect, it } from "vitest";

const SITE = "https://dotnet-primer.emmz.dev";
const ISSUES_URL =
  "https://github.com/emmz-makes-stuff/dotnet-primer/issues/new";
const DIST = fileURLToPath(new URL("../dist/", import.meta.url));

/** Every area and standalone slug from the sidebar (design D3). */
const EXPECTED_SLUGS = [
  "platform/dotnet-10-and-cli",
  "aspnet-core",
  "aspnet-core/configuration",
  "aspnet-core/dependency-injection",
  "aspnet-core/middleware",
  "observability/logging",
  "observability/tracing",
  "observability/metrics",
  "data/ef-core",
  "services/grpc",
  "testing",
  "security",
  "performance/async-and-performance",
  "deploy/containers",
  "deploy/orchestration-and-aws",
  "csharp",
  "coming-from-3-5",
  "cheatsheets/dotnet-cli",
  "cheatsheets/configuration",
];

/**
 * Pages delivered with full content (spec: topic-page-content). They must not
 * show the "Coming soon" placeholder aside. Add a page here when it's finished.
 */
const FULL_CONTENT_SLUGS: string[] = [
  "aspnet-core",
  "aspnet-core/configuration",
  "cheatsheets/configuration",
];

/**
 * Topic page → its cheatsheet (spec: site-structure, Cheatsheets per area).
 * Each must link to the other from its main content.
 */
const CHEATSHEETS: Record<string, string> = {
  "platform/dotnet-10-and-cli": "cheatsheets/dotnet-cli",
  "aspnet-core/configuration": "cheatsheets/configuration",
};

const REPO_TREE =
  "https://github.com/emmz-makes-stuff/dotnet-primer/tree/main/samples/exercises/";
const EXERCISES = fileURLToPath(
  new URL("../../samples/exercises/", import.meta.url),
);

if (!existsSync(join(DIST, "index.html"))) {
  throw new Error(
    "site/dist/ is missing or empty: run `npm --prefix site run build` first",
  );
}

function walk(dir: string): string[] {
  return readdirSync(dir).flatMap((name) => {
    const full = join(dir, name);
    return statSync(full).isDirectory() ? walk(full) : [full];
  });
}

/** dist/a/b/index.html -> "/a/b/"; dist/404.html -> "/404.html" */
function pagePath(file: string): string {
  const rel = relative(DIST, file).split(sep).join("/");
  if (rel === "index.html") return "/";
  if (rel.endsWith("/index.html"))
    return `/${rel.slice(0, -"index.html".length)}`;
  return `/${rel}`;
}

const htmlFiles = walk(DIST).filter(
  (f) => f.endsWith(".html") && !relative(DIST, f).startsWith("pagefind"),
);

type Page = { path: string; url: string; root: HTMLElement };
const pages: Page[] = htmlFiles.map((file) => {
  const path = pagePath(file);
  return {
    path,
    url: new URL(path, SITE).href,
    root: parse(readFileSync(file, "utf8")),
  };
});

/** Content pages carry the Learn and Report links; the built-in 404 does not. */
const contentPages = pages.filter((p) => p.path !== "/404.html");

/** Does a site-internal path resolve to a built file? */
function resolves(pathname: string): boolean {
  const p = decodeURIComponent(pathname).replace(/^\/+/, "");
  const candidates =
    p === "" || p.endsWith("/")
      ? [join(DIST, p, "index.html")]
      : [join(DIST, p), join(DIST, p, "index.html"), join(DIST, `${p}.html`)];
  return candidates.some((c) => existsSync(c) && statSync(c).isFile());
}

describe("content pages", () => {
  it("finds the built pages", () => {
    expect(contentPages.length).toBeGreaterThanOrEqual(
      EXPECTED_SLUGS.length + 1,
    );
  });

  it("each shows an On Microsoft Learn block with at least one learn.microsoft.com link", () => {
    const failures = contentPages
      .filter(
        (p) =>
          p.root.querySelectorAll(
            '.learn-links a[href^="https://learn.microsoft.com/"]',
          ).length === 0,
      )
      .map((p) => p.path);
    expect(
      failures,
      `pages without a Microsoft Learn link: ${failures.join(", ")}`,
    ).toEqual([]);
  });

  it("each has a Report a problem link prefilled with the page title and full URL", () => {
    const failures: string[] = [];
    for (const p of contentPages) {
      const a = p.root.querySelector("a[data-report-problem]");
      const href = a?.getAttribute("href");
      if (!href) {
        failures.push(`${p.path}: no Report a problem link`);
        continue;
      }
      if (!href.startsWith(`${ISSUES_URL}?`)) {
        failures.push(`${p.path}: link goes to ${href}`);
        continue;
      }
      const params = new URL(href).searchParams;
      if (!params.get("title")?.trim())
        failures.push(`${p.path}: no prefilled title`);
      if (!params.get("body")?.includes(p.url)) {
        failures.push(`${p.path}: body does not contain ${p.url}`);
      }
    }
    expect(failures, failures.join("\n")).toEqual([]);
  });
});

describe("internal links", () => {
  it("every link to another site page resolves to a built file", () => {
    const broken: string[] = [];
    for (const p of pages) {
      for (const a of p.root.querySelectorAll("a[href]")) {
        const href = a.getAttribute("href")!;
        if (href.startsWith("#") || /^(mailto|tel|javascript):/.test(href))
          continue;
        const target = new URL(href, p.url);
        if (target.origin !== SITE) continue;
        if (!resolves(target.pathname))
          broken.push(`${p.path} → ${target.pathname}`);
      }
    }
    expect(broken, `broken internal links:\n${broken.join("\n")}`).toEqual([]);
  });
});

describe("site structure", () => {
  const index = pages.find((p) => p.path === "/")!;

  it("the introduction links every area and standalone section", () => {
    const hrefs = new Set(
      index.root
        .querySelectorAll("a[href]")
        .map((a) => new URL(a.getAttribute("href")!, SITE).pathname),
    );
    const missing = EXPECTED_SLUGS.filter((s) => !hrefs.has(`/${s}/`));
    expect(
      missing,
      `introduction does not link: ${missing.join(", ")}`,
    ).toEqual([]);
  });

  it("every expected area page is built", () => {
    const missing = EXPECTED_SLUGS.filter((s) => !resolves(`/${s}/`));
    expect(missing).toEqual([]);
  });

  it("search is wired: the Pagefind bundle exists and the DI page is indexed", () => {
    expect(existsSync(join(DIST, "pagefind", "pagefind.js"))).toBe(true);
    const di = pages.find(
      (p) => p.path === "/aspnet-core/dependency-injection/",
    )!;
    expect(di.root.querySelector("[data-pagefind-body]")).not.toBeNull();
  });
});

/** The page's own content: excludes the sidebar, header, TOC and footer. */
function content(p: Page): HTMLElement | null {
  return p.root.querySelector(".sl-markdown-content");
}

function pageAt(slug: string): Page | undefined {
  return pages.find((p) => p.path === `/${slug}/`);
}

/** Site-internal link targets (pathnames) in a page's main content. */
function contentLinks(p: Page): Set<string> {
  return new Set(
    (content(p)?.querySelectorAll("a[href]") ?? [])
      .map((a) => new URL(a.getAttribute("href")!, p.url))
      .filter((u) => u.origin === SITE)
      .map((u) => u.pathname),
  );
}

function hiddenFromAssistiveTech(el: HTMLElement): boolean {
  for (let n: HTMLElement | null = el; n; n = n.parentNode as HTMLElement) {
    if (n.getAttribute?.("aria-hidden") === "true") return true;
  }
  return false;
}

/** aria-label, else the text of the elements aria-labelledby points to. */
function accessibleName(p: Page, svg: HTMLElement): string {
  const label = svg.getAttribute("aria-label")?.trim();
  if (label) return label;
  return (svg.getAttribute("aria-labelledby") ?? "")
    .split(/\s+/)
    .filter(Boolean)
    .map((id) => p.root.querySelector(`[id="${id}"]`)?.textContent ?? "")
    .join(" ")
    .trim();
}

describe("full-content pages", () => {
  it("are built and show no Coming soon placeholder", () => {
    const failures: string[] = [];
    for (const slug of FULL_CONTENT_SLUGS) {
      const p = pageAt(slug);
      if (!p) failures.push(`/${slug}/: not built`);
      else if (content(p)?.querySelector('aside[aria-label="Coming soon"]'))
        failures.push(`/${slug}/: still shows the Coming soon placeholder`);
    }
    expect(failures, failures.join("\n")).toEqual([]);
  });

  it("topic pages and their cheatsheets link to each other", () => {
    const failures: string[] = [];
    for (const [topic, sheet] of Object.entries(CHEATSHEETS)) {
      const t = pageAt(topic);
      const s = pageAt(sheet);
      if (!t || !s) {
        failures.push(`${topic} ↔ ${sheet}: page not built`);
        continue;
      }
      if (!contentLinks(t).has(`/${sheet}/`))
        failures.push(`/${topic}/ does not link to /${sheet}/`);
      if (!contentLinks(s).has(`/${topic}/`))
        failures.push(`/${sheet}/ does not link back to /${topic}/`);
    }
    expect(failures, failures.join("\n")).toEqual([]);
  });
});

describe("exercises", () => {
  it("every exercise folder is linked from a site page", () => {
    const folders = readdirSync(EXERCISES).filter((name) =>
      statSync(join(EXERCISES, name)).isDirectory(),
    );
    expect(folders.length).toBeGreaterThan(0);
    const hrefs = new Set(
      pages.flatMap((p) =>
        p.root
          .querySelectorAll("a[href]")
          .map((a) => a.getAttribute("href")!.replace(/\/$/, "")),
      ),
    );
    const unlinked = folders.filter((f) => !hrefs.has(`${REPO_TREE}${f}`));
    expect(
      unlinked,
      `exercise folders no page links to: ${unlinked.join(", ")}`,
    ).toEqual([]);
  });
});

describe("diagrams", () => {
  it("every content SVG is an image with an accessible name, or hidden", () => {
    const failures: string[] = [];
    for (const p of contentPages) {
      for (const svg of content(p)?.querySelectorAll("svg") ?? []) {
        if (hiddenFromAssistiveTech(svg)) continue;
        if (svg.getAttribute("role") !== "img")
          failures.push(`${p.path}: <svg> without role="img"`);
        else if (!accessibleName(p, svg))
          failures.push(`${p.path}: diagram without an accessible name`);
      }
    }
    expect(failures, failures.join("\n")).toEqual([]);
  });
});

describe("deployment", () => {
  it("dist/CNAME declares the custom domain", () => {
    expect(readFileSync(join(DIST, "CNAME"), "utf8").trim()).toBe(
      "dotnet-primer.emmz.dev",
    );
  });
});
