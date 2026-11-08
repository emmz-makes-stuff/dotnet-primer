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
      customCss: ["./src/styles/theme.css"],
      components: {
        MarkdownContent: "./src/components/MarkdownContent.astro",
        EditLink: "./src/components/EditLink.astro",
      },
      lastUpdated: true,
    }),
  ],
});
