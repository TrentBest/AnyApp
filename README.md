# AnyApp

AnyApp is the desktop proof of the composition architecture.

It is the Windows desktop counterpart to the browser-facing WebPage/WebForge host: the desktop of the Workshop, not another application-specific framework.

## Startup is now the common Workshop Moniker

AnyApp enters through the same Workshop Moniker Experience surface used to establish the Workshop identity before another Experience is entered.

The native manifestation follows this boundary:

~~~text
Experience manifest
      |
      v
FSM_COS
      |
      v
Moniker MicroBundle
      |
      v
semantic GuiNode
      |
      v
GUI.WPF
      |
      v
Workshop shell + native presentation
~~~

The shell is host presentation infrastructure. The Moniker content is still composed through the MicroBundle/FSM_COS path. The current development branch contains a temporary local Moniker compatibility artifact so the native manifestation can be proven before the canonical published Experience is available from the MicroBundle Repository.

The intended final path is:

~~~text
TheSingularityWorkshop.Experiences.Moniker
             |
             v
immutable Experience / MicroBundle artifacts
             |
             v
Azure Blob-backed MicroBundle Repository
             |
             v
AnyApp + WebPage
             |
             v
same Experience identity
~~~

For local development, an explicit manifest can still be supplied:

~~~powershell
dotnet run --project AnyApp.csproj -- --experience-file=experiences/moniker/1.0.0/manifest.json
~~~

The repository endpoint used by the Workshop Experience exploration portal can be overridden:

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

## Trusted Windows distribution

AnyApp's public distribution and code-signing strategy is documented in [docs/TRUSTED-DISTRIBUTION.md](docs/TRUSTED-DISTRIBUTION.md). The host must not require users to disable Windows security controls or install an untrusted root certificate.

## Development

AnyApp is currently a direct `.csproj` host; the repository does not require a solution file.

~~~powershell
dotnet restore AnyApp.csproj
dotnet build AnyApp.csproj --configuration Release
dotnet test tests/TheSingularityWorkshop.AnyApp.Tests/TheSingularityWorkshop.AnyApp.Tests.csproj --configuration Release
~~~

The CI workflow uses a Windows runner because WPF is Windows-specific.

## Browser-only Deep Dive handoff

AnyApp does not embed the Workshop's educational Deep Dive implementation.

When an Experience exposes a Deep Dive capability, AnyApp presents a browser handoff that opens:

~~~text
WebPage /deep-dive/{ExperienceId}
~~~

The WebPage URL is supplied through --webpage-url=... or the SINGULARITY_WORKSHOP_WEBPAGE_URL environment variable. The native host launches the user's default browser with shell execution.

This is deliberate:

- Experience and MicroBundle ontology remain native/runtime concerns.
- Deep Dive presentation remains a WebPage concern.
- AnyApp does not take a dependency on Blazor or the WebPage UI.
- Browser-tab reuse is not assumed to be universally automatable; a native host may open the URL while the browser decides how it handles an already-open page.

The result is a useful asymmetry: the ontology is portable; the Workshop's education surface is ours.
