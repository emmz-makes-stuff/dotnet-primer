---
title: "Docker"
description: "Get a Docker engine for the primer's containers: Rancher Desktop on Windows and macOS, Docker Engine on Linux, and how to check it works."
learn:
  - title: "Install WSL"
    url: https://learn.microsoft.com/windows/wsl/install
---

By the end of this page you'll have a Docker engine running on your machine, and you'll have checked that it runs containers. On Windows, you'll also have checked that Windows-side tools such as .NET can reach it.

The primer runs some tools in **containers**, so you need a Docker engine: the [Observability](/observability/logging/#see-it-locally) pages run a local dashboard in one, the [EF Core](/data/ef-core/) page and exercise 07's tests run SQL Server in one, the [Testing](/testing/) pages start real databases with Testcontainers, and [Containers](/deploy/containers/) builds and runs images. Exercises 01–06 don't need it, so you can skip this until you reach Observability.

Pick your platform. Each one has a default that's free to use at work, and the alternatives say which licence they need. Licence terms are as they were at the time of writing: check each vendor's own terms.

## Windows

On Windows, install [**Rancher Desktop**](https://rancherdesktop.io/). It's free and open source (Apache 2.0) for any use, it's set up from a settings window rather than the command line, and it runs its engine in WSL 2, the Windows Subsystem for Linux. You get `docker` in PowerShell, and Windows tools such as .NET reach the engine directly.

:::note[Not yet tested end to end on Windows]
These steps come from Rancher Desktop's and Microsoft's docs, and the advanced steps below have been checked on Linux, but nobody has run them all on a Windows machine yet. If a step doesn't work for you, please [open an issue](https://github.com/emmz-makes-stuff/dotnet-primer/issues/new?title=Docker+on+Windows%3A+&labels=content&body=Page%3A+https%3A%2F%2Fdotnet-primer.emmz.dev%2Fplatform%2Fdocker%2F%23windows%0A%0AWindows+version+%28from+%60winver%60%29%3A%0ARancher+Desktop+version+%28Help+%3E+About%29%2C+or+for+the+advanced+steps+the+WSL+version+%28from+%60wsl+--version%60%29%3A%0AThe+step+that+failed%3A%0AThe+command%2C+and+its+output%3A%0A) with the step that failed and what it printed. The link fills in the details to send.
:::

1. In PowerShell **as administrator**, install WSL, then restart if Windows asks you to. Rancher Desktop needs WSL installed first, but it brings its own Linux distro:

   ```powershell
   wsl --install --no-distribution
   ```

2. Download `Rancher.Desktop.Setup.X.Y.Z.msi` from Rancher Desktop's [latest release](https://github.com/rancher-sandbox/rancher-desktop/releases/latest) and run it. Choose to install it **for everyone** when asked: that also installs its privileged service, which needs an administrator.

3. Open **Rancher Desktop**. In **Preferences**, set **Container Engine** to **dockerd (moby)**, which is the engine `docker` talks to, and on the **Kubernetes** page untick **Enable Kubernetes**, which the primer doesn't use and which costs memory. Select **Apply**, and wait until Rancher Desktop says it's ready. If a first-run window asks the same questions, make the same choices there.

4. In a normal PowerShell, tell Windows tools where the engine is, then open a new terminal so it sees the variable. This is the setting Rancher Desktop's [Testcontainers guide](https://docs.rancherdesktop.io/how-to-guides/using-testcontainers/) gives:

   ```powershell
   [Environment]::SetEnvironmentVariable("DOCKER_HOST", "npipe:////./pipe/docker_engine", "User")
   ```

Rancher Desktop has to be running for `docker` to work: you can make it start when you log in, under **Preferences**, **Application**. The ports a container publishes are on Windows' `localhost`, so the Observability dashboard opens in your browser at `http://localhost:18888`, and an app you `dotnet run` sends it telemetry on `localhost:4317`.

**Alternative: Docker Desktop.** [Docker Desktop](https://docs.docker.com/desktop/features/wsl/) works the same way, with its engine in WSL 2 and `docker` on Windows' `PATH`, and needs no `DOCKER_HOST`. If you set one for Rancher Desktop, remove it with `[Environment]::SetEnvironmentVariable("DOCKER_HOST", $null, "User")`, then open a new terminal. It's free for personal use, education, non-commercial open source, and companies with fewer than 250 employees **and** less than $10 million in annual revenue. Larger companies and government entities need a paid subscription: see Docker's [licence terms](https://docs.docker.com/subscription-billing/desktop-license/).

<details id="docker-engine-in-wsl-2">
<summary>Advanced: Docker Engine in WSL 2, without a desktop app</summary>

If you'd rather not run a desktop app, run **Docker Engine** inside a Linux distro in [WSL 2](https://learn.microsoft.com/windows/wsl/install), the Windows Subsystem for Linux. Docker Engine is free for any use. .NET stays on Windows, and reaches the engine over a local port.

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

   The engine logs warnings that this port isn't encrypted, and that a future version will refuse to start without TLS. See the caution below. If `systemctl restart docker` fails instead, see the last item in [Pitfalls](#pitfalls).

6. In a normal PowerShell, point Windows tools at that port, then open a new terminal so it sees the variable. WSL forwards Windows' `localhost` to the distro, so `localhost:2375` on Windows reaches the engine:

   ```powershell
   [Environment]::SetEnvironmentVariable("DOCKER_HOST", "tcp://localhost:2375", "User")
   ```

   Tools that talk to Docker from Windows read `DOCKER_HOST`, Testcontainers among them. This replaces any `DOCKER_HOST` you set for Rancher Desktop.

:::caution[The port has no authentication]
Anything that can reach `localhost:2375` can control Docker, and through it become root in your Ubuntu distro. That includes every program you run, and possibly scripts in a web page. Keep it bound to `127.0.0.1` exactly as above: never `0.0.0.0`, and never on a machine that other people use. If that's not acceptable where you work, use Rancher Desktop instead (above).
:::

Run `docker` commands, such as the Observability dashboard's `docker run`, in the **Ubuntu terminal**. The ports a container publishes are forwarded to Windows too, so the dashboard opens in your Windows browser at `http://localhost:18888`, and an app you `dotnet run` on Windows sends it telemetry on `localhost:4317`. If you want `docker` in PowerShell as well, Docker's [static binaries](https://docs.docker.com/engine/install/binaries/) include a Windows `docker.exe` that uses the same `DOCKER_HOST`.

Check this setup with [Check Docker](#check-docker) in the Ubuntu terminal. Then check that Windows tools reach the engine: in a new PowerShell, this prints the same version as `docker info`:

```powershell
(Invoke-RestMethod http://localhost:2375/version).Version
```

</details>

## macOS

Install [**Rancher Desktop**](https://rancherdesktop.io/), the same free app as on Windows.

:::note[Not yet tested end to end on macOS]
These steps come from Rancher Desktop's docs, and nobody has run them on a Mac for this page yet. If a step doesn't work for you, please [open an issue](https://github.com/emmz-makes-stuff/dotnet-primer/issues/new?title=Docker+on+macOS%3A+&labels=content&body=Page%3A+https%3A%2F%2Fdotnet-primer.emmz.dev%2Fplatform%2Fdocker%2F%23macos%0A%0AmacOS+version+%28from+%60sw_vers%60%29%3A%0ARancher+Desktop+version+%28Rancher+Desktop+%3E+About%29%3A%0AThe+step+that+failed%3A%0AThe+command%2C+and+its+output%3A%0A) with the step that failed and what it printed. The link fills in the details to send.
:::

1. Download the `.dmg` from Rancher Desktop's [latest release](https://github.com/rancher-sandbox/rancher-desktop/releases/latest): `aarch64` for Apple silicon, `x86_64` for Intel. Open it, drag **Rancher Desktop** to **Applications**, and open it from there. It needs macOS 13 or later.
2. In **Preferences**, make the same choices as on Windows: **Container Engine** set to **dockerd (moby)**, and **Enable Kubernetes** unticked. Select **Apply**, and wait until it's ready.

`docker` then works in a new terminal. Rancher Desktop puts Docker's socket at `~/.rd/docker.sock` rather than the usual `/var/run/docker.sock`. The `docker` command finds it by itself, but some tools, Testcontainers among them, need more: turn on **Administrative Access** under **Preferences**, **Application**, so it creates `/var/run/docker.sock`, or follow Rancher Desktop's [Testcontainers guide](https://docs.rancherdesktop.io/how-to-guides/using-testcontainers/).

**Alternatives.** [**OrbStack**](https://orbstack.dev/download) (`brew install orbstack`) is quick and light, but it's only free for personal use: freelance, business and commercial use need a paid licence for each user, after a 30-day trial (see its [licensing](https://docs.orbstack.dev/licensing) page). **Docker Desktop** has the same terms as on Windows. [**Colima**](https://github.com/abiosoft/colima) is free and open source, with no app window: install it and the `docker` client with `brew install colima docker`, then run `colima start`. The `docker` command finds Colima by itself, but some tools look for Docker's socket at `/var/run/docker.sock`, where Colima doesn't put it. Point them at Colima's socket in your shell profile:

```sh
export DOCKER_HOST="unix://$HOME/.colima/default/docker.sock"
```

**Images for x64 only, on Apple silicon.** Some images the primer uses are published for x64 (`linux/amd64`) only, such as SQL Server on the [EF Core](/data/ef-core/) page. On an Apple silicon Mac they run under Rosetta emulation, which your tool may need switching on. In Rancher Desktop, under **Preferences**, **Virtual Machine**, **Emulation**, keep the **VZ** type (the default), tick **Enable Rosetta support**, and select **Apply**. OrbStack needs no setting. In Docker Desktop, turn on **Use Rosetta for x86_64/amd64 emulation on Apple Silicon**, which is off by default. Start Colima with `colima start --vm-type=vz --vz-rosetta`. Microsoft supports SQL Server's images on x86-64 only, and says emulation such as Rosetta 2 [isn't tested or supported](https://learn.microsoft.com/sql/linux/install-upgrade/quickstart-install-docker): it's fine for the exercises, but not for anything you rely on. On an Intel Mac, there's nothing to do.

## Linux

Install **Docker Engine** from Docker's own repository, using [its install page for your distro](https://docs.docker.com/engine/install/). On Ubuntu, that's the commands in step 3 of the [advanced Windows steps](#docker-engine-in-wsl-2). Don't use your distro's `docker.io` package, which lags behind Docker's releases: Docker's page says how to remove it first.

Then let your user run `docker`, and log out and back in:

```sh
sudo usermod -aG docker $USER
sudo systemctl enable --now docker   # most distros already did this when it installed
```

Membership of the `docker` group is the same as root on that machine, so only add your own account.

## Check Docker

In PowerShell on Windows, or your terminal on macOS or Linux (on the advanced Windows path, the Ubuntu terminal):

```sh
docker run --rm hello-world                 # prints "Hello from Docker!" and some text
docker info --format '{{.ServerVersion}}'   # prints the engine's version, such as 29.9.3
```

## Pitfalls

- **`docker` isn't found, or only `nerdctl` works, with Rancher Desktop.** It's using the containerd engine. Fix: in **Preferences**, set **Container Engine** to **dockerd (moby)**, then **Apply**.
- **`permission denied while trying to connect to the docker API at unix:///var/run/docker.sock`** (on Linux, or the advanced Windows path). Your user isn't in the `docker` group yet, or the terminal was opened before you added it. Fix: `sudo usermod -aG docker $USER`, then log in again. In WSL, that means `wsl --shutdown` in PowerShell and reopening Ubuntu.
- **`Cannot connect to the Docker daemon at unix:///…/docker.sock. Is the docker daemon running?`**, or on Windows **`open //./pipe/docker_engine: The system cannot find the file specified`**. The engine isn't running. The path in the message says which engine `docker` looked for: `~/.rd/docker.sock` or the `docker_engine` pipe is Rancher Desktop, and `/var/run/docker.sock` is Docker Engine (or Rancher Desktop on macOS with Administrative Access). Fix: with Rancher Desktop, open it and wait until it's ready. On Linux, `sudo systemctl enable --now docker`. On the advanced Windows path, reopen Ubuntu, because WSL stops a distro that has been idle for a while, and systemd starts the engine again. If it still isn't running, `sudo systemctl status docker` says why: if it failed after you added the port, see the last item below.
- **Windows tools can't find Docker.** `DOCKER_HOST` isn't set in that terminal, it still names an engine you've stopped using, or the engine isn't running. Fix: open a new terminal, and start Rancher Desktop (on the advanced path, open Ubuntu: WSL stops a distro that has been idle for a while). Then run [Check Docker](#check-docker) in PowerShell.
- **The engine won't start after you add the port** (the advanced Windows path). `sudo systemctl restart docker` fails. Fix: `journalctl -u docker` says why. If it says "the following directives are specified both as a flag and in the configuration file: hosts", remove `hosts` from `/etc/docker/daemon.json`, because the port belongs in the override file only. If it says that binding without TLS is no longer allowed, you have a newer engine than this page was written for: follow the [remote access](https://docs.docker.com/engine/daemon/remote-access/) page, or use Rancher Desktop.
