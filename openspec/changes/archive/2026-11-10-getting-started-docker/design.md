# Design

## Context

- **The page:** `platform/dotnet-10-and-cli.md` is full content. Its sections run: modern .NET,
  SDK/runtime/TFM, "Install .NET with dotnetup" (with its subsections), "Pin the SDK with
  global.json", the project file, the CLI loop, Pitfalls, and "Check your setup". The page has no
  Docker content today, apart from `-t:PublishContainer` in the CLI loop.
- **Where the site uses Docker:**
  - today: the Observability pages' dashboard (`docker run … aspire-dashboard:13.6`, ports 18888,
    4317 and 4318);
  - planned: the Testing pages (Testcontainers), Containers, and the data pages.

  `samples/README.md` and `docs/dependencies.md` both say "Docker Desktop or OrbStack".
- **Decisions Emmz made** (2026-11-09):
  - **Windows:** Docker Engine inside a WSL 2 distro is the default, with Docker Desktop mentioned
    for readers who don't need a licence or can pay for one. .NET stays on **Windows**, and reaches
    the engine through `DOCKER_HOST` over TCP.
  - **macOS:** OrbStack recommended, with Docker Desktop and Colima as alternatives.
  - **Linux:** covered too.
  - **Checks:** the Docker section gets its own check, and "Check your setup" stays as it is.
- **Licences** (as checked on 2026-11-09; D7 rechecks them when the page is written):
  - **Docker Desktop** is free for personal use, education, non-commercial open source, and
    companies with fewer than 250 employees **and** less than $10M annual revenue. Above that, it
    needs a paid subscription.
  - **OrbStack** is free for personal and non-commercial educational use. Freelance, business and
    commercial use need a paid licence for each user.
  - **Docker Engine** (Apache 2.0) and **Colima** (MIT) are free for any use.

## Goals / Non-Goals

**Goals:**
- Every reader can get a working Docker engine on their platform, and confirm it.
- On Windows, the engine also works for Windows-side .NET: Testcontainers, and whatever later pages
  need.

**Non-Goals:**
- Rootless Docker, Podman, Rancher Desktop, and Kubernetes.
- Windows containers. The primer only uses Linux containers.
- A Windows `docker.exe`. The reader runs `docker` commands in the WSL terminal. D3 mentions the
  optional static CLI in one line.
- Setting up Testcontainers itself. The Testing pages own that, and this section only makes it
  possible.
- Changing "Check your setup", the samples, or the site tests.

## Decisions

### D1. Placement and shape

**Revised in §5 (2026-11-10):** Docker is its own page, `/platform/docker/`, titled "Docker". It is
listed in the Getting started group straight after ".NET 10 and the dotnet CLI". Emmz asked for this
while she reviewed the §2 copy, which reverses the rejection below. The .NET 10 page keeps a short
`## Get Docker` pointer. **Moved in §8:** it was where the section had been, after the dotnetup
sections. It is now the page's last section, after `## Check your setup`, where a reader moves on
(a code-review finding that Emmz accepted).

The page follows the topic-page structure:
- an outcome sentence;
- what Docker is used for in the primer, with links to the pages that need it, and "exercises 01–06
  don't need it; set it up before Observability";
- `## Windows`, `## macOS`, `## Linux` and `## Check Docker` (the Windows heading was "Windows
  (WSL 2)" until §7);
- `## Pitfalls`, holding the four Docker pitfalls (D6), which leave the .NET 10 page.

Each platform part gives a **default path** as numbered, copyable steps, then the alternatives in
one short paragraph with their licence terms.

**Superseded first version:** a `## Get Docker` section on the .NET 10 page, after the dotnetup
sections. The separate page was considered then and rejected, to keep machine setup in one place.
Emmz chose the separate page once she had read the section.

### D2. Windows: Docker Engine in WSL 2 (the advanced path since §7)

**Revised in §7:** this is no longer the default. Rancher Desktop is (D9). These steps stay on the
page in a collapsed `<details>` block, "Advanced: Docker Engine in WSL 2, without a desktop app", for
readers who want no GUI. The steps, the caution and the dashboard paragraph are unchanged inside it.

These are the steps, run in this order:

1. **WSL 2 and Ubuntu.** `wsl --install -d Ubuntu` in an administrator PowerShell, then a restart
   if Windows asks for one. Then create the Linux user.
2. **systemd.** New Ubuntu distros on current WSL have systemd turned on already. The page checks
   with `systemctl is-system-running` (any answer but "offline" will do). If it isn't on, the page
   shows the `/etc/wsl.conf` `[boot]` `systemd=true` lines, then `wsl --shutdown`.
3. **Docker Engine** from Docker's apt repository, exactly as in Docker's "Install Docker Engine on
   Ubuntu" page: the keyring, the source, then `docker-ce`, `docker-ce-cli`, `containerd.io`,
   `docker-buildx-plugin` and `docker-compose-plugin`. The page links to Docker's page as the source
   of truth. We don't use the `get-docker.sh` convenience script, which Docker itself says isn't for
   production and which warns under WSL.
4. **The `docker` group.** `sudo usermod -aG docker $USER`, then `wsl --shutdown` and reopen. The
   page says that membership is root-equivalent on that distro.
5. **The TCP endpoint for Windows.** A systemd drop-in,
   `/etc/systemd/system/docker.service.d/override.conf`, that clears `ExecStart` and starts
   `dockerd -H fd:// -H tcp://127.0.0.1:2375 --containerd=/run/containerd/containerd.sock`. Then
   `systemctl daemon-reload` and `systemctl restart docker`.
   - **Probed in §1** on Engine 29.9.3: no extra flag is needed. dockerd logs `[DEPRECATION NOTICE]`
     warnings ("In future versions this will be a hard failure preventing the daemon from
     starting!"). The page says so, and says what to check if a later Engine refuses to start.
   - **Why a drop-in, not `daemon.json`'s `hosts`:** the packaged unit already passes `-H fd://`, and
     dockerd refuses to start when `hosts` is set in both places.
6. **`DOCKER_HOST` on Windows.** In PowerShell:
   `[Environment]::SetEnvironmentVariable("DOCKER_HOST", "tcp://localhost:2375", "User")`, then open
   a new terminal. WSL 2 forwards Windows' `localhost` to ports that WSL listens on at loopback, so
   Windows reaches the daemon at `localhost:2375`. Testcontainers for .NET reads `DOCKER_HOST`.

**Security**, stated on the page in a `:::caution`:
- The TCP endpoint has **no authentication**. Anything running as you on the machine can control
  Docker, and so become root in the distro.
- It listens on **loopback only**: never `0.0.0.0`, and never on a machine that others share.

**The dashboard on Windows:** the reader runs the Logging page's `docker run` in the WSL terminal.
The published ports (18888, 4317 and 4318) are forwarded to Windows `localhost`. So the Windows
browser opens `http://localhost:18888`, and an app run on Windows exports to `localhost:4317`. The
Logging page needs no change, apart from its link to this section.

**Docker Desktop alternative:** one paragraph. It has a WSL 2 backend, it puts `docker` on Windows'
`PATH`, and Windows .NET finds it without `DOCKER_HOST`. It needs a subscription above the
small-business limits. A reader who uses it skips steps 3–6.

**Alternatives considered:**
- **Do the primer inside WSL** (dotnet in WSL, too). Emmz preferred keeping .NET on Windows.
- **Expose the daemon through `wsl.exe docker` wrappers or an npiperelay socket bridge.** These are
  more moving parts than one loopback TCP port, and Testcontainers' support for them is uneven.
- **TLS on the TCP endpoint.** This is secure, but the certificate setup is long. Loopback-only
  binding makes the risk acceptable on a single-user development machine, and the caution says so.

### D3. macOS

**Revised in §7:** the default is now **Rancher Desktop** (D9). OrbStack moves into the
alternatives, with its licence terms.

- **Default: Rancher Desktop**, from the `.dmg` on its GitHub releases page (Apple silicon `aarch64`
  or Intel `x86_64`), with the same first-run choices as on Windows (D9).
- **Alternatives, one paragraph:**
  - **OrbStack:** `brew install orbstack`. It's free for personal use only, and freelance or work use
    needs a paid licence.
  - **Docker Desktop:** free under the small-business limits, and a subscription above them.
  - **Colima:** free and open source. Install it with `brew install colima docker`, then run
    `colima start`.

  For Colima, the page says that tools which look for `/var/run/docker.sock` may need
  `DOCKER_HOST` pointed at Colima's socket. Only a pointer: the Testing pages own the details.
- **The optional Windows CLI** (from D2): one line in the Windows part says a static `docker.exe`
  from Docker's download site can use the same `DOCKER_HOST`, for readers who want `docker` in
  PowerShell.

### D4. Linux

Docker Engine from Docker's own repository, linked to Docker's per-distro install pages. Ubuntu's
commands are shown, the same as Windows step 3. Then the `docker` group, with the same
root-equivalence note. The page says not to use the distro's `docker.io` package, because it lags
behind Docker's releases. Most distros start the daemon with systemd when it is installed.
`sudo systemctl enable --now docker` covers the rest.

### D5. The check

`### Check Docker`, for all platforms:

```sh
docker run --rm hello-world               # prints "Hello from Docker!"
docker info --format '{{.ServerVersion}}' # prints the engine version, e.g. 29.x.y
```

Then, for Windows only, in PowerShell:
`(Invoke-RestMethod http://localhost:2375/version).Version`, which prints the same version. That
proves Windows-side tools reach the engine through the same endpoint `DOCKER_HOST` names, with no
Windows `docker.exe` needed.

### D6. Pitfalls

These go in the Docker page's Pitfalls section (§5; first written on the .NET 10 page), in the
site's "problem. Fix:" style:
- **`permission denied … /var/run/docker.sock`:** you're not in the `docker` group yet, or the
  terminal predates it. Fix: `usermod`, then a new login (on Windows, `wsl --shutdown`).
- **`Cannot connect to the Docker daemon`:** the daemon isn't running. On WSL, that usually means
  systemd isn't on. Fix: the systemd check from D2 step 2, then `sudo systemctl enable --now docker`.
- **Windows tools can't find Docker:** `DOCKER_HOST` isn't set in that terminal, or WSL isn't
  running. Fix: open a new terminal, start the distro, and run the D5 Windows check.
- **dockerd won't start after adding the TCP endpoint:** `hosts` is set in both `daemon.json` and
  the unit (dockerd says "the following directives are specified both as a flag and in the
  configuration file: hosts"), or a newer Engine refuses TCP without TLS. Fix: `journalctl -u docker`
  shows which. The endpoint belongs in the drop-in only.

### D7. Facts and links

When the page is written, check these sources, and record what they said in a devlog note:
- Docker's Ubuntu install page (the commands, verbatim);
- Microsoft's WSL install page and its systemd page;
- Docker's Desktop licence FAQ;
- OrbStack's licensing page;
- Colima's README.

Frontmatter `learn:` gains the WSL install page (the schema only accepts Microsoft Learn URLs, so
Docker's pages are linked in the body instead). Run the
links through the same redirect check as before: Learn URLs without a locale, and no redirects.

### D8. Verification

- **Linux and the WSL Linux side:** run the D2 steps 3–5 and D4 commands **verbatim** in a throwaway
  OrbStack Ubuntu machine (`orb create ubuntu …`), which has systemd like WSL. Then run D5's
  `docker` checks, and `curl http://127.0.0.1:2375/version` from inside it. This settles whether
  the current dockerd needs a non-TLS flag on the TCP endpoint (it has warned about non-TLS TCP
  since v26, and newer versions may refuse it without one). Delete the machine afterwards.
  - The OrbStack machine uses OrbStack, which is already installed and is listed under Docker in
    `docs/dependencies.md`. No new dependency.
- **Windows:** this Mac can't run WSL. The Windows recipe is a **human check**: Emmz, or a Windows
  tester she names, follows it on a Windows 11 machine. They report the output of D5's checks, and
  whether the dashboard opens in the Windows browser.
- **macOS:** the OrbStack path is this machine's own setup, so D5's checks run here. The Colima and
  Docker Desktop paths are only quoted from their docs, not run.
- **Site:** `make gates`, then the usual human check of the page in light and dark, the copy
  buttons, and search for "WSL".

### D9. Rancher Desktop as the Windows and macOS default (§7)

Emmz chose this on 2026-11-10. Rancher Desktop is free (Apache 2.0) and is "more comfortable for
developers who've never done anything there wasn't a menu item for in Visual Studio". It's a desktop
app with a settings window, it puts `docker` on the `PATH`, and on Windows it serves the standard
`docker_engine` named pipe.

- **Windows:**
  1. WSL first: `wsl --install` in an administrator PowerShell, then restart. Rancher Desktop
     requires WSL "prior to running the Rancher Desktop installer".
  2. The `Rancher.Desktop.Setup.X.Y.Z.msi` from the GitHub releases page, installed "for everyone",
     so its privileged service is installed too. That needs admin rights.
  3. On first run, choose **dockerd (moby)** as the container engine, and untick
     **Enable Kubernetes**. Kubernetes is on by default, and the primer doesn't use it.
  4. `DOCKER_HOST=npipe:////./pipe/docker_engine` as a user environment variable, as Rancher
     Desktop's Testcontainers guide says ("No other environment variables are needed. Container
     ports are accessible on localhost").
- **macOS:** the same first-run choices. For Testcontainers, the page points to Rancher Desktop's
  guide, which offers **Administrative Access** (it creates `/var/run/docker.sock`) or three
  variables (`DOCKER_HOST=unix://$HOME/.rd/docker.sock`, `TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE`,
  `TESTCONTAINERS_HOST_OVERRIDE`). The page doesn't copy them: the Testing pages will own that.
- **The check:** the D5 `docker` commands, run in PowerShell or the macOS terminal. The
  `Invoke-RestMethod` check stays in the advanced block only.
- **Not tested:** nothing was installed on this Mac (Emmz's choice), so the "not yet tested" note
  covers Rancher Desktop on Windows and macOS, and the advanced WSL steps: one note on each
  platform, each with its own prefilled issue title.

**Alternatives considered:** Docker Engine in WSL as the Windows default (the first version, now the
advanced path); OrbStack as the macOS default (paid for work use).

## Risks / Trade-offs

- **[The unauthenticated TCP endpoint]** → loopback only, a caution on the page, and the Docker
  Desktop alternative named for readers who can't accept it.
- **[WSL localhost forwarding doesn't reach a loopback-bound port]** (for example in mirrored
  networking mode, or after a WSL change) → the Windows human check tests it. If it fails, the
  fallback is binding to the WSL VM's address and putting that address in `DOCKER_HOST`. Stop and
  agree that with Emmz before writing it.
- **[Install commands drift]** (Docker's apt steps, WSL defaults) → the page links Docker's and
  Microsoft's pages as the source of truth, and the commands are checked verbatim on the day (D7, D8).
- **[Licence terms change]** → the page states them "at the time of writing", and links each
  vendor's terms.
- **[The recipes are untested on Windows and macOS]** (§4: no Windows tester; §7: nothing installed on
  this Mac for Rancher Desktop) → Emmz accepted shipping them. Since §7, the note opens the page's
  platform parts: one note on Windows (covering Rancher Desktop and the advanced WSL steps) and one on
  macOS. Each links a GitHub issue prefilled with its own title ("Docker on Windows: " or "Docker on
  macOS: "), the `content` label, and prompts for the OS and Rancher Desktop (or WSL) versions, the
  step and its output. Remove or soften a note once a run passes on that platform.
- **[A future Engine refuses TCP without TLS]** (Engine 29.9.3 warns that it will) → the D6
  "dockerd won't start" pitfall says to read `journalctl -u docker`. If that release lands while
  the page is live, it becomes a review fix: add the opt-in flag Docker names, or move to TLS.
- **[The daemon is stopped when WSL idles]** → WSL shuts a distro down after it has been idle for a
  while. With systemd, `docker` starts again as soon as the distro does, but Windows tools fail
  while the distro is down. The pitfall in D6 covers it.
