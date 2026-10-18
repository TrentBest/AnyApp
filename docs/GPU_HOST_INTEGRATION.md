# AnyApp — Native GPU Host Integration

**Status:** host-integration plan; no graphics API selected and no GPU pixels claimed  
**Active branch:** `development`  
**Scope:** integrate Visualize GPU presentation into the existing AnyApp/WPF host without moving WPF or native graphics types into the portable composition contracts.

## Architectural contract

AnyApp is the first concrete host, not the owner of Experience composition semantics.

```text
Experience manifest
        |
        v
AnyApp host: select/load manifest + provide host services
        |
        v
FSM_COS: arbitrate and compose declared MicroBundles
        |
        v
RuntimeAssembly
        |
        +--> composed GUI / Experience capabilities
        |
        +--> Renderer capability --> Renderer frame
                                      |
                                      v
                              Visualize frame/resource contracts
                                      |
                                      v
                         Visualize.GPU backend (future)
                                      |
                                      v
                         AnyApp-owned WPF surface adapter
                                      |
                                      v
                           existing WPF window / pixels
```

The dependency direction matters:

- **FSM_COS** owns composition and arbitration; AnyApp must not duplicate them.
- **MicroBundles** provide reusable, versioned capabilities/content. A manifest requests those capabilities; it does not encode WPF controls or GPU handles.
- **Renderer** determines the representation/work appropriate to the observer and produces a frame/command stream.
- **Visualize core** owns backend-neutral frame/resource semantics.
- **Visualize.GPU** will own GPU buffers, pipelines, submission, synchronization, and GPU-resource disposal behind an API adapter.
- **AnyApp** owns the WPF window, dispatcher, input, DPI/size/visibility notifications, host-specific surface bridge, and backend lifetime. The host may supply narrow platform services through inversion of control, but those services must not leak WPF types into FSM_COS or Visualize core.
- **GUI.WPF** remains the semantic GUI-to-WPF presentation adapter. A GPU surface is a separate rendering path, not a reason to replace or entangle the semantic GUI renderer.

Windows/WPF is the first implementation target because AnyApp currently targets `net8.0-windows`, `win-x64`, and uses `MainWindow.xaml`'s existing `RootHost`. This does not claim that WPF itself is cross-platform. The reusable runtime/composition and Visualize contracts remain .NET/API-neutral; other hosts supply their own surface and platform adapters.

## Current verified baseline

- `ExperienceManifest` is serialized and host-neutral. It translates requested bundle IDs/versions into FSM_COS's `RuntimeManifest` and supplies version-scoped configuration separately.
- `AnyAppRuntime.ComposeAsync` routes composition through `FsmCos`, using the local catalog for local bundles and a repository-backed catalog for manifests declaring published artifacts.
- `MainWindow.LaunchManifestAsync` validates host semantic intent, calls the runtime composition path, and passes the resulting `RuntimeAssembly` to presentation selection.
- `RendererDesktopProofView` consumes the Renderer diagnostic workload and the Renderer-to-Visualize frame adapter, then draws a WPF `DrawingContext` projection. It is a CPU/WPF diagnostic, **not** a GPU rendering proof.
- The current Visualize GPU project defines API-neutral device/buffer contracts and workload encoding, but no selected native API backend or real surface presentation is available yet.

These facts are the baseline for the next integration; they are not claims that a GPU backend has been implemented.

## Backend audit checkpoint (candidate, not selected)

The current leading prototype candidate is native WebGPU through the Silk.NET 2.23.0 binding/runtime pair:

- [Silk.NET.WebGPU 2.23.0](https://www.nuget.org/packages/Silk.NET.WebGPU/2.23.0) provides the C# API binding.
- [Silk.NET.WebGPU.Native.WGPU 2.23.0](https://www.nuget.org/packages/Silk.NET.WebGPU.Native.WGPU/2.23.0) provides the native wgpu runtime package.
- The [native package MSBuild target](https://github.com/dotnet/Silk.NET/blob/main/src/Native/Silk.NET.WebGPU.Native.WGPU/build/net461/Silk.NET.WebGPU.Native.WGPU.targets) selects `wgpu_native.dll` for `win-x64` and copies it to build output when the Windows runtime is selected.
- Silk.NET's [surface helper](https://github.com/dotnet/Silk.NET/blob/main/src/WebGPU/Silk.NET.WebGPU/WebGPUSurface.cs) supports a Win32 HWND surface, and the [WebGPU native surface contract](https://webgpu-native.github.io/webgpu-headers/Surfaces.html) documents Windows HWND presentation.

This is evidence that a native WPF-hosted proof is technically plausible, not yet proof that the exact package pair works in AnyApp's publish modes. Before adoption, pin and restore the pair in an isolated spike, inspect the resolved `win-x64` assets and license notices, and verify the native library loads from the packaged output.

**Host integration constraint:** Silk.NET's helper accepts an `INativeWindowSource`; AnyApp's current WPF window is not itself that abstraction. Do not add Silk.NET.Windowing just to obtain a window if that creates a second window/event loop. The prototype should acquire the existing WPF host's native handle only after its lifetime is established, then pass that handle through a narrow host-owned bridge to WebGPU surface creation. Surface recreation, zero-size suspension, and orderly release remain mandatory.

The API is deliberately **not selected yet**. Keep the Visualize.GPU contracts independent, and compare this path against the existing shortlist before adding a native dependency to AnyApp.

## First native GPU slice

Keep the current `--renderer-proof` path as the CPU/WPF comparison. Add a separate, explicit GPU proof mode only after the backend candidate passes the package/runtime/surface audit.

1. **Backend gate:** audit the exact binding and native runtime package pair, target frameworks/RID assets, transitive licenses/notices, shader path, and WPF surface API. Do not add an API package to AnyApp just because it compiles.
2. **Host-owned surface:** attach to a visual/handle created by the existing WPF window after initialization. Do not create a second window or event loop. Keep WPF and HWND details in a narrow AnyApp adapter assembly or host-only layer.
3. **First pixels:** draw one indexed triangle from the existing canonical workload on a real surface. Record API, adapter/device, driver, runtime package versions, workload hash, and an image/readback proving the output. A queue submission or successful build alone is insufficient.
4. **Lifecycle:** handle nonzero resize and DPI changes, zero-size/minimized suspension, surface recreation, dispatcher affinity, in-flight resource lifetime, and deterministic shutdown. Do not add a perpetual render loop until the host cadence and visibility policy are explicit.
5. **Frame bridge:** consume the existing Renderer-to-Visualizer frame adapter and Visualize resource catalog. Do not generate a second workload or bypass the shared frame contract for the demo.
6. **Truthful diagnostics:** report backend/device and upload/submit/present facts separately. If GPU timestamps are unavailable, report GPU execution time as unavailable; never label CPU queue-submit duration as GPU time.
7. **Verification:** keep contract tests and Windows compile CI distinct from a device run. A native GPU proof is ready for user inspection only after the executable launches and visible GPU-produced pixels are verified on a named device or explicitly identified software adapter.

## Acceptance gates

- [ ] Existing manifest → FSM_COS → RuntimeAssembly path remains unchanged.
- [ ] Existing Moniker/GUI.WPF startup remains the default.
- [ ] Existing `--renderer-proof` remains an explicitly labelled CPU/WPF comparison.
- [ ] No WPF, HWND, Vulkan, WebGPU, or Direct3D type enters FSM_COS, Renderer core contracts, or Visualize core.
- [ ] GPU proof reuses the canonical workload and Visualize frame/resource contracts.
- [ ] Resize, minimization, resource retirement, and repeated shutdown are exercised.
- [ ] Windows CI compiles the adapter; real-device evidence is recorded separately.
- [ ] No package publication, release, merge to `master`, or branch creation is implied by this plan.

## Source references

- [AnyApp runtime composition](../AnyAppRuntime.cs)
- [Experience manifest](../ExperienceManifest.cs)
- [Native host startup](../MainWindow.xaml.cs)
- [Current WPF diagnostic view](../RendererDesktopProofView.cs)
- [Visualize GPU surface lifecycle plan](https://github.com/TrentBest/TheSingularityWorkshop.Visualize/blob/work/gpu-foundation/docs/GPU_SURFACE_LIFECYCLE_PLAN.md)
- [Visualize GPU backend implementation plan](https://github.com/TrentBest/TheSingularityWorkshop.Visualize/blob/work/gpu-foundation/docs/GPU_BACKEND_IMPLEMENTATION.md)
