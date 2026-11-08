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
];

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

describe("deployment", () => {
  it("dist/CNAME declares the custom domain", () => {
    expect(readFileSync(join(DIST, "CNAME"), "utf8").trim()).toBe(
      "dotnet-primer.emmz.dev",
    );
  });
});
