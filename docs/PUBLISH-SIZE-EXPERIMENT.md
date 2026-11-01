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

`SatelliteResourceLanguages=en` restricts satellite resource assemblies; it is not a claim that every runtime-pack resource or every language-dependent behavior has been removed. The report inventories files left in each publish directory and records exact executable and directory byte counts plus deltas against the baseline profile.

## Interpreting the results

- Compare compression-only with baseline to isolate compression's size effect.
- Compare satellite-language-only with baseline to isolate the satellite filter's effect.
- Compare the combined profile with both individual profiles to identify interaction effects.
- Use exact byte counts for comparisons; MiB is a display convenience.
- The workflow's per-file inventory covers files physically present in each publish directory. A single-file executable contains embedded bundle entries, so this inventory alone is not a substitute for the deeper bundle-manifest treemap and per-entry analysis required by issue #42.
- Compression can affect startup cost. Size results alone do not establish an acceptable default; cold-start and real application behavior must also be measured.
- Satellite filtering must be checked against the cultures actually required by AnyApp and its WPF dependencies before adoption.

## Guardrails

This is an experiment, not a production configuration change. It does not enable trimming, manually delete framework assemblies, alter the normal publish profile, publish NuGet packages, create a release, or deploy an executable. Keep a self-contained distribution option available. Any recommended default must be supported by startup, rendering, accessibility, serialization, and offline-startup evidence.
