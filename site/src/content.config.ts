import { defineCollection } from "astro:content";
import { z } from "astro/zod";
import { docsLoader } from "@astrojs/starlight/loaders";
import { docsSchema } from "@astrojs/starlight/schema";

/**
 * Every content page must point readers at the authoritative docs on
 * Microsoft Learn (spec: content-page-standards). Making the field required
 * means a page without one fails `astro build`, not a later review.
 */
const learnLink = z.object({
  title: z.string().min(1),
  url: z.url({
    protocol: /^https$/,
    hostname: /^learn\.microsoft\.com$/,
    error: "Microsoft Learn links must start with https://learn.microsoft.com/",
  }),
});

export const collections = {
  docs: defineCollection({
    loader: docsLoader(),
    schema: docsSchema({
      extend: z.object({
        learn: z
          .array(learnLink)
          .min(1, "Every page needs at least one Microsoft Learn link"),
      }),
    }),
  }),
};
