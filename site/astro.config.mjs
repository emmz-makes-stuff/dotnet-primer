// @ts-check
import { defineConfig } from "astro/config";
import starlight from "@astrojs/starlight";

export default defineConfig({
  site: "https://dotnet-primer.emmz.dev",
  integrations: [
    starlight({
      title: "dotnet-primer",
      description:
        "From .NET 3.5 to .NET 10: ASP.NET Core, EF Core, gRPC and containers for engineers making the jump.",
      editLink: {
        baseUrl:
          "https://github.com/emmz-makes-stuff/dotnet-primer/edit/main/site/",
      },
      social: [
        {
          icon: "github",
          label: "GitHub",
          href: "https://github.com/emmz-makes-stuff/dotnet-primer",
        },
      ],
      customCss: [
        "@fontsource-variable/atkinson-hyperlegible-next",
        "@fontsource-variable/fraunces",
        "@fontsource-variable/jetbrains-mono",
        "./src/styles/theme.css",
      ],
      components: {
        MarkdownContent: "./src/components/MarkdownContent.astro",
        EditLink: "./src/components/EditLink.astro",
      },
      lastUpdated: true,
      // The order of the "Learning path" group IS the learning path (design D3):
      // ASP.NET Core is the spine, leading with Configuration → DI → Middleware.
      // Order after Middleware is provisional; reorder here, URLs don't change.
      sidebar: [
        {
          label: "Getting started",
          items: [
            { slug: "platform/dotnet-10-and-cli" },
            { slug: "platform/docker" },
          ],
        },
        {
          label: "Learning path",
          items: [
            { slug: "aspnet-core" },
            { slug: "aspnet-core/configuration" },
            { slug: "aspnet-core/dependency-injection" },
            { slug: "aspnet-core/middleware" },
            { slug: "observability/logging" },
            { slug: "observability/tracing" },
            { slug: "observability/metrics" },
            { slug: "data/ef-core" },
            { slug: "services/grpc" },
            { slug: "testing" },
            { slug: "security" },
            { slug: "performance/async-and-performance" },
            { slug: "deploy/containers" },
            { slug: "deploy/orchestration-and-aws" },
          ],
        },
        { label: "C# language", items: [{ slug: "csharp" }] },
        { label: "Coming from .NET 3.5", items: [{ slug: "coming-from-3-5" }] },
        {
          label: "Cheatsheets",
          items: [
            { slug: "cheatsheets/dotnet-cli" },
            { slug: "cheatsheets/configuration" },
            { slug: "cheatsheets/dependency-injection" },
            { slug: "cheatsheets/middleware" },
            { slug: "cheatsheets/logging" },
            { slug: "cheatsheets/tracing" },
            { slug: "cheatsheets/metrics" },
            { slug: "cheatsheets/ef-core" },
            { slug: "cheatsheets/grpc" },
            { slug: "cheatsheets/testing" },
            { slug: "cheatsheets/security" },
            { slug: "cheatsheets/async-and-performance" },
            { slug: "cheatsheets/containers" },
          ],
        },
      ],
    }),
  ],
});
