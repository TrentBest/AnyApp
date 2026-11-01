<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# 01 — The native host boundary

## 01 — What: a host makes a composed runtime usable on a platform

A composition engine can assemble capabilities without owning a desktop window, keyboard input, process lifetime, or native presentation surface. A native host supplies those platform-facing responsibilities so a composed runtime can become an application a person can launch and observe.

AnyApp is the Workshop's native Windows desktop host. It selects an Experience manifest, obtains the MicroBundles needed by that manifest, delegates composition to FSM_COS, and provides the native application surface around the result.

A useful analogy is a theatre: the composition describes which performers and roles are present; the host provides the building, doors, lighting controls, and audience-facing stage. The analogy stops at the boundary: FSM_COS is not a theatre manager, and AnyApp must not recreate its composition semantics.

## 02 — Why: keep portable composition separate from native concerns

If windowing, WPF, HWND handles, or a GPU API leak into portable composition contracts, every consumer inherits desktop assumptions. A browser or server host would then need to carry dependencies it does not own.

The separation has a cost: AnyApp needs adapters between portable runtime/presentation contracts and native facilities. The benefit is that the same Experience composition can be hosted by different platforms without making the composition engine platform-specific.

AnyApp is not the Experience, scheduler, MicroBundle repository, or authoritative composition engine. The current Foundry host-boundary work is separately tracked in [issue #27](https://github.com/TrentBest/AnyApp/issues/27); do not claim that Foundry already owns AnyApp's runtime lifecycle until that integration is implemented and tested.

## 03 — How: manifest, catalog, composition, host

The source-backed composition path has four conceptual stages:

1. **Select a manifest.** The host obtains an `ExperienceManifest` describing the Experience and its requested bundles.
2. **Resolve capabilities.** For local development, `AnyAppMicroBundleCatalog` resolves source-backed bundles. For repository-backed manifests, `RepositoryMicroBundleCatalog` validates immutable artifact identities and preloads the dependency closure.
3. **Materialize and compose.** The host materializes verified artifact bytes into `IMicroBundle` implementations and calls FSM_COS to create a `RuntimeAssembly`.
4. **Present through the host.** AnyApp owns native startup and presentation integration. Shared semantic GUI and rendering contracts remain in their owning repositories.

The implemented entry point is deliberately small:

```csharp
var runtime = AnyAppRuntime.Compose(manifest);
```

For repository-backed Experiences, `AnyAppRuntime.ComposeAsync` preloads the closure and resolved configuration before calling FSM_COS. It does not turn AnyApp into a second composition engine.

## 04 — See it: trace the contract in source and tests

Source mapping on `development`:

| Concept | Implementation | Evidence |
|---|---|---|
| Local composition entry point | [AnyAppRuntime.cs](../../AnyAppRuntime.cs) | [AnyAppRuntimeTests.cs](../../tests/TheSingularityWorkshop.AnyApp.Tests/AnyAppRuntimeTests.cs) |
| Launch request parsing | [AnyAppLaunchRequest.cs](../../AnyAppLaunchRequest.cs) | [AnyAppLaunchRequestTests.cs](../../tests/TheSingularityWorkshop.AnyApp.Tests/AnyAppLaunchRequestTests.cs) |
| Startup reconciliation | [ExperienceStartupCoordinator.cs](../../ExperienceStartupCoordinator.cs) | [ExperienceStartupCoordinatorTests.cs](../../tests/TheSingularityWorkshop.AnyApp.Tests/ExperienceStartupCoordinatorTests.cs) |
| Repository catalog and artifact closure | [RepositoryMicroBundleCatalog.cs](../../RepositoryMicroBundleCatalog.cs) | [RepositoryMicroBundleCatalogIntegrationTests.cs](../../tests/TheSingularityWorkshop.AnyApp.Tests/RepositoryMicroBundleCatalogIntegrationTests.cs) |
| Assembly materialization | [AssemblyMicroBundleArtifactMaterializer.cs](../../Composition/AssemblyMicroBundleArtifactMaterializer.cs) | [RepositoryMicroBundleCatalogIntegrationTests.cs](../../tests/TheSingularityWorkshop.AnyApp.Tests/RepositoryMicroBundleCatalogIntegrationTests.cs) |

The integration test uses a local fake HTTP endpoint and real REST adapter/materializer path to verify transitive artifact loading and composition while preserving the authored root bundle list. This is integration-test evidence for that path, not proof of remote-service availability, all possible artifacts, or production security review.

To run the repository test project on a Windows development machine:

```powershell
dotnet test tests/TheSingularityWorkshop.AnyApp.Tests/TheSingularityWorkshop.AnyApp.Tests.csproj --configuration Release
```

This command is source-checked against the repository's documented workflow. A successful run must be recorded with its actual commit and workflow/test result before this chapter is labeled runtime-verified.

## 05 — Limits, practice, and next steps

### What this boundary does not guarantee

- A `RuntimeAssembly` is not automatically a Foundry `WorkflowDefinition`; issue #27 tracks the host/execution contract.
- The existence of a GPU host integration plan does not mean a production GPU backend is implemented.
- A successful composition test does not prove the complete interactive desktop experience.
- A cached Experience cannot guarantee offline launch if required artifacts are absent or invalid.
- Repository artifact delivery and artifact materialization are separate responsibilities; a successful HTTP response alone does not prove the bytes are a valid MicroBundle assembly.

### Practice

1. Predict which responsibilities would become platform-coupled if WPF types were added to FSM_COS.
2. Trace a repository-backed manifest from its immutable artifact identity to the `IMicroBundle` instance used by composition.
3. Explain why a source-mode build and a public-package compatibility build are separate evidence.
4. Find one native responsibility in `MainWindow.xaml.cs` and identify the portable contract it adapts, if any.

### Further reading

- [FSM_COS documentation](https://github.com/TrentBest/TheSingularityWorkshop.FSM_COS/tree/development) — composition/runtime theory belongs there.
- [AnyApp documentation index](../DOCUMENTATION_INDEX.md) — authoritative subsystem guides and evidence routes.
- [CI dependency modes](../CI-DEPENDENCY-MODES.md) — source versus package validation.
- [Foundry host-boundary issue #27](https://github.com/TrentBest/AnyApp/issues/27) — current integration work and acceptance criteria.

<text></text>


---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="200" /></p>
