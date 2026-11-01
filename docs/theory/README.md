<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# AnyApp theory and learning path

This is a modular, source-grounded introduction to the domain AnyApp owns: hosting a composed Workshop runtime on a native desktop platform. It does not replace FSM_COS composition theory, GUI/Visualize/Renderer theory, repository contracts, or Experience-domain documentation.

## Intended learning sequence

1. [01 — The native host boundary](01-native-host-boundary.md) — why a host exists, what it owns, and how composition reaches the desktop.
2. [Startup and offline recovery](../EXPERIENCE-STARTUP-AND-OFFLINE-RECOVERY.md) — how a host resumes an Experience without confusing catalog unavailability with a publication change.
3. [Repository-backed composition](../../README.md#repository-backed-composition) — how immutable artifact identities are delivered and materialized before FSM_COS composes them.
4. [Presentation and rendering boundaries](../GPU_HOST_INTEGRATION.md) — how host-owned native presentation relates to shared rendering contracts; current GPU material is a plan with acceptance gates, not proof of production GPU rendering.
5. [Trusted desktop distribution](../TRUSTED-DISTRIBUTION.md) — how a desktop executable is built and distributed without weakening platform security.

The first chapter is an implemented, source-checked chapter. The remaining entries route to existing focused guides; they are not represented as completed textbook chapters. They should be expanded into standalone theory chapters as the source/test inventory and implementation evidence mature.

## How to use this path

Read in order for a first introduction, or open an individual chapter independently. Each chapter should connect a motivating problem, precise vocabulary, reasoning and trade-offs, a living model, implementation mapping, a worked example, evidence and limits, practice, and further reading.

## Domain ownership

- **AnyApp:** native process/window host, host-side startup policy and adapters, platform-specific integration.
- **FSM_COS:** MicroBundle composition and RuntimeAssembly construction.
- **Experiences:** reusable Experience behavior and manifests.
- **MicroBundleRepository:** artifact identity and delivery contracts.
- **GUI / Visualize / Renderer:** semantic presentation, visualization, and rendering contracts.
- **Foundry:** execution lifecycle for Foundry-managed workflows; do not assume a composed RuntimeAssembly is a Foundry WorkflowDefinition.

When a concept belongs to another repository, this path links to its owner rather than duplicating or redefining that theory.


---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="200" /></p>
