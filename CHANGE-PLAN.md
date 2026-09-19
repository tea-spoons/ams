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
<!-- review-items:start -->
- [ ] **P1** Remove the per-tick allocations: cache the flattened order (invalidate it in `AddChild`/`RemoveChild`) or walk with an explicit stack, and replace the LINQ. Add a test that asserts zero allocated bytes around `Tick`.
- [ ] **P1** Define a deterministic iteration order (an ordered list, or sort by a stable id) and add a test that inserting the same containers in a different order gives bit-identical values.
- [ ] **P1** Reset `AmsTicker`'s static state at `SubsystemRegistration` and test it with domain reload disabled.
- [ ] **P1** Add golden-value tests for the formula, average values, parent/child propagation and `TransformType`, plus randomized tests comparing `DefaultCalculator` against a `decimal` reference.
- [ ] **P1** Run the tests in CI. The kit's `run-tests` needs a Unity project, so this waits for package-mode support in `unity-ci-kit` (planned there; GameCI's test runner has a `packageMode` for the same reason).
- [ ] **P2** Pre-size the internal collections (a `capacity` argument, as Stat-System does) and document that `AmsContainer` is the "source" handle for removing everything one item added.
- [ ] **P2** Add a `Documentation~` folder with the formula, a diagram of the proxy hierarchy, and a guide to using `InstrumentedCalculator` for client/server parity.
- [ ] **P2** Add a `CHANGELOG.md`. Unity's package layout lists one next to `README.md`, and the `unity-ci-kit` validator warns without it.
<!-- review-items:end -->

<!-- review:start -->
## Review (September 2026)

Reviewed as a senior Unity engineer would: I read the code and compared the package with similar open-source projects (September 2026). Those projects are listed for ideas only. Nothing was copied from them, and their licenses are noted in case code is ever reused. Priorities: **P0** correctness bug or broken metadata, **P1** should be done soon, **P2** nice to have.

### Compared with

| Project | License | Worth noting |
|---|---|---|
| [meredoth/Stat-System](https://github.com/meredoth/Stat-System) | Apache-2.0 | Modifiers run in a fixed order by type (flat, additive, multiplicative), so the order they were added in cannot change the result. A modifier can name a source object, which allows removing everything from one source. The constructor accepts a `capacity` for the modifier lists, to limit GC. |
| [Kryzarel, Character Stats (articles)](https://medium.com/@kryzarel/character-stats-attributes-in-unity-pt-2-additive-modifiers-optimizations-1dfb2d42f3c8) | article and Asset Store product | Modifiers carry a `Source`. The second article is titled "Additive Modifiers & Optimizations". |
| [SeawispHunter.RolePlay.Attributes](https://github.com/shanecelis/SeawispHunter.RolePlay.Attributes) | not checked | Generic attributes that are modified non-destructively and notify on change. |

### Findings from reading the code

- **[Perf]** `AmsProxy.Tick` walks the tree three times (`AmsProxy.cs`, around lines 193-209), using a recursive `Flattened()` iterator and a LINQ `.Where`. Every walk allocates iterator objects in proportion to the depth of the tree, and `AmsTicker` runs it every frame when automatic ticking is on.
- **[Determinism]** Children and containers are stored in `HashSet`s (`AmsProxy.cs`, lines 14 and 16). Their iteration order is not a contract, and sums of `double`s depend on order. `InstrumentedCalculator` exists to find client/server drift, so the order should be defined.
- **[Lifecycle]** `AmsTicker.isApplicationQuitting` is set to true on quit and never reset (`AmsTicker.cs`, lines 19 and 79). With Enter Play Mode Options (domain reload off) `Instance` can stay `null` in the next play session. `service-locator` already resets its statics at `SubsystemRegistration`.
- **[Tests]** One test file (73 lines) for about 2,300 lines of code. The formula in the README has no golden-value tests.
<!-- review:end -->

## Notes and ideas

_Add your own here._
