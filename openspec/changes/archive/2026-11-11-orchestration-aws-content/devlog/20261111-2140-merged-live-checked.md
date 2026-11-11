# Merged to main, live site checked

<!-- Merge: main fast-forwarded b4bbc45..f84cfd8 · Emmz said "go ahead" -->

- `main` was fast-forwarded to `f84cfd8`, and `main` and `change/orchestration-aws-content` were pushed.
- On `main` at `f84cfd8`: the `site` run (43102341719) passed, `gates success` and `deploy success`;
  the `samples` run (43102341660) passed.
- Live checks (fetched):
  - `/deploy/orchestration-and-aws/`: 200, and the §7 text is there (`MapDefaultEndpoints`, the
    healthCheck fragment's title, `secretsmanager:GetSecretValue`), as is Exercise 13. No "Coming
    soon".
  - `/cheatsheets/orchestration-and-aws/`: 200, with its back link.
  - `/platform/docker/` links to the Orchestration page (§6), and `/observability/tracing/` has the
    §7 wording.
  - The exercise 13 tree link on GitHub: **200** now (it was 404 before the merge).

## Next

- Offer `/opsx:archive`. After the sync, check `openspec validate --specs --strict` (a grown MODIFIED
  requirement can go over 500 characters after the merge).
- Leftover: the `health-orders` and `hs-probe` local Docker images.
