# site-deployment Specification

## Purpose

Defines how the primer site is published. Every accepted change on `main` reaches readers at the public
custom domain over HTTPS, and a change that fails the site's quality gates never gets deployed.

## Requirements

### Requirement: Served publicly on the custom domain over HTTPS
The site SHALL be publicly reachable at `https://dotnet-primer.emmz.dev/`, served as static files from
GitHub Pages. The built site SHALL declare that domain, so the custom domain survives every deploy.

#### Scenario: Reader visits the domain
- **WHEN** anyone requests `https://dotnet-primer.emmz.dev/`
- **THEN** the introduction page is served over HTTPS with a valid certificate, with no authentication

#### Scenario: Custom domain persists across deploys
- **WHEN** a new deploy completes
- **THEN** GitHub Pages still serves the site on `dotnet-primer.emmz.dev`, with no manual
  re-configuration

### Requirement: Deploy on push to main
A push to the `main` branch SHALL build and deploy the site automatically. A manual run on `main` SHALL
also be possible.

#### Scenario: Content change goes live
- **WHEN** a commit that changes a page is pushed to `main` and all site gates pass
- **THEN** the updated page is live on `https://dotnet-primer.emmz.dev/` once the workflow completes

### Requirement: Gates block a bad deploy
The deploy SHALL run only after the site build, test, format-check and lint gates have all passed for
that commit. Pull requests SHALL run the same gates without deploying.

#### Scenario: Failing gate stops the deploy
- **WHEN** a commit pushed to `main` fails any site gate
- **THEN** no deploy happens, and the previously deployed site stays live

#### Scenario: Pull request is checked but not deployed
- **WHEN** a pull request targeting `main` is opened or updated
- **THEN** the site gates run and report their status on the pull request, and nothing is deployed
