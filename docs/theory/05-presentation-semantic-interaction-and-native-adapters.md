<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# 05 — Presentation, semantic interaction, and the native adapter

**Status:** source-checked theory chapter  
**Implementation baseline:** development

## Learning objectives

After this chapter, you should be able to distinguish semantic presentation from native controls and pixels; explain why a host adapter must not own domain behavior; trace AnyApp's current semantic GUI path into WPF; distinguish GUI presentation, Renderer frame production, Visualize adaptation, and GPU output; and design tests for identity, provenance, observer isolation, and lifecycle.

## 1. The portability problem

A reusable Experience should not need to know whether it is presented in a Windows desktop window, a browser, a server-rendered surface, or a future VR environment. Yet people interact with concrete text, controls, images, spatial objects, animation, and feedback on a particular device.

This creates a translation problem:

- the Experience/domain defines meaning and authoritative behavior;
- a presentation contract describes what can be shown and which semantic actions may be offered;
- a host adapts that contract to its platform;
- the platform displays pixels and captures physical input.

If those layers collapse, portability disappears. WPF controls leak into reusable domain code, or the host starts implementing Experience rules because a click handler is convenient.

> **Portable semantics cross the host boundary; native mechanics stay on the host side.**

## 2. Five distinct concepts

| Concept | Meaning | Primary owner |
|---|---|---|
| Domain state and rules | Authoritative facts, validation, and state transitions | Experience/domain MicroBundles |
| Semantic presentation | Platform-neutral description of content, structure, and affordances | GUI / Experience presentation contracts |
| Native adapter | Translation into controls, input events, focus, accessibility, and host lifecycle | AnyApp for WPF |
| Render frame | Observer-appropriate representation of visual work | Renderer |
| Backend output | Actual drawing, resource use, and presentation on a device | Visualize backend + host surface adapter |

A semantic node is not a WPF Button. A WPF Button is not an authoritative domain action. A Renderer frame is not a GPU command buffer. A GPU surface is not the Experience itself.

## 3. The current AnyApp semantic GUI path

The current desktop host has a source-backed path from a semantic root to WPF controls:

    Composed RuntimeAssembly
              |
              v
    Selected GUI / Experience capability
              |
              v
    Semantic GuiNode root
              |
              v
    AnyApp host adapter
              |
              v
    GUI.WPF renderer
              |
              v
    Native WPF controls in the existing window

The host compatibility surface currently exposes a root node through IAnyAppSurface:

    public interface IAnyAppSurface
    {
        GuiNode Root { get; }
    }

The host can also discover the shared GUI MicroBundle surface through MicroBundleGuiSurface.TryGetRoot. The selected semantic root is passed to WpfGuiRenderer.Render, which creates the native WPF presentation.

This proves that a composed capability can expose a semantic GUI root and AnyApp can adapt it to WPF. It does not define a general typed Experience action/event contract, make a GUI surface an executable participant, prove that every MicroBundle is interactive, or move authoritative domain rules into the host.

AnyApp also contains shell-specific navigation and WebBridge command handling. Those are host integrations, not the canonical cross-host interaction contract.

## 4. Presentation is not interaction authority

A visible affordance can invite an action, but the control itself cannot decide whether that action is valid.

Consider an Experience showing a door:

1. The Experience exposes semantic state saying the door is visible and currently available.
2. The presentation model describes a door affordance.
3. AnyApp maps it to a native control.
4. A person activates the control.
5. AnyApp captures native input and translates it into the agreed typed action.
6. The authoritative Experience/domain participant validates and applies the action.
7. The host receives a typed outcome or updated state and presents it.

The critical step is not the click. It is the domain-owned validation and state transition.

A host must not turn a presentation identifier into an implicit command, infer executable behavior from a provider ID, or treat a loaded bundle as a runnable participant. The current IAnyAppSurface.Root contract exposes presentation structure only. A portable interaction contract remains a separate cross-repository requirement coordinated with Experiences, FSM_COS, and FSM_Execution.

The eventual contract needs typed action identity and payload, versioning, validation, runtime scope, authority, execution routing, observable outcomes, cancellation, and lifecycle rejection. Its concrete types must come from the owning repositories; AnyApp should adapt them rather than invent a competing protocol.

## 5. Renderer and Visualize are a separate pipeline

Semantic GUI presentation and visual rendering solve different problems. AnyApp's diagnostic Renderer path makes that distinction concrete:

    Canonical diagnostic workload
              |
              v
    Renderer representation selection
              |
              v
    Immutable Renderer RenderFrame
              |
              v
    Visualize frame adapter
              |
              v
    VisualizerFrame
              |
              v
    WPF diagnostic projection

The diagnostic workload gives each stable entity multiple representation candidates. Renderer selects a permitted representation for the observer's current view/detail constraints. The resulting frame is adapted through the Renderer-to-Visualize frame contract. AnyApp then draws a diagnostic projection using WPF.

The current proof also models observer provenance: observer identity and revision, source revision, frame sequence, one immutable frame associated with that evaluation, and host panel routing that fans the same completed envelope/frame to multiple panels.

Two observers may inspect the same source revision while retaining separate observer revisions and frame identities. Changing one observer must not mutate the other's panel state or invalidate its representation cache. Current deterministic tests cover this isolation and same-frame fan-out.

This is an architecture proof with a strict evidence boundary: the visible diagnostic marks are CPU/WPF projections. They are not GPU-produced pixels, a production Visualize backend, or a geometry-quality benchmark.

## 6. A GPU backend does not replace the semantic host

The planned GPU path adds a device-specific implementation after the shared frame/resource contracts:

    Renderer frame
          |
          v
    Visualize core frame/resource semantics
          |
          v
    Visualize.GPU backend
          |
          v
    AnyApp-owned WPF surface bridge
          |
          v
    Existing native window

Renderer decides which representation is appropriate and produces the frame. Visualize core describes backend-neutral frame/resource semantics. Visualize.GPU will own GPU buffers, pipelines, submission, synchronization, and GPU resource disposal. AnyApp owns the WPF window, dispatcher, input, DPI/size/visibility notifications, native surface bridge, and backend lifetime. GUI.WPF remains the semantic GUI-to-WPF adapter; a GPU surface is a separate rendering path.

No graphics API is selected by the current integration plan. A candidate binding or successful compilation is not evidence of a working backend. A real GPU proof requires visible GPU-produced pixels on an identified device or explicitly identified software adapter, plus lifecycle evidence for resize, minimization, resource retirement, and shutdown.

## 7. Lifecycle and failure semantics

Presentation adapters are stateful resources. A robust host must account for more than constructing a control tree once.

| Event | Host-side responsibility | Domain/runtime responsibility |
|---|---|---|
| Initial presentation | Create native surface and adapt current semantic state | Supply authoritative state/presentation data |
| Native input | Capture and translate input to a typed action | Validate and apply the action |
| State update | Replace or reconcile the displayed projection | Publish authoritative state |
| Resize / DPI change | Update platform surface and layout | Remain platform-independent |
| Minimize / zero-size surface | Suspend or defer backend work safely | Continue according to runtime policy |
| Capability unavailable | Present a truthful unavailable state | Report capability/readiness accurately |
| Runtime stopping | Reject new submissions and detach presentation safely | Execution owner cancels and quiesces work |
| Window closing | Release host-owned controls/surface resources | Execution owner performs runtime cleanup |

A stale frame should not silently become current merely because it is still drawable. Presentation should retain source and observer provenance so the host can identify which state a user is seeing. Input should be associated with the runtime identity and revision it targets; stale or stopped runtimes must reject it rather than redirecting it to a newer Experience.

## 8. Test boundaries, not just screenshots

A screenshot can prove that something was drawn. It cannot prove that the correct semantic state produced it, that an action reached the right participant, or that one observer did not corrupt another.

**Semantic adapter tests**
- A known semantic root maps to the expected native control structure.
- Missing or unsupported elements fail visibly and predictably.
- Rendering does not mutate domain state.
- Native event translation preserves typed action identity and runtime scope.

**Interaction contract tests**
- Valid actions reach the intended participant.
- Malformed, stale, unauthorized, or unsupported actions are rejected.
- Authoritative state changes originate in the Experience/domain participant.
- Outcomes and failures are observable by the host.
- A presentation-only bundle is never treated as executable.

**Frame/provenance tests**
- The same completed frame can be routed to multiple panels without reconstruction.
- Observer revisions and source revisions remain distinguishable.
- Updating one observer does not mutate another observer's frame or cache.
- Stale frames are not presented as current without an explicit policy.

**Native/backend lifecycle tests**
- Dispatcher affinity and native surface ownership are respected.
- Resize, DPI, minimization, and surface recreation are exercised.
- In-flight resources are retired safely.
- Repeated shutdown is deterministic.
- GPU claims are backed by actual device/pixel evidence, not only compilation or queue submission.

## 9. Design invariants

1. **Meaning precedes mechanics.** Native controls are projections of semantic presentation, not the source of domain truth.
2. **The host adapts; it does not author domain rules.** WPF event handlers must not become a second Experience implementation.
3. **Presentation is not execution.** A root node, provider declaration, or loaded bundle does not imply an executable participant.
4. **Frames carry provenance.** Source, observer, and frame identity remain traceable through routing and presentation.
5. **Observers are isolated.** One observer's view changes must not invalidate or mutate another observer's state.
6. **Backend claims require backend evidence.** A CPU/WPF projection is not GPU rendering.
7. **Lifecycle ownership is explicit.** AnyApp owns native surfaces; FSM_Execution owns execution lifecycle once its typed handoff exists.
8. **Shared contracts stay platform-neutral.** WPF, HWND, and graphics API types remain outside portable composition and semantic contracts.

## 10. Evidence and limits

This chapter is grounded in the AnyApp development source and current host integration documents:

- [Experience host adapter contract](../EXPERIENCE_HOST_ADAPTER_CONTRACT.md)
- [GPU host integration plan](../GPU_HOST_INTEGRATION.md)
- [Native host implementation](../../MainWindow.xaml.cs)
- [Current WPF diagnostic view](../../RendererDesktopProofView.cs)
- [AnyApp runtime composition](../../AnyAppRuntime.cs)

The source-backed baseline includes a semantic GUI root adapted to WPF and a Renderer-to-Visualize diagnostic frame path projected through WPF. The portable typed interaction contract and RuntimeAssembly-to-FSM_Execution handoff remain separate upstream gaps. The native GPU backend remains a plan, not a completed implementation.

This is a source and contract review, not a claim that every Experience, device, accessibility path, or native backend has been exhaustively tested.

## 11. Exercises

1. A GUI node describes a Purchase button. List what the presentation layer may display and what validation must remain in the authoritative domain participant.
2. Two observers inspect the same world revision from different viewpoints. Design the minimum provenance tuple needed to keep their frames distinguishable.
3. A WPF control still displays a frame from a runtime that has stopped. Define the host and execution-owner responsibilities that prevent a click from being applied to a replacement runtime.
4. Explain why a successful WebGPU queue submission is weaker evidence than a verified image produced on a named device.
5. Design a test proving a GUI adapter can re-render updated semantic state without mutating source domain state.

## Further reading

- [Theory chapter 02 — AnyApp as a host](02-anyapp-as-a-host.md)
- [Theory chapter 04 — Trust boundaries and safe host orchestration](04-trust-boundaries-and-safe-host-orchestration.md)
- [Experience host adapter contract](../EXPERIENCE_HOST_ADAPTER_CONTRACT.md)
- [GPU host integration](../GPU_HOST_INTEGRATION.md)

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
