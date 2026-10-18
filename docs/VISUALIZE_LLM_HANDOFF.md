# VisualizeLLM handoff — AnyApp renderer-adapter build

**Status:** request for VisualizeLLM investigation; no Visualize source changes made by AnyApp.  
**Branch context:** AnyApp `development`, consuming Visualize and Renderer `development` checkouts in CI.  
**Owner boundary:** AnyApp owns its host and integration orchestration. VisualizeLLM owns the Visualize repository and its Renderer-to-Visualize adapter project.

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

## Requested VisualizeLLM work

1. **Reproduce the adapter build in source mode** using the same checkout layout and properties as AnyApp CI:
   - `WorkshopUseRepositorySources=true`
   - `WorkshopSourceRoot=<workspace>/_dependencies`
   - Target `integrations/RendererAdapter/TheSingularityWorkshop.Renderer.VisualizeAdapter.csproj`.
2. **Inspect evaluated MSBuild state**, not just the XML: `WorkshopRendererProjectPath`, `ProjectReference`, `PackageReference`, `ResolvedProjectReferencePaths`, and `ReferencePath`. Confirm whether the adapter resolves the intended Renderer source project or silently falls back to a package.
3. **Make the smallest fix within Visualize** if the adapter's own project configuration, source path calculation, or compile items cause the unresolved Renderer types. Keep the adapter at the integration boundary; do not add a Visualize dependency to Renderer core or duplicate Renderer frame/representation contracts.
4. **Prove both supported modes**:
   - source mode against the sibling Renderer checkout;
   - package mode against the declared Renderer package version.
5. **Report the commit and CI evidence**, including the exact command(s) and whether the adapter compiles with warnings treated as errors. Do not publish a package as part of this repair.
6. **Resolve the distribution boundary for the adapter.** AnyApp's CI now deliberately separates source integration from public-NuGet compatibility. The adapter is currently a Visualize source project and directly project-references Visualize, so the public-feed AnyApp baseline cannot be fully source-free for this slice. Please recommend and, if appropriate, implement a packable adapter package/project whose released form references the published Renderer and Visualize packages while development source mode references the sibling projects. Keep publication disabled until separately authorized.

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
