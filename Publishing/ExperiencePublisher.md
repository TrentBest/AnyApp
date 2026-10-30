# ExperiencePublisher

**Moniker:** The Singularity Workshop

**Purpose:** Resolve an authored Experience through FSM_COS during publication, capture every MicroBundle's deterministic effective configuration, upload immutable bundle/configuration artifacts, and write the resolved manifest only after all artifact uploads succeed.

## Class card

- **Inputs:** authored ExperienceManifest, IMicroBundleCatalog, IMicroBundleRepository, output manifest path
- **Outputs:** ExperiencePublicationResult, immutable artifact identities, resolved manifest, per-bundle configuration sidecars
- **Collaborators:** FSM_COS, MicroBundleDomain, MicroBundleRepository.Core
- **Failure boundary:** a failed publication does not write the final manifest; orphaned content-addressed artifacts are harmless and reusable
- **Runtime contract:** the output manifest carries the flat dependency-first load order and convergence round, allowing FSM_COS to restore configuration and skip runtime arbitration

## Manifest schema and compatibility

- `bundles` contains only explicit Experience composition roots and their per-root configuration identities.
- `artifactCatalogue` contains immutable `(bundleId, artifactVersion, contentHash)` identities for every artifact in the published dependency closure. Catalogue entries are metadata, not composition requests.
- `resolvedConfigurationCatalogue` contains the immutable resolved-configuration artifact identity for every bundle in that closure, keyed by `(bundleId, bundleVersion)` and carrying its artifact version, SHA-256 hash, and local sidecar path. Dependency configuration is not stored by promoting dependencies into `bundles`.
- Runtime repository configuration loading consumes the closure-wide catalogue. Legacy manifests without it fall back to configuration identities on their root entries; a pre-resolved flat plan must pin configuration for every plan entry.
- `resolvedPlan.loadOrder` may include catalogue entries that are not present in `bundles`; the runtime load plan includes the full closure while FSM_COS receives only the authored roots.
- New publications write `artifactCatalogue`. Legacy manifests without it are interpreted as flat publications: artifact identities are derived from the pinned entries in `bundles`. This compatibility path cannot supply identities for dependencies omitted from an old manifest.
- A present catalogue is authoritative, including an empty catalogue. Duplicate IDs, conflicting root/catalogue identities, unpinned entries, and missing dependency addresses are rejected rather than guessed or fetched by an unpinned version.

## Constraints

- Source-integration only until the MicroBundleDomain resolved-configuration contract is included in a published package and the dependent package versions are updated.
- Every included MicroBundle must implement IMicroBundleResolvedConfiguration.
- Assembly bytes and resolved configuration bytes are independently SHA-256 addressed.
- This service publishes MicroBundle/configuration artifacts and writes the Experience manifest locally. It does not publish that manifest to a hosted Experience catalog; the current catalog API is read-only.
- It never publishes NuGet packages.
