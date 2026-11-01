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

Workflow [#504](https://github.com/TrentBest/AnyApp/actions/runs/41360140100) passed the four-profile publish matrix. Its embedded manifests were inspected locally from the uploaded CI artifact using the same parser now being added to CI:

| Profile | Executable | Embedded entries | Compressed entries | Stored embedded payload |
|---|---:|---:|---:|---:|
| baseline | 162,844,055 bytes (155.30 MiB) | 468 | 0 | 152,299,036 bytes |
| compression-only | 72,058,634 bytes (68.72 MiB) | 468 | 466 | 62,386,440 bytes |
| satellite-language-only | 146,162,962 bytes (139.39 MiB) | 247 | 0 | 136,035,468 bytes |
| compression-and-satellite-language | 66,300,040 bytes (63.23 MiB) | 247 | 245 | 56,642,315 bytes |

The baseline contains 221 satellite resource assemblies (17 in each of 13 cultures: `cs`, `de`, `es`, `fr`, `it`, `ja`, `ko`, `pl`, `pt-BR`, `ru`, `tr`, `zh-Hans`, and `zh-Hant`). Together they occupy 16,263,568 bytes (15.51 MiB). No English satellite assemblies were present in this bundle, so `SatelliteResourceLanguages=en` removed all 221 satellite entries in this build. This is a measured result for this dependency graph, not proof that English is the only culture AnyApp or its dependencies may require.

The embedded manifest groups the baseline payload as follows. Categories are heuristic groupings based on bundle file type and assembly names; the CSV preserves each exact path and size.

| Category | Entries | Uncompressed bytes |
|---|---:|---:|
| .NET runtime | 198 | 70,677,112 |
| WPF | 17 | 33,856,032 |
| Windows Forms | 4 | 22,156,528 |
| Satellite resources | 221 | 16,263,568 |
| Native runtime libraries | 5 | 8,214,968 |
| Workshop/application assemblies | 21 | 1,084,416 |
| Configuration | 2 | 46,412 |

The bundle contains 466 compressible entries in the compression profiles. The two entries not compressed are `AnyApp.deps.json` and `AnyApp.runtimeconfig.json`. The residual between executable size and stored embedded payload includes the native apphost, alignment padding, and bundle manifest/metadata; it is not all application code.

## Guardrails

This is an experiment, not a production configuration change. It does not enable trimming, manually delete framework assemblies, alter the normal publish profile, publish NuGet packages, create a release, or deploy an executable. Keep a self-contained distribution option available. Any recommended default must be supported by startup, rendering, accessibility, serialization, and offline-startup evidence.
