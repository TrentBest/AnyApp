# AnyApp

AnyApp is the desktop proof of the composition architecture.

It is the Windows desktop counterpart to the browser-facing WebPage/WebForge host: the **desktop of the Workshop**, not another application-specific framework.

## Purpose

The first responsibility of AnyApp is deliberately small:

1. create a runtime manifest;
2. hand that manifest to FSM_COS;
3. let FSM_COS compose the requested MicroBundle;
4. retrieve the composed capability from the resulting RuntimeAssembly;
5. hand its platform-neutral GUI surface to GUI.WPF;
6. manifest that surface as native WPF controls.

The boundary is:

~~~text
AnyApp
  |
  | RuntimeManifest
  v
FSM_COS
  |
  | RuntimeAssembly
  v
AnyApp MicroBundle
  |
  | semantic GuiNode
  v
GUI.WPF
  |
  v
native WPF desktop
~~~

AnyApp therefore gives us a concrete desktop validation path for FSM_COS without making FSM_COS depend on WPF.

## What this proves

The first vertical slice proves:

~~~text
MicroBundle
    ↓
FSM_COS
    ↓
RuntimeAssembly
    ↓
semantic GUI
    ↓
GUI.WPF
    ↓
desktop
~~~

The MicroBundle does not reference WPF controls or WPF lifecycle. FSM_COS does not reference GUI.WPF. GUI.WPF does not reference AnyApp.

The host is the place where those pieces are deliberately brought together.

## Current scope

This is a proof surface, not the finished AnyApp shell. It establishes:

- .NET 8 WPF application packaging;
- NuGet consumption of TheSingularityWorkshop.FSM_COS;
- NuGet consumption of TheSingularityWorkshop.GUI.WPF;
- composition through FsmCos;
- RuntimeAssembly retrieval;
- semantic GUI creation inside the composed MicroBundle;
- native WPF manifestation;
- an automated test proving the COS path.

The next layers can add independent MicroBundles, Hub routing, Workshop navigation, repository-backed artifact delivery, and richer desktop behavior without changing this boundary.

## Architectural intent

AnyApp should remain the host.

It should **not** absorb:

- FSM_COS composition semantics;
- GUI.Core semantic definitions;
- GUI.WPF renderer internals;
- MicroBundle repository semantics;
- Workshop experience logic.

The eventual desktop path is therefore:

~~~text
AnyApp
  ↓
FSM_COS
  ↓
independently published MicroBundles
  ↓
RuntimeAssembly
  ↓
GUI semantic surface / Hub
  ↓
GUI.WPF
  ↓
WPF desktop
~~~

This is the desktop proof that the same composition boundary can support desktop manifestation without coupling the composition kernel to the platform.

## Development

The repository includes `AnyApp.sln`, containing both the WPF host and its test project. After cloning, restore the solution before building so the generated `obj/project.assets.json` files are created locally.

~~~powershell
dotnet restore AnyApp.sln
dotnet build AnyApp.sln --configuration Release
dotnet test AnyApp.sln --configuration Release
dotnet run --project AnyApp.csproj
~~~

The CI workflow uses a Windows runner because WPF is Windows-specific.
