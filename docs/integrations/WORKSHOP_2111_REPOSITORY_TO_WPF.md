<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# Workshop Landing 2111: repository-to-WPF composition

## 01 — Theory: artifact delivery is not composition

The Workshop Landing MicroBundle is authored in The, not in AnyApp. Its semantic surface is a public, host-neutral GuiNode Root. AnyApp retrieves the immutable artifact, materializes the declared MicroBundle, and gives it to FSM_COS. The host then discovers the semantic root and asks GUI.WPF to manifest it.

The intended dependency direction is:

~~~text
The / WorkshopLandingMicroBundle (2111)
                 |
                 v
       immutable Experience artifact
                 |
                 v
        MicroBundle Repository
                 |
                 v
          AnyApp host catalog
                 |
                 v
       artifact materialization
                 |
                 v
              IMicroBundle
                 |
                 v
              FSM_COS
                 |
                 v
           RuntimeAssembly
                 |
                 v
        public semantic GuiNode
                 |
                 v
             GUI.WPF
                 |
                 v
          native WPF element tree
~~~

The repository stores and delivers opaque, content-addressed bytes. It does not interpret .NET assemblies or construct GUI trees. AnyApp owns the current assembly artifact representation and materialization step. FSM_COS owns composition. GUI.WPF owns the native manifestation adapter.

A host-specific conditional such as `if bundleId == 2111 then new WorkshopLandingMicroBundle()` would bypass the artifact path this proof is intended to establish and is explicitly excluded.

## 02 — The artifact envelope contract

The Workshop artifact build script wraps assembly bytes in a binary FSMB version-1 envelope before calculating the artifact SHA-256 and uploading the bytes.

| Offset | Size | Field | Encoding |
|---:|---:|---|---|
| 0 | 4 | Magic | ASCII FSMB |
| 4 | 4 | Format version | UInt32 little-endian |
| 8 | 8 | Bundle ID | UInt64 little-endian |
| 16 | 4 | Assembly byte length | UInt32 little-endian |
| 20 | variable | Assembly payload | Exact declared length |

The repository content hash covers the entire envelope, not only the embedded DLL. The materializer validates the envelope version, BundleId, and exact payload length before passing the embedded assembly bytes to .NET.

Raw assembly artifacts remain supported for compatibility with existing raw-assembly artifacts. A payload beginning with the FSMB magic is treated as an envelope; malformed envelopes fail closed and are never retried as raw assemblies.

This is currently an AnyApp host-owned artifact format. If a second host needs the same envelope, that is evidence for extracting a shared artifact-format package—not a reason to make the repository understand assemblies.

## 03 — Implementation and automated proof

When The's source checkout is available, the integration test references the actual `Workshop.MicroBundle` project. It reads the compiled assembly, creates the same FSMB v1 envelope as the Workshop artifact script, calculates the immutable SHA-256 identity, and serves the artifact and empty configuration through a local fake REST endpoint.

The test verifies:

1. AnyApp's repository catalog retrieves and validates the immutable artifact.
2. The materializer extracts the embedded assembly and discovers the public parameterless MicroBundle with ID 2111.
3. The existing FSM_COS path composes the requested root into a RuntimeAssembly.
4. `MicroBundleGuiSurface.TryGetRoot` discovers the public GuiNode Root without a bundle-specific host branch.
5. `WpfGuiRenderer.Render` converts that semantic root into a native WPF element tree on an STA thread.

Run the source integration test from an AnyApp checkout when The is available as a sibling directory or under `_dependencies/The`:

~~~powershell
dotnet test tests/TheSingularityWorkshop.AnyApp.Tests/TheSingularityWorkshop.AnyApp.Tests.csproj --configuration Release -p:WorkshopUseRepositorySources=true
~~~

The AnyApp workflow checks out private `TrentBest/The` only when the repository Actions secret `GITHUB_PAT` is configured with read-only Contents access. Without that secret or a local sibling checkout, the source-specific test is excluded—not replaced with a synthetic 2111 bundle. A green CI run without that secret therefore does not claim to have executed the real 2111 test. Never put the token in source control or chat.

The automated test uses a local fake repository. It opens no desktop window, creates no cloud resources, uploads no artifact, and publishes no package.

## 04 — What this proof does and does not establish

**Automated proof target:** repository retrieval, content identity, FSMB envelope parsing, real assembly materialization, FSM_COS composition, semantic root discovery, and WPF native-tree conversion.

**Not established by this test:** an interactive desktop window, visual fidelity at display scale, input/command behavior, a public repository publication, offline cache lifecycle, or a production deployment.

The WPF adapter test creates the native element tree but deliberately does not open a window. A separate visible desktop demonstration should follow only after this integration test is green and its output has been inspected. Artifact publication remains opt-in; this proof uses a local fake repository.

## 05 — Related representation work

The same semantic composition should eventually be observable through three connected views:

1. **SVG** — the documentation-facing visual artifact.
2. **Renderer** — the structured semantic/compositional frame an AI can inspect.
3. **Visualization** — the manifestation produced after the Renderer-to-Visualize adapter and a Visualize backend.

Those views must share one source composition and identity. They must not be independently authored approximations, and an SVG/CPU backend must not be described as GPU rendering. The cross-repository demonstrator is tracked in [Visualize issue #40](https://github.com/TrentBest/TheSingularityWorkshop.Visualize/issues/40).

## Status

The FSMB materializer and repository-catalog envelope test are now present on AnyApp's `development` branch. The real 2111 source-to-WPF test is present as an opt-in test and is **not verified** until it runs successfully with The's source available. A visible desktop demonstration remains a separate milestone.

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
