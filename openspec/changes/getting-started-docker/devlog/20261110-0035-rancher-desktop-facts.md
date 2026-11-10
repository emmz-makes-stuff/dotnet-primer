# Rancher Desktop facts (task 7.1)

<!-- Section: 7 · Base: d46ccf7 -->

Emmz changed the default on 2026-11-10: "we should recommend Rancher Desktop for Windows, that's
free and will be more comfortable for developers who've never done anything there wasn't a menu
item for in Visual Studio". She then chose:
- Rancher Desktop on **both** Windows and macOS;
- to **keep** Docker Engine in WSL 2 as an advanced Windows option;
- **not** to install Rancher Desktop on this Mac. So the macOS Rancher steps come from the docs, and
  the "not yet tested" note covers them.

## Sources, checked 2026-11-10

- **Licence:** Apache License 2.0 (the `LICENSE` in `rancher-sandbox/rancher-desktop`).
- **Latest release:** **v1.25.0**, published 2026-11-05, from the GitHub API. Its assets include
  `Rancher.Desktop.Setup.1.25.0.msi`, `Rancher.Desktop-1.25.0.aarch64.dmg` and
  `Rancher.Desktop-1.25.0.x86_64.dmg`.
- **Installation** (<https://docs.rancherdesktop.io/getting-started/installation/>):
  - **Windows requirements:** Windows 11 (Home included) or Windows Server 2025, virtualization, and
    an internet connection. "Rancher Desktop requires Windows Subsystem for Linux on Windows. This
    must be installed prior to running the Rancher Desktop installer." 8 GB and 4 CPUs are
    recommended.
  - **Admin rights** are needed to install the "Rancher Desktop Privileged Service". Without it,
    services are exposed on 127.0.0.1 only.
  - **The Windows install:** download `Rancher.Desktop.Setup.X.Y.Z.msi` from the GitHub releases
    page, run it, and agree to the licence. "Installing for everyone is preferred in order to
    install the Rancher Desktop Privileged Service."
  - **The macOS install:** macOS 13 (Ventura) or later, Apple silicon or Intel. Download
    `Rancher.Desktop-X.Y.Z.dmg`, drag it to Applications, and launch it. "The recommended
    installation method is via the DMG from GitHub. The Homebrew formula is not maintained by the
    Rancher Desktop team." So the page uses the DMG, not `brew`.
  - **Bundled tools:** Helm, kubectl, nerdctl, Moby (docker) and Docker Compose.
- **Container engine**
  (<https://docs.rancherdesktop.io/ui/preferences/container-engine/general/>): "containerd …
  nerdctl or dockerd (moby) which enables the Docker API and the use of the Docker CLI. Only one
  container runtime will function at a time."
- **Kubernetes** (<https://docs.rancherdesktop.io/ui/preferences/kubernetes/>): "By default,
  Kubernetes is enabled." Unticking **Enable Kubernetes** runs "just containerd or dockerd by
  itself for reduced resource consumption".
- **Testcontainers**
  (<https://docs.rancherdesktop.io/how-to-guides/using-testcontainers/>):
  - **Windows, native Win32:** `$env:DOCKER_HOST = "npipe:////./pipe/docker_engine"`. "No other
    environment variables are needed. Container ports are accessible on localhost."
  - **macOS:** three variables, `DOCKER_HOST="unix://$HOME/.rd/docker.sock"`,
    `TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/var/run/docker.sock` and
    `TESTCONTAINERS_HOST_OVERRIDE=$(rdctl info --field ip-address)`. These need the VZ VM type,
    which is the macOS default.
- **Administrative Access**
  (<https://docs.rancherdesktop.io/ui/preferences/application/general/>): it "allows Rancher Desktop
  to create the Docker socket at the default location: /var/run/docker.sock. Without this access,
  the socket is instead created at ~/.rd/docker.sock and is accessible via the rancher-desktop
  Docker context."
- **FAQ** (<https://docs.rancherdesktop.io/faq/>): `brew install rancher` fails if Homebrew already
  manages docker, kubectl, helm or nerdctl. That's another reason to use the DMG.

## Planning artifacts updated

- **The delta spec** replaces the Windows requirement with three:
  - "Windows and macOS readers get Rancher Desktop by default";
  - "Advanced Windows path without a desktop app";
  - "Untested steps say so and invite reports" (one note per platform).
- **The design:** D2 is relabelled as the advanced path, D3 is revised, D9 is new, and the "untested"
  risk is widened.
- **The proposal:** What Changes.
- **`tasks.md` 7.2:** one note per platform, each with its own issue title. This replaces my first
  idea of one generic "Docker setup: " title, to match the spec's "near the top of that platform's
  part".

`openspec validate getting-started-docker --strict` passes.

## Next

Task 7.2: rewrite the Windows and macOS parts of `/platform/docker/`.
