# AnyApp

AnyApp is the desktop proof of the composition architecture.

It is the Windows desktop counterpart to the browser-facing WebPage/WebForge host: the desktop of the Workshop, not another application-specific framework.

## Startup is now Experience selection

AnyApp is a host. It no longer embeds an Experience manifest in MainWindow.xaml.cs.

When no explicit launch manifest is supplied, AnyApp enters the Experience Browser flow. The browser is intentionally spatial rather than a conventional application launcher: published Experiences appear as doors, and entering a door retrieves its manifest before composition:

~~~text
AnyApp
  |
  | no launch manifest
  v
Experience Browser endpoint
  |
  | published Experiences
  v
user selection
  |
  | selected artifact
  v
Experience manifest
  |
  v
FSM_COS
  |
  v
RuntimeAssembly
  |
  v
GUI.WPF
~~~

For local development, an explicit manifest can be supplied without making it part of the host:

~~~powershell
dotnet run --project AnyApp.csproj -- --experience-file=experiences/moniker/1.0.0/manifest.json
~~~

The repository endpoint can be overridden for the browser flow:

~~~powershell
dotnet run --project AnyApp.csproj -- --repository-endpoint=http://localhost:5000/
~~~

The first serialized Experience lives in its own versioned folder:

~~~text
experiences/
└── moniker/
    └── 1.0.0/
        └── manifest.json
~~~

That source artifact is the shape we will publish into the immutable .experience repository artifact. AnyApp does not treat the source file as a built-in startup manifest.

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
