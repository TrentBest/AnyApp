# FSM_COS alpha.7 — AnyApp integration proof

## Purpose

Use AnyApp as the shortest desktop line of sight from the FSM_COS alpha.7 candidate into a running host. This is a consumer integration check, not a claim that AnyApp is a browser accessibility test or that the alpha.7 package has been published.

## Exact candidate under test

- Repository: [TheSingularityWorkshop.FSM_COS](https://github.com/TrentBest/TheSingularityWorkshop.FSM_COS)
- Commit: `f9c2ee0fa2fd2d06a9c94559c72ef5a9e63ea6cc`
- Candidate package version: `0.1.0-alpha.7`
- Package publication: intentionally not performed.

The integration workflow checks out that exact FSM_COS commit into AnyApp's expected source-dependency path. It does not silently test whichever commit happens to be at the moving `development` branch.

## What the proof demonstrates

```text
AnyApp Experience manifest
          |
          v
     FSM_COS alpha.7
          |
          v
    RuntimeAssembly
          |
          +---- Moniker MicroBundle
          +---- Wave-motion MicroBundle
          +---- Renderer MicroBundle
          |
          v
  semantic GUI surface + permitted motion
          |
          v
       GUI.WPF
          |
          v
   observable desktop window
```

The existing consumer tests assert that composition returns the expected RuntimeAssembly and MicroBundles, and that the host can derive and present the Moniker semantic GUI surface. The running AnyApp Moniker Experience is the live visual proof; the tests are the repeatable composition proof.

## Run locally

From the AnyApp repository root, populate the source checkouts required by `AnyApp.csproj` and its transitive source graph under `_dependencies/`, using the same dependency set as [the .NET workflow](../.github/workflows/dotnet.yml). For FSM_COS specifically, check out the exact candidate commit into:

```text
_dependencies/TheSingularityWorkshop.FSM_COS
```

Then restore, build, and test in repository-source mode:

```powershell
$sourceRoot = "$PWD/_dependencies"
dotnet restore tests/TheSingularityWorkshop.AnyApp.Tests/TheSingularityWorkshop.AnyApp.Tests.csproj -p:WorkshopUseRepositorySources=true -p:WorkshopSourceRoot="$sourceRoot"
dotnet build AnyApp.csproj --configuration Release --no-restore -p:WorkshopUseRepositorySources=true -p:WorkshopSourceRoot="$sourceRoot"
dotnet test tests/TheSingularityWorkshop.AnyApp.Tests/TheSingularityWorkshop.AnyApp.Tests.csproj --configuration Release --no-restore -p:WorkshopUseRepositorySources=true -p:WorkshopSourceRoot="$sourceRoot"
dotnet run --project AnyApp.csproj -- --experience-file=experiences/moniker/development/manifest.json -p:WorkshopUseRepositorySources=true -p:WorkshopSourceRoot="$sourceRoot"
```

AnyApp is Windows/WPF-specific. Run the visual step on Windows. The alpha.7 package reference in `AnyApp.csproj` is for the eventual public-package mode; because alpha.7 is not yet published, the source-mode integration workflow is the supported way to exercise this candidate before publication.

## Scope boundary

This proves the candidate can serve as AnyApp's composition engine under the source integration graph. It does **not** close FSM_COS's separate browser-hosted rendering, browser accessibility-tree, keyboard, or screen-reader gate. Those remain a WebPage/browser verification task. It also does not authorize package publication or merging this candidate integration.
