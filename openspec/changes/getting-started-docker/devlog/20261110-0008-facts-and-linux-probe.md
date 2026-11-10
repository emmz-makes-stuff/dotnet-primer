# Facts check and Linux probe (tasks 1.1, 1.2)

<!-- Section: 1 · Base: df92b93 -->

## 1.1 Sources, checked 2026-11-10

- **Docker, "Install Docker Engine on Ubuntu"**
  (<https://docs.docker.com/engine/install/ubuntu/>, 200):
  - **The apt steps**, used verbatim in 1.2:
    - `sudo apt update`, `sudo apt install ca-certificates curl`;
    - `sudo install -m 0755 -d /etc/apt/keyrings`;
    - `sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc`,
      then `sudo chmod a+r /etc/apt/keyrings/docker.asc`;
    - the deb822 source written with `sudo tee /etc/apt/sources.list.d/docker.sources <<EOF`
      (Types / URIs / Suites from `UBUNTU_CODENAME` / Components stable / Architectures from
      `dpkg --print-architecture` / Signed-By). This is new: it replaces the old one-line
      `docker.list`;
    - `sudo apt update`;
    - `sudo apt install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin`.
  - **Conflicting packages** to remove first: `docker.io`, `docker-compose`, `docker-compose-v2`,
    `docker-doc`, `docker-buildx`, `podman-docker`, `containerd` and `runc`. Docker gives a one-line
    `apt remove $(dpkg --get-selections …)` for them. The page links to it, and doesn't copy it.
  - **The current version** in the repo is 5:29.9.3.
- **Docker, "Configure remote access for Docker daemon"**
  (<https://docs.docker.com/engine/daemon/remote-access/>, 200):
  - It shows the systemd override, `ExecStart=` then `ExecStart=/usr/bin/dockerd -H fd:// -H tcp://127.0.0.1:2375`,
    through `systemctl edit docker.service`.
  - It warns that remote access without TLS "is **not recommended**, and will require explicit
    opt-in in a future release".
  - **Our drop-in keeps `--containerd=…`**, as the packaged unit does (see 1.2).
- **Docker Desktop licence**: the page has moved to
  <https://docs.docker.com/subscription-billing/desktop-license/>. The old `/subscription/` URL
  301s, so the site links to the new one. It's free for small businesses ("fewer than 250 employees
  AND less than $10 million in annual revenue"), personal use, education and non-commercial open
  source. A paid subscription is needed for "professional use in larger organizations",
  **government entities**, and commercial use beyond the free limits.
- **OrbStack licensing** (<https://docs.orbstack.dev/licensing>, 200): "Freelance, business, and
  commercial users of OrbStack must purchase a license for each user." The ToS also covers
  non-profit and government entities, and anyone who generates more than $10,000/year from work
  related to using OrbStack. Non-commercial educational use is free. There's a 30-day Pro trial.
- **Colima** (<https://github.com/abiosoft/colima>, 200): `brew install colima`. The Docker
  client comes separately with `brew install docker`. Then `colima start`, after which "you can
  use the `docker` client on macOS … with no additional setup".
- **WSL install** (<https://learn.microsoft.com/windows/wsl/install>; Learn 302s to `/en-us/`, the
  locale redirect the site's other Learn links already accept): `wsl --install`, then
  `-d <Distribution>`. The page was last updated on 2025-08-06.
- **WSL systemd** (<https://learn.microsoft.com/windows/wsl/systemd>): "Systemd is now the default
  for the current version of Ubuntu that will be installed using the `wsl --install` command". For
  other distros: WSL 0.67.6 or later, `[boot]` `systemd=true` in `/etc/wsl.conf`, then
  `wsl.exe --shutdown`.
- **WSL networking** (<https://learn.microsoft.com/windows/wsl/networking>): "you can access it
  from a Windows app … using localhost (just like you normally would)". That's NAT mode, the
  default. Mirrored mode (`networkingMode=mirrored`, Windows 11 22H2 and later) also supports
  localhost. **The docs don't say whether this covers a port bound only to 127.0.0.1 in WSL.** The
  Windows human check (4.1) has to confirm it.
- Docker's `desktop/features/wsl/` and `engine/install/linux-postinstall/` pages both return 200,
  for linking.

## 1.2 Linux probe

This ran in a throwaway OrbStack machine, `docker-probe` (Ubuntu 24.04.5, arm64, isolated, systemd
"degraded", which is normal in a container-like VM). It has since been deleted.

- **The apt steps (1.1), verbatim:** the source came out as `Suites: noble`,
  `Architectures: arm64`. After install, `docker` was **active** and **enabled**, and the
  packaged unit is `ExecStart=/usr/bin/dockerd -H fd:// --containerd=/run/containerd/containerd.sock`.
- **`sudo usermod -aG docker $USER`:** the same shell then got
  `permission denied while trying to connect to the docker API at unix:///var/run/docker.sock`,
  which is the pitfall text. A fresh login listed `docker` in `id -nG`.
- **The drop-in** at `/etc/systemd/system/docker.service.d/override.conf`:
  ```
  [Service]
  ExecStart=
  ExecStart=/usr/bin/dockerd -H fd:// -H tcp://127.0.0.1:2375 --containerd=/run/containerd/containerd.sock
  ```
  `daemon-reload` and `restart` exit 0. The journal shows `API listen on 127.0.0.1:2375` and
  `API listen on /run/docker.sock`, and `ss` shows `127.0.0.1:2375` LISTEN. **No TLS flag was
  needed on Engine 29.9.3**, but it warns:
  - "Binding to IP address without --tlsverify is insecure and gives root access on this machine to
    everyone who has access to your network."
  - "Binding to an IP address, even on localhost, can also give access to scripts run in a browser."
  - "[DEPRECATION NOTICE] In future versions this will be a hard failure preventing the daemon from
    starting!"
- **D5 checks:**
  - `docker run --rm hello-world` prints "Hello from Docker!";
  - `docker info --format '{{.ServerVersion}}'` prints `29.9.3`;
  - `curl -s http://127.0.0.1:2375/version` returns JSON with `"Version":"29.9.3"`;
  - after `sudo systemctl restart docker`, both endpoints come back.
- **Pitfall texts captured:**
  - With the daemon stopped: `Cannot connect to the Docker daemon at unix:///var/run/docker.sock. Is the docker daemon running?`
  - With `hosts` also in `/etc/docker/daemon.json`: the restart fails, and the journal says
    `unable to configure the Docker daemon with file /etc/docker/daemon.json: the following directives are specified both as a flag and in the configuration file: hosts: (from flag: [fd:// tcp://127.0.0.1:2375], from file: [...])`.
  - A gotcha for testing this: repeated quick restarts trip systemd's `start-limit-hit`. Run
    `systemctl reset-failed docker` before retrying. This is not a page pitfall.
- **macOS (this machine, OrbStack):** `hello-world` prints "Hello from Docker!", and
  `docker info` prints `29.8.2` (context `orbstack`).

The design was updated to match: D2 step 5 no longer needs a TLS flag, the D6 pitfall uses the real
error text, and there's a new risk for a future Engine that refuses TCP without TLS.

## Next

Section 2, task 2.1: write `## Get Docker` on the Getting started page, using the commands above.
