# Proposal

## Why

The primer already uses Docker: the Observability pages run the Aspire dashboard in a container, and
the later Testing, Containers and data pages will use Testcontainers and container images. Nothing on
the site says how to get Docker, though. Most of the engineers work on **Windows**, where the usual
answer, Docker Desktop, needs a paid subscription at any company with 250 or more staff or $10M or
more in revenue.

Emmz asked for this while reading the observability-content copy, and agreed it should be its own
change once that one was archived.

## What Changes

- **Add a "Get Docker" section to Getting started** (`platform/dotnet-10-and-cli.md`). It sits
  between the dotnetup install sections and "Pin the SDK with global.json". It says which primer pages
  need Docker (none of exercises 01–06 do), and then covers each platform:
  - **Windows, with WSL 2** (the default path):
    - install WSL 2 and an Ubuntu distro;
    - turn on systemd;
    - install **Docker Engine** from Docker's apt repository;
    - add the user to the `docker` group;
    - make the daemon listen on `tcp://127.0.0.1:2375` as well as its socket;
    - set `DOCKER_HOST=tcp://localhost:2375` on Windows, so .NET on Windows (Testcontainers,
      and a Windows `docker` CLI if the reader adds one) reaches the engine in WSL.

    It says plainly that the TCP port has no authentication, which is why it is bound to loopback.
    **Docker Desktop**, with its WSL 2 backend, is the alternative for readers who don't need a
    subscription, or whose company has one.
  - **macOS:** **OrbStack** recommended, with **Docker Desktop** and **Colima** as alternatives. It
    notes which of them need a licence for work use.
  - **Linux:** Docker Engine from Docker's own repository (not the distro's `docker.io` package),
    and the `docker` group.
- **A Docker check** at the end of the section: `docker run --rm hello-world` and
  `docker info --format '{{.ServerVersion}}'`. On Windows there is one more: a check that Windows-side
  .NET can reach the engine through `DOCKER_HOST`. "Check your setup" at the end of the page stays as
  it is.
- **Pitfalls** for the Docker setup, added to the page's Pitfalls list. For example: the daemon isn't
  running after a WSL restart, `permission denied` on the socket before re-login, and Windows not
  seeing the engine.
- **Point the other Docker mentions at the new section:** the samples README's Docker prerequisite,
  the Logging page's dashboard paragraph, and `docs/dependencies.md`'s install hint.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `topic-page-content`: a new requirement that the Getting started page explains how to get Docker
  on Windows (Docker Engine in WSL 2, reachable from Windows), macOS and Linux, notes the licences,
  and gives a check.

## Impact

- **Site:**
  - `site/src/content/docs/platform/dotnet-10-and-cli.md`: the new section and its pitfalls;
  - `site/src/content/docs/observability/logging.md`: one link;
  - the frontmatter `learn:` links.
- **Docs:** `samples/README.md` (the Docker prerequisite) and `docs/dependencies.md` (the install
  hint).
- **No code changes:** no sample or test changes. `site/tests/dist.test.ts` needs no change, because
  the page is already full content.
- **Verification is by hand.** Nothing in CI runs Windows or WSL, and this Mac can't check the
  Windows path. A human (Emmz, or a Windows tester) has to run the Windows recipe. The Linux commands
  can be checked in a Linux VM or container on this Mac.
