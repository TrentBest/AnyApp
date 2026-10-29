# The Singularity Workshop

# AnyApp startup, offline recovery, and cached Experiences

AnyApp starts by reconciling the last locally usable Experience with the current publication catalog. This is a recovery policy for the desktop host; it does not change FSM_COS composition rules or authorize the host to choose an arbitrary Experience.

## What happens at startup

1. AnyApp loads the last cached manifest and its optional publication record.
2. If there is no usable cached manifest, AnyApp opens the Forge/browse surface so the user can select an Experience.
3. If the catalog is reachable, AnyApp compares the cached publication identity and version with the catalog.
4. If the publication is unchanged, AnyApp can resume the cached manifest without fetching the immutable manifest again.
5. If the publication changed, AnyApp fetches the current manifest and verifies that its Experience identity and version match the catalog entry before using it.
6. If the catalog cannot be reached, AnyApp may resume the last valid cached manifest. It does not rewrite the cached publication identity based on an offline observation.
7. If a previously published Experience is no longer present in the catalog, the host follows the Forge path rather than silently treating the old publication as current.

“Forge” means the host's browse/selection surface. It does not mean that AnyApp automatically selects or executes a different Experience.

## Offline behavior

An unavailable catalog is not evidence that a publication has changed or been withdrawn. When a valid local manifest is available, a network failure or catalog-request timeout allows the host to continue from that cached manifest while leaving publication identity unchanged.

This is a best-effort offline path, not a guarantee that every Experience can run offline. The manifest and its required artifacts must already be available and usable locally; any later repository access required by composition can still fail independently.

Explicit cancellation initiated by the caller remains cancellation. It is not converted into an offline-success result.

## How the cache stays recoverable

The cache uses generation-scoped storage:

- `generations/{generation-id}/manifest.json` stores the manifest bytes.
- `generations/{generation-id}/configuration/{BundleId}.bin` stores configuration sidecars for that generation.
- `generations/{generation-id}/publication.json`, when present, stores the publication identity and a SHA-256 digest of the manifest bytes.
- `active-generation.txt` identifies the active generation.

A save writes the candidate generation before promoting the active-generation pointer. The pointer is the commit point: a failed write before promotion leaves the previously active generation selected. Older generations are retained rather than being overwritten as part of the save. The publication record is checked against both the manifest digest and the manifest's Experience identity/version before it is trusted.

Legacy flat cache files remain supported for compatibility. New writes use generation-based storage.

## What users should do

- **First launch or no valid cache:** open Forge and choose an Experience while the catalog is available.
- **Catalog temporarily unavailable:** restart or retry when the service is reachable. If a valid cached manifest exists, AnyApp can resume it without changing the recorded publication identity.
- **A publication is missing or has changed:** use Forge to inspect/select an available Experience. Do not assume the cached version is still the catalog's current publication.
- **Cache files appear damaged:** preserve the cache for diagnosis. Do not manually edit the manifest, publication record, generation folders, or active pointer; the digest and identity checks are intended to detect inconsistent data.

## Trust and composition boundaries

The catalog supplies publication state; the cached manifest supplies the last locally known composition request; the repository supplies immutable MicroBundle artifacts; FSM_COS composes a validated runtime; AnyApp owns desktop startup and presentation. These roles are deliberately separate.

Offline recovery does not bypass repository-source identity validation, change the meaning of manifest roots, or treat transitive artifact dependencies as additional composition roots.

## Verification

The automated AnyApp test suite covers cache-generation promotion failure and catalog timeout recovery without a live catalog. The CI workflow builds the Renderer source graph, restores and builds AnyApp in repository-source mode, and runs the AnyApp tests on a Windows runner.

For current verification, consult the [AnyApp CI workflow](https://github.com/TrentBest/AnyApp/actions/workflows/dotnet.yml) and the tracked [startup/cache recovery work](https://github.com/TrentBest/AnyApp/issues/30). This document describes intended observable behavior; it is not a claim that offline execution can succeed when required artifacts are absent.

---

<p align="center">
  <img src="../Avatar.jpg" alt="The Singularity Workshop" width="112" />
  <br />
  <strong>The Singularity Workshop</strong><br />
  <em>Forging Software for the Singularity.</em>
</p>
