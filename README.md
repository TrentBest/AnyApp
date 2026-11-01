<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# AnyApp

AnyApp is the desktop proof of the composition architecture.

It is the Windows desktop counterpart to the browser-facing WebPage/WebForge host: the desktop of the Workshop, not another application-specific framework.

## 01 — What: the native Workshop host

AnyApp is the native Windows host for Workshop Experiences. It selects an Experience manifest, resolves the required MicroBundles, delegates composition to FSM_COS, and provides the platform-facing application and presentation surface.

## 02 — Why: portable composition, native experience

Composition should not need to know about WPF windows, Windows process lifetime, or a particular GPU API. AnyApp owns those native concerns while FSM_COS owns composition and reusable Experience behavior remains in MicroBundles. This boundary lets different hosts consume the same composition without turning the composition engine into a desktop framework.

## 03 — How: select, resolve, compose, present

```text
Experience manifest
        |
        v
AnyApp host policy and catalog
        |
        v
FSM_COS composition
        |
        v
RuntimeAssembly
        |
        v
AnyApp native presentation
```

AnyApp is a host, not a second composition engine, repository authority, or Experience-specific scheduler. The current Foundry execution boundary is tracked separately in [issue #27](https://github.com/TrentBest/AnyApp/issues/27); do not treat that integration as complete until its contract and tests are in place.

## 04 — See it in a minute

On Windows with the repository's development dependencies available:

```powershell
dotnet run --project AnyApp.csproj -- --experience-file=experiences/moniker/development/manifest.json
```

This launches the local development Moniker Experience path described below. It is a development proof, not a claim that the public executable distribution or every Experience is production-ready.

## 05 — Choose your next step

- [Documentation index](docs/DOCUMENTATION_INDEX.md) — focused subsystem guides, contracts, and verification.
- [Theory and learning path](docs/theory/README.md) — learn the native-host domain from first principles.
- [Agent onboarding PR #35](https://github.com/TrentBest/AnyApp/pull/35) — proposed repository-specific contributor instructions; not merged yet.
- [CI dependency modes](docs/CI-DEPENDENCY-MODES.md) — distinguish source integration from public-package compatibility.

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

This opt-in source path references StoryBuilder's MicroBundle and Persistence projects and explicitly points the MicroBundle at AnyApp's GUI.Core project, preserving one semantic GUI assembly identity. The native window restores a host-local, versioned binary workspace when one exists, renders the session's semantic tree through GUI.WPF, and dispatches explicit create commands for timelines, characters, scenes, manuscripts, timeline events, manuscript sections, and event-to-section linking. Successful edits are saved through `FSM_Serialization` using a temporary file and atomic replacement, then the view is rebuilt from current domain state. If an existing save cannot be read, AnyApp preserves it and avoids silently replacing it with an empty workspace.

This remains an integration slice, not a release claim: an automated two-process persistence probe verifies that a saved event-to-section link survives process restart ([AnyApp workflow](https://github.com/TrentBest/AnyApp/actions/runs/39863022603)). The full interactive create → link → save → restart → reopen workflow and validated composition-package consumption through the real AnyApp composition path remain outstanding. Normal/package builds do not gain a StoryBuilder package dependency.

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

AnyApp remains a proving ground rather than a finished product. Physical measurements are tied to the exact CI artifact and commit; they are not estimates or promises of final distribution size.

The latest combined source-mode build and Windows publish gate measured the following candidate before PR #41 was merged:

| Measurement | Exact size | Binary size |
|---|---:|---:|
| Self-contained, single-file `AnyApp.exe` | 162,844,055 bytes | 155.30 MiB |
| Publish directory | 163,857,259 bytes | 156.27 MiB |

Evidence: [workflow run #491](https://github.com/TrentBest/AnyApp/actions/runs/41333568522), source commit `9271fe459e383a99258989ed969afa46365dd664`. The executable includes the .NET 8 Windows runtime and WPF payload. The application and Workshop assemblies were only a small fraction of the measured binary; see [issue #42](https://github.com/TrentBest/AnyApp/issues/42) for the bundle inventory and category breakdown.

The reproducible controlled comparison is [Windows publish-size experiment](docs/PUBLISH-SIZE-EXPERIMENT.md), implemented by `.github/workflows/publish-size-matrix.yml`. It measures compression and satellite-resource language filtering independently and together. It does not change the default publish configuration. The older `.github/workflows/baseline.yml` remains the public-package compatibility baseline and is intentionally separate from development source integration.


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

## Experience startup and offline recovery

The startup reconciliation rules, offline fallback behavior, and crash-safe cache-generation model are documented in [Experience Startup and Offline Recovery](docs/EXPERIENCE-STARTUP-AND-OFFLINE-RECOVERY.md). The guide distinguishes catalog unavailability from a publication change and explains when AnyApp returns to Forge.

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

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="200" /></p>
