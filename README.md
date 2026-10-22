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

The shell is host presentation infrastructure. The Moniker content and wave motion are independent MicroBundles composed through FSM_COS. The default development launch uses `experiences/moniker/development/manifest.json`, so both source projects are composed locally without publishing artifacts. The immutable repository-backed manifest remains at `experiences/moniker/1.0.0/manifest.json`; until its motion artifact is published and added to that manifest, it remains the separate compatibility path.

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
dotnet run --project AnyApp.csproj -- --experience-file=experiences/moniker/development/manifest.json
~~~

The repository endpoint used by the Workshop Experience exploration portal can be overridden:

~~~powershell
dotnet run --project AnyApp.csproj -- --repository-endpoint=http://localhost:5000/
~~~

## StoryBuilder native authoring proof

When the StoryBuilder repository is checked out at `_dependencies/StoryBuilder` alongside the other source-mode dependencies, AnyApp can launch its native authoring surface directly:

~~~powershell
dotnet run --project AnyApp.csproj -p:WorkshopUseRepositorySources=true -- --storybuilder
~~~

This opt-in source path references StoryBuilder's MicroBundle project and explicitly points it at AnyApp's GUI.Core project, preserving one semantic GUI assembly identity. The AnyApp window creates a fresh in-memory `StoryBuilderSession` for that launch, renders the session's current semantic tree through GUI.WPF, and dispatches only the four declared create commands (timeline, character, scene, manuscript). Successful commands rebuild the view from the updated domain state.

This is an integration slice, not a release claim: workspace persistence in the native host, package validation/consumption through the real AnyApp composition path, and end-to-end desktop CI verification remain outstanding. Normal/package builds do not gain a StoryBuilder package dependency.

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
MonikerMicroBundle (3101) + Wave Motion (3102) + Renderer
      |
      v
FSM_COS composition
      |
      v
Renderer FSM selects representation
      |
      v
static semantic GuiNode + allowed motion
      |
      v
GUI.WPF visualizer
      |
      v
native WPF
~~~

The Moniker content MicroBundle remains static and previewable. `TheSingularityWorkshop.Experiences.Moniker.Motion.Wave` independently contributes wave behavior. The development manifest composes content, motion, and the Renderer MicroBundle through FSM_COS. AnyApp advances the Renderer representation FSM and only enables the wave when the Moniker is in the near/interactive representation; GUI.WPF remains the visible presentation adapter. This is the first end-to-end composition slice: Renderer representation now gates visible motion, while the Renderer `RenderFrame` command stream and GPU backend remain a separate next milestone.

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

## Semantic intent exchange

AnyApp demonstrates the three-layer semantic exchange without coupling the foundation packages together:

~~~text
ProtocolAi
   WHAT
    |
    v
GrammarAi
    HOW
    |
    v
FSM_UserIO
SemanticIntent
    |
    v
AnyApp / Experience host
~~~

The host owns its Experience vocabulary in `WorkshopSemanticExchange`. ProtocolAi supplies deterministic protocol/symbol identity, GrammarAi describes which identities are structurally admitted, and FSM_UserIO carries the application intent.

The Forge manifest declares:

~~~json
"intent": {
  "name": "open.forge",
  "protocolId": 4200
}
~~~

AnyApp validates that intent against its host grammar before composition. This is a host integration; it does not create an upward dependency chain between the foundation packages.

## Physical baseline

AnyApp remains a proving ground rather than a finished product. Its current physical measurements are therefore baselines, not promises of final distribution size:

| Publish form | Directory | EXE |
|---|---:|---:|
| Framework-dependent | 0.33 MB | 0.15 MB |
| Self-contained | 160.10 MB | 0.15 MB |
| Single-file self-contained | 154.33 MB | 146.48 MB |
| Blank WPF Release milestone | — | ~149 KB |

The deployment-mode distinction matters: self-contained and single-file deployments carry the .NET runtime, while the small application executable is a different measurement.

The reproducible CI measurement path lives in `.github/workflows/baseline.yml`. The deeper runtime/performance discussion belongs with FSM_COS rather than the native host.

## Native host boundary

AnyApp is the first native host for RuntimeAssemblies produced by FSM_COS.

It is not an Experience, not a scheduler, and not a replacement for FSM_COS. Its job is to provide the native runtime and presentation substrate in which a composed RuntimeAssembly becomes observable.

> **FSM_COS constructs the composition. The host makes the composition real.**

The Workshop therefore keeps the boundary explicit:

~~~text
Ontology + MicroBundle selection
            |
            v
     Runtime Manifest
            |
            v
         FSM_COS
            |
            v
     RuntimeAssembly
        /       \
       v         v
    AnyApp    WebForge
 native host  web/server host
~~~

AnyApp owns native concerns such as process lifetime, windowing, input, timing, resource management, rendering, and future presentation facilities. Experience-specific behavior remains reusable MicroBundle capability.

The native rendering path is deliberately incremental:

~~~text
RuntimeAssembly
    -> native host
    -> presentation surface
    -> primitive rendering
    -> spatial composition
    -> GUI
    -> complete Experiences
~~~

A future server host can consume the same RuntimeAssembly model without becoming part of AnyApp or FSM_COS. Provider-specific deployment remains outside the composition boundary.

## What does not belong in AnyApp

AnyApp should not become:

- a second scheduler;
- an Experience-specific state-machine system;
- a duplicate MicroBundle composition system;
- an ontology interpreter;
- a publication/catalog authority;
- a cloud-provider-specific runtime;
- a WebForge implementation;
- a collection of one-off visual effects that should be reusable capabilities.

AnyApp consumes the architecture rather than silently replacing it.

## Trusted Windows distribution

AnyApp's public distribution and code-signing strategy is documented in [docs/TRUSTED-DISTRIBUTION.md](docs/TRUSTED-DISTRIBUTION.md). The host must not require users to disable Windows security controls or install an untrusted root certificate.

## Development

The two CI dependency modes are documented in [CI Dependency Modes](docs/CI-DEPENDENCY-MODES.md): development integration uses checked-out dependency sources, while public-package compatibility restores from NuGet without a local development feed.

AnyApp is currently a direct `.csproj` host; the repository does not require a solution file.

~~~powershell
dotnet restore AnyApp.csproj
dotnet build AnyApp.csproj --configuration Release
dotnet test tests/TheSingularityWorkshop.AnyApp.Tests/TheSingularityWorkshop.AnyApp.Tests.csproj --configuration Release
~~~

The CI workflow uses a Windows runner because WPF is Windows-specific.

## Renderer desktop proof

The host boundary and acceptance gates for the next native GPU milestone are recorded in [GPU Host Integration](docs/GPU_HOST_INTEGRATION.md). This plan preserves the manifest → FSM_COS → RuntimeAssembly path and keeps WPF surface ownership in AnyApp; it does not claim that the current diagnostic projection is GPU-rendered.

Launch the native desktop diagnostic surface with:

```powershell
dotnet run --project AnyApp.csproj -- --renderer-proof
```

This mode consumes the shared deterministic diagnostic command workload from the Renderer development source. Each of 1,000 stable diagnostic entities supplies five representation candidates; the toolbar selects a maximum detail, and the actual Renderer command reducer selects one permitted variant per entity. The reduced commands are assembled into a platform-neutral Renderer frame and passed through Visualize's real Renderer-to-Visualizer frame adapter. The live counters expose candidate, selected, omitted, and represented-entity counts.

The visible marks are a small WPF diagnostic projection of the adapted VisualizerFrame. This verifies that AnyApp can host the shared Renderer decision path and the Renderer-to-Visualize frame contract; it is **not** the production Visualize backend, a geometry-quality benchmark, or proof of GPU rendering. The normal Moniker startup remains unchanged when the flag is absent.


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


## ProtocolAI in a real application

AnyApp is a concrete consumer of [ProtocolAI](https://github.com/TrentBest/TheSingularityWorkshop.ProtocolAi), not merely an AI demonstration.

The host-owned WorkshopSemanticExchange defines the Workshop's Experience-intent vocabulary with ProtocolAI, carries those identities through FSM_UserIO.SemanticIntent, and uses GrammarAI to determine which protocol symbols are structurally admitted.

The important point is that this works **without an AI model**. An intent such as `open.forge` is deterministic application semantics. An AI model can be one source of that intent, but so can a human, GUI, deep link, another application, or ordinary code.

The ownership is:

```text
ProtocolAI  -> WHAT identity means
GrammarAI   -> HOW identities may be structured
FSM_UserIO  -> carries semantic intent
AnyApp      -> decides what the intent does
FSM_COS     -> composes the runtime
```

The ProtocolAI documentation maintains the API contract; this file and WorkshopSemanticExchange provide the concrete host example. If either side changes its semantic contract, both should be reviewed together.


## Singularity Studios desktop proving ground

Singularity Studios is the proposed reusable data-driven filming studio shared with the browser host. AnyApp's role is to consume the same versioned stage/take fixture and production semantics, not to implement a second studio model. The desktop integration direction, first repeatable take, deterministic replay, physical/audio boundaries, and acceptance gates are documented in the [Singularity Studios AnyApp handoff](docs/integrations/SINGULARITY_STUDIOS_ANYAPP_HANDOFF.md). The browser workbench plan is in the [WebPageLLM integration handoff](https://github.com/TrentBest/WebPage/blob/development/docs/integrations/SINGULARITY_STUDIOS_WEBPAGELLM_HANDOFF.md).

This is a design handoff only; no studio simulator or physics/audio integration is claimed to be implemented.


## Visualize adapter coordination

AnyApp's current source-mode build failure has a focused handoff for VisualizeLLM: [Visualize Renderer-adapter build request](docs/VISUALIZE_LLM_HANDOFF.md). It records the observed unresolved Renderer frame/representation types, the MSBuild evidence to inspect, the source/package-mode acceptance gates, and the division of responsibility between Visualize and AnyApp. This is a coordination request, not a claim that the adapter has already been repaired.


---

<p align="center">
  <a href="https://github.com/TrentBest/WebPage" aria-label="Visit The Singularity Workshop">
    <img src="https://raw.githubusercontent.com/TrentBest/WebPage/development/TheSingularityWorkshop/wwwroot/Images/TheSingularityWorkshopLivingAvatar.svg" alt="The Singularity Workshop living avatar — a digital vortex of information converging on its central eye" width="112" />
  </a><br />
  <strong>The Singularity Workshop</strong><br />
  <em>Forging Software for the Singularity.</em><br />
  <a href="https://github.com/TrentBest">GitHub</a> · <a href="https://github.com/TrentBest/WebPage">Workshop</a>
</p>
