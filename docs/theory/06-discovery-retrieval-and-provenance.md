<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# 06 — Discovery, retrieval, and provenance

**Status:** source-grounded architecture chapter; upstream discovery contracts pending  
**Implementation baseline:** development

## Learning objectives

After this chapter, you should be able to:

1. Separate authored-content discovery from Experience/capability discovery.
2. Explain why a search result is not proof of exact-revision retrieval, artifact availability, compatibility, authorization, composition, or execution.
3. Model immutable source revisions and immutable published artifact identities without conflating them.
4. Preserve provenance from query through retrieval, model-assisted reasoning, and presentation.
5. Design bounded, cancellable, read-only discovery interfaces that fail closed.
6. Allocate discovery responsibilities to Experiences, MicroBundleRepository, FSM_COS, and the native host without creating duplicate registries.

## 1. The discovery problem

A native host becomes useful when a person or an assistant can find the right material and then do something meaningful with it. In a growing Workshop, however, "find something" can refer to several fundamentally different operations:

- locating a tutorial, theory chapter, design note, image description, or other authored content;
- locating an Experience definition by topic, identity, or declared capability;
- determining whether a particular immutable artifact revision has been published and can be retrieved;
- asking FSM_COS to compose a selected, compatible set of MicroBundles;
- determining whether a composed runtime has a supported execution contract;
- authorizing a particular host operation for a particular caller and runtime.

A single search endpoint that blurs these questions invites false conclusions. A document title is not a runtime capability. A provider declaration is not proof that the provider's artifact is available. A repository artifact is not permission to load it. A successful composition is not proof of executability.

> **Discovery identifies candidates. Retrieval establishes exact bytes or content. Authority and runtime contracts decide what may happen next.**

This chapter treats discovery as a read-only information problem first. Activation, composition, execution, and publication remain separate operations owned by their respective systems.

## 2. Two query families, not one universal index

AnyApp's LLM-facing discovery work requires two distinct query families. They may be presented through one assistant experience, but their schemas, semantics, evidence, and failure modes must remain distinguishable.

### 2.1 Authored-content discovery

This query family answers questions such as:

- "Find the chapter explaining startup recovery."
- "Show the theory and implementation notes for the bridge."
- "Retrieve the exact design note that describes observer provenance."
- "What changed between these two authored revisions?"

A content result should carry enough information to identify and verify the source, not merely a title and snippet.

| Field | Purpose |
|---|---|
| Stable content ID | Identifies the logical authored item across revisions |
| Title and bounded excerpt | Helps a reader judge relevance without retrieving everything |
| Content kind | Distinguishes theory, implementation note, tutorial, story, asset metadata, and other authored forms |
| Owner/source | Identifies the repository or authoritative content owner |
| Source path | Locates the item within its source |
| Immutable source revision | Pins the exact version being described |
| Digest | Supports integrity checking of retrieved content |
| Retrieval handle | Identifies how to request that exact revision |
| Provenance | Records origin, revision, and relevant derivation/supersession information |
| Supersession relationship | Indicates whether a newer revision replaces or qualifies the result |

A content query must not silently substitute "latest" after a caller has selected a particular revision. If the requested revision cannot be retrieved, the result is unavailable; a newer revision may be offered separately, with its own identity.

### 2.2 Experience and capability discovery

This query family answers different questions:

- "Find Experiences about orbital mechanics."
- "Which authored Experience declares a rendering capability?"
- "What MicroBundles are required or optional by this Experience definition?"
- "Which published immutable artifact corresponds to this manifest revision?"

A capability result must distinguish the authored definition from its publication and from the repository's current knowledge of the artifact.

| Field | Purpose |
|---|---|
| Experience identity | Stable identity of the authored Experience |
| Definition revision | Exact authored definition being described |
| Topic/semantic metadata | Supports bounded candidate matching |
| Required/optional relationships | Describes authored composition intent |
| Provider declaration | Records a provider only when the source explicitly declares one |
| Manifest identity/revision | Identifies the published composition manifest |
| Artifact identity | Identifies an immutable published artifact revision |
| Maturity/readiness | Reports the source's explicit status, not an inferred guarantee |
| Availability | Reports what the relevant catalog/repository has actually confirmed |
| Verification provenance | Records the evidence and time/source for artifact verification |

Do not synthesize a provider declaration from a MicroBundle name, folder name, README description, or display label. Do not turn a semantic match into an executable provider. An absent declaration means "not declared by this source," not "probably available under a guessed name."

### 2.3 Why the distinction matters

A tutorial can describe an Experience that has never been published. A published Experience can reference an artifact that is temporarily unavailable. A repository can contain verified bytes that are incompatible with a particular host. A composed assembly can contain loaded MicroBundles without a supported execution participant.

These are not edge cases to hide behind one boolean such as `available`, `ready`, or `compatible`. They are different facts, owned and established by different systems.

## 3. The evidence ladder

The following stages form an evidence ladder. A system must not claim a later stage merely because an earlier one succeeded.

| Stage | What is established | What is not established |
|---|---|---|
| Candidate discovery | A source reports a relevant match | Exact content retrieval or artifact existence |
| Source pinning | A stable identity and immutable revision are selected | That the revision can currently be fetched |
| Exact retrieval | The requested revision's content/bytes were returned | Integrity, compatibility, or permission unless separately checked |
| Integrity verification | Retrieved bytes match the expected digest/identity | Trustworthiness, safety, or authorization |
| Compatibility assessment | Declared requirements match a named host/runtime contract | Caller permission or successful composition |
| Authorization | This caller may request this scoped operation | That the operation will succeed |
| Composition | FSM_COS successfully constructs a RuntimeAssembly | That the assembly has an execution contract |
| Execution | FSM_Execution accepts and runs a supported typed participant | That every possible side effect is authorized |
| Presentation | The host displays an appropriate result/state | That displayed content is itself authoritative |

This model prevents a common category error: treating a positive result from one subsystem as a certificate for the whole pipeline.

The stages are related, but they are not a single monotonic "trust score." For example, a caller can be authorized to read a document but not to activate an Experience. A valid immutable artifact can be incompatible with the current host. A composition can succeed while execution remains unsupported.

## 4. Identity: authored revisions are not artifact revisions

The system needs multiple identities because it stores and publishes multiple kinds of things.

### 4.1 Authored source identity

An authored item has a logical identity and a revision. The revision pins the exact source state used for a query or retrieval. A Git commit, content revision, or other immutable source identifier can serve as the revision, provided the source owner defines its semantics.

Conceptually:

`ContentRef = (ContentId, SourceRevision)`

The digest is useful evidence about the returned bytes, but it should not replace the source owner's identity model. A digest alone cannot explain which logical item the bytes represent or why that revision supersedes another.

### 4.2 Experience publication identity

An Experience definition and its published manifest/artifact closure are related but not interchangeable.

The host's current publication path uses an Experience identity, version, and content hash to pin the manifest publication. Individual MicroBundle artifacts also have immutable identities and digests. A publication can therefore refer to a set of immutable artifacts, not just one source file.

Conceptually:

`ExperiencePublication = (ExperienceId, Version, ContentHash)`

and each artifact reference retains its own source-defined immutable identity.

The authored definition revision answers "what did the author define?" The publication identity answers "which published composition did the catalog identify?" The artifact identity answers "which exact bytes did the repository deliver?" Keep all three when they are available.

### 4.3 Never repair identity by guessing

If a result lacks a source revision, artifact digest, or stable identity required by the next operation, the caller must not invent one from a title, branch name, timestamp, or provider label. Return an incomplete/unavailable result and ask the authoritative owner to supply the missing contract.

## 5. Retrieval is a separate operation

Search results should be compact and bounded. Exact retrieval should be an explicit operation against a selected immutable reference.

A safe content retrieval sequence is:

1. Query the authoritative source with a bounded query and result limit.
2. Return candidate metadata and excerpts with source provenance.
3. Let the caller select a stable content ID and exact revision.
4. Authorize the requested read within the caller's scope.
5. Retrieve that exact revision through the source-owned retrieval contract.
6. Enforce byte/character limits and cancellation.
7. Verify the returned identity and digest against the selected reference.
8. Preserve the retrieval receipt/provenance alongside any summary or model response.

A safe artifact retrieval sequence is related but distinct:

1. Select a published Experience/manifest identity.
2. Resolve its immutable artifact references using the owning publication contract.
3. Request exact artifacts from MicroBundleRepository.
4. Verify artifact identity and digest.
5. Materialize the verified artifact closure required by the composition request.
6. Pass the composition request to FSM_COS.
7. Keep compatibility, authorization, composition, and execution outcomes separate.

AnyApp already has a source-backed exact-revision publication retrieval path through `ExperienceCatalogClient`: it validates publication identity, version/hash, returned identity, encoded bytes, recomputed SHA-256, and parsed manifest identity. This is important, but it is not a general authored-content query/retrieval API and it does not replace Experiences-owned semantic discovery.

## 6. Provenance must survive every transformation

An LLM-facing system often transforms information several times: search result, excerpt, retrieved document, extracted facts, generated summary, and user-facing answer. If provenance is discarded at the first transformation, later consumers cannot tell which source revision supports a statement.

A useful provenance envelope should preserve, as applicable:

- source owner and stable source ID;
- exact source revision;
- retrieval handle or artifact identity;
- content digest and verification result;
- query/selection context;
- retrieval time and source response identity;
- supersession or derivation relationship;
- transformation identity/version for extracted or summarized material;
- references from each derived claim back to the source evidence.

A summary is a derived representation, not a replacement for its sources. A model may rank, summarize, compare, or explain retrieved material, but it must not silently rewrite source identity or assert that an unverified source is authoritative.

When sources conflict, retain both identities and expose the conflict. Do not merge incompatible statements into a synthetic "latest truth" unless the authoritative owner defines a resolution rule.

## 7. Boundedness, cancellation, and fail-closed behavior

Discovery is an externally influenced input surface even when it is read-only. Queries can be malformed, enormous, adversarial, or intentionally expensive. Retrieval can return oversized content or stall on remote I/O.

A production contract should define limits for:

- query length and accepted query structure;
- page size, maximum pages, and total result count;
- excerpt size and total response size;
- retrieval byte/character limits;
- request deadlines and cancellation propagation;
- concurrent requests and per-caller admission;
- source scope and permitted repositories/content classes;
- cache lifetime and invalidation semantics;
- error categories that distinguish no match, stale revision, unavailable source, denied access, cancellation, malformed input, and integrity failure.

The host should apply caller authorization and scope before disclosing content. A timeout is not proof that a result does not exist. A catalog outage is not proof that a publication has been deleted. A missing artifact is not proof that the authored Experience is invalid. Preserve these distinctions in result types and user-visible explanations.

Fail closed means refusing to advance when required evidence is missing or invalid. It does not mean deleting cached content, erasing provenance, or converting every failure into "not found."

## 8. Ownership: each repository supplies its own truth

A discovery pipeline must not become an excuse for AnyApp to maintain a second copy of every neighboring repository's domain model.

| Owner | Authoritative responsibility | AnyApp's role |
|---|---|---|
| Experiences | Authored Experience definitions, manifests, semantic requirements, explicitly authored provider declarations, and source-owned discovery/retrieval of its content | Query through the published contract and present results with provenance |
| MicroBundleRepository | Immutable artifact identity, delivery, and byte verification | Request exact artifacts and preserve verification evidence |
| FSM_COS | Dependency resolution, MicroBundle loading/arbitration, RuntimeAssembly construction | Submit a valid composition request and consume the typed result |
| FSM_Execution | Supported typed participants, scheduling, cancellation, outcomes, and cleanup | Integrate only through its published execution contract |
| AnyApp | Native process/window lifecycle, caller/context authorization, host policy, LLM-facing orchestration, and presentation | Enforce host scope; coordinate read-only operations; never impersonate source owners |
| HeadlessAi | Provider-neutral model invocation | Optional reasoning over explicitly supplied, scoped evidence; no authority grant |

The source-owned Experiences discovery prerequisite is tracked in [Experiences #64](https://github.com/TrentBest/TheSingularityWorkshop.Experiences/issues/64). The host-facing orchestration and two-query-family requirement are tracked in [AnyApp #62](https://github.com/TrentBest/AnyApp/issues/62).

The current source audit found useful building blocks, but not the missing general contract:
- `ExperienceManifest` and `SemanticCapabilityProvider` describe authored structures and declarations.
- `ArtifactIdentity` and `ArtifactDescriptor` identify immutable artifacts.
- `ExperienceManifestResolver` participates in manifest/dependency resolution.
- `SemanticRequirementResolver.ResolveProviders(...)` matches against a caller-supplied collection; it does not enumerate a source-owned provider catalog.
- `ExperienceRoomCatalog` is a typed renderer-independent room/exhibit catalog, not a general authored-content index.
- `ExperienceCatalogClient` retrieves published Experience manifests/artifacts by exact publication identity; it is not general content discovery.

The correct next move is to implement the missing source-owned contracts in Experiences, then have AnyApp consume them. Do not add an AnyApp-owned authored-content crawler, semantic-provider registry, or duplicate Experience catalog to make the demo appear complete.

## 9. LLM orchestration is not an authority boundary

A model can make discovery more useful by translating a natural-language question into bounded queries, ranking candidate results, asking for exact retrieval, and summarizing retrieved evidence.

The model must not be allowed to:
- broaden the caller's repository or content scope;
- access arbitrary filesystem paths or unrestricted repository credentials;
- invent content IDs, revisions, provider declarations, or artifact identities;
- turn a search match into an activation or composition request without explicit host policy;
- execute retrieved code, commands, scripts, or instructions;
- publish, modify, or delete source content;
- treat retrieved text as trusted instructions for the host or model.

Treat all retrieved text, manifests, metadata, and model output as data. Apply host authorization outside the model. If an assistant proposes a follow-on operation, the host must validate it against the actual contract and caller permissions before any operation is attempted.

A read-only assistant can be valuable without becoming an autonomous runtime controller. Keep query planning, evidence retrieval, answer generation, and action authorization as separate steps.

## 10. Worked example: finding and opening an Experience

Suppose a user asks: "Find the Workshop Experience that teaches orbital mechanics and show me its introduction."

1. AnyApp authenticates the caller and determines the allowed discovery scope.
2. The assistant issues an Experience/capability query, not a generic content query disguised as an Experience search.
3. Experiences returns candidate authored definitions with stable IDs, revisions, semantic metadata, and explicit provider declarations where they exist.
4. AnyApp displays candidates and distinguishes authored definitions from published revisions.
5. The user selects one exact definition/publication identity.
6. If the introduction is authored content, the assistant uses the content query/retrieval family to find and retrieve the exact introduction revision. If it is contained in the published manifest/artifact, the relevant publication/artifact path is used instead.
7. The retrieved content is checked against its selected identity and digest, and provenance is retained.
8. The assistant summarizes the introduction and cites the exact source revision.
9. If the user separately requests opening the Experience, AnyApp performs the distinct authorization and compatibility checks, retrieves the exact artifact closure, and asks FSM_COS to compose it.
10. A successful RuntimeAssembly is presented only through supported host surfaces. Execution is not claimed unless the FSM_Execution contract accepts that assembly and defines the relevant participant lifecycle.

At no point does the model infer that the Experience is runnable merely because it found an introduction or a provider declaration.

## 11. Failure semantics

| Failure | Correct interpretation | Unsafe interpretation |
|---|---|---|
| No query matches | No candidate was returned within this query/scope | The content or Experience does not exist anywhere |
| Source unavailable | The source could not answer now | The selected content was deleted |
| Exact revision missing | This revision could not be retrieved | Silently substitute current/latest |
| Digest mismatch | Retrieved bytes do not match the selected identity | Continue with the bytes anyway |
| Provider not declared | The source did not declare a provider | Guess one from a bundle name |
| Artifact unavailable | Repository could not supply the selected artifact | Rewrite the authored definition |
| Caller denied | This caller cannot perform this scoped read/action | Ask the model to bypass the host |
| Composition rejected | FSM_COS did not construct the requested assembly | Build a host-side dependency resolver |
| No execution contract | The assembly has no supported execution handoff | Treat loaded MicroBundles as runnable |

## 12. Design invariants

1. **Two query families remain distinct.** Authored content and Experience/capability discovery have different identities and evidence.
2. **Every selected result is revision-pinned.** Exact retrieval never silently moves to another revision.
3. **Identity is source-owned.** AnyApp does not invent IDs, revisions, provider declarations, or artifact references.
4. **Discovery is not retrieval.** A candidate match does not prove exact content or artifact availability.
5. **Integrity is not trust.** A matching digest proves byte consistency with an expected digest, not that the content is safe or authorized.
6. **Authorization is external to the model.** LLM output cannot expand scope or grant permission.
7. **Provenance survives transformation.** Summaries and derived claims retain references to their exact evidence.
8. **Operations are bounded and cancellable.** Query, retrieval, concurrency, and response sizes have explicit limits.
9. **Failures preserve meaning.** Timeout, absence, denial, staleness, and integrity failure remain distinguishable.
10. **Owners own their contracts.** AnyApp orchestrates; Experiences discovers authored material; MicroBundleRepository delivers artifacts; FSM_COS composes; FSM_Execution executes supported participants.

## 13. Evidence and limits

This chapter is grounded in the AnyApp `development` source and current cross-repository contract work:

- [AnyApp #62 — LLM-facing discovery](https://github.com/TrentBest/AnyApp/issues/62)
- [Experiences #64 — authored-content and provider discovery](https://github.com/TrentBest/TheSingularityWorkshop.Experiences/issues/64)
- [Experience host adapter contract](../EXPERIENCE_HOST_ADAPTER_CONTRACT.md)
- [Startup, recovery, and continuity](03-startup-recovery-and-continuity.md)
- [Trust boundaries and safe host orchestration](04-trust-boundaries-and-safe-host-orchestration.md)
- [AnyApp publication/catalog client](../../ExperienceCatalogClient.cs)
- [AnyApp runtime composition](../../AnyAppRuntime.cs)

The source-backed baseline includes exact published Experience manifest/artifact retrieval and FSM_COS composition. The general source-owned authored-content discovery/retrieval contract and provider enumeration remain upstream work. This chapter specifies the architecture and acceptance reasoning; it does not claim those missing APIs are implemented.

The discussion is a source/contract review, not a security audit, proof of repository-wide indexing completeness, or claim that all artifact availability and compatibility states are currently observable.

## 14. Exercises

1. A search result names a tutorial and an Experience that references it. Explain why the two results need separate identities and retrieval paths.
2. Design a result type that distinguishes "no match," "source unavailable," "revision stale," and "caller denied" without collapsing them into a nullable result.
3. A model receives an excerpt from an old revision and a newer revision is published during the conversation. Define how the answer should identify the source it actually used.
4. Explain why a provider declaration is not proof of a retrievable artifact or executable participant.
5. Design a test proving exact-revision retrieval never substitutes the latest revision after a 404 or stale-revision response.
6. Define limits for query size, result count, excerpt length, retrieval size, deadlines, and concurrent requests for a local desktop host.
7. A verified artifact is incompatible with the current host. Explain why integrity verification must remain separate from compatibility and authorization.
8. Draw the ownership path from authored Experience definition to repository artifact delivery, FSM_COS composition, FSM_Execution, and AnyApp presentation.

## Further reading

- [Theory chapter 02 — AnyApp as a host](02-anyapp-as-a-host.md)
- [Theory chapter 03 — Startup, recovery, and continuity](03-startup-recovery-and-continuity.md)
- [Theory chapter 04 — Trust boundaries and safe host orchestration](04-trust-boundaries-and-safe-host-orchestration.md)
- [Experience host adapter contract](../EXPERIENCE_HOST_ADAPTER_CONTRACT.md)
- [AnyApp #62 — LLM-facing discovery](https://github.com/TrentBest/AnyApp/issues/62)
- [Experiences #64 — authored-content and provider discovery](https://github.com/TrentBest/TheSingularityWorkshop.Experiences/issues/64)

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
