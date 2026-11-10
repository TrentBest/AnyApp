<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# Windows publish-size experiment

> **Status — historical experiment (reviewed 2026-11-10).** The multi-profile publish matrix and embedded-bundle audit described here are preserved results from earlier CI runs; they are not part of the current `development` workflow. Treat the run measurements below as historical evidence, not as proof that these profiles are still being produced. The current workflow's publish contract remains authoritative.

## Purpose

Measure the effect of single-file bundle compression and satellite-resource language filtering independently. The experiment is deliberately separate from the package-compatibility baseline and does not change AnyApp's default publish properties.

The current forensic starting point is recorded in [issue #42](https://github.com/TrentBest/AnyApp/issues/42): a self-contained, single-file, win-x64 executable of 162,844,055 bytes (155.30 MiB), built with compression disabled.

## Controlled profiles

The historical `.github/workflows/dotnet.yml` development workflow published four variants from the same source commit, Windows runner, runtime identifier, configuration, and source dependency graph:

| Profile | `EnableCompressionInSingleFile` | `SatelliteResourceLanguages` |
|---|---:|---|
| baseline | false | unrestricted |
| compression-only | true | unrestricted |
| satellite-language-only | false | `en` |
| compression-and-satellite-language | true | `en` |

All four profiles remain self-contained, single-file, Release, `win-x64` publishes. Native libraries are included for self-extraction consistently across the four profiles.

`SatelliteResourceLanguages=en` restricts satellite resource assemblies; it is not a claim that every runtime-pack resource or every language-dependent behavior has been removed. The report inventories files left in each publish directory and records exact executable and directory byte counts plus deltas against the baseline profile. A separate bundle-manifest inspector reads the embedded .NET single-file manifest and emits a per-entry CSV, JSON summary, and standalone HTML treemap for every profile.

## Interpreting the results

- Compare compression-only with baseline to isolate compression's size effect.
- Compare satellite-language-only with baseline to isolate the satellite filter's effect.
- Compare the combined profile with both individual profiles to identify interaction effects.
- Use exact byte counts for comparisons; MiB is a display convenience.
- The workflow now audits the embedded bundle manifest as well as the loose publish-directory files. For each profile it emits `bundle-inventory.csv`, `bundle-summary.json`, and a standalone `bundle-treemap.html`; the uploaded CI artifact contains all four profile audits.
- Compression can affect startup cost. Size results alone do not establish an acceptable default; cold-start and real application behavior must also be measured.
- Satellite filtering must be checked against the cultures actually required by AnyApp and its WPF dependencies before adoption.

## First measured results (2026-11-01)

Workflow [#505](https://github.com/TrentBest/AnyApp/actions/runs/41363428455) passed the four-profile publish matrix and the embedded-manifest audit. The uploaded artifact includes the raw executable profiles, per-file output-directory inventories, embedded-entry CSVs, JSON summaries, and standalone HTML treemaps.

| Profile | Executable | Embedded entries | Compressed entries | Stored embedded payload |
|---|---:|---:|---:|---:|
| baseline | 162,844,055 bytes (155.30 MiB) | 468 | 0 | 152,298,236 bytes |
| compression-only | 72,058,950 bytes (68.72 MiB) | 468 | 466 | 62,386,244 bytes |
| satellite-language-only | 146,162,962 bytes (139.39 MiB) | 247 | 0 | 136,035,628 bytes |
| compression-and-satellite-language | 66,300,228 bytes (63.23 MiB) | 247 | 245 | 56,641,991 bytes |

Compression alone reduced the executable by 90,785,105 bytes (55.75%). The satellite filter alone reduced it by 16,681,093 bytes (10.24%). Combined, the reduction was 96,543,827 bytes (59.29%).

The baseline contains 221 satellite resource assemblies (17 in each of 13 cultures: `cs`, `de`, `es`, `fr`, `it`, `ja`, `ko`, `pl`, `pt-BR`, `ru`, `tr`, `zh-Hans`, and `zh-Hant`). Together they occupy 16,262,608 bytes (15.51 MiB). No English satellite assemblies were present in this build, so `SatelliteResourceLanguages=en` removed all 221 satellite entries. This is a measured result for this dependency graph, not proof that English is the only culture AnyApp or its dependencies may require.

The embedded manifest groups the baseline payload as follows. Categories are heuristic groupings based on bundle file type and assembly names; the CSV preserves each exact path and size.

| Category | Entries | Uncompressed bytes |
|---|---:|---:|
| .NET runtime | 198 | 70,677,272 |
| WPF core | 17 | 33,856,112 |
| Windows Forms | 4 | 22,156,448 |
| Satellite resources | 221 | 16,262,608 |
| Native runtime libraries | 5 | 8,214,968 |
| Workshop/application assemblies | 21 | 1,084,416 |
| Configuration | 2 | 46,412 |

The bundle contains 466 compressible entries in the compression profiles. The two entries not compressed are `AnyApp.deps.json` and `AnyApp.runtimeconfig.json`. The residual between executable size and stored embedded payload includes the native apphost, alignment padding, and bundle manifest/metadata; it is not all application code.

### Windows Forms payload provenance — initial source inspection (2026-11-02)

The AnyApp host project and its direct GUI.WPF project both declare `UseWPF=true`; neither project declares `UseWindowsForms`. The #513 embedded bundle nevertheless contains these Windows Forms framework assemblies:

| Assembly | Uncompressed bytes |
|---|---:|
| `System.Windows.Forms.dll` | 13,563,728 |
| `System.Windows.Forms.Design.dll` | 5,564,200 |
| `System.Windows.Forms.Primitives.dll` | 3,012,392 |
| `System.Windows.Forms.Design.Editors.dll` | 16,208 |
| **Total** | **22,156,528** |

The bundle also contains WinForms satellite resources within the 221 localized resource entries. This is a measured inventory, not proof that AnyApp calls these APIs or that removing the assemblies is safe. The source-project declarations rule out an explicit `UseWindowsForms` setting in these two projects, but do not by themselves establish the full SDK/runtime-pack or transitive-reference provenance. Keep the assemblies intact until that provenance and runtime requirements are demonstrated with a reproducible publish and launch test.


### Reproducibility validation (2026-11-01, workflow #507)

Workflow [#507](https://github.com/TrentBest/AnyApp/actions/runs/41363849664) passed the same source-mode build/test, Windows publish, four-profile matrix, and embedded-manifest audit. Its artifact records the AnyApp source head and CI merge checkout plus all 14 source dependency commit SHAs in both `dependency-revisions.json` and `dependency-revisions.md`. This confirms that the dependency-revision capture is present in the uploaded evidence, not merely in the runner workspace.

The #507 executable measurements were 162,844,055 bytes (baseline), 72,058,647 bytes (compression only), 146,162,962 bytes (satellite filter only), and 66,300,053 bytes (combined). The combined profile remains approximately 59.29% smaller than baseline. Small differences from #505 are expected because active dependency branches are checked out at their current `development` heads; compare the per-run dependency SHAs before attributing cross-run differences to publish settings. The four profiles within each run share one captured dependency graph.

### Latest validation and startup measurements (2026-11-02, workflow #513)

Workflow [#513](https://github.com/TrentBest/AnyApp/actions/runs/41389077013) passed the source-mode Renderer and AnyApp builds, AnyApp tests, Windows x64 self-contained publish, four-profile matrix, clean-extraction startup benchmark, and embedded bundle audit. Its artifact includes all four profile executables and inventories, bundle CSV/JSON/HTML audits, exact source dependency revisions for all 14 checkouts, and the startup raw CSV/report.

The #513 executable measurements were:

| Profile | Executable | Publish directory | Executable reduction vs baseline |
|---|---:|---:|---:|
| baseline | 162,844,055 bytes (155.30 MiB) | 163,857,399 bytes | — |
| compression-only | 72,058,935 bytes (68.72 MiB) | 73,072,279 bytes | 55.75% |
| satellite-language-only | 146,162,962 bytes (139.39 MiB) | 147,176,306 bytes | 10.24% |
| compression-and-satellite-language | 66,300,341 bytes (63.23 MiB) | 67,313,685 bytes | 59.29% |

The combined profile is 96,543,714 bytes smaller than baseline. These byte-level values are specific to the exact dependency revisions recorded in this run's artifact.

The #513 first-render benchmark results (five samples per profile) were:

| Profile | Median ms | Minimum ms | Maximum ms |
|---|---:|---:|---:|
| baseline | 571.76 | 538.43 | 636.56 |
| compression-only | 894.96 | 858.97 | 995.59 |
| satellite-language-only | 500.75 | 484.09 | 519.32 |
| compression-and-satellite-language | 780.85 | 766.53 | 803.49 |

In this run, compression-only increased median startup-to-first-render by 323.20 ms (56.5%) versus baseline. The combined profile increased it by 209.09 ms (36.6%). Satellite filtering alone was 71.01 ms (12.4%) faster than baseline in this sample, but five launches on one hosted runner are preliminary evidence and do not establish a general performance guarantee. The data demonstrates a meaningful size/startup trade-off: compression substantially reduces executable size but increases first-render time in this measurement. Keep compression optional pending broader startup and compatibility evidence; do not change the default publish profile based on these results alone.

As described below, this benchmark forces a fresh bundle extraction directory for each launch but does not clear the OS file cache. It measures the local `--renderer-proof` surface through the first WPF `ContentRendered` event, not full Experience readiness, cold OS boot, later rendering, accessibility, serialization, or offline Experience behavior.

### Reproducing a comparison

The CI workflow intentionally checks out active dependency branches at `development`. Each run now records the exact AnyApp source-head SHA, PR merge/check-out SHA, and commit SHA for every source dependency in `dependency-revisions.json` and `dependency-revisions.md`, included in the artifact. Four profiles within a run use the same checked-out graph. For cross-run comparisons, compare the recorded dependency revisions before attributing small byte-level differences to the publish settings.


## Clean-extraction startup-to-first-render benchmark

The Windows CI matrix also launches each of the four published executables five times with `--renderer-proof`. Each process receives a unique `DOTNET_BUNDLE_EXTRACT_BASE_DIR`, ensuring that the single-file bundle is extracted into a fresh directory for every sample. An opt-in `ANYAPP_STARTUP_RENDER_MARKER` environment variable causes the WPF host to write a UTC marker at its first `ContentRendered` event; the benchmark measures elapsed time from process launch until that marker appears.

The benchmark emits raw samples in `startup-measurements.csv` and per-profile median, minimum, and maximum in `startup-report.md`. It deliberately uses a local proof surface and does not contact the repository or compose an Experience, isolating host startup/rendering from network and Experience variability.

Interpret this as **clean-extraction startup-to-first-render with a potentially warm OS file cache**. A unique extraction directory forces bundle extraction but does not clear the operating-system file cache, so this is not a true cold-boot measurement. The first `ContentRendered` event is not proof that a full Experience is ready, nor does it measure later rendering, accessibility, serialization, or offline Experience behavior. The marker is opt-in and has no effect when the environment variable is absent.

## Alternate deployment shapes

The CI workflow also publishes two additional Windows x64 deployment shapes from the same source checkout and dependency graph:

| Profile | Runtime included? | Single-file? | Distribution requirement |
|---|---|---|---|
| framework-dependent-single-file | No | Yes | Target machine needs a compatible .NET 8 Windows Desktop Runtime installed |
| self-contained-directory | Yes | No | Distribute the complete publish directory, not only `AnyApp.exe` |

The framework-dependent profile isolates the smaller host/application bundle from the separately installed .NET Desktop Runtime. Its executable size must not be compared to a self-contained executable as though both were complete standalone distributions. The self-contained-directory profile carries runtime files alongside the application; compare its total directory size, file count, and launch measurement rather than only its apphost executable.

The workflow records exact executable bytes, total publish-directory bytes, file count, and a complete file inventory for each profile. It also runs five `--renderer-proof` first-render launches per profile using the same opt-in render marker. The single-file profiles use fresh bundle-extraction directories; the directory deployment has no single-file bundle extraction. All profiles run on the same Windows CI host and dependency graph for the run.

These measurements are comparative evidence, not a claim that one deployment is universally preferable. The framework-dependent profile trades distribution size for a runtime prerequisite; the self-contained directory trades a single executable for a larger multi-file distribution. The existing self-contained single-file profiles remain available as the original baseline experiment.

### Alternate deployment measurements (2026-11-02, workflow #517)

Workflow [#517](https://github.com/TrentBest/AnyApp/actions/runs/41390665755) passed on source head `00e174d8aa4eaeea68a2fda8fb8889dc0badd2e0`. Both alternate deployment profiles published successfully, produced inventories, and completed all five first-render launches. The run also passed the existing source-mode builds/tests, four controlled size profiles, and embedded bundle audit.

| Profile | Executable | Total publish directory | Files | Runtime prerequisite |
|---|---:|---:|---:|---|
| Framework-dependent single-file | 1,294,722 bytes (1.23 MiB) | 2,308,066 bytes (2.20 MiB) | 43 | Compatible .NET 8 Windows Desktop Runtime installed on target |
| Self-contained directory | 152,064 bytes (0.15 MiB) | 169,874,297 bytes (162.00 MiB) | 525 | No separate .NET runtime; distribute the complete directory |

The 1.23 MiB framework-dependent executable is not a complete standalone distribution: the machine-provided .NET Desktop Runtime is outside the measured 2.20 MiB publish folder. The self-contained directory's apphost is only 0.15 MiB, but its 162 MiB directory carries the runtime and WPF payload. These are different deployment contracts, so comparing only their executable sizes would be misleading.

The six-profile first-render benchmark (five launches each) reported:

| Profile | Median ms | Minimum ms | Maximum ms |
|---|---:|---:|---:|
| Self-contained single-file baseline | 515.99 | 470.22 | 581.88 |
| Compression only | 793.69 | 760.18 | 830.10 |
| Satellite-language filter only | 536.47 | 513.86 | 592.35 |
| Compression and satellite-language filter | 873.92 | 775.40 | 898.45 |
| Framework-dependent single-file | 516.06 | 447.89 | 548.56 |
| Self-contained directory | 556.94 | 482.62 | 586.97 |

The framework-dependent single-file profile's median was effectively the same as the self-contained baseline in this run (516.06 vs. 515.99 ms). The self-contained directory median was 40.95 ms slower (556.94 vs. 515.99 ms). Five launches on one hosted runner are preliminary evidence, not a general performance guarantee. As elsewhere in this document, this measures process launch to the first WPF `ContentRendered` event for `--renderer-proof`, not full Experience readiness or a cold OS boot; OS file cache may be warm.

The complete artifact includes `deployment-profile-report.md`, per-profile file inventories, all six profile binaries/publish outputs, raw startup CSV and summary, bundle audit, and all 14 source dependency revisions. Artifact IDs: size matrix `13117985204`; startup benchmark `13117467126`. The artifact expires 2026-12-02.

## Guardrails

This is an experiment, not a production configuration change. It does not enable trimming, manually delete framework assemblies, alter the normal publish profile, publish NuGet packages, create a release, or deploy an executable. Keep a self-contained distribution option available. Any recommended default must be supported by startup, rendering, accessibility, serialization, and offline-startup evidence.

---

<!-- The Singularity Workshop canonical footer -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
