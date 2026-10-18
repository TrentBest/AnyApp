# Renderer / Visualize LLM → AnyApp LLM

## 2026-10-18: Latest integration run findings

This is a cross-repository handoff only; no AnyApp source or workflow files are changed by this note.

### 1. Baseline failure still demonstrates the RenderFrame resolution problem

- [AnyApp Baseline run 39096641574](https://github.com/TrentBest/AnyApp/actions/runs/39096641574), commit `4f5ce2a15d70ee89a202435bcaea96ff42e6dc72`.
- `Test AnyApp composition contracts` was skipped because restore failed.
- The failure output available from the later baseline run [39096591192](https://github.com/TrentBest/AnyApp/actions/runs/39096591192) contains CS0246 at `Visualize/integrations/RendererAdapter/RendererFrameAdapter.cs` lines 13 and 16: `RenderFrame` could not be found.
- Renderer owns the authoritative public `TheSingularityWorkshop.Renderer.RenderFrame`; neither a duplicate type nor a public API change is justified by this error alone.

### 2. Source-mode build graph now gets past Renderer and adapter restore, but fails in AnyApp's broader source graph

- [AnyApp .NET run 39096591199](https://github.com/TrentBest/AnyApp/actions/runs/39096591199), job `117584226291`.
- Renderer source dependency restore: passed.
- Renderer reference diagnostics: passed.
- Renderer Release source build: passed.
- AnyApp test graph restore in repository-source mode: passed.
- Renderer adapter source-mode restore: passed.
- Adapter reference diagnostics: step passed, but the output needs inspection to establish the resolved assembly used for `RenderFrame`.
- The subsequent AnyApp build failed in `MicroBundleRepository/src/REST/TheSingularityWorkshop.MicroBundleRepository.Rest.csproj(29,5)` with:
  `WorkshopUseRepositorySources=true requires TheSingularityWorkshop.FSM_COS beside this repository.`
- The workflow checks out FSM_COS under AnyApp's `_dependencies`, but MicroBundleRepository's project-local source resolver appears to expect a sibling directory relative to the MicroBundleRepository checkout. Please inspect path contract and checkout layout; align them without masking the missing-source validation.

### 3. Current rerun

[AnyApp .NET run 39096641581](https://github.com/TrentBest/AnyApp/actions/runs/39096641581) is the follow-up run after the broader dependency graph was added. At last poll it was in `Restore AnyApp test graph in repository-source mode`; its final adapter diagnostics/build/test result is not yet known.

### Suggested next checks

1. Inspect the exact adapter `ResolveReferences` diagnostic artifact/log from run 39096591199 and confirm whether the selected `RenderFrame` assembly is the checked-out Renderer Release output or the cached `0.1.0-alpha.2` package DLL.
2. Fix the source-root/sibling path contract for MicroBundleRepository's FSM_COS reference in the owning repository/workflow. Do not remove the guard merely to force CI green.
3. Recheck the public NuGet baseline separately; it must compile against published compatible packages, independent of source-mode success.

Renderer/Visualize status: Visualize's own build/tests and GUI.SVG package-proof passed in [run 39096627477](https://github.com/TrentBest/TheSingularityWorkshop.Visualize/actions/runs/39096627477). Renderer normal build/test/package passed in [run 39086967875](https://github.com/TrentBest/TheSingularityWorkshop.Renderer/actions/runs/39086967875), but its source-integration test exposed an FSM_API `DestroyInstance` disposal exception; that issue has separately been handed to FSM_API in `RendererLLM.md`.

No package was published, no public API was changed, and no GPU-pixel output has been verified by these runs.
