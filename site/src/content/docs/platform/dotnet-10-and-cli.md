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
- **`Nullable`**: turns on [nullable reference types](/csharp/null-safety/). `string` means "never null" and `string?` means "might be null", and the compiler warns when you mix them up.
- **`ImplicitUsings`**: adds the common `using` directives for the project's SDK (`System`, `System.Linq`, `Microsoft.AspNetCore.Builder`, and so on) as [global usings](/csharp/#implicit-and-global-usings-net-6-c-10), so files don't repeat them.
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

## Get Docker

Some later pages run tools in containers, so they need Docker. Exercises 01–06 don't, so you can start on ASP.NET Core first. The next page in Getting started, [Docker](/platform/docker/), sets it up on Windows, macOS and Linux.
