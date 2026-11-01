<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# Workshop Landing 2111: repository-to-WPF composition

## 01 — Theory: artifact delivery is not composition

The Workshop Landing MicroBundle is authored in The, not in AnyApp. Its semantic surface is a public, host-neutral GuiNode Root. AnyApp must retrieve the immutable artifact, materialize the declared MicroBundle, and give it to FSM_COS. The host may then discover the semantic root and ask GUI.WPF to manifest it.

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

A host-specific conditional such as if bundleId == 2111 then new WorkshopLandingMicroBundle() would bypass the very artifact path this proof is intended to establish and is therefore explicitly excluded.

## 02 — The artifact envelope contract

The Workshop artifact build script currently wraps the assembly bytes in a binary FSMB version-1 envelope before calculating the artifact SHA-256 and uploading the bytes.

| Offset | Size | Field | Encoding |
|---:|---:|---|---|
| 0 | 4 | Magic | ASCII FSMB |
| 4 | 4 | Format version | UInt32 little-endian; currently 1 |
| 8 | 8 | Bundle ID | UInt64 little-endian |
| 16 | 4 | Assembly byte length | UInt32 little-endian |
| 20 | variable | Assembly payload | Exact declared length |

The repository content hash covers the entire envelope, not only the embedded DLL. The materializer validates the envelope version, BundleId, and exact payload length before passing the embedded assembly bytes to .NET.

Raw assembly artifacts remain supported for compatibility with the existing AnyApp repository-catalog tests and earlier raw-assembly artifacts. A payload beginning with the FSMB magic is treated as an envelope and malformed envelopes fail closed; they are never retried as raw assemblies.

This is currently an AnyApp host-owned artifact format. If a second host needs the same assembly envelope, that is evidence for extracting a shared artifact-format package; it is not a reason to make the repository understand assemblies.

## 03 — Implementation and automated proof

The integration test uses the actual Workshop.MicroBundle project from The when its source checkout is available. It reads the compiled assembly, creates the same FSMB v1 envelope as the Workshop artifact script, calculates the immutable SHA-256 identity, and serves the artifact and empty configuration through a local fake REST endpoint.

The test then verifies:

1. AnyApp's repository catalog retrieves and validates the immutable artifact.
2. The materializer extracts the embedded assembly and discovers the public parameterless MicroBundle with ID 2111.
3. The existing FSM_COS path composes the requested root into a RuntimeAssembly.
4. MicroBundleGuiSurface.TryGetRoot discovers the public GuiNode Root without a bundle-specific host branch.
5. WpfGuiRenderer.Render converts that semantic root into a native WPF element tree on an STA thread.

Run the source integration test from an AnyApp checkout when The is available as a sibling directory:

~~~powershell
dotnet test tests/TheSingularityWorkshop.AnyApp.Tests/TheSingularityWorkshop.AnyApp.Tests.csproj --configuration Release -p:WorkshopUseRepositorySources=true
~~~

The test project detects either ../The or _dependencies/The. The source integration test detects either a local sibling checkout or _dependencies/The. AnyApp's CI attempts the private The checkout only when the GITHUB_PAT repository secret is configured with read access; without that credential, the optional test is excluded rather than replaced by a synthetic 2111 implementation. A green CI run without that secret therefore does not claim to have executed the real 2111 test.

The automated test creates no cloud resources, uploads no artifact, and publishes no package.

## 04 — What this proof does and does not establish

**Automated proof target:** repository retrieval, content identity, FSMB envelope parsing, real assembly materialization, FSM_COS composition, semantic root discovery, and WPF native-tree conversion.

**Not established by this test:** an interactive desktop window, visual fidelity at display scale, input/command behavior, a public repository publication, offline cache lifecycle, or a production deployment.

The WPF adapter test creates the native element tree but deliberately does not open a window. A separate visible desktop demonstration should follow only after this integration test is green and its output has been inspected. The immutable artifact publication path must remain opt-in; this proof uses a local fake repository.

## 05 — Related representation work

The same semantic composition should eventually be observable through three connected views:

1. **SVG** — the documentation-facing visual artifact.
2. **Renderer** — the structured semantic/compositional frame an AI can inspect.
3. **Visualization** — the manifestation produced after the Renderer-to-Visualize adapter and a Visualize backend.

Those views must share one source composition and identity. They must not be independently authored approximations, and an SVG/CPU backend must not be described as GPU rendering. The cross-repository demonstrator is tracked in [Visualize issue #40](https://github.com/TrentBest/TheSingularityWorkshop.Visualize/issues/40).

## Status

This is an active integration proof under [AnyApp PR #47](https://github.com/TrentBest/AnyApp/pull/47). The code and test are not considered verified until the associated CI run completes successfully and the result is reviewed. The visible desktop demonstration remains a separate next milestone.

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
