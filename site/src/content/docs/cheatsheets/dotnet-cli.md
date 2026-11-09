---
title: dotnet CLI cheatsheet
description: Installing SDKs with dotnetup, and the dotnet commands you'll use every day, on one page.
prev: false
next: false
learn:
  - title: .NET CLI overview
    url: https://learn.microsoft.com/dotnet/core/tools/
  - title: dotnetup overview
    url: https://learn.microsoft.com/dotnet/core/tools/dotnetup/
  - title: dotnet new templates
    url: https://learn.microsoft.com/dotnet/core/tools/dotnet-new
  - title: .NET tools
    url: https://learn.microsoft.com/dotnet/core/tools/global-tools
  - title: EF Core command-line tools
    url: https://learn.microsoft.com/ef/core/cli/dotnet
  - title: dotnet format
    url: https://learn.microsoft.com/dotnet/core/tools/dotnet-format
---

Back to [.NET 10 and the `dotnet` CLI](/platform/dotnet-10-and-cli/).

## Install and select SDKs

`dotnetup` is in public preview, so its commands can change. Check the
[dotnetup reference](https://learn.microsoft.com/dotnet/core/tools/dotnetup/reference/dotnetup) if one
fails.

Get `dotnetup` on macOS and Linux:

```sh
curl -fsSL https://aka.ms/dotnetup/get-dotnetup.sh | bash
```

On Windows, in PowerShell:

```powershell
irm https://aka.ms/dotnetup/get-dotnetup.ps1 | iex
```

Then, in a new terminal:

```sh
dotnetup init        # interactive setup: SDK channel and access mode
dotnetup --version
```

```sh
dotnetup install lts                       # latest LTS SDK
dotnetup sdk install 10.0.4xx              # a feature band; also 10.0, 10, latest, or exact 10.0.403
dotnetup sdk install                       # the SDK this folder's global.json asks for
dotnetup runtime install aspnetcore@10.0   # a runtime on its own (runtime@10.0 for the base runtime)
dotnetup list                              # install specs, the versions they resolved to, and why
dotnetup update                            # move every channel spec to its newest version
dotnetup sdk uninstall 10.0.4xx            # drop a spec; files go once no spec needs them
dotnetup sdk uninstall 10.0 --source GlobalJson   # drop a spec that came from global.json
```

```sh
dotnet --version          # SDK selected for this folder (honours global.json)
dotnet --list-sdks        # every SDK this dotnet can see
dotnet --list-runtimes    # every runtime this dotnet can see
dotnet new globaljson --sdk-version 10.0.400 --roll-forward latestFeature
```

## Create

```sh
dotnet new list web                             # templates matching "web"
dotnet new webapi -n Orders.Api                 # ASP.NET Core API project
dotnet new install xunit.v3.templates           # once: adds the xUnit v3 templates
dotnet new xunit3 -n Orders.Tests -f net10.0    # xUnit v3 test project (defaults to net8.0)
dotnet new sln -n Orders                        # solution file (Orders.slnx)
dotnet sln add Orders.Api Orders.Tests
dotnet sln list
```

## Build, run, test

```sh
dotnet build                          # restore + build
dotnet run --project Orders.Api       # build + run
dotnet watch --project Orders.Api     # run with hot reload; Ctrl+R restarts
dotnet test                           # run tests
```

## Packages and references

```sh
dotnet add Orders.Api package Microsoft.EntityFrameworkCore.Sqlite
dotnet remove Orders.Api package Microsoft.EntityFrameworkCore.Sqlite
dotnet add Orders.Tests reference Orders.Api
dotnet list package --outdated
```

## Tools

```sh
dotnet new tool-manifest          # .config/dotnet-tools.json: commit it
dotnet tool install dotnet-ef     # local tool, recorded in the manifest
dotnet tool restore               # after a clone: install the manifest's tools
dotnet tool list                  # tools in the manifest
dotnet tool run dotnet-ef --version
dotnet ef --version               # same, by the tool's command name
dnx dotnet-ef -- --version        # run a tool once, without installing it
```

```sh
dotnet tool install -g dotnet-outdated-tool     # global tool: on PATH for your user, in every folder
dotnet tool list -g
dotnet tool update -g dotnet-outdated-tool      # install -g on an installed tool also updates it
dotnet tool uninstall -g dotnet-outdated-tool
```

## User secrets

Local development only. See [Configuration](/aspnet-core/configuration/) for where they fit.

```sh
dotnet user-secrets init --project Orders.Api     # adds UserSecretsId to the .csproj
dotnet user-secrets set "Smtp:Password" "p@ssw0rd" --project Orders.Api
dotnet user-secrets list --project Orders.Api
dotnet user-secrets remove "Smtp:Password" --project Orders.Api
dotnet user-secrets clear --project Orders.Api
```

## EF Core migrations

The project needs the `Microsoft.EntityFrameworkCore.Design` package and the `dotnet-ef` tool (see
Tools). Run these from the project folder, or pass `--project`.

```sh
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add InitialCreate    # writes Migrations/
dotnet ef migrations list
dotnet ef database update                 # apply every pending migration
dotnet ef database update 0               # revert every migration
dotnet ef migrations remove               # delete the last migration, if it isn't applied
dotnet ef migrations script -o migrate.sql
dotnet ef migrations script --idempotent -o migrate.sql   # safe to rerun; not supported on SQLite
dotnet ef migrations add AddCustomer --project Orders.Data --startup-project Orders.Api
```

## Formatting

```sh
dotnet format                         # fix whitespace, code style and analyzer issues
dotnet format --verify-no-changes     # fail (exit 2) if anything would change: use in CI
dotnet format whitespace              # only whitespace; also: style, analyzers
```

## Publish

```sh
dotnet publish Orders.Api -c Release                                 # framework-dependent, to bin/Release/net10.0/publish/
dotnet publish Orders.Api -c Release -o out                          # same, into ./out
dotnet publish Orders.Api -c Release -r linux-x64 --self-contained   # bundles the runtime
dotnet publish Orders.Api -c Release -t:PublishContainer             # container image, no Dockerfile
```
