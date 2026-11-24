<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# AnyApp theory and learning path

<!-- CIAI:BADGES:START -->
[![Sith Policy](https://github.com/TrentBest/AnyApp/actions/workflows/sith-policy.yml/badge.svg)](https://github.com/TrentBest/AnyApp/actions/workflows/sith-policy.yml)
<!-- CIAI:BADGES:END -->

This is a modular, source-grounded introduction to the domain AnyApp owns: hosting a composed Workshop runtime on a native desktop platform. It does not replace FSM_COS composition theory, GUI/Visualize/Renderer theory, repository contracts, or Experience-domain documentation.

## Intended learning sequence

1. [01 — The native host boundary](01-native-host-boundary.md) — why a host exists, what it owns, and how composition reaches the desktop.
2. [02 — AnyApp as a host](02-anyapp-as-a-host.md) — the deeper model: identity, discovery, authorization, composition, execution, presentation, failure semantics, and ownership.
3. [03 — Startup, recovery, and continuity](03-startup-recovery-and-continuity.md) — how a host reconciles cached state with a publication authority, preserves recoverable generations, and reasons under network failure.
4. [04 — Trust boundaries and safe host orchestration](04-trust-boundaries-and-safe-host-orchestration.md) — how the host separates Origin validation, authentication, authorization, discovery, retrieval, composition, and execution while containing untrusted browser, repository, and model input.
5. [05 — Presentation, semantic interaction, and the native adapter](05-presentation-semantic-interaction-and-native-adapters.md) — how semantic GUI roots become native controls, why presentation is not execution authority, and how Renderer/Visualize frame paths remain distinct.
6. [06 — Discovery, retrieval, and provenance](06-discovery-retrieval-and-provenance.md) — why authored-content discovery and Experience/capability discovery are separate, how exact revisions are retrieved, and how provenance survives model-assisted reasoning.
7. [Repository-backed composition](../../README.md#repository-backed-composition) — how immutable artifact identities are delivered and materialized before FSM_COS composes them.
8. [Presentation and rendering boundaries](../GPU_HOST_INTEGRATION.md) — how host-owned native presentation relates to shared rendering contracts; current GPU material is a plan with acceptance gates, not proof of production GPU rendering.
9. [Trusted desktop distribution](../TRUSTED-DISTRIBUTION.md) — how a desktop executable is built and distributed without weakening platform security.

Chapters 01–06 are source-grounded theory chapters. The remaining entries route to existing focused guides; they are not represented as completed standalone textbook chapters. Expand them as the source/test inventory and implementation evidence mature.

## How to use this path

Read in order for a first introduction, or open an individual chapter independently. Each chapter should connect a motivating problem, precise vocabulary, reasoning and trade-offs, a living model, implementation mapping, a worked example, evidence and limits, practice, and further reading.

## Domain ownership

- **AnyApp:** native process/window host, host-side startup policy and adapters, platform-specific integration.
- **FSM_COS:** MicroBundle composition and RuntimeAssembly construction.
- **Experiences:** reusable Experience behavior, manifests, and authored metadata.
- **MicroBundleRepository:** immutable artifact identity, delivery, and byte verification.
- **FSM_Execution:** execution lifecycle and scheduling contracts it explicitly supports; it does not make every composed RuntimeAssembly executable by implication.
- **GUI / Visualize / Renderer:** semantic presentation, visualization, and rendering contracts.

When a concept belongs to another repository, this path links to its owner rather than duplicating or redefining that theory.

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
