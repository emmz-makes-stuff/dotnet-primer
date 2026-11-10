---
title: "Docker"
description: "Get a Docker engine for the primer's containers: Docker Engine in WSL 2 on Windows, OrbStack on macOS, Docker Engine on Linux, and how to check it works."
learn:
  - title: "Install WSL"
    url: https://learn.microsoft.com/windows/wsl/install
---

By the end of this page you'll have a Docker engine running on your machine, and you'll have checked that it runs containers. On Windows, you'll also have checked that Windows-side tools such as .NET can reach it.

The primer runs some tools in **containers**, so you need a Docker engine: the [Observability](/observability/logging/#see-it-locally) pages run a local dashboard in one, the [Testing](/testing/) pages start real databases with Testcontainers, and [Containers](/deploy/containers/) builds and runs images. Exercises 01–06 don't need it, so you can skip this until you reach Observability.

Pick your platform. Each one has a default that's free to use at work, and the alternatives say which licence they need. Licence terms are as they were at the time of writing: check each vendor's own terms.

## Windows (WSL 2)

On Windows, run **Docker Engine** inside a Linux distro in [WSL 2](https://learn.microsoft.com/windows/wsl/install), the Windows Subsystem for Linux. Docker Engine is free for any use. .NET stays on Windows, and reaches the engine over a local port.

:::note[Not yet tested end to end on Windows]
These steps have been checked on Linux, but nobody has run all of them on a Windows machine yet. If a step doesn't work for you, please [open an issue](https://github.com/emmz-makes-stuff/dotnet-primer/issues/new?title=Docker+on+Windows%3A+&labels=content&body=Page%3A+https%3A%2F%2Fdotnet-primer.emmz.dev%2Fplatform%2Fdocker%2F%23windows-wsl-2%0A%0AWindows+version+%28from+%60winver%60%29%3A%0AWSL+version+%28from+%60wsl+--version%60%29%3A%0AThe+step+that+failed%3A%0AThe+command%2C+and+its+output%3A%0A) with the step that failed and what it printed. The link fills in the details to send.
:::

1. In PowerShell **as administrator**, install WSL 2 and Ubuntu. Restart if Windows asks you to, then open **Ubuntu** from the Start menu and create your Linux user:

   ```powershell
   wsl --install -d Ubuntu
   ```

2. In the Ubuntu terminal, check that systemd is running. Any answer except `offline` is fine, `degraded` included:

   ```sh
   systemctl is-system-running
   ```

   Ubuntu on current WSL has systemd on by default. If you see `offline`, add these lines to `/etc/wsl.conf` (`sudo nano /etc/wsl.conf`), run `wsl --shutdown` in PowerShell, and reopen Ubuntu:

   ```ini
   [boot]
   systemd=true
   ```

3. Install Docker Engine from Docker's own `apt` repository. These are the commands from Docker's [Install Docker Engine on Ubuntu](https://docs.docker.com/engine/install/ubuntu/) page, which is the source of truth if they change:

   ```sh
   # Add Docker's official GPG key:
   sudo apt update
   sudo apt install ca-certificates curl
   sudo install -m 0755 -d /etc/apt/keyrings
   sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
   sudo chmod a+r /etc/apt/keyrings/docker.asc

   # Add the repository to Apt sources:
   sudo tee /etc/apt/sources.list.d/docker.sources <<EOF
   Types: deb
   URIs: https://download.docker.com/linux/ubuntu
   Suites: $(. /etc/os-release && echo "${UBUNTU_CODENAME:-$VERSION_CODENAME}")
   Components: stable
   Architectures: $(dpkg --print-architecture)
   Signed-By: /etc/apt/keyrings/docker.asc
   EOF
   sudo apt update

   sudo apt install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
   ```

   The install starts the engine, and systemd starts it again whenever Ubuntu starts.

4. Let your user run `docker` without `sudo`, then restart WSL so the change takes effect: run `wsl --shutdown` in PowerShell, and reopen Ubuntu.

   ```sh
   sudo usermod -aG docker $USER
   ```

5. Make the engine listen on `127.0.0.1:2375` as well as its usual socket, so programs on Windows can reach it. This adds a systemd override for the `docker` service, then restarts it:

   ```sh
   sudo mkdir -p /etc/systemd/system/docker.service.d
   sudo tee /etc/systemd/system/docker.service.d/override.conf <<'EOF'
   [Service]
   ExecStart=
   ExecStart=/usr/bin/dockerd -H fd:// -H tcp://127.0.0.1:2375 --containerd=/run/containerd/containerd.sock
   EOF
   sudo systemctl daemon-reload
   sudo systemctl restart docker
   ```

   The engine logs warnings that this port isn't encrypted, and that a future version will refuse to start without TLS. See the caution below.

6. In a normal PowerShell, point Windows tools at that port, then open a new terminal so it sees the variable. WSL forwards Windows' `localhost` to the distro, so `localhost:2375` on Windows reaches the engine:

   ```powershell
   [Environment]::SetEnvironmentVariable("DOCKER_HOST", "tcp://localhost:2375", "User")
   ```

   Tools that talk to Docker from Windows read `DOCKER_HOST`, Testcontainers among them.

:::caution[The port has no authentication]
Anything that can reach `localhost:2375` can control Docker, and through it become root in your Ubuntu distro. That includes every program you run, and possibly scripts in a web page. Keep it bound to `127.0.0.1` exactly as above: never `0.0.0.0`, and never on a machine that other people use. If that's not acceptable where you work, use Docker Desktop instead (below).
:::

Run `docker` commands, such as the Observability dashboard's `docker run`, in the **Ubuntu terminal**. The ports a container publishes are forwarded to Windows too, so the dashboard opens in your Windows browser at `http://localhost:18888`, and an app you `dotnet run` on Windows sends it telemetry on `localhost:4317`. If you want `docker` in PowerShell as well, Docker's [static binaries](https://docs.docker.com/engine/install/binaries/) include a Windows `docker.exe` that uses the same `DOCKER_HOST`.

**Alternative: Docker Desktop.** [Docker Desktop](https://docs.docker.com/desktop/features/wsl/) runs its engine in WSL 2 for you, puts `docker` on Windows' `PATH`, and needs no `DOCKER_HOST`. If you use it, skip steps 3–6. It's free for personal use, education, non-commercial open source, and companies with fewer than 250 employees **and** less than $10 million in annual revenue. Larger companies and government entities need a paid subscription: see Docker's [licence terms](https://docs.docker.com/subscription-billing/desktop-license/).

## macOS

Install [**OrbStack**](https://orbstack.dev/download), open it once, and `docker` is on your `PATH`:

```sh
brew install orbstack
```

OrbStack is free for personal use. Freelance, business and commercial use need a paid licence for each user, after a 30-day trial: see its [licensing](https://docs.orbstack.dev/licensing) page.

**Alternatives.** **Docker Desktop** has the same terms as on Windows. [**Colima**](https://github.com/abiosoft/colima) is free and open source: install it and the `docker` client with `brew install colima docker`, then run `colima start`. The `docker` command finds Colima by itself, but some tools look for Docker's socket at `/var/run/docker.sock`, where Colima doesn't put it. Point them at Colima's socket in your shell profile:

```sh
export DOCKER_HOST="unix://$HOME/.colima/default/docker.sock"
```

## Linux

Install **Docker Engine** from Docker's own repository, using [its install page for your distro](https://docs.docker.com/engine/install/). On Ubuntu, that's the commands in step 3 of the [Windows steps](#windows-wsl-2). Don't use your distro's `docker.io` package, which lags behind Docker's releases: Docker's page says how to remove it first.

Then let your user run `docker`, and log out and back in:

```sh
sudo usermod -aG docker $USER
sudo systemctl enable --now docker   # most distros already did this when it installed
```

Membership of the `docker` group is the same as root on that machine, so only add your own account.

## Check Docker

In your Linux or macOS terminal (on Windows, the Ubuntu terminal):

```sh
docker run --rm hello-world                 # prints "Hello from Docker!" and some text
docker info --format '{{.ServerVersion}}'   # prints the engine's version, such as 29.9.3
```

On Windows, also check that Windows tools reach the engine. In a new PowerShell, this prints the same version:

```powershell
(Invoke-RestMethod http://localhost:2375/version).Version
```

## Pitfalls

- **`permission denied while trying to connect to the docker API at unix:///var/run/docker.sock`.** Your user isn't in the `docker` group yet, or the terminal was opened before you added it. Fix: `sudo usermod -aG docker $USER`, then log in again. In WSL, that means `wsl --shutdown` in PowerShell and reopening Ubuntu.
- **`Cannot connect to the Docker daemon at unix:///var/run/docker.sock. Is the docker daemon running?`** The engine isn't running. In WSL, that usually means systemd is off. Fix: run the systemd check from [step 2](#windows-wsl-2), then `sudo systemctl enable --now docker`.
- **Windows tools can't find Docker.** `DOCKER_HOST` isn't set in that terminal, or Ubuntu isn't running: WSL stops a distro that has been idle for a while. Fix: open a new terminal, open Ubuntu so the engine starts, and run the Windows check in [Check Docker](#check-docker).
- **The engine won't start after you add the port.** `sudo systemctl restart docker` fails. Fix: `journalctl -u docker` says why. If it says "the following directives are specified both as a flag and in the configuration file: hosts", remove `hosts` from `/etc/docker/daemon.json`, because the port belongs in the override file only. If it says that binding without TLS is no longer allowed, you have a newer engine than this page was written for: follow the [remote access](https://docs.docker.com/engine/daemon/remote-access/) page, or use Docker Desktop.
