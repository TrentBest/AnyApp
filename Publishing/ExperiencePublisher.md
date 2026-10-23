# ExperiencePublisher

**Moniker:** The Singularity Workshop

**Purpose:** Resolve an authored Experience through FSM_COS during publication, capture every MicroBundle's deterministic effective configuration, upload immutable bundle/configuration artifacts, and write the resolved manifest only after all artifact uploads succeed.

## Class card

- **Inputs:** authored ExperienceManifest, IMicroBundleCatalog, IMicroBundleRepository, output manifest path
- **Outputs:** ExperiencePublicationResult, immutable artifact identities, resolved manifest, per-bundle configuration sidecars
- **Collaborators:** FSM_COS, MicroBundleDomain, MicroBundleRepository.Core
- **Failure boundary:** a failed publication does not write the final manifest; orphaned content-addressed artifacts are harmless and reusable
- **Runtime contract:** the output manifest carries the flat dependency-first load order and convergence round, allowing FSM_COS to restore configuration and skip runtime arbitration

## Constraints

- Source-integration only until the MicroBundleDomain resolved-configuration contract is included in a published package and the dependent package versions are updated.
- Every included MicroBundle must implement IMicroBundleResolvedConfiguration.
- Assembly bytes and resolved configuration bytes are independently SHA-256 addressed.
- This service publishes MicroBundle/configuration artifacts and writes the Experience manifest locally. It does not publish that manifest to a hosted Experience catalog; the current catalog API is read-only.
- It never publishes NuGet packages.
