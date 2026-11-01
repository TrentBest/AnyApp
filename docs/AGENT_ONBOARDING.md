<!-- The Singularity Workshop moniker -->
<p align="center">
  <a href="https://github.com/TrentBest">
    <img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" />
  </a>
</p>

# AnyApp agent onboarding

This guide is the repository-specific entry point for AI agents and contributors. It supplements the [Workshop Documentation Standard](https://github.com/TrentBest/TheSingularityWorkshop.FSM_COS/blob/master/DOCUMENTATION_STANDARD.md) and the [FSM_COS agent onboarding material](https://github.com/TrentBest/TheSingularityWorkshop.FSM_COS/tree/master/Agents). Source, tests, and the active development branch remain authoritative for implementation facts.

## 1. Start on the correct branch

- **Active implementation branch: `development`.**
- **`master` is the stable baseline**, not a complete representation of current development.
- Read the `development` README and its linked architecture/handoff documents before concluding that a capability is absent.
- Inspect open PRs and branch ancestry before starting overlapping work. Prefer extending the active development line through a focused feature branch and PR.
- Do not revive historical feature branches merely because they contain code or documentation not present on `master`.

A master-only inspection can produce a false negative. AnyApp's development line contains substantial newer work: manifest-driven startup, source-mode composition, repository-backed MicroBundle delivery, semantic intent validation, native Renderer proof, StoryBuilder integration, startup/offline recovery, and trusted-distribution planning. Verify each claim against the current source and tests before describing it as complete.

## 2. Understand the ownership boundary

AnyApp is the **native Windows desktop host**. It provides the process, window, input, timing, resource, and native presentation substrate. It selects an Experience manifest and hands the composition request to FSM_COS.

Keep the dependency direction:

```text
Experience manifest / selected MicroBundles
                  |
                  v
               FSM_COS
                  |
                  v
            RuntimeAssembly
                  |
                  v
        AnyApp native host
                  |
                  v
       native presentation surface
```

AnyApp consumes the architecture; it must not become a second implementation of it.

- **FSM_COS** owns runtime composition semantics and returns a RuntimeAssembly.
- **MicroBundleDomain / MicroBundleRepository** own their respective contracts and repository delivery concerns.
- **Experiences** own reusable Experience behavior and manifests.
- **GUI / Visualize / Renderer** own their respective semantic UI, visualization, and rendering contracts.
- **AnyApp** owns desktop hosting, native integration, startup policy, and host-side adapters.
- **WebPage / WebForge** are separate web hosts. Do not pull browser UI or server responsibilities into the desktop host without an explicit cross-host design.

Do not move WPF, HWND, GPU API, or other platform-specific types into FSM_COS or platform-neutral core contracts. Keep native adapters narrow and host-owned.

## 3. Read before changing code

Start with these sources on `development`:

1. [README](https://github.com/TrentBest/AnyApp/blob/development/README.md) — current architecture, startup, composition, development commands, and known maturity limits.
2. [CI dependency modes](https://github.com/TrentBest/AnyApp/blob/development/docs/CI-DEPENDENCY-MODES.md) — source integration versus public NuGet compatibility. These are separate proofs.
3. [Startup and offline recovery](https://github.com/TrentBest/AnyApp/blob/development/docs/EXPERIENCE-STARTUP-AND-OFFLINE-RECOVERY.md) — cache, catalog, Forge fallback, and offline behavior.
4. [Trusted distribution](https://github.com/TrentBest/AnyApp/blob/development/docs/TRUSTED-DISTRIBUTION.md) — Windows distribution and signing constraints.
5. [GPU host integration](https://github.com/TrentBest/AnyApp/blob/development/docs/GPU_HOST_INTEGRATION.md) — native GPU direction and acceptance gates; it is a plan, not proof of GPU rendering.
6. [Visualize LLM handoff](https://github.com/TrentBest/AnyApp/blob/development/docs/VISUALIZE_LLM_HANDOFF.md) — ownership of the Visualize/Renderer adapter investigation.
7. [Open AnyApp pull requests](https://github.com/TrentBest/AnyApp/pulls) — current review surfaces and their stated safety boundaries.

If a linked path has moved, locate its replacement on `development`; do not silently substitute a similarly named document from `master`.

## 4. Build and verify

AnyApp is a direct `.csproj` host; do not assume a solution file exists.

```powershell
dotnet restore AnyApp.csproj
dotnet build AnyApp.csproj --configuration Release
dotnet test tests/TheSingularityWorkshop.AnyApp.Tests/TheSingularityWorkshop.AnyApp.Tests.csproj --configuration Release
```

The normal development CI uses Windows and repository-source dependency mode. Keep the source-mode properties consistent across restore, build, and test; see the dependency-mode guide for the exact workflow contract.

The public-package baseline is a separate compatibility check and currently has documented unavailable-package blockers. Do not hide those blockers with a local NuGet feed or by weakening the public-feed test. A source-mode green build does not prove public-package compatibility, and vice versa.

Label evidence accurately:
- **Verified:** executed against a named commit/workflow run and target.
- **Source-checked:** inspected in source but not executed.
- **Conceptual/planned:** design direction without implementation proof.

A build is not a runtime demonstration. A test fixture is not proof of full interactive behavior. A WPF diagnostic projection is not proof of production GPU rendering.

## 5. Desktop executable and Pages are different delivery concerns

If working on a Pages site that presents or distributes AnyApp.exe, preserve this distinction:

- **The desktop executable** is produced by a Windows build/publish workflow with an explicit configuration, target runtime identifier, deployment mode, version, and output verification.
- **GitHub Pages** is a static web hosting surface. It can present download instructions and, if deliberately configured, host a downloadable file; it does not compile, sign, install, or update the Windows application by itself.
- Prefer immutable, versioned GitHub Release assets as the authoritative binary distribution source. A Pages site can link to those assets and explain installation/verification.
- Publish checksums and, when signing is implemented, signature-verification instructions alongside the binary. Do not claim an executable is signed, trusted, self-updating, or production-ready without evidence.
- Distinguish framework-dependent, self-contained, and single-file output. Record the RID and actual artifact size; do not treat the small app host executable as the total self-contained deployment size.
- Do not make a Pages workflow publish a NuGet package, create a GitHub Release, upload a public binary, or deploy a production distribution unless that exact action has been explicitly authorized.

Before changing the Pages or executable pipeline, inspect the current workflow, artifact retention, permissions, branch triggers, and any existing release/signing gate. Coordinate with the agent already implementing the Pages experience; avoid creating a competing deployment pipeline.

## 6. Documentation requirements

Follow the shared Workshop standard. In particular:

- Teach the problem and rationale before implementation detail; use theory then implementation.
- Keep README orientation concise and link to focused architecture, learning, API/contract, tutorial, UML, and verification documents where applicable.
- Separate implemented behavior, current development state, intended architecture, and future work.
- Identify evidence provenance and exact revision for generated diagrams or measured claims.
- Inventory applicable UML/documentation views and mark planned or not-applicable items honestly.
- Keep code, tests, examples, and docs aligned. Raise issues for missing implementation or missing evidence rather than describing planned work as complete.
- Use the animated Workshop moniker at the top. Use the canonical footer image:
  `https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png`
  Do not substitute Avatar.jpg, a personal avatar, or the SVG moniker as the footer.

## 7. Change and authorization rules

Agents may independently inspect, test, document, and make focused branch-local changes. Open a PR for review and include exact base/head branches, commit SHA, changed files, test commands/results, and unresolved risks.

Do not:
- push directly to `master` or `development`;
- merge a PR or rewrite/retarget its base without approval;
- publish NuGet packages or public desktop releases;
- deploy Pages or production infrastructure;
- create or rotate credentials, change repository secrets, or broaden workflow permissions;
- delete branches or historical evidence;
- claim another repository's responsibility or change its contract without coordinating with its owner.

When blocked, state the exact decision/input required, what work is waiting, and what can continue independently. Prefer a small evidence-backed PR over a broad speculative rewrite.

## 8. Current known constraints

- AnyApp is a proving ground, not a finished desktop product.
- Development source integration and public NuGet compatibility are separate validation modes.
- The public-package baseline has documented unavailable dependency packages; publication is a separate user-authorized gate.
- The native Renderer diagnostic surface is not production GPU rendering.
- The GPU host integration document is an implementation plan with explicit backend/device/lifecycle acceptance gates.
- Trusted Windows distribution must not ask users to disable Windows security controls or install an untrusted root certificate.
- Experience startup/offline recovery does not guarantee offline operation when required artifacts are not cached.
- The repository does not require a solution file.

When you discover a newer state, update this list and its evidence links rather than preserving stale limitations.

---

<p align="center">
  <a href="https://github.com/TrentBest">
    <img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" />
  </a>
  <br />
  <strong>The Singularity Workshop</strong><br />
  <em>Forging Software for the Singularity.</em>
</p>
