# dotnet-primer — gate targets.
#
# Every gate prints its own exit code as `LABEL_EXIT:<n>` and exits with it, so a
# result can be quoted rather than inferred from output that may look clean.
# `gates` runs the whole set with -k, so one run reports every failure.
# Stacks: `site` (Astro + Starlight, in site/) and `samples` (.NET 10, in samples/).
# site-test reads site/dist, so it needs site-build first; `gates` orders them.

SHELL := /bin/bash

CHANGES := $(notdir $(patsubst %/,%,$(filter-out %/archive/,$(wildcard openspec/changes/*/))))

.PHONY: site-build site-test site-format site-lint samples-build samples-test samples-format \
        build test format lint validate changes gates deps

site-build:
	@npm --prefix site run build; code=$$?; echo "SITE_BUILD_EXIT:$$code"; exit $$code

site-test:
	@npm --prefix site test; code=$$?; echo "SITE_TEST_EXIT:$$code"; exit $$code

site-format:
	@npm --prefix site run format:check; code=$$?; echo "SITE_FORMAT_EXIT:$$code"; exit $$code

site-lint:
	@npm --prefix site run lint; code=$$?; echo "SITE_LINT_EXIT:$$code"; exit $$code

samples-build:
	@dotnet build samples/Primer.slnx; code=$$?; echo "SAMPLES_BUILD_EXIT:$$code"; exit $$code

samples-test:
	@dotnet test --solution samples/Primer.Solutions.slnx && \
		dotnet run --project samples/exercises/09-testing/Checker -- Solution.Tests; \
		code=$$?; echo "SAMPLES_TEST_EXIT:$$code"; exit $$code

samples-format:
	@dotnet format samples/Primer.slnx --verify-no-changes; code=$$?; echo "SAMPLES_FORMAT_EXIT:$$code"; exit $$code

# samples has no separate lint: analyzers + TreatWarningsAsErrors make samples-build the lint.

# Both-stack conveniences.
build:
	@$(MAKE) --no-print-directory -k site-build samples-build; code=$$?; echo "BUILD_EXIT:$$code"; exit $$code

test:
	@$(MAKE) --no-print-directory -k site-test samples-test; code=$$?; echo "TEST_EXIT:$$code"; exit $$code

format:
	@$(MAKE) --no-print-directory -k site-format samples-format; code=$$?; echo "FORMAT_EXIT:$$code"; exit $$code

lint:
	@$(MAKE) --no-print-directory -k site-lint; code=$$?; echo "LINT_EXIT:$$code"; exit $$code

# Validates every active change. No active change is a failure, not a silent pass.
validate:
	@fail=0; \
	if [ -z "$(CHANGES)" ]; then \
		echo "no active change found under openspec/changes/"; fail=1; \
	else \
		for c in $(CHANGES); do openspec validate $$c --strict || fail=1; done; \
	fi; \
	echo "VALIDATE_EXIT:$$fail"; exit $$fail

changes:
	@echo "$(CHANGES)"

gates:
	@$(MAKE) --no-print-directory -k site-build site-lint site-format site-test \
		samples-build samples-format samples-test validate; code=$$?; \
	echo "GATES_EXIT:$$code"; exit $$code

# Every tool and login in docs/dependencies.md — keep the two in step. Not part of
# `gates`: it checks the machine, not the code, and auth checks may need the network.
deps:
	@fail=0; \
	node -e "const[a,b]=process.versions.node.split('.').map(Number);process.exit(a>22||(a===22&&b>=23)?0:1)" 2>/dev/null || { echo "MISSING/WRONG VERSION node — need >=22.23: brew install node"; fail=1; }; \
	npm --version >/dev/null 2>&1 || { echo "MISSING npm — ships with node"; fail=1; }; \
	command -v dotnetup >/dev/null 2>&1 && dotnetup --version >/dev/null 2>&1 || { echo "MISSING dotnetup — see docs/dependencies.md"; fail=1; }; \
	dotnet --list-sdks 2>/dev/null | grep -q '^10\.0\.4' || { echo "MISSING dotnet SDK 10.0.4xx — dotnetup sdk install 10.0.4xx"; fail=1; }; \
	dotnet-counters --version 2>/dev/null | grep -q "^10\." || { echo "MISSING dotnet-counters 10.x — dotnet tool install -g dotnet-counters"; fail=1; }; \
	dotnet-trace --version 2>/dev/null | grep -q "^10\." || { echo "MISSING dotnet-trace 10.x — dotnet tool install -g dotnet-trace"; fail=1; }; \
	docker info --format '{{.ServerVersion}}' >/dev/null 2>&1 || { echo "MISSING docker (daemon not running?) — see https://dotnet-primer.emmz.dev/platform/docker/ (Rancher Desktop on Windows and macOS)"; fail=1; }; \
	openspec --version >/dev/null 2>&1 || { echo "MISSING openspec — npm i -g @fission-ai/openspec"; fail=1; }; \
	gh --version >/dev/null 2>&1 || { echo "MISSING gh — brew install gh"; fail=1; }; \
	gh auth status >/dev/null 2>&1 || { echo "NOT AUTHENTICATED gh — gh auth login"; fail=1; }; \
	git --version >/dev/null 2>&1 || { echo "MISSING git — xcode-select --install"; fail=1; }; \
	echo "DEPS_EXIT:$$fail"; exit $$fail
