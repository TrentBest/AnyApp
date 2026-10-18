# VisualizeLLM handoff — AnyApp renderer-adapter build

**Status:** historical integration investigation; the immediate AnyApp source-mode failure is resolved. The separately packaged adapter/distribution boundary remains open.  
**Branch context:** AnyApp `development`, consuming Visualize and Renderer `development` checkouts in CI.  
**Owner boundary:** AnyApp owns its host and integration orchestration. VisualizeLLM owns the Visualize repository and its Renderer-to-Visualize adapter project.

## Resolution update — 2026-10-18

The original unresolved Renderer-type failure is no longer the current blocker. AnyApp source-mode workflow [#39100390680](https://github.com/TrentBest/AnyApp/actions/runs/39100390680) completed successfully on commit [`bc53092`](https://github.com/TrentBest/AnyApp/commit/bc530929d2cb2e24044bf766c2e292b27e0a083f). The workflow successfully built the Renderer source graph, restored the adapter and AnyApp test graph in repository-source mode, built AnyApp, and ran its tests.

The failing test fixture had hard-coded `BundleVersion: "1.0.0"` for the Renderer MicroBundle, whose descriptor declares `0.1.0-alpha.2`. The fixture now obtains the version from `new RendererMicroBundle().Descriptor.Version`. FSM_COS exact version resolution was behaving as designed; the fixture was stale. No Visualize source change was needed to resolve this incident.

**Do not treat the historical compiler symptom below as an active failure.** The remaining Visualize-side question is packaging the Renderer adapter so the public-feed compatibility graph can consume a released adapter rather than a source project. That is a separate distribution task and does not authorize publishing.

## Why this handoff exists

AnyApp's Windows CI restores/builds the Renderer source graph successfully as a standalone project, but the combined AnyApp test dependency graph fails while compiling the Visualize Renderer adapter. The reported unresolved names include:

- `RenderFrame`
- `RendererRepresentation`

The adapter source imports `TheSingularityWorkshop.Renderer` and uses those types in `RendererFrameAdapter.cs`. The adapter project intends to reference the Renderer project in repository-source mode when `WorkshopUseRepositorySources=true`, and otherwise to consume the published Renderer package.

Relevant files on Visualize `development`:

- [Adapter project](https://github.com/TrentBest/TheSingularityWorkshop.Visualize/blob/development/integrations/RendererAdapter/TheSingularityWorkshop.Renderer.VisualizeAdapter.csproj)
- [Frame adapter](https://github.com/TrentBest/TheSingularityWorkshop.Visualize/blob/development/integrations/RendererAdapter/RendererFrameAdapter.cs)
- [Visualize repository](https://github.com/TrentBest/TheSingularityWorkshop.Visualize/tree/development)

Relevant AnyApp evidence/configuration:

- [AnyApp project references](https://github.com/TrentBest/AnyApp/blob/development/AnyApp.csproj) passes `WorkshopUseRepositorySources=true` and a shared `WorkshopSourceRoot` to both Renderer and the Visualize adapter project references.
- [Windows .NET workflow](https://github.com/TrentBest/AnyApp/blob/development/.github/workflows/dotnet.yml) checks out Renderer, Visualize, FSM_API, ProtocolAi, and MicroBundleDomain into `_dependencies`; it separately restores and diagnoses the adapter in source mode, then builds AnyApp and tests.
- [GPU host integration contract](GPU_HOST_INTEGRATION.md) defines the boundary: Renderer decides representation and produces a frame; Visualize maps and executes presentation work; AnyApp owns the WPF host/surface.

The observed failure is a project-graph/reference-resolution failure symptom, not evidence that these types should be copied into Visualize or duplicated by AnyApp. Please verify the exact evaluated project references and compiler references in the failing combined graph before choosing a fix.

## Remaining VisualizeLLM work — adapter distribution

The source-mode build now passes. Keep the following original reproduction notes as historical context only; they are not a request to re-investigate the resolved fixture mismatch.

1. **Preserve source-mode compatibility** while designing a packable adapter project:
   - `WorkshopUseRepositorySources=true`
   - `WorkshopSourceRoot=<workspace>/_dependencies`
   - Target `integrations/RendererAdapter/TheSingularityWorkshop.Renderer.VisualizeAdapter.csproj`.
2. **Define both supported dependency modes** for the packable adapter: sibling project references in development source mode, and published Renderer/Visualize packages in package mode.
3. **Keep the adapter at the integration boundary.** Do not add a Visualize dependency to Renderer core or duplicate Renderer frame/representation contracts.
4. **Prove both modes in CI**, including warning-free adapter compilation and public-feed restore, without creating a local feed.
5. **Keep publication disabled** until separately authorized. A green source build is not permission to publish.

## Acceptance criteria for AnyApp

- The adapter resolves `RenderFrame` and `RendererRepresentation` from the authoritative Renderer assembly in both intended modes.
- The adapted frame preserves the Renderer command order, geometry/material IDs, transforms, index/instance ranges, entity IDs, and explicit representation mapping.
- Visualize remains backend-neutral; WPF, HWND, or graphics-API types do not leak into Visualize core contracts.
- AnyApp continues to own the native window and host lifecycle. The existing CPU/WPF `--renderer-proof` remains accurately labelled and is not represented as GPU-produced output.
- A green standalone adapter build is necessary but not sufficient: AnyApp will rerun its full build and test workflow after the Visualize change lands on the shared development source.

## Separate AnyApp-side issue to keep visible

The same AnyApp test graph has also reported unresolved Renderer dependencies involving FSM_API, MicroBundleDomain, and ProtocolAi. Renderer source mode succeeds in its earlier standalone workflow step, so that is a second graph-consistency symptom. **AnyApp-side mitigation committed:** [5990997](https://github.com/TrentBest/AnyApp/commit/599099705cc2a2197eddca0acc14eb4e3e5c5097) now passes `WorkshopUseRepositorySources=true` and the shared `WorkshopSourceRoot` consistently to the test-graph restore, AnyApp build, and test commands. This is intended to keep the restored assets and subsequent builds in the same source mode; the newly triggered CI runs must confirm whether it resolves the issue. This remains AnyApp's responsibility. VisualizeLLM owns only the Visualize adapter side, not the AnyApp workflow or other peer repositories.

## Coordination boundary

This document is the cross-repository request. VisualizeLLM should make any code change in the Visualize repository and return the commit/verification details to AnyApp. AnyApp will then validate the complete consumer graph. No release, package publication, merge to `master`, or unrelated repository edits are implied.
