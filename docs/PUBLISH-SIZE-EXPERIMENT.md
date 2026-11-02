<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# Windows publish-size experiment

## Purpose

Measure the effect of single-file bundle compression and satellite-resource language filtering independently. The experiment is deliberately separate from the package-compatibility baseline and does not change AnyApp's default publish properties.

The current forensic starting point is recorded in [issue #42](https://github.com/TrentBest/AnyApp/issues/42): a self-contained, single-file, win-x64 executable of 162,844,055 bytes (155.30 MiB), built with compression disabled.

## Controlled profiles

The existing `.github/workflows/dotnet.yml` development workflow publishes four variants from the same source commit, Windows runner, runtime identifier, configuration, and source dependency graph:

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


### Reproducibility validation (2026-11-01, workflow #507)

Workflow [#507](https://github.com/TrentBest/AnyApp/actions/runs/41363849664) passed the same source-mode build/test, Windows publish, four-profile matrix, and embedded-manifest audit. Its artifact records the AnyApp source head and CI merge checkout plus all 14 source dependency commit SHAs in both `dependency-revisions.json` and `dependency-revisions.md`. This confirms that the dependency-revision capture is present in the uploaded evidence, not merely in the runner workspace.

The #507 executable measurements were 162,844,055 bytes (baseline), 72,058,647 bytes (compression only), 146,162,962 bytes (satellite filter only), and 66,300,053 bytes (combined). The combined profile remains approximately 59.29% smaller than baseline. Small differences from #505 are expected because active dependency branches are checked out at their current `development` heads; compare the per-run dependency SHAs before attributing cross-run differences to publish settings. The four profiles within each run share one captured dependency graph.

### Reproducing a comparison

The CI workflow intentionally checks out active dependency branches at `development`. Each run now records the exact AnyApp source-head SHA, PR merge/check-out SHA, and commit SHA for every source dependency in `dependency-revisions.json` and `dependency-revisions.md`, included in the artifact. Four profiles within a run use the same checked-out graph. For cross-run comparisons, compare the recorded dependency revisions before attributing small byte-level differences to the publish settings.


## Clean-extraction startup-to-first-render benchmark

The Windows CI matrix also launches each of the four published executables five times with `--renderer-proof`. Each process receives a unique `DOTNET_BUNDLE_EXTRACT_BASE_DIR`, ensuring that the single-file bundle is extracted into a fresh directory for every sample. An opt-in `ANYAPP_STARTUP_RENDER_MARKER` environment variable causes the WPF host to write a UTC marker at its first `ContentRendered` event; the benchmark measures elapsed time from process launch until that marker appears.

The benchmark emits raw samples in `startup-measurements.csv` and per-profile median, minimum, and maximum in `startup-report.md`. It deliberately uses a local proof surface and does not contact the repository or compose an Experience, isolating host startup/rendering from network and Experience variability.

Interpret this as **clean-extraction startup-to-first-render with a potentially warm OS file cache**. A unique extraction directory forces bundle extraction but does not clear the operating-system file cache, so this is not a true cold-boot measurement. The first `ContentRendered` event is not proof that a full Experience is ready, nor does it measure later rendering, accessibility, serialization, or offline Experience behavior. The marker is opt-in and has no effect when the environment variable is absent.

## Guardrails

This is an experiment, not a production configuration change. It does not enable trimming, manually delete framework assemblies, alter the normal publish profile, publish NuGet packages, create a release, or deploy an executable. Keep a self-contained distribution option available. Any recommended default must be supported by startup, rendering, accessibility, serialization, and offline-startup evidence.

---

<!-- The Singularity Workshop canonical footer -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
