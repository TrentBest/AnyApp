# Experience host adapter contract

**Status:** source-backed host contract and integration boundary  
**Branch:** `development`  
**Audience:** Experience authors, native/web host implementers, FSM_COS and FSM_Foundry maintainers

This document defines what AnyApp currently accepts from a composed Experience, what it owns as a host, and what remains an explicit cross-repository contract dependency. It is deliberately not an execution contract for FSM_Foundry and does not invent a second Experience runtime.

## 1. Architectural invariant

An Experience is authored once as a manifest-defined composition of MicroBundles. Hosts discover that Experience, request composition from FSM_COS, and present the resulting semantic surface through their native platform.

```text
Experience identity + manifest
          |
          v
     Host discovery
          |
          v
      FSM_COS
          |
          v
    RuntimeAssembly
       /       \
      v         v
Presentation   FSM_Foundry
adapter        typed execution
(AnyApp)       (upstream contract pending)
      \         /
       v       v
     Host-observable Experience
```

AnyApp is a host/presentation adapter. It is not the owner of Experience rules, a MicroBundle scheduler, or an alternative executor. The RuntimeAssembly-to-Foundry path is tracked separately by [AnyApp #46](https://github.com/TrentBest/AnyApp/issues/46) and [FSM_Foundry #23](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Foundry/issues/23).

## 2. Current source-backed host surface

The following describes the implementation on `development`; it does not imply that every planned cross-host capability already exists.

### 2.1 Discovery and identity

AnyApp can enter an Experience through:
- a local JSON manifest loaded by `ExperienceManifest.LoadFile`;
- the repository catalogue, through `ExperienceCatalogClient.ListAsync` and `GetManifestAsync`;
- a published Experience deep link, identified by Experience ID, version, and content hash;
- startup reconciliation of the last local manifest/publication with the available catalogue.

The manifest is the composition request. Its ordered bundle roots, immutable artifact identities, resolved configuration identities, optional semantic intent, and resolved dependency plan are converted into an FSM_COS `RuntimeManifest`. Artifact closure is preloaded before repository-backed composition.

Relevant implementation:
- [ExperienceManifest.cs](../ExperienceManifest.cs)
- [ExperienceCatalogClient.cs](../ExperienceCatalogClient.cs)
- [AnyAppLaunchRequest.cs](../AnyAppLaunchRequest.cs)
- [ExperienceStartupCoordinator.cs](../ExperienceStartupCoordinator.cs)
- [AnyAppRuntime.cs](../AnyAppRuntime.cs)

### 2.2 Composition and manifestation

The host calls `AnyAppRuntime.Compose` or `ComposeAsync`. These create an FSM_COS composition instance and return its actual typed `RuntimeAssembly`. AnyApp does not flatten the assembly into JSON or reinterpret the dependency plan as execution order.

For presentation, the host selects a composed bundle and asks the GUI surface adapter for its semantic root. The current compatibility surface is:

```csharp
public interface IAnyAppSurface
{
    GuiNode Root { get; }
}
```

The current host also supports the shared GUI MicroBundle surface discovery path through `MicroBundleGuiSurface.TryGetRoot`. The WPF adapter turns the resulting semantic `GuiNode` into native controls.

Relevant implementation:
- [IAnyAppSurface.cs](../IAnyAppSurface.cs)
- [MainWindow.xaml.cs](../MainWindow.xaml.cs)
- [AnyAppRuntime.cs](../AnyAppRuntime.cs)

### 2.3 Interaction: current limit

The current `IAnyAppSurface` contract exposes a root node only. It does **not** define a general typed Experience action/event contract, an execution participant, or a way to submit an action to Foundry.

AnyApp currently contains host-specific interaction paths, including Forge navigation wiring and a local WebBridge command switch. Those are presentation/shell integrations; they are not the canonical, portable Experience interaction contract and must not be mistaken for one.

The Experience domain owns authoritative, host-independent semantics. A host may translate native input into the agreed typed interaction value and forward it, but it must not implement game rules or other Experience domain behavior locally.

## 3. Ownership boundaries

| Concern | Owner | AnyApp responsibility |
|---|---|---|
| Experience identity, manifest, required MicroBundles and artifact closure | Experiences / publication contract | Discover and validate the selected identity and manifest |
| Bundle resolution, loading, dependency ordering, arbitration | FSM_COS and MicroBundleDomain | Supply the manifest/configuration and consume the resulting assembly |
| Semantic Experience behavior and authoritative state transitions | Experience MicroBundles | Present state and forward typed user actions |
| Typed executable participant/provider mapping | Domain + FSM_COS + FSM_Foundry contract | Do not infer executability from a loaded bundle or GUI surface |
| Scheduling, activation, cancellation, outcomes and cleanup | FSM_Foundry | Submit the typed assembly/plan when the upstream contract exists; present lifecycle state |
| Native windowing, input capture, WPF presentation and host navigation | AnyApp | Implement the Windows adapter |
| Browser, server and VR presentation/input | Their respective hosts | Keep the same Experience identity and semantics |

## 4. Required portable interaction contract

The general Experience interaction boundary is not yet implemented in AnyApp. It must be agreed with the Experience/domain owners before a host-specific API is promoted as canonical.

The contract must provide, at minimum:

1. **Typed identity:** an action is associated with the Experience/runtime identity and the intended participant or capability; it is not an unscoped string command.
2. **Typed payload:** the action carries a domain-defined value with explicit validation and versioning. Hosts do not invent or reinterpret domain rules.
3. **Host neutrality:** the same semantic action can originate from WPF, browser, server, or VR input adapters.
4. **Authority:** the Experience/domain implementation validates and applies the action. The host is not authoritative for game state or business rules.
5. **Execution routing:** if an action requires executable work, it is routed through the agreed Foundry-owned execution session; AnyApp does not tick FSM_API groups or call bundle lifecycle methods itself.
6. **Observable result:** the host receives a typed outcome/state update suitable for presentation, including rejected/invalid actions and execution failure.
7. **Lifecycle safety:** action submission is rejected once the relevant runtime is stopping or disposed; cancellation and cleanup remain Foundry-owned.

The concrete public types, capability registration, participant discovery, ordering, and scheduling semantics must be taken from the owning cross-repository contracts. This document intentionally does not define competing types for them.

## 5. RuntimeAssembly → Foundry handoff

A loaded `IMicroBundle` is not automatically executable. The current MicroBundle contract provides composition/loading and arbitration; provider IDs are declarations, not executable object references. FSM_COS returns a typed `RuntimeAssembly`, but does not execute it.

Foundry must define the typed provider/participant adapter and own:
- binding a loaded provider identity to an explicit compatible typed adapter;
- rejecting missing, ambiguous, or incompatible registrations;
- runtime-scoped FSM_API definitions/handles and exclusive scheduling ownership;
- tick ordering and cancellation;
- quiescing in-flight work before cleanup;
- terminal outcomes and observable lifecycle state.

In particular, group-wide FSM_API updates/removal cannot be treated as per-runtime operations unless Foundry proves exclusive group ownership. Exact handle/definition ownership is required to prevent one Experience from advancing or destroying another.

See [FSM_Foundry #23](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Foundry/issues/23) for the authoritative upstream work. AnyApp must integrate with that typed contract once it exists; it must not bridge through a synthetic `WorkflowDefinition`, a local runner, or per-Experience process launching.

## 6. Acceptance tests for the host boundary

The AnyApp host integration is complete only when tests demonstrate all of the following:

- A published identity resolves to the matching manifest and immutable artifact closure.
- The same manifest composes through FSM_COS and yields the actual `RuntimeAssembly`.
- AnyApp discovers and presents a semantic surface from that assembly without owning Experience rules.
- A host input is converted to the agreed typed action and routed to the correct Experience participant.
- The Experience's authoritative state/result returns through the execution contract and is presented by AnyApp.
- A presentation-only MicroBundle remains valid and is not incorrectly treated as executable.
- Two concurrently composed runtimes remain isolated when one is ticked, cancelled, or disposed.
- Missing/ambiguous providers, invalid actions, cancellation, execution faults, and cleanup are observable and fail closed.
- The same Experience identity and typed action semantics remain consumable by WebApp and later MyVR; no AnyApp dependency is introduced into the Experience domain.

Until these tests pass against the real development source graph, describe the handoff as planned or blocked—not implemented.

## 7. Change discipline

- Change the canonical domain/interaction contract in its owning repository, then consume it from AnyApp.
- Keep AnyApp-specific WPF details behind the host adapter.
- Keep composition, execution, and presentation as separate capabilities.
- Update this document when source or tests change; source and executable tests remain authoritative.
- Do not publish packages, merge release branches, deploy, or alter `master` as part of this development integration.
