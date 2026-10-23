# Experience Manifest Publishing and Runtime Contract

## Purpose

AnyApp is a manifest-driven desktop host. Selecting a different Experience manifest must select a different runtime composition; it must not require a new AnyApp compilation.

AnyApp selects the desktop Experience and supplies its published inputs to FSM_COS. It does not own MicroBundle dependency resolution or arbitration rules.

## Canonical ownership

- **Experience manifest:** identifies the Experience and its immutable MicroBundle versions/artifacts. A published manifest records the complete, deduplicated MicroBundle closure, not instructions for the runtime to recursively discover an unknown set of artifacts.
- **Per-MicroBundle configuration file:** contains only explicit overrides for that bundle. If no override exists, the MicroBundle's own defaults apply. Configuration schema and default interpretation belong to the bundle, not AnyApp.
- **MicroBundleDomain:** owns the neutral MicroBundle contract and any contract needed for a bundle to export/import publish-resolved configuration.
- **MicroBundleRepository:** locates, retrieves, materializes, and verifies immutable artifact bytes. It does not arbitrate or compose.
- **FSM_COS:** resolves and validates the composition during authoring/publishing, performs bounded arbitration, and produces the resolved assembly plan. At runtime it validates the published plan and performs the planned assembly; it must not repeat authoring-time arbitration for a successfully resolved publication.
- **AnyApp:** chooses the manifest, provides desktop host services and per-bundle overrides, and presents the resulting runtime through the desktop GUI adapter.

## Publish pipeline

~~~text
Authored Experience manifest
        + per-bundle override files
        + immutable MicroBundle artifacts
                    |
                    v
        FSM_COS publish/preflight
                    |
          recursive dependency closure
          version and identity validation
          cycle rejection and deduplication
          deterministic dependency order
          bounded arbitration / convergence
          resolved configuration capture
                    |
                    v
        Immutable resolved publication
        - flat bundle closure
        - bundle ID + version + SHA-256
        - dependency edges / load order
        - effective per-bundle configuration
        - configuration content hashes
        - arbitration convergence evidence
                    |
                    v
        Repository publication / manifest hash
~~~

A publish is successful only when the dependency closure is complete, identities and hashes match, all configuration is valid, and arbitration converges within the configured bound. The output must be deterministic for identical inputs. A failed publish must not create a runnable resolved manifest.

## Runtime pipeline

~~~text
AnyApp selects Experience manifest
                    |
                    v
Read flat resolved bundle/configuration plan
                    |
                    v
Check local cache by immutable identity
                    |
          +---------+----------+
          |                    |
      cache hit             cache miss
          |                    |
          |              Repository retrieval
          |              SHA-256 verification
          +---------+----------+
                    |
                    v
        FSM_COS planned assembly
        - validate plan identity
        - load in published dependency order
        - apply bundle defaults + explicit resolved overrides
        - do not rerun publish-time arbitration
                    |
                    v
        RuntimeAssembly -> AnyApp host / GUI
~~~

The runtime does not compile an Experience-specific executable. It consumes the manifest and the exact artifacts/configuration that publication resolved.

## Configuration precedence

1. A MicroBundle's own defaults are the baseline.
2. An authored per-bundle configuration file overrides only fields it explicitly contains, according to that bundle's configuration contract.
3. Publish-time arbitration resolves conflicts and records the effective configuration/results in the immutable publication.
4. Runtime-local overrides, if a future host explicitly permits them, must be a separately identified layer and must not mutate the published manifest or its hashes.

Missing configuration means **use the bundle defaults**; it does not mean “replace defaults with an empty configuration.”

## Integrity and deterministic identity

- Bundle artifacts are identified by bundle ID, explicit version, and SHA-256.
- Configuration artifacts are separately identified and hashed.
- The resolved manifest binds the exact bundle/configuration versions and the dependency edges used during arbitration.
- Runtime rejects missing, duplicate, mismatched, cyclic, or hash-invalid plan entries rather than silently resolving a different composition.
- The published manifest's content hash identifies the complete immutable publication.

## Implementation status — development branch

Existing code already has versioned Experience requests, repository-backed assembly materialization, optional per-bundle configuration input, RuntimeManifestSchedule, and bounded FSM_COS arbitration. The local manifest path now also supports separate manifest-relative configuration files.

The following are still required before this contract is implemented end to end:

1. A publisher that computes and serializes the flat, deduplicated closure and dependency order from the actual MicroBundle artifacts.
2. A MicroBundleDomain contract for capturing and restoring publish-resolved per-bundle configuration/results, with explicit handling for bundles that cannot support pre-resolved execution.
3. FSM_COS support for consuming the published plan as the runtime load order and skipping arbitration only when the publication explicitly proves it was resolved.
4. Repository-backed immutable configuration artifact retrieval and hash verification.
5. Tests proving two different manifests run through the same AnyApp binary, absent overrides preserve bundle defaults, and runtime loading does not repeat arbitration.

A successful ordinary FsmCos.Execute is not evidence that these publish-time/runtime-separation requirements are complete.
