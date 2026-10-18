# CI dependency modes

AnyApp follows the Workshop's two-mode dependency rule.

## Development source integration

The `.github/workflows/dotnet.yml` workflow runs for pushes to `development` and pull requests targeting `development`. It checks out the dependent repositories' `development` branches under `_dependencies/` and builds the source graph with:

```powershell
-p:WorkshopUseRepositorySources=true
-p:WorkshopSourceRoot="$env:GITHUB_WORKSPACE/_dependencies"
```

The property is passed consistently to restore, build, and test. Project files select sibling project references in this mode instead of retaining stale package versions. The checkout set includes FSM_API, FSM_COS, FSM_UserIO, FSM_REST, ProtocolAi, GrammarAi, GUI, MicroBundleDomain, MicroBundleRepository, Renderer, Visualize, and Experiences.

Do not add a local NuGet feed to this workflow as a substitute for source references. Source integration should prove the checked-out development projects and their transitive project graph, not silently consume locally packed snapshots.

## Published-package compatibility

The `.github/workflows/baseline.yml` workflow restores the test graph from the public NuGet feed explicitly:

```powershell
dotnet restore tests/TheSingularityWorkshop.AnyApp.Tests/TheSingularityWorkshop.AnyApp.Tests.csproj --source https://api.nuget.org/v3/index.json
```

It does not create or register a local package feed. In normal/package mode, AnyApp's project file selects published packages for FSM_API, FSM_COS, FSM_UserIO, ProtocolAi, GrammarAi, GUI.WPF, MicroBundleRepository.Rest, Renderer, and the Moniker Experience packages. The workflow then tests and measures the publish forms using that restored graph.

## Current integration exception

The Visualize Renderer adapter is currently consumed as a source project because it is an integration project in the Visualize repository rather than a separately published adapter package. The adapter itself must be source-built, and its current project file directly references the Visualize project. Therefore the public-feed baseline validates the published NuGet dependency graph for the package-backed dependencies, but it is not yet a fully source-free Visualize/adapter graph.

This exception is intentionally visible rather than masked by locally packing development artifacts. The [VisualizeLLM handoff](VISUALIZE_LLM_HANDOFF.md) owns the Visualize-side investigation. AnyApp owns its dependency selection, source graph, and consumer CI. Once a supported published adapter package exists, the package-mode baseline should consume that package too.

## Guardrails

- Development builds select checked-out `development` source for dependencies that support repository-source mode.
- Public compatibility checks use only the public NuGet feed; no locally packed dependency artifacts may satisfy them.
- Publishing remains behind each repository's explicit manual Boolean gate and required compatibility checks.
- A successful source-integration build is not evidence that public packages are compatible; both paths must pass.
- CI changes do not publish packages or merge branches.
