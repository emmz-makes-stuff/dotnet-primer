---
title: dotnet CLI cheatsheet
description: The dotnet commands you'll use every day, on one page.
prev: false
next: false
learn:
  - title: .NET CLI overview
    url: https://learn.microsoft.com/dotnet/core/tools/
  - title: dotnet new templates
    url: https://learn.microsoft.com/dotnet/core/tools/dotnet-new
---

Back to [.NET 10 and the `dotnet` CLI](/platform/dotnet-10-and-cli/).

## SDKs

```sh
dotnet --version          # SDK selected for this folder (honours global.json)
dotnet --list-sdks        # every installed SDK
dotnet new globaljson --sdk-version 10.0.400 --roll-forward latestFeature
```

## Create

```sh
dotnet new list                       # available templates
dotnet new webapi -n Orders.Api       # ASP.NET Core API project
dotnet new install xunit.v3.templates # once: adds the xUnit v3 templates
dotnet new xunit3 -n Orders.Tests     # xUnit v3 test project
dotnet new sln -n Orders              # solution file (.slnx)
dotnet sln add Orders.Api Orders.Tests
```

## Build, run, test

```sh
dotnet build                          # restore + build
dotnet run --project Orders.Api       # build + run
dotnet watch --project Orders.Api     # run with hot reload
dotnet test                           # run tests
```

## Packages and references

```sh
dotnet add Orders.Api package Microsoft.EntityFrameworkCore.SqlServer
dotnet add Orders.Tests reference Orders.Api
dotnet list package --outdated
```

## Publish

```sh
dotnet publish Orders.Api -c Release                     # framework-dependent output
dotnet publish Orders.Api -c Release -t:PublishContainer # container image, no Dockerfile
```

:::note[Coming soon]
More sections are on the way: tools, user secrets, EF Core migrations, and formatting.
:::
