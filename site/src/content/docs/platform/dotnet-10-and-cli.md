---
title: ".NET 10 and the dotnet CLI"
description: "The modern .NET platform, installing SDKs with dotnetup, project files, and the one command-line tool that drives it all."
learn:
  - title: "What's new in .NET 10"
    url: https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/overview
  - title: ".NET CLI overview"
    url: https://learn.microsoft.com/dotnet/core/tools/
  - title: "dotnetup overview"
    url: https://learn.microsoft.com/dotnet/core/tools/dotnetup/
  - title: "Releases and support for .NET"
    url: https://learn.microsoft.com/dotnet/core/releases-and-support
  - title: "global.json overview"
    url: https://learn.microsoft.com/dotnet/core/tools/global-json
  - title: "Central package management"
    url: https://learn.microsoft.com/nuget/consume-packages/central-package-management
  - title: "Install WSL"
    url: https://learn.microsoft.com/windows/wsl/install
---

By the end of this page you'll have a .NET 10 SDK installed and managed with `dotnetup`, and you'll know which SDK a folder uses and why. You'll be able to read every line of a modern project file, and drive the whole create–build–run–test–publish loop from the `dotnet` command line. The page ends by running the first exercise's tests, which proves your machine is ready for the rest of the primer.

:::tip[Cheatsheet]
Keep the [`dotnet` CLI cheatsheet](/cheatsheets/dotnet-cli/) open while you work.
:::

## Modern .NET, if you're coming from 3.5

.NET Framework 3.5 was Windows-only, installed once per machine, and serviced through Windows Update. **Modern .NET** (once called .NET Core, and just ".NET" since .NET 5) is its open-source, cross-platform successor. The same app builds and runs on Windows, macOS and Linux, including in Linux containers. Several versions install side by side, and each app picks the version it runs on.

A new major version ships **every November**:

- **Even-numbered releases are LTS** (Long Term Support): supported for at least three years. .NET 8 and .NET 10 are LTS.
- **Odd-numbered releases are STS** (Standard Term Support): supported for two years. .NET 9 and .NET 11 are STS.

The quality of the two is the same. Only the length of support differs. The primer uses **.NET 10 LTS**, supported until November 2028, so every sample targets `net10.0`.

Project files look very different too: a modern `.csproj` is a few lines long, with no list of source files and no GUIDs. [The SDK-style project file](#the-sdk-style-project-file) walks through one. For a fuller old-to-new mapping, see [Coming from .NET 3.5](/coming-from-3-5/).

## SDK, runtime and target framework

Three things share version numbers, and it pays to keep them apart:

- **The SDK** is what you build with: the `dotnet` CLI commands, the C# compiler, MSBuild, NuGet and the project templates. An SDK version such as `10.0.403` reads as major `10`, minor `0`, feature band `4xx`, patch `03`. Feature bands add tooling features during a release's life; patches are fixes.
- **A runtime** is what an app runs on. The base runtime (`Microsoft.NETCore.App`) runs console apps and services. ASP.NET Core apps also need the **ASP.NET Core runtime** (`Microsoft.AspNetCore.App`), which sits on top of it. Runtimes have their own patch numbers: SDK 10.0.403 ships with runtime 10.0.14.
- **The target framework** is what your project says it runs on, as a moniker in the project file: `net10.0`. It decides which APIs you can call and which runtime the app needs.

An SDK includes a runtime, so installing an SDK is enough to build and run everything in the primer. One SDK also builds for many targets: SDK 10 can build a `net8.0` project as well as a `net10.0` one. So always use the newest SDK you're allowed to, and choose the runtime with `TargetFramework`. A server that only runs apps needs just the runtime.

```sh
dotnet --list-sdks        # SDKs this dotnet can see
dotnet --list-runtimes    # Microsoft.NETCore.App and Microsoft.AspNetCore.App versions
```

## Install .NET with dotnetup

:::caution[dotnetup is in public preview]
`dotnetup` is Microsoft's cross-platform manager for user-level .NET installs. It's in public preview, so its commands and output can change before general availability. If a command here fails, check the [dotnetup reference](https://learn.microsoft.com/dotnet/core/tools/dotnetup/reference/dotnetup).

It's for development machines. Production servers and container images get .NET in other ways: the installers, the OS package manager, or an official .NET base image (see [Containers](/deploy/containers/)). For those, and if you use Visual Studio, follow Microsoft's install guide for [Windows](https://learn.microsoft.com/dotnet/core/install/windows), [macOS](https://learn.microsoft.com/dotnet/core/install/macos) or [Linux](https://learn.microsoft.com/dotnet/core/install/linux), and read [when to use dotnetup](https://learn.microsoft.com/dotnet/core/tools/dotnetup/choose-dotnetup).
:::

`dotnetup` installs SDKs and runtimes into one folder in your user profile, without admin rights. It remembers _why_ each one is there, so it can update or remove it later.

### Get dotnetup and run setup

Download it with Microsoft's script. On macOS or Linux:

```sh
curl -fsSL https://aka.ms/dotnetup/get-dotnetup.sh | bash
```

On Windows, in PowerShell:

```powershell
irm https://aka.ms/dotnetup/get-dotnetup.ps1 | iex
```

The script tells you how to put `dotnetup` on your `PATH`. Open a new terminal, then run the interactive setup. Learn shows its output like this, with your versions and paths in place of the placeholders:

```text
> dotnetup init
Welcome to dotnetup!

SDK Channel: latest
Mode: <recommended-mode> (Suggested)

<recommended-mode> modifies <configuration-target> to set PATH and DOTNET_ROOT to prefer <install-path>.

Would you like to install .NET with the recommended settings?
> Yes, proceed with defaults and install

Installing SDK <resolved-version> to <install-path>...
<download and installation progress>
Installed at <install-path>:
  SDK <resolved-version>
<mode-specific environment guidance>
Setup complete!
```

Choose **No, customize setup** to pick a different channel or access mode. You can run `dotnetup init` again at any time to change your choices.

### Access modes

The **access mode** decides how your terminals find the `dotnet` that `dotnetup` manages:

- **`shell`**, the default on macOS and Linux when `dotnetup` recognises your shell, adds `PATH` and `DOTNET_ROOT` to your shell profile. Every new terminal uses the managed `dotnet`.
- **`none`** changes nothing. You run the managed `dotnet` through `dotnetup`: `dotnetup dotnet -- --version`.
- **`everywhere`**, the default on Windows, changes the system `PATH`. That needs elevation, affects every user, and makes the managed install win over machine-wide ones, including the SDKs Visual Studio installs. Projects that need an SDK that only Visual Studio installed then fail to build. If you use Visual Studio, choose `shell` or `none`, or let setup migrate your existing installs.

Switch later with `dotnetup env set shell` or `dotnetup env set none`. With `none`, Learn shows `env set` replying like this:

```text
> dotnetup env set none
dotnetup is on your PATH. dotnet is not — run it with 'dotnetup dotnet <command>'.
Open a new terminal for the change to take effect.
```

### Channels and versions

You install by **channel**, and `dotnetup` records it as an **install spec**. An update moves each spec to the newest version that matches:

| You ask for   | You get                        | After `dotnetup update` |
| ------------- | ------------------------------ | ----------------------- |
| `lts`         | the latest LTS SDK             | moves to newer LTS      |
| `latest`      | the latest stable SDK          | moves to newer stable   |
| `10` / `10.0` | the latest 10.x / 10.0 SDK     | moves within 10 / 10.0  |
| `10.0.4xx`    | the latest SDK in the 4xx band | stays in 10.0.4xx       |
| `10.0.403`    | exactly that version           | never moves             |

```sh
dotnetup install lts                       # the top-level install is for SDKs
dotnetup sdk install 10.0.4xx              # one feature band
dotnetup runtime install aspnetcore@10.0   # just the ASP.NET Core runtime (runtime@10.0 for the base one)
```

### Install the SDK a repo asks for

Run `dotnetup sdk install` with no channel inside a repository, and it reads the nearest [`global.json`](#pin-the-sdk-with-globaljson). It turns that file's version and roll-forward policy into a channel, and records the file as the reason. In the primer's repo, whose `global.json` asks for `10.0.400` with `latestFeature`, it says:

```text
.NET SDK 10.0 will be installed since …/dotnet-primer/global.json specifies that version.
```

So the spec is `10.0`, not `10.0.4xx`: `latestFeature` accepts any 10.0 feature band, so `dotnetup` follows all of them. Learn's [repository SDK requirements](https://learn.microsoft.com/dotnet/core/tools/dotnetup/concepts/repositories) page lists the full mapping. That's fine for building, but a future `dotnetup update` could move it to a 10.0.5xx SDK. The primer's setup checks expect 4xx, so [Check your setup](#check-your-setup) installs `10.0.4xx` by name.

### List, update and uninstall

```sh
dotnetup list        # each spec, the version it resolved to, and its source
dotnetup update      # move every channel spec to its newest version
dotnetup sdk uninstall 10.0.4xx
dotnetup sdk uninstall 10.0 --source GlobalJson
```

`dotnetup list` shows one row per spec. The **Source** column says `Command` for one you typed, or the path of the `global.json` it came from.

Uninstalling removes a **spec**, not a version. Files are deleted only once no spec needs them any more. If `10.0.4xx` and the repo's `10.0` both resolved to 10.0.403, uninstalling `10.0.4xx` keeps 10.0.403, and `dotnetup` says which spec still needs it. A spec that came from `global.json` needs `--source GlobalJson`, because uninstall only matches specs you typed unless told otherwise.

## Get Docker

The primer runs some tools in **containers**, so you need a Docker engine: the [Observability](/observability/logging/#see-it-locally) pages run a local dashboard in one, the [Testing](/testing/) pages start real databases with Testcontainers, and [Containers](/deploy/containers/) builds and runs images. Exercises 01–06 don't need it, so you can skip this until you reach Observability.

Pick your platform. Each one has a default that's free to use at work, and the alternatives say which licence they need. Licence terms are as they were at the time of writing: check each vendor's own terms.

### Windows (WSL 2)

On Windows, run **Docker Engine** inside a Linux distro in [WSL 2](https://learn.microsoft.com/windows/wsl/install), the Windows Subsystem for Linux. Docker Engine is free for any use. .NET stays on Windows, and reaches the engine over a local port.

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

### macOS

Install [**OrbStack**](https://orbstack.dev/download), open it once, and `docker` is on your `PATH`:

```sh
brew install orbstack
```

OrbStack is free for personal use. Freelance, business and commercial use need a paid licence for each user, after a 30-day trial: see its [licensing](https://docs.orbstack.dev/licensing) page.

**Alternatives.** **Docker Desktop** has the same terms as on Windows. [**Colima**](https://github.com/abiosoft/colima) is free and open source: install it and the `docker` client with `brew install colima docker`, then run `colima start`. The `docker` command finds Colima by itself, but some tools look for Docker's socket at `/var/run/docker.sock`, where Colima doesn't put it. Point them at Colima's socket in your shell profile:

```sh
export DOCKER_HOST="unix://$HOME/.colima/default/docker.sock"
```

### Linux

Install **Docker Engine** from Docker's own repository, using [its install page for your distro](https://docs.docker.com/engine/install/). On Ubuntu, that's the commands in step 3 of the [Windows steps](#windows-wsl-2). Don't use your distro's `docker.io` package, which lags behind Docker's releases: Docker's page says how to remove it first.

Then let your user run `docker`, and log out and back in:

```sh
sudo usermod -aG docker $USER
sudo systemctl enable --now docker   # most distros already did this when it installed
```

Membership of the `docker` group is the same as root on that machine, so only add your own account.

### Check Docker

In your Linux or macOS terminal (on Windows, the Ubuntu terminal):

```sh
docker run --rm hello-world                 # prints "Hello from Docker!" and some text
docker info --format '{{.ServerVersion}}'   # prints the engine's version, such as 29.9.3
```

On Windows, also check that Windows tools reach the engine. In a new PowerShell, this prints the same version:

```powershell
(Invoke-RestMethod http://localhost:2375/version).Version
```

## Pin the SDK with global.json

`global.json` sits at the root of a repository and says which SDK the `dotnet` commands there must use. `dotnet` looks for it in the current folder, then in each parent folder. The primer's own:

```json title="global.json"
{
  "sdk": {
    "version": "10.0.400",
    "rollForward": "latestFeature"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

- **`version`** is the minimum SDK.
- **`rollForward`** says how far `dotnet` may go above it. `latestFeature` means "the highest installed 10.0 SDK, at 10.0.400 or later". Other policies are stricter: `latestPatch` stays in the 4xx band, and `disable` demands the exact version.
- **`test.runner`** makes `dotnet test` use Microsoft.Testing.Platform, the newer test runner the samples use. It's why the exercises run with `dotnet test --project …`.

Check what a folder gets with `dotnet --version`. On a machine whose newest SDK is a .NET 11 preview, it reports a 10.0.4xx SDK inside the repo, and the .NET 11 preview anywhere else. Create a `global.json` with:

```sh
dotnet new globaljson --sdk-version 10.0.400 --roll-forward latestFeature
```

## The SDK-style project file

Here is the project file of the first exercise's web app, in full:

```xml title="samples/exercises/01-configuration/Solution/Configuration.Solution.csproj"
<Project Sdk="Microsoft.NET.Sdk.Web">
</Project>
```

The `Sdk` attribute does the work. `Microsoft.NET.Sdk.Web` brings in the build logic for an ASP.NET Core app and references the ASP.NET Core shared framework. A class library or test project uses plain `Microsoft.NET.Sdk`. Every `.cs` file under the project's folder is compiled, so there's no list of files to maintain.

Everything else comes from **`Directory.Build.props`**. MSBuild imports the nearest one above each project automatically, so settings shared by every project live in one place:

```xml title="samples/Directory.Build.props"
<Project>
  <!-- Shared settings for every sample project (design D9). -->
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <!-- Analyzers + warnings-as-errors make the build the samples lint gate. -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

- **`TargetFramework`**: the moniker the project builds for. Use `TargetFrameworks` (plural, `;`-separated) to build for several.
- **`Nullable`**: turns on nullable reference types. `string` means "never null" and `string?` means "might be null", and the compiler warns when you mix them up.
- **`ImplicitUsings`**: adds the common `using` directives for the project's SDK (`System`, `System.Linq`, `Microsoft.AspNetCore.Builder`, and so on) as global usings, so files don't repeat them.
- **`TreatWarningsAsErrors`**, **`AnalysisLevel`** and **`EnforceCodeStyleInBuild`**: run the built-in code analyzers and style rules in every build, and fail it on any warning. The primer uses this as its lint check.

The test project shows the rest of the everyday elements:

```xml title="samples/exercises/01-configuration/Solution.Tests/Configuration.Solution.Tests.csproj"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Shared test source: the same tests also run against the starter. -->
    <Compile Include="../tests/**/*.cs" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="xunit.v3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../Solution/Configuration.Solution.csproj" />
  </ItemGroup>

</Project>
```

- **`OutputType`** `Exe`: xUnit v3 test projects are executables that run their own tests.
- **`IsPackable`** `false`: `dotnet pack` skips it.
- **`Compile Include`**: adds source files from outside the project folder. The primer uses it so that one set of tests runs against both the starter and the solution. Most projects never need it.
- **`Using Include`**: one global `using Xunit;` for the whole project, the same mechanism `ImplicitUsings` uses.
- **`PackageReference`**: a NuGet package. There's no `Version` here, because of central package management (below).
- **`ProjectReference`**: another project in the solution, so the tests can call the app's code.

**Central package management** puts every package version in one **`Directory.Packages.props`** at the repo root, so all projects agree on versions:

```xml title="samples/Directory.Packages.props"
<Project>
  <!-- Central package management: versions live here, projects reference without versions. -->
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.14" />
    <PackageVersion Include="xunit.v3" Version="4.0.2" />
  </ItemGroup>
</Project>
```

## The dotnet CLI loop

One `dotnet` command drives everything, the same on every OS and in CI. A minimal project with tests, from an empty folder:

```sh
dotnet new webapi -n Orders.Api                   # ASP.NET Core API project
dotnet new xunit3 -n Orders.Tests -f net10.0      # test project; the template defaults to net8.0
dotnet new sln -n Orders                          # creates Orders.slnx
dotnet sln add Orders.Api Orders.Tests
dotnet add Orders.Tests reference Orders.Api
dotnet add Orders.Api package Microsoft.EntityFrameworkCore.Sqlite
```

The `xunit3` template comes from a template pack: install it once with `dotnet new install xunit.v3.templates`. A **solution** lists projects so that one command can build or test them all. .NET 10 creates the XML-based `.slnx` format, which is much easier to read and merge than the old `.sln`.

Then the inner loop:

```sh
dotnet build                          # restore packages, then compile
dotnet run --project Orders.Api       # build and start the app
dotnet watch --project Orders.Api     # run, and hot-reload as you edit
dotnet test                           # build and run every test project in the solution
```

`build`, `run` and `test` restore NuGet packages first, so there's no separate restore step. They work on the project or solution in the current folder, or the one you pass.

**Publishing** produces what you deploy. Don't deploy the `bin/` output of `dotnet build`:

```sh
dotnet publish Orders.Api -c Release                                 # framework-dependent: needs the runtime installed
dotnet publish Orders.Api -c Release -r linux-x64 --self-contained   # bundles the runtime for one platform
dotnet publish Orders.Api -c Release -t:PublishContainer             # a container image, no Dockerfile needed
```

A framework-dependent publish is small, but the machine needs the matching runtime. A self-contained one carries its own runtime for one OS and CPU. The container target builds an image on the official ASP.NET Core base image: see [Containers](/deploy/containers/).

## Pitfalls

- **`global.json` asks for an SDK you don't have.** Every `dotnet` command in the repo fails with "A compatible .NET SDK was not found", then the requested version, the `global.json` path and your installed SDKs. Fix: `dotnetup sdk install` from the repo root installs a match. Or install the band by name, as in [Check your setup](#check-your-setup).
- **Running from the wrong folder.** `global.json` only applies at or below its folder, so outside the repo `dotnet --version` shows your newest SDK, perhaps a preview. Fix: run commands from inside the repo, and check with `dotnet --version`.
- **`dotnet` not found after setup.** With access mode `none`, `dotnetup` doesn't put `dotnet` on your `PATH`, and with `shell`, only new terminals see it. Fix: open a new terminal, use `dotnetup dotnet -- <command>`, or switch with `dotnetup env set shell`.
- **Windows `everywhere` mode hides Visual Studio's SDKs.** The managed install takes precedence over machine-wide ones, so a project that needs an SDK only Visual Studio installed stops building. Fix: choose `shell` or `none` with `dotnetup init`, or migrate existing installs into `dotnetup` (`--migrate-from-system`).
- **Two installs on one machine.** If you installed .NET with the installer before, you now have a machine-wide copy as well, and which one a terminal uses depends on `PATH`. Fix: check `dotnet --list-sdks`, whose paths show which install answers. To bring the old SDKs under `dotnetup`, add `--migrate-from-system` to a `dotnetup sdk install`.
- **A `Version` on a `PackageReference` under central package management.** The build fails with error NU1008, because versions belong in `Directory.Packages.props`. Fix: move the version to a `PackageVersion` there, and remove it from the project.
- **Deploying `bin/` output.** `dotnet build` output is for local runs, and it doesn't include everything an app needs on another machine. Fix: deploy the output of `dotnet publish`.
- **`permission denied while trying to connect to the docker API at unix:///var/run/docker.sock`.** Your user isn't in the `docker` group yet, or the terminal was opened before you added it. Fix: `sudo usermod -aG docker $USER`, then log in again. In WSL, that means `wsl --shutdown` in PowerShell and reopening Ubuntu.
- **`Cannot connect to the Docker daemon at unix:///var/run/docker.sock. Is the docker daemon running?`** The engine isn't running. In WSL, that usually means systemd is off. Fix: run the systemd check from [step 2](#windows-wsl-2), then `sudo systemctl enable --now docker`.
- **Windows tools can't find Docker.** `DOCKER_HOST` isn't set in that terminal, or Ubuntu isn't running: WSL stops a distro that has been idle for a while. Fix: open a new terminal, open Ubuntu so the engine starts, and run the Windows check in [Check Docker](#check-docker).
- **The engine won't start after you add the port.** `sudo systemctl restart docker` fails. Fix: `journalctl -u docker` says why. If it says "the following directives are specified both as a flag and in the configuration file: hosts", remove `hosts` from `/etc/docker/daemon.json`, because the port belongs in the override file only. If it says that binding without TLS is no longer allowed, you have a newer engine than this page was written for: follow the [remote access](https://docs.docker.com/engine/daemon/remote-access/) page, or use Docker Desktop.

## Check your setup

**Goal:** prove that your machine can build and test the primer's samples.

**Where:** a terminal, on a machine with `dotnetup` set up as above.

**Steps:**

1. Clone the repo and go into it:

   ```sh
   git clone https://github.com/emmz-makes-stuff/dotnet-primer.git
   cd dotnet-primer
   ```

2. Install the SDK band the samples pin:

   ```sh
   dotnetup sdk install 10.0.4xx
   ```

3. Check that the repo selects it. This should print `10.0.4` followed by a patch number:

   ```sh
   dotnet --version
   ```

**Check your work:** run the first exercise's solution tests. They should all pass.

```sh
dotnet test --project samples/exercises/01-configuration/Solution.Tests
```

If they do, you're ready for [ASP.NET Core](/aspnet-core/).
