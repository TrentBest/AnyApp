<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# 02 — AnyApp as a host: identity, composition, and experience

## Chapter purpose

This chapter develops a general theory of an application host using AnyApp as a concrete case. It is written so that the conceptual material can be lifted into a textbook independently of this repository. The implementation references are evidence and examples, not prerequisites for understanding the model.

**Central thesis:** an application host turns a selected, authorized, portable composition into a usable platform experience. It does not own the meaning of that composition, the rules that resolve it, or the execution semantics of its participants.

AnyApp is the Workshop's native Windows host. Its architectural value is not that it is a WPF application; it is that it demonstrates how a platform-specific application can host reusable, manifest-defined Experiences without absorbing their domain logic.

## Learning objectives

After this chapter, a reader should be able to:

1. Distinguish an Experience, a manifest, a MicroBundle, a RuntimeAssembly, and a host.
2. Explain why identity, discovery, authorization, composition, execution, and presentation are separate operations.
3. Assign responsibilities to the host and to the domain systems it consumes.
4. Trace a repository-backed Experience from selection to native presentation.
5. Explain why a successfully composed runtime is not necessarily an executable or interactive application.
6. Identify the evidence required before a host integration can be called complete.

## 1. The problem: portable meaning must meet a real platform

A portable composition can describe capabilities without knowing whether it will appear in a desktop window, browser tab, server process, headset, or another environment. A person, however, does not interact with an abstract composition. They interact through a platform: a display, input devices, process lifetime, storage, network connections, accessibility facilities, and operating-system security boundaries.

If the composition system owns these platform concerns, portability erodes. A browser consumer inherits desktop assumptions; a server consumer inherits UI dependencies; a new host must reproduce or work around platform-specific behavior.

If every host independently implements composition, the opposite failure occurs: different hosts can interpret the same Experience differently, resolve dependencies inconsistently, or accidentally create competing authorities for domain behavior.

The host architecture resolves this tension by separating **portable composition** from **platform manifestation**.

A host is therefore neither a thin window wrapper nor a second runtime engine. It is a policy-bearing adapter between a portable Experience system and a concrete environment.

## 2. Vocabulary: name the things that must not be conflated

| Term | Meaning | What it does not prove |
|---|---|---|
| **Experience** | A coherent authored application experience with domain meaning, identity, and behavior supplied by its owning domain and bundles. | That it is published, available, authorized, or runnable on a particular host. |
| **Manifest** | A versioned description of an Experience and the requested composition roots, configuration, and relevant immutable references. | That every referenced artifact exists, is trusted, or is compatible. |
| **MicroBundle** | A reusable unit of capability or presentation behavior that participates in the Workshop composition model. | That every loaded bundle is an executable task or has a universal lifecycle. |
| **Artifact identity** | A stable reference to a particular immutable published artifact, typically including version and content digest. | That the artifact has been retrieved, verified, authorized, or activated. |
| **RuntimeAssembly** | The typed result produced by FSM_COS composition: a runtime-scoped assembly of the actual loaded bundle participants and associated composition context. | That the assembly has a general execution scheduler or can be translated into another workflow model. |
| **Host** | The platform-facing application that selects, authorizes, composes, presents, and manages the user-facing process boundary. | Ownership of domain rules, dependency resolution, or another repository's execution lifecycle. |
| **Presentation surface** | A semantic or platform-specific representation through which a person observes and interacts with an Experience. | Authority to decide the Experience's domain state transitions. |

The distinctions are operational. A manifest can be valid while an artifact is unavailable. An artifact can be retrieved while its bytes fail verification. A verified artifact can be compatible with the composition contract but not with a particular host. A RuntimeAssembly can be successfully composed while the execution contract for its participants remains undefined.

A robust host reports these states separately instead of collapsing them into a single Boolean called “ready.”

## 3. The host boundary as a sequence of decisions

A useful abstract pipeline is:

[
	ext{Discover} ightarrow 	ext{Identify} ightarrow 	ext{Authorize}
ightarrow 	ext{Resolve} ightarrow 	ext{Compose}
ightarrow 	ext{Execute} ightarrow 	ext{Present}
]

This sequence is a conceptual map, not a claim that every operation belongs to AnyApp or that every Experience requires an executable phase.

### 3.1 Discover

Discovery answers: *What authored or published Experiences and capabilities can be described to this caller?*

Discovery is a bounded information operation. It may return titles, descriptions, stable identifiers, authored metadata, publication references, and provenance. It does not grant access to private content, retrieve arbitrary source files, or activate a capability.

Authored content discovery and capability/provider discovery are different query families. A tutorial is content; a provider declaration is a claim that a capability can be supplied under a particular contract. A bundle name alone is not evidence of a provider declaration.

### 3.2 Identify

Identification pins the selected object to an unambiguous identity and revision. For published artifacts, immutable version and digest references prevent a moving label such as “latest” from silently changing the bytes that a composition request means.

Identity must survive the whole path: user selection, manifest retrieval, artifact closure, materialization, composition, diagnostics, and later audit.

### 3.3 Authorize

Authorization is a host and service boundary, not a property of a manifest or a model response. The host determines which principal may discover, retrieve, select, or activate a resource, under which scope and policy.

A public identifier is not a credential. A provider declaration is not permission to invoke it. Discovery is not authorization. The host must enforce access before disclosing protected metadata or content and again at the operation that requires authority.

### 3.4 Resolve and compose

Repository delivery and dependency resolution are distinct from composition. The repository supplies immutable artifact bytes and verification evidence. FSM_COS owns MicroBundle dependency resolution, loading, arbitration, and construction of the RuntimeAssembly.

AnyApp supplies the selected manifest and applicable configuration, then consumes the typed result. It must not create a parallel resolver or infer dependency order from presentation order.

### 3.5 Execute

Execution is a separate concern from composition. A loaded participant is not automatically a work item, and a provider identifier is not an executable object reference. Scheduling, activation, cancellation, outcomes, and cleanup require an explicit execution contract owned by the execution system.

Consequently, the host must not turn a RuntimeAssembly into a synthetic workflow merely because a workflow runner already exists. Similar-looking concepts are not interchangeable contracts.

### 3.6 Present

Presentation translates semantic output into platform affordances: windows, controls, rendering surfaces, input capture, navigation, accessibility, and native lifecycle integration.

The host can decide how a semantic button is drawn. The Experience's domain implementation decides what the corresponding action means and whether it is valid. A click is an input event, not proof that a domain transition succeeded.

## 4. Responsibility allocation

The architecture is easiest to preserve when each subsystem has one authoritative job.

| System | Authoritative responsibility | Host relationship |
|---|---|---|
| **AnyApp** | Native process/window lifecycle, host policy, launch selection, platform adapters, presentation, and host-side orchestration. | Owns the Windows manifestation. |
| **Experiences** | Authored Experience definitions, manifests, semantic requirements, and domain-owned authored metadata. | Supplies what can be selected and what the composition requests. |
| **MicroBundleRepository** | Immutable artifact identity, delivery, and byte-level verification. | Supplies artifact material; it does not decide domain meaning or host authorization. |
| **FSM_COS** | MicroBundle resolution, loading, dependency closure, arbitration, and RuntimeAssembly construction. | Receives the composition request and returns a typed assembly. |
| **FSM_Execution** | The execution lifecycle and scheduling contracts it explicitly supports. | Receives only the typed execution handoff defined by its owner; the host does not substitute a local runner. |
| **GUI / Renderer / Visualize** | Their respective semantic presentation, rendering, and visualization contracts. | Are consumed through their own integration boundaries; Renderer and Visualize are MicroBundle experiences, not assumed NuGet runtime packages. |
| **HeadlessAi** | Provider-neutral model invocation. | May be an optional host capability; it does not authorize access or grant tools to a model. |

A host may orchestrate calls across these systems, but orchestration does not transfer ownership. An adapter translates between contracts; it does not become the authority behind either contract.

## 5. Worked example: launching a repository-backed Experience

Consider a person selecting a published Experience in AnyApp.

1. **Selection:** AnyApp receives a selected Experience identity. The identity is not trusted merely because it arrived from a UI control or deep link.
2. **Publication lookup:** the host obtains the publication record and requests the matching manifest.
3. **Pinning:** the host checks that the returned manifest identity and version match the selected publication. Artifact references remain immutable.
4. **Artifact preparation:** the repository catalog preloads the required artifact closure and associated configuration. Each artifact is checked against its immutable identity before materialization.
5. **Composition:** AnyApp converts the manifest to the FSM_COS runtime manifest and invokes FSM_COS. FSM_COS resolves and loads the MicroBundles, arbitrates, and returns a RuntimeAssembly.
6. **Presentation:** AnyApp discovers the supported semantic GUI surface and adapts it to WPF. The desktop host owns the window and native controls; the Experience retains its domain meaning.
7. **Interaction:** native input is not allowed to mutate authoritative domain state by itself. A portable typed interaction and execution contract must exist before general Experience actions can be routed. Until then, host-specific navigation is only host-specific navigation.

This example deliberately ends with a qualification: successful composition and visible presentation do not prove that arbitrary Experience behavior is executable. The boundary is honest about what is implemented and what remains a dependency.

## 6. Failure semantics: preserve the reason a step failed

A host should preserve distinct failure classes because each implies a different safe response.

- **Not discoverable:** no matching item was returned within the caller's permitted discovery scope.
- **Not authorized:** the identity may exist, but the caller is not permitted to learn or use the protected resource.
- **Stale identity:** the selected publication or revision no longer matches the returned manifest/artifact identity.
- **Unavailable artifact:** the repository cannot supply the required immutable bytes.
- **Integrity failure:** retrieved bytes do not match the expected digest or artifact structure.
- **Composition failure:** FSM_COS cannot resolve, load, or arbitrate the requested composition.
- **Host incompatibility:** the composition cannot be manifested by the current platform adapter.
- **Execution unavailable:** the assembly exists, but no compatible, explicitly registered execution contract is available.
- **Presentation failure:** the semantic surface cannot be represented by the current host.
- **User cancellation:** the caller intentionally stopped an operation; this must not be disguised as offline success.

The correct response is not always to retry. A network timeout may permit use of a previously verified local cache under explicit recovery policy. A digest mismatch must not. An authorization denial must not fall back to an unprotected path. A missing execution contract must not cause the host to invent one.

## 7. Design invariants

The following invariants are suitable as review questions and test properties:

1. **One owner per authority.** A host adapter may translate a contract but may not silently duplicate its authoritative resolver, scheduler, domain state, or repository.
2. **Identity is stable end to end.** A selected immutable revision remains identifiable through retrieval, materialization, composition, diagnostics, and audit.
3. **Discovery is read-only and bounded.** Query size, result count, excerpt size, retrieval scope, time, and cancellation are constrained.
4. **Authorization precedes disclosure and action.** A model, manifest, identifier, or UI event cannot grant itself authority.
5. **Composition is not execution.** A RuntimeAssembly is not reinterpreted as a different execution model.
6. **Presentation is not domain authority.** Native controls render and collect input; domain owners validate and apply semantic actions.
7. **Failure is explicit.** Missing, stale, invalid, incompatible, unauthorized, cancelled, and unavailable states remain distinguishable.
8. **Evidence matches the claim.** Source inspection proves structure; deterministic tests prove covered behavior; a Windows run proves only the path actually exercised on that machine.

## 8. Implementation map and evidence limits

The following source paths on the AnyApp `development` branch illustrate the host boundary:

| Concern | Source | What it demonstrates |
|---|---|---|
| Composition entry point | [AnyAppRuntime.cs](../../AnyAppRuntime.cs) | Local and repository-backed composition delegate to FSM_COS and return RuntimeAssembly. |
| Native application lifecycle | [MainWindow.xaml.cs](../../MainWindow.xaml.cs) | WPF startup, launch selection, shell navigation, and native presentation. |
| Startup reconciliation | [ExperienceStartupCoordinator.cs](../../ExperienceStartupCoordinator.cs) | Cached-manifest/publication reconciliation with explicit network-unavailable and cancellation behavior. |
| Repository artifact catalog | [RepositoryMicroBundleCatalog.cs](../../RepositoryMicroBundleCatalog.cs) | Host-side adapter to repository artifact retrieval and closure preparation. |
| Host contract | [EXPERIENCE_HOST_ADAPTER_CONTRACT.md](../EXPERIENCE_HOST_ADAPTER_CONTRACT.md) | Current boundaries, missing typed interaction, and upstream execution dependency. |
| Verification | [AnyApp tests](../../tests/TheSingularityWorkshop.AnyApp.Tests) | Deterministic evidence for the behaviors covered by the test suite. |

Source presence is not proof of every production property. A local fake HTTP endpoint does not prove remote availability. A successful composition test does not prove general execution. A semantic GUI node does not prove portable interaction. A green CI run does not prove that a person has used the complete desktop experience.

## 9. Exercises

1. A manifest references an artifact with a valid-looking digest, but the repository returns different bytes. Which stage failed, who owns the decision, and why must the host stop?
2. A model discovers a capability provider and asks the host to invoke it. List the separate authorization, compatibility, and execution checks that must occur before invocation.
3. Two hosts present the same Experience on WPF and in a browser. Which contracts should remain identical, and which adapters should differ?
4. Explain why a presentation-only MicroBundle must remain valid even if no execution adapter exists.
5. Design a test proving that cancelling one runtime does not stop another independently composed runtime.
6. Identify a claim in a README that is stronger than its linked test evidence. Rewrite it to match the evidence.

## Further reading

- [Theory path](README.md) — chapter sequence and learning map.
- [Experience host adapter contract](../EXPERIENCE_HOST_ADAPTER_CONTRACT.md) — exact current host boundary.
- [Startup and offline recovery](../EXPERIENCE-STARTUP-AND-OFFLINE-RECOVERY.md) — cache and catalog reconciliation.
- [FSM_COS](https://github.com/TrentBest/TheSingularityWorkshop.FSM_COS/tree/development) — composition authority.
- [FSM_Execution](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Execution/tree/development) — execution authority and pending RuntimeAssembly handoff.

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
