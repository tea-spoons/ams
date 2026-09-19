# Change plan

> Draft. This file tracks what changed on the way to this repo and what I plan to change next. Edit freely.

## Origin

Originally developed at Bigpoint. Published here with Bigpoint's permission for research, education and other noncommercial use. Copyright (c) 2026 Bigpoint; see [LICENSE.md](LICENSE.md).

## Changes made before publishing

- Namespaces are now `TeaSpoons.*` and the package id is `com.tea-spoons.ams` (assemblies renamed to match).
- Internal build, registry and tracker references were removed; the repo uses GitHub Actions (`CI` and `Release`) built on `unity-ci-kit`.
- Added `LICENSE.md` (PolyForm Noncommercial 1.0.0), an install section in the README, and package metadata (author, license and documentation URLs).
- 0.1.0: `com.tea-spoons.logging` is no longer a dependency. AMS uses it when the project has it (`TEASPOONS_LOGGING`, set from the asmdef `versionDefines`) and otherwise falls back to Unity logs, which are compiled out unless `UNITY_EDITOR` or `DEVELOPMENT_BUILD` is defined. Added EditMode tests that run with and without the Logging package.

## Planned changes

- [x] Tag and publish `v0.0.3` with the Release workflow.
- [x] Tag and publish `v0.1.0` with the Release workflow.

## Notes and ideas

_Add your own here._
