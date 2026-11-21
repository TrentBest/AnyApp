<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# 03 — Startup, recovery, and continuity

## Chapter purpose

A desktop application is not merely a process that starts from zero. It is a host that must decide whether previously selected work can be resumed, whether the outside world has changed, and what to do when the network or local storage is unreliable.

This chapter develops a general model of startup reconciliation using AnyApp as a concrete example. The implementation is deliberately conservative: it distinguishes a locally usable composition request from the publisher's current statement about that publication.

**Central thesis:** recovery is a decision under partial knowledge. A host must preserve the last verified local state without pretending that an unavailable authority has confirmed it is current.

## Learning objectives

After this chapter, a reader should be able to:

1. Distinguish a cached manifest, a publication record, a catalog listing, and immutable artifact bytes.
2. Explain why network failure is not evidence that a publication changed, disappeared, or remains current.
3. Trace startup reconciliation through first launch, unchanged publication, changed publication, catalog outage, and missing publication.
4. Explain generation-based cache promotion and why the active-generation pointer is a commit point.
5. Distinguish caller cancellation, transient unavailability, invalid data, and a missing publication.
6. State what offline recovery guarantees—and what it does not.
7. Identify the tests and evidence needed before claiming that a host can recover safely.

## 1. The continuity problem

A desktop host is expected to remember what a person was using. But the remembered selection and the publisher's current catalog answer different questions:

- The local cache answers: **What composition request did this host last retain?**
- The publication record answers: **Which immutable publication identity was associated with that cached manifest?**
- The catalog answers: **Which publication does the publisher currently advertise?**
- The artifact repository answers: **Can the exact bytes required by that publication be retrieved and verified?**

These facts must not be collapsed into a single boolean such as `isCurrent`. A valid local manifest may be usable even when the catalog is unreachable. Conversely, a reachable catalog does not make a damaged local cache trustworthy, and a valid manifest does not prove that every required MicroBundle artifact is already available.

The recovery problem is therefore not “load the last file.” It is a reconciliation between local evidence and an external publication authority.

## 2. Vocabulary

| Term | Meaning |
|---|---|
| Cached manifest | The last locally retained Experience composition request |
| Publication record | Local record of the published Experience identity associated with a cached manifest |
| Catalog | Publisher-facing listing of currently advertised Experience publications |
| Publication identity | Experience ID, version, and content hash |
| Immutable manifest artifact | Exact manifest bytes addressed by a publication identity |
| Generation | A coherent set of cached manifest/configuration/publication files |
| Active-generation pointer | The small record selecting which cache generation is currently authoritative locally |
| Reconciliation | Comparing local state with available publication state and selecting a safe host action |
| Offline resume | Continuing from a valid cached manifest when catalog access is unavailable |

A publication identity is not just an Experience ID. AnyApp represents it as the tuple `(ExperienceId, Version, ContentHash)`. The hash distinguishes immutable content, even when the Experience ID and version text happen to match.

## 3. The reconciliation model

The host's startup coordinator accepts storage and catalog operations as functions. It does not own the UI or perform HTTP itself. That separation makes the policy testable and leaves platform effects with the host.

The conceptual sequence is:

```text
Load cached manifest + publication record
                 |
                 v
       Is a manifest available?
          /             \
        no               yes
        |                 |
  Browse / Forge           v
                 Query publication catalog
                       /       \
                 unavailable   available
                     |             |
                     v             v
              Resume cached    Find same Experience
              manifest,        in current catalog
              do not rewrite       |
              publication       /     \
                              absent   present
                                |        |
                         Apply missing  Compare full
                         publication    identity
                         policy             |
                                      /          \
                                  unchanged     changed
                                      |            |
                                Resume cache   Fetch exact
                                               publication
                                                    |
                                          Verify identity + bytes
                                                    |
                                              Resume refreshed
```

The diagram is a conceptual summary. The actual implementation has deliberately distinct outcomes for “browse Experiences” and “launch Forge,” and it treats some malformed or unexpected conditions more conservatively than a network outage.

### 3.1 First launch and unusable local state

If no cached manifest is available, there is no prior composition request to resume. The coordinator returns the browse-Experiences decision.

If loading the cached manifest or publication record fails with an unexpected exception, the coordinator returns the Forge decision. It does not silently manufacture a replacement cache entry or select a different Experience.

This is an important recovery principle: absence of trusted local state is not permission to guess.

### 3.2 Catalog unavailable

If listing the catalog fails with `HttpRequestException`, the coordinator returns the cached manifest with no publication object and `PersistLastManifest: false`.

The same offline-resume decision is used for an `OperationCanceledException` when the caller's cancellation token was not canceled. This accounts for HTTP client timeouts, which can also surface as cancellation exceptions.

If the caller's own token is canceled, the coordinator rethrows cancellation. User intent is not reinterpreted as successful offline recovery.

The cached publication identity is not rewritten. The host has learned only that it could not ask the catalog—not that the publication changed or stayed current.

### 3.3 Catalog reachable, publication unchanged

The coordinator locates the current catalog entry for the cached Experience ID. It considers the publication unchanged only when:

- Experience IDs match;
- version strings match using ordinal comparison; and
- content hashes match, ignoring hexadecimal letter case.

It also checks that the cached manifest version matches the current publication version. When these conditions hold, the cached manifest can be resumed and the current publication record retained.

This is stronger than comparing a display name or version alone. The immutable hash participates in the identity decision.

### 3.4 Catalog reachable, publication changed

If the current catalog identity differs from the cached publication identity—or if no cached publication record exists—the coordinator fetches the manifest identified by the current catalog entry.

The catalog client validates the publication record before constructing a request: the Experience ID must be nonzero, the version nonempty, and the content hash a 64-character hexadecimal SHA-256 value.

The returned artifact is then checked in layers:

1. The response identity must match the requested Experience ID, version, and content hash.
2. The Base64 payload must decode.
3. SHA-256 is recomputed from the downloaded bytes and compared with the requested content hash.
4. The manifest is parsed.
5. The parsed manifest's Experience ID and version must match the publication.

Only after those checks does the catalog client return the manifest to the startup coordinator.

This is an identity and integrity check, not a claim that all referenced MicroBundle artifacts have already been downloaded, loaded, or executed.

### 3.5 Publication no longer listed

If the catalog is reachable but no longer contains the cached Experience ID, AnyApp does not silently treat the cached publication as the current publication.

When a cached publication record exists, the coordinator selects the Forge path. When there is no cached publication record, the current implementation returns the cached manifest with `PersistLastManifest: true`.

That distinction is subtle and should be preserved when documenting or changing the implementation. A missing catalog entry is not the same as a catalog outage: the former is an observed absence in a successful catalog response; the latter is lack of knowledge.

## 4. Cache generations and atomic promotion

A cache is a small persistence system. A manifest may depend on configuration sidecars and a publication record. Updating these files independently in place creates mixed states: a new manifest paired with old configuration, or a publication record describing bytes that were never fully written.

AnyApp's generation layout groups related files:

```text
cache/
  generations/
    {generation-id}/
      manifest.json
      configuration/
        {BundleId}.bin
      publication.json
  active-generation.txt
```

A save writes the candidate generation first and promotes the active-generation pointer only after the generation has been written. The pointer is the commit point.

This gives the cache a simple transactional shape:

1. Construct a new generation without changing the currently active pointer.
2. Write the manifest and its associated configuration/publication data into that generation.
3. Promote the active pointer to the new generation.
4. Retain older generations rather than overwriting them during the save.

If a write fails before pointer promotion, the previously active generation remains selected. The candidate may be incomplete, but it has not become the active generation.

The publication record includes a SHA-256 digest of the manifest bytes and is checked against the manifest's Experience identity and version before it is trusted. This binds the local publication metadata to the exact cached manifest bytes.

Legacy flat cache files remain supported for compatibility; new writes use generation-based storage. Compatibility is a migration concern, not a reason to weaken validation of new generation records.

## 5. Failure semantics

The following table describes the distinctions the host must preserve. It combines the source-backed coordinator behavior with the operational implications of the surrounding cache and artifact boundaries.

| Condition | Safe interpretation | Host response |
|---|---|---|
| No cached manifest | No prior composition request is available | Browse/select an Experience |
| Cache load failure | Local state could not be trusted or read | Enter Forge/recovery path; preserve evidence |
| Catalog network failure | Current publication state is unknown | Resume cached manifest without rewriting publication identity |
| HTTP timeout | Catalog access did not complete | Same offline decision, unless caller cancellation was requested |
| Caller cancellation | The caller withdrew the operation | Propagate cancellation |
| Same immutable publication | Local publication identity agrees with catalog | Resume cached manifest |
| Different publication identity | Publisher advertises a different immutable revision | Fetch and validate the exact current manifest |
| Publication absent from successful catalog response | Catalog was reached and did not list the Experience | Follow the coordinator's missing-publication policy; do not call this an outage |
| Invalid publication metadata or artifact bytes | The response cannot establish the requested identity/integrity | Do not accept the refreshed manifest; enter recovery path |
| Required MicroBundle artifact unavailable later | Manifest recovery succeeded, but composition dependencies are incomplete | Composition may still fail; offline resume is not a guarantee of execution |

A general rule follows: **classify failures by what is known, not by what would be convenient.** A timeout, an explicit cancellation, a 404-like missing publication, a corrupt hash, and a local disk failure are not interchangeable exceptions.

## 6. Worked trace: a desktop resumes an Experience

Imagine that a person last used Experience 42, version `3.1`, with a recorded SHA-256 content hash. The local cache contains a manifest, its publication record, and configuration sidecars.

**Case A — the network is down.** The host reads the cache successfully, but catalog listing fails with a network exception. It resumes the cached manifest and does not rewrite the publication record. The host has continuity, but it does not claim that version 3.1 is still the publisher's current version.

**Case B — the catalog is reachable and unchanged.** The catalog advertises the same Experience ID, version, and hash. The host resumes the cached manifest without downloading the manifest again.

**Case C — the catalog advertises a new hash.** The host requests the exact publication and validates response identity, decoded bytes, recomputed digest, and parsed manifest identity. A mismatch is rejected rather than treated as a successful update.

**Case D — the Experience is no longer listed.** The host does not silently relabel the cached version as current. It follows the missing-publication path so the person can inspect available Experiences.

The cases show why “resume” and “current” are separate claims. The first is a statement about local recoverability; the second requires a successful reconciliation with the publisher.

## 7. Design invariants

A reliable startup/recovery design should preserve these invariants:

1. **No authority by outage:** inability to reach the catalog cannot establish that a publication changed, was withdrawn, or remains current.
2. **No silent substitution:** recovery does not select an unrelated Experience to hide a failure.
3. **Immutable identity:** Experience ID, version, and content hash jointly identify the publication.
4. **Verify before trust:** downloaded bytes and parsed manifest identity must agree with the requested publication.
5. **Cancellation remains cancellation:** caller cancellation is not converted into an offline success.
6. **Pointer-last persistence:** the active-generation pointer changes only after the candidate generation is written.
7. **Preserve prior recovery state:** a failed candidate write must not displace the prior active generation.
8. **Separate manifest from artifact closure:** recovering a manifest does not prove that every referenced MicroBundle artifact is locally available.
9. **Separate recovery from execution:** resuming a composition request does not prove that FSM_COS composition, FSM_Execution handoff, or Experience interaction will succeed.

## 8. Evidence and limits

The implementation mapping is intentionally explicit:

- `ExperienceStartupCoordinator.ReconcileAsync` implements the host-neutral reconciliation policy through injected load/list/fetch functions.
- `PublishedExperience` carries Experience ID, version, and content hash.
- `ExperienceCatalogClient.ListAsync` validates catalog entries and rejects duplicate immutable publication identities.
- `ExperienceCatalogClient.GetManifestAsync` validates the requested/returned identity, Base64 payload, SHA-256 digest, and parsed manifest identity.
- The startup/offline recovery guide documents the generation directory layout, pointer promotion, legacy flat-cache compatibility, and operational guidance.
- The automated test suite is documented as covering cache-generation promotion failure and catalog-timeout recovery without a live catalog.

This chapter does not claim that offline execution is guaranteed, that every cache corruption is automatically repairable, or that the current coordinator distinguishes every HTTP status into a bespoke user-facing message. The source-backed behavior is narrower: a valid cached manifest can be resumed during catalog unavailability, while publication identity is left unchanged; successful catalog observations trigger identity reconciliation.

Before changing this policy, inspect the coordinator, cache implementation, and tests together. A change that appears to simplify startup can accidentally turn uncertainty into false freshness or destroy the last recoverable generation.

## 9. Exercises

1. **Knowledge classification:** For each failure in the table, state what the host knows, what it does not know, and which state transitions are justified.
2. **Hash collision of labels:** Explain why matching Experience ID and version is insufficient when the content hash differs.
3. **Transaction design:** Draw the cache write sequence and identify the exact point after which the new generation becomes active.
4. **Cancellation:** Explain why a timeout may be treated as catalog unavailability while caller-requested cancellation must propagate.
5. **Offline boundary:** Give an example where the manifest resumes successfully but later composition fails because an immutable MicroBundle artifact is absent.
6. **Policy review:** Should a missing publication ever allow the host to resume a cached manifest? State the product policy explicitly and explain how it differs from an outage. Compare your answer with the current coordinator behavior.

## Further reading

- [Startup and offline recovery implementation guide](../EXPERIENCE-STARTUP-AND-OFFLINE-RECOVERY.md)
- [Experience host adapter contract](../EXPERIENCE_HOST_ADAPTER_CONTRACT.md)
- [Chapter 02 — AnyApp as a host](02-anyapp-as-a-host.md)
- [Chapter 01 — The native host boundary](01-native-host-boundary.md)
- [AnyApp startup/cache recovery issue #30](https://github.com/TrentBest/AnyApp/issues/30)
- [FSM_Execution RuntimeAssembly contract issue #23](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Execution/issues/23)

---

<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
