# AnyApp

AnyApp is the desktop proof of the composition architecture.

It is the Windows desktop counterpart to the browser-facing WebPage/WebForge host: the desktop of the Workshop, not another application-specific framework.

## Startup is now manifest-driven


![AnyApp repository-backed composition](docs/assets/anyapp-composition.svg)

The diagram above is the intended host boundary: the repository delivers verified opaque bytes, AnyApp materializes them, and FSM_COS remains responsible for composition.

AnyApp is the host. It does not choose an Experience by embedding the Experience definition in the host.

Normal startup now follows the Workshop entry sequence:

~~~text
AnyApp
  |
  | no explicit launch manifest
  v
Workshop-authored splash sequence
  |
  | currently one authored image: the Workshop avatar
  v
last successfully run Experience manifest
  |
  | if none exists or it can no longer compose
  v
Forge Experience manifest
  |
  v
FSM_COS
  |
  v
RuntimeAssembly
  |
  v
host manifestation
~~~

The splash is host-owned presentation around composition; **FSM_COS remains the composition boundary**. The initial splash sequence contains one Workshop-authored image and is intentionally structured so additional authored splash Experiences can be added later without changing the composition kernel.

The Forge is the user's entry Experience when there is no usable last-run Experience. It is not a special launcher screen. Its semantic surface contains a diegetic EXPLORE EXPERIENCES portal; entering that portal opens the Experience repository flow.

The Forge manifest lives beside the other source manifests:

~~~text
experiences/
├── forge/
│   └── 1.0.0/
│       └── manifest.json
└── moniker/
    └── 1.0.0/
        └── manifest.json
~~~

A successfully entered non-Forge Experience is persisted as the next startup candidate outside the application binary. Forge itself is not persisted as the last-run Experience, so it remains the fallback entry point.

For local development, an explicit manifest can still be supplied:

~~~powershell
dotnet run --project AnyApp.csproj -- --experience-file=experiences/moniker/1.0.0/manifest.json
~~~

The repository endpoint used by the Forge's Experience exploration portal can be overridden:

~~~powershell
dotnet run --project AnyApp.csproj -- --repository-endpoint=http://localhost:5000/
~~~

## Composition boundary

Once a manifest has been selected, the runtime path remains:

~~~text
RuntimeManifest
      |
      v
   FSM_COS
      |
      v
RuntimeAssembly
      |
      v
MonikerMicroBundle
      |
      v
semantic GuiNode
      |
      v
GUI.WPF
      |
      v
native WPF
~~~

The MicroBundle remains static and previewable. MonikerExperience.ExecutePresentation(...) adds the wave behavior only when the selected Experience executes.

## Repository-backed composition

Published Experiences can carry immutable artifact identities for their MicroBundles. The host-side composition path keeps repository delivery separate from runtime interpretation:

~~~text
Experience manifest
       |
       | bundle ID + version + SHA-256
       v
AnyApp RepositoryMicroBundleCatalog
       |
       v
MicroBundleRepository.Rest
       |
       v
verified opaque assembly bytes
       |
       v
AnyApp artifact materializer
       |
       v
IMicroBundle
       |
       v
FSM_COS
       |
       v
RuntimeAssembly
~~~

This dependency direction is intentional:

~~~text
AnyApp
 ├── FSM_COS
 ├── MicroBundleRepository.Rest ──> FSM_Rest
 └── artifact representation/materialization
          |
          └── interprets repository bytes

MicroBundleRepository.Core
 └── artifact identity + opaque bytes + repository contracts
~~~

The repository does not know how an artifact becomes an `IMicroBundle`. The current AnyApp vertical slice uses a compiled .NET assembly envelope, but that representation is host-owned. If WebForge or another host later needs the same representation, it can become a separate shared artifact-format package based on that demonstrated second consumer.

## Architectural intent

AnyApp should remain the host.

It should not absorb:

- FSM_COS composition semantics;
- GUI.Core semantic definitions;
- GUI.WPF renderer internals;
- MicroBundle repository semantics;
- Workshop Experience logic.

The repository is responsible for discovering and delivering published Experience artifacts. AnyApp is responsible for selecting one and handing its manifest to FSM_COS.

## Development

AnyApp is currently a direct `.csproj` host; the repository does not require a solution file.

~~~powershell
dotnet restore AnyApp.csproj
dotnet build AnyApp.csproj --configuration Release
dotnet test tests/TheSingularityWorkshop.AnyApp.Tests/TheSingularityWorkshop.AnyApp.Tests.csproj --configuration Release
~~~

The CI workflow uses a Windows runner because WPF is Windows-specific.
