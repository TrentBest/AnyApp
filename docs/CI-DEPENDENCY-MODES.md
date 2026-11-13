<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# CI dependency modes

AnyApp follows the Workshop's two-mode dependency rule.

## Local Visual Studio and CLI development

The repository includes a root `Directory.Build.props` shared by AnyApp and its test project. Unless explicitly overridden, it selects repository-source mode and resolves one `WorkshopSourceRoot`:

- In CI, when `_dependencies/FSM_API/FSM_API/FSM_API.csproj` exists, the root is the repository's `_dependencies/` directory.
- In a local sibling workspace, the root is the parent directory containing the checked-out Workshop repositories.

Open `AnyApp.sln` in Visual Studio and select `Release | Any CPU`; restore/build/test use the same source graph without a special property. The CLI behaves the same way. Missing required source checkouts fail with an actionable MSBuild error; source mode never silently falls back to NuGet.

To deliberately evaluate the published-package compatibility graph, pass `-p:WorkshopUseRepositorySources=false` to restore and subsequent build/test/publish commands. CI baseline does this explicitly.

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
dotnet restore tests/TheSingularityWorkshop.AnyApp.Tests/TheSingularityWorkshop.AnyApp.Tests.csproj -p:WorkshopUseRepositorySources=false -p:WorkshopSourceRoot="$env:GITHUB_WORKSPACE/_dependencies" --source https://api.nuget.org/v3/index.json
```

It does not create or register a local package feed. In normal/package mode, AnyApp's project file selects published packages for FSM_API, FSM_COS, FSM_UserIO, ProtocolAi, GrammarAi, GUI.WPF, MicroBundleRepository.Rest, and the Moniker Experience packages. Renderer remains a checked-out source/MicroBundle composition input in both modes and is never consumed as a NuGet package. The baseline checks out Renderer under `_dependencies/` and passes that shared root explicitly. The workflow then tests and measures the publish forms using the package graph plus the Renderer source/MicroBundle input.

**Current release-readiness blocker:** public-feed restore has confirmed that `TheSingularityWorkshop.MicroBundleRepository.Rest`, `TheSingularityWorkshop.Experiences.Moniker`, and `TheSingularityWorkshop.Experiences.Moniker.Motion.Wave` are not currently available on NuGet.org. The package-mode baseline is expected to fail until those dependency packages are published (or the host's package-mode dependency plan changes deliberately). This baseline is restricted to `master` pushes/pull requests and manual dispatch so the ordinary `development` source-integration workflow is not confused with release readiness. Do not paper over this with a local package feed, and do not publish dependencies without explicit authorization.

## Current integration exception

The Visualize Renderer adapter is currently consumed as a source project because it is an integration project in the Visualize repository rather than a separately published adapter package. The adapter itself must be source-built, and its current project file directly references the Visualize project. Therefore the public-feed baseline validates the published NuGet dependency graph for the package-backed dependencies, but it is not yet a fully source-free Visualize/adapter graph.

The Visualize Renderer adapter is included only in repository-source mode, where the source workflow builds it from the Visualize checkout. Package mode does not pretend that this adapter is a NuGet dependency; it validates the ordinary package-backed host dependencies plus the Renderer source/MicroBundle input. The [VisualizeLLM handoff](VISUALIZE_LLM_HANDOFF.md) owns the Visualize-side investigation, while AnyApp owns its dependency selection, source graph, and consumer CI.

## Guardrails

- Development builds select checked-out `development` source for dependencies that support repository-source mode.
- Public compatibility checks use only the public NuGet feed; no locally packed dependency artifacts may satisfy them.
- Publishing remains behind each repository's explicit manual Boolean gate and required compatibility checks.
- A successful source-integration build is not evidence that public packages are compatible; both paths must pass.
- CI changes do not publish packages or merge branches.

---

<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="200" /></p>
