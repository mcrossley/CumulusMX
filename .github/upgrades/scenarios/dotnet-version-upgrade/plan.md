# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade CumulusMX solution to .NET 10 (net10.0)
**Scope**: Solution with multiple projects (apps, libraries, test projects) currently targeting .NET Core 3.1. Expect package updates and some API fixes.

## Tasks

### 01-update-project-tfms: Update project TargetFrameworks

Update each project file to target net10.0. Prefer single-targeting to net10.0. Verify SDK-style settings and convert non-SDK projects if necessary.

**Done when**: All projects have <TargetFramework>net10.0</TargetFramework> (or appropriate multi-targeting), and projects restore packages.

---

### 02-update-nuget-packages: Update NuGet packages

Upgrade NuGet packages to versions compatible with net10.0. For deprecated or insecure packages, find maintained replacements.

**Done when**: No package compatibility warnings remain and restore succeeds.

---

### 03-fix-compilation-errors: Resolve compilation and API-breaking changes

Address code changes required by net10.0 (API changes, removed/renamed members). Apply minimal code updates to restore builds.

**Done when**: Solution builds without errors.

---

### 04-fix-warnings-and-nullability: Treat warnings as errors and enable nullable where appropriate

Fix all build warnings in projects modified by the upgrade. Consider enabling nullable reference types per project and address CS86xx warnings incrementally.

**Done when**: Projects modified by the upgrade build with zero warnings.

---

### 05-update-tests-and-run: Update and run unit tests

Update test projects and packages as needed, then run the test suite and fix failing tests.

**Done when**: All tests pass.

---

### 06-final-validation-and-commit: Final validation

Run full solution build, run tests, and perform a smoke test. Commit changes according to commit strategy.

**Done when**: Build and tests pass and changes are committed to `upgrade-dotnet-10` branch.
