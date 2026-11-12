<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# AnyApp documentation index

This is the navigation map for AnyApp, the native Windows host in The Singularity Workshop. It separates orientation, theory, operational guides, exact contracts, implementation evidence, and cross-repository handoffs. Source and tests on `development` remain authoritative for implementation behavior.

## 01 — Start here

- [Repository README](../README.md) — purpose, architecture overview, quick verification, and current maturity limits.
- [Agent onboarding](AGENT_ONBOARDING.md) — repository-specific architecture, verification, and two-branch workflow guidance.
- [Theory and learning path](theory/README.md) — progressive explanation of the native-host domain.
- [CI dependency modes](CI-DEPENDENCY-MODES.md) — source integration versus public-package compatibility.

## 02 — Architecture and domain guides

- [Pages playground](PAGES-PLAYGROUND.md) — design direction for interactive Experiences, synchronized Renderer/Visualize inspection, safe experimentation, and phased delivery.\n- [Experience host adapter contract](EXPERIENCE_HOST_ADAPTER_CONTRACT.md) — current discovery/manifestation surface, ownership boundaries, typed interaction gap, and cross-host acceptance gates.

| Subject | Authoritative guide | Scope |
|---|---|---|
| Native host and platform boundary | [Theory chapter 01](theory/01-native-host-boundary.md) | Why AnyApp exists and what belongs in the host |
| Startup and offline recovery | [Experience startup and offline recovery](EXPERIENCE-STARTUP-AND-OFFLINE-RECOVERY.md) | Cached manifest reconciliation and fallback decisions |
| Experience publication | [Experience publish pipeline](EXPERIENCE_PUBLISH_PIPELINE.md) and [publisher reference](../Publishing/ExperiencePublisher.md) | Host-side publishing flow and implementation |
| Repository-backed MicroBundles | [Workshop Landing 2111 integration proof](integrations/WORKSHOP_2111_REPOSITORY_TO_WPF.md) and [README: repository-backed composition](../README.md#repository-backed-composition) | FSMB envelope, repository delivery, materialization, FSM_COS composition, semantic root, and WPF handoff |
| GPU/presentation boundary | [GPU host integration](GPU_HOST_INTEGRATION.md) | Native GPU direction and acceptance gates; not proof of a production backend |
| WebApp bridge | [WebApp bridge protocol](WEBAPP_BRIDGE_PROTOCOL.md) | Local bridge contract and trust boundary |
| Trusted Windows distribution | [Trusted distribution](TRUSTED-DISTRIBUTION.md) | Signing and safe distribution constraints |
| metaDev | [metaDev](METADEV.md) | Development intelligence integration |
| Visualize/Renderer handoff | [Visualize LLM handoff](VISUALIZE_LLM_HANDOFF.md) | Current adapter coordination and unresolved evidence |
| Singularity Studios | [AnyApp integration handoff](integrations/SINGULARITY_STUDIOS_ANYAPP_HANDOFF.md) | Proposed desktop integration; design handoff, not implemented simulator proof |

## 03 — Contracts, UML, and implementation references

- [ExperiencePublisher UML](UML/ExperiencePublisherUML.md) — the currently inventoried UML companion.
- [Composition visual](assets/anyapp-composition.svg) — authored architecture visual; consult its caption/source context before treating it as execution evidence.
- [WebApp bridge protocol](WEBAPP_BRIDGE_PROTOCOL.md) — protocol-level detail.
- [Publishing/ExperiencePublisher.md](../Publishing/ExperiencePublisher.md) — publisher type reference.

The initial UML inventory is incomplete: one explicit UML companion is present in the current development tree. Public-type and workflow coverage still needs a systematic source/test scan; do not infer completeness from this list.

## 04 — Verification and evidence

- [CI dependency modes](CI-DEPENDENCY-MODES.md) — what source-mode and public-package workflows prove, and what they do not.
- [CiAi evidence integration](CIAI_EVIDENCE_INTEGRATION.md) — exact-run observation, provenance/TRX artifacts, and trusted-ingestion boundaries.
- [Historical publish-size experiment](PUBLISH-SIZE-EXPERIMENT.md) — archived size/startup measurements and the bundle inspector.
- [Startup/offline recovery tests](https://github.com/TrentBest/AnyApp/tree/development/tests/TheSingularityWorkshop.AnyApp.Tests) — test project for host startup, persistence, repository catalog, and runtime composition.
- [GitHub Actions workflows](https://github.com/TrentBest/AnyApp/tree/development/.github/workflows) — CI, baseline measurements, and executable build workflow.
- [Historical FSM_COS alpha.7 integration proof](FSM_COS_ALPHA7_INTEGRATION.md) — records the pinned source candidate and the explicit boundary that no alpha.7 package was published.

A green build is not proof of interactive runtime behavior, production GPU rendering, trusted code signing, or public-package compatibility unless the corresponding workflow actually exercises that claim.

## 05 — Choose your next step

- To understand why a native host exists, follow the [theory path](theory/README.md).
- To build and test the current host, follow the [README development commands](../README.md#development) and [CI dependency modes](CI-DEPENDENCY-MODES.md).
- To change a specific subsystem, start with its authoritative guide above, then trace the named source and tests.
- To propose a new host capability, first identify which neighboring repository owns the contract and open a coordinated issue before duplicating it in AnyApp.

## Maintenance rules

- Keep one authoritative guide per topic; link to it rather than copying its contract.
- Label claims as verified, source-checked, conceptual, or planned.
- Add source/test links when documenting behavior.
- Update this index when documents move or new authoritative guides are added.


---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
