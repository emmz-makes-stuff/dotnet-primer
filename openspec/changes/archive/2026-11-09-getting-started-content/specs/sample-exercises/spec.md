# Spec Delta

## MODIFIED Requirements

### Requirement: Pinned .NET 10 SDK
Building or testing anything under `samples/` SHALL use a .NET SDK 10.0.4xx feature band, whatever newer
SDK is installed as the machine default. If no matching SDK is installed, the build SHALL fail with an
error that names the required version. The samples README SHALL tell engineers how to install the
pinned SDK with `dotnetup`.

#### Scenario: Machine default is a newer SDK
- **WHEN** an engineer whose default SDK is a .NET 11 preview builds the samples
- **THEN** the build uses an installed .NET 10.0.4xx SDK and targets `net10.0`

#### Scenario: Required SDK missing
- **WHEN** no 10.0.4xx SDK is installed
- **THEN** `dotnet` fails and reports the required SDK version, instead of building with another SDK

#### Scenario: Engineer installs the pinned SDK
- **WHEN** an engineer follows the samples README prerequisites from the repository root
- **THEN** `dotnetup` installs an SDK that satisfies `global.json`, and `dotnet --version` in the repo
  reports a 10.0.4xx SDK
