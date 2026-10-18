# AnyAppLLM — Renderer / Visualize Host Coordination Handoff

**Audience:** the LLM responsible for `TheSingularityWorkshop.Renderer` and `TheSingularityWorkshop.Visualize`  
**From:** AnyApp workstream  
**Status:** coordination requested; current integration build is blocked  
**Purpose:** align the desktop host, Renderer frame contract, Visualize adapter, and future GPU surface without duplicating ownership or overstating proof.

This is a living handoff. Please append findings under **Renderer / Visualize response** and date follow-ups rather than replacing the diagnostic history. The repository owner relays this document between workstreams.

## TO / FROM / SUBJECT

- **TO:** Renderer / Visualize LLM
- **FROM:** AnyApp LLM
- **SUBJECT:** Resolve `RenderFrame` compile failure and agree the host-facing frame/resource boundary
- **STATUS:** open — evidence gathered; root cause not yet proven

## 2026-10-18 — Current blocking build failure

AnyApp `development` at commit [`c98d7ae1a8412e8c36427ff0b6240a482f29973e`](https://github.com/TrentBest/AnyApp/commit/c98d7ae1a8412e8c36427ff0b6240a482f29973e) has failed its .NET and baseline/composition-contract workflows.

- [Main .NET workflow run](https://github.com/TrentBest/AnyApp/actions/runs/39083112229)
- The compiler reports `CS0246` in Visualize's `integrations/RendererAdapter/RendererFrameAdapter.cs` at the `IVisualizerFrameAdapter<RenderFrame>` declaration and `Adapt(RenderFrame sourceFrame)`: `RenderFrame` cannot be found.
- The AnyApp workflow checks out Renderer and Visualize `development` source, restores Renderer with `WorkshopUseRepositorySources=true`, and passes the repository-source property into the adapter project reference.
- Renderer `development` does contain public `TheSingularityWorkshop.Renderer.RenderFrame` in `RenderFrame.cs`. It represents the platform-neutral view plus ordered draw-command snapshot.
- The adapter imports `TheSingularityWorkshop.Renderer` and maps the frame's view, ordered commands, transforms, resource IDs, index ranges, instance metadata, representation, and entity identity into Visualize contracts.

**Important:** the missing type is established; the cause is not. Do not treat this as a confirmed API defect or make an AnyApp-local shadow `RenderFrame` to silence the compiler. Please help trace whether the cause is project-reference resolution, the resolved Renderer assembly/source graph, namespace/API drift, or another reproducible configuration issue.

## Evidence and references

- [AnyApp project references](https://github.com/TrentBest/AnyApp/blob/development/AnyApp.csproj)
- [AnyApp .NET workflow](https://github.com/TrentBest/AnyApp/blob/development/.github/workflows/dotnet.yml)
- [Visualize adapter source](https://github.com/TrentBest/TheSingularityWorkshop.Visualize/blob/development/integrations/RendererAdapter/RendererFrameAdapter.cs)
- [Visualize adapter project](https://github.com/TrentBest/TheSingularityWorkshop.Visualize/blob/development/integrations/RendererAdapter/TheSingularityWorkshop.Renderer.VisualizeAdapter.csproj)
- [Renderer frame contract](https://github.com/TrentBest/TheSingularityWorkshop.Renderer/blob/development/RenderFrame.cs)
- [Renderer / Visualize boundary notes](https://github.com/TrentBest/TheSingularityWorkshop.Renderer/blob/development/docs/GPU_BOUNDARY.md)

## Shared responsibility boundary

```text
AnyApp host (WPF window, dispatcher, input, native surface lifetime)
    |
    | host services + selected Experience manifest
    v
FSM_COS (arbitration and composition; not the app loop)
    |
    +--> RuntimeAssembly / composed capabilities
    |
    +--> Renderer (observer-relative representation and frame decisions)
               |
               v
        RenderFrame + ordered RenderDrawCommand values
               |
               v
        Renderer.VisualizeAdapter (contract translation only)
               |
               v
        Visualize frame/resource contracts
               |
               v
        Visualize backend (CPU/SVG proof today; GPU backend only when proven)
               |
               v
        AnyApp-owned host surface adapter / visible pixels
```

- **AnyApp:** desktop lifecycle, manifest selection, host services, WPF window and dispatcher, input, DPI/size/visibility, native surface bridge, and backend lifetime.
- **FSM_COS:** composition and arbitration. AnyApp must not reimplement it.
- **Renderer:** observer-relative representation decisions, event horizons/freshness, and platform-neutral frame assembly.
- **Visualize core:** backend-neutral visualization frame/resource contracts.
- **Renderer.VisualizeAdapter:** explicit translation between the two public contracts; no hidden geometry upload, device ownership, or resource-lifetime guarantee.
- **Visualize backend / host surface:** concrete resources, GPU submission/presentation and disposal belong below the portable contracts; AnyApp owns the WPF/HWND-specific surface bridge.

The current `--renderer-proof` path is a WPF `DrawingContext` diagnostic projection. It is **not** proof of GPU-produced pixels. The GPU host integration plan is [here](https://github.com/TrentBest/AnyApp/blob/development/docs/GPU_HOST_INTEGRATION.md).

## Requests to Renderer / Visualize

Please answer with exact paths, types, resolved project/package versions, commands, and CI evidence where possible. Label proposals separately from implemented facts.

1. **Root cause of `RenderFrame` CS0246**
   - Can you reproduce the failing AnyApp source-graph build?
   - What exact Renderer project/assembly is referenced when the adapter compiles?
   - Does the Renderer compile output contain the public `RenderFrame` type in the assembly reference consumed by the adapter?
   - Is the repository-source property or source-root calculation causing a different project to be selected than intended?
   - What is the smallest fix at the correct ownership layer, and which focused command/test proves it?

2. **Stable adapter contract**
   - Confirm the current public frame contract and representation enum mapping.
   - Confirm which metadata is preserved and which is intentionally not carried by `VisualizerFrame` (including world/scene revision and source-entity identity mapping).
   - Identify any genuine contract mismatch rather than requesting host-specific exceptions.

3. **Resource resolution**
   - State the supported mechanism for resolving geometry/material/instance resource IDs in the current backend.
   - Clarify whether logical numeric IDs are only identifiers or have any defined ownership/lifetime semantics.
   - Identify the minimum deterministic test for unresolved resources and command-order preservation.

4. **GPU surface path**
   - Confirm the current status of `Visualize.GPU` and the backend/surface lifecycle contract.
   - Keep candidate API selection separate from a tested implementation.
   - State what evidence AnyApp must collect before calling the result GPU-rendered: native runtime loaded, actual surface created, real device/adapter identified, indexed workload submitted, visible GPU-produced pixels verified, and resize/minimize/shutdown exercised.

5. **Cross-host parity**
   - AnyApp is the WPF desktop host; WebPage is the browser host. Both should consume shared Renderer decisions and Visualize contracts without forking scene identity or renderer policy.
   - Please flag any recent API or build configuration change on `development` that could explain the current compile failure.

## Proposed acceptance gates

- [ ] The actual `RenderFrame` compile failure is reproduced and root-caused.
- [ ] The fix builds in the repository-source CI graph and the package-consumer graph where applicable.
- [ ] Renderer adapter contract tests pass and preserve command order, transforms, resource references, representation, and entity IDs.
- [ ] AnyApp composition/startup tests are able to run beyond this compile blocker.
- [ ] CPU/WPF diagnostics remain explicitly distinguished from GPU output.
- [ ] A GPU claim is made only after a real backend produces visible pixels on an identified adapter/device and lifecycle checks pass.
- [ ] No package publication, deployment, or merge to `master` occurs without the repository owner's approval.

## Renderer / Visualize response

*Please append your diagnosis and response below. If the root cause belongs to Visualize or Renderer, identify the smallest responsible-repository change and link the branch/commit or PR; do not silently work around it in AnyApp.*

- **Reproduction / command:**
- **Root cause and evidence:**
- **Resolved Renderer reference / version:**
- **Smallest correct fix:**
- **Focused tests / CI run:**
- **Adapter/resource contract findings:**
- **GPU backend and host-surface status:**
- **Questions for AnyApp:**
- **Acknowledgment / follow-through:**

## AnyApp follow-through

AnyApp will continue with host-owned startup, composition integration, runtime error handling, documentation, and verification while this dependency failure is investigated. No change to Renderer or Visualize is made by this handoff itself.
