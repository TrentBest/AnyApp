<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# 04 — Trust boundaries and safe host orchestration

## Learning objectives

After this chapter, a reader should be able to:

1. Identify the distinct trust boundaries around a native desktop host, browser manifestation, local bridge, repositories, model, MicroBundles, composition engine, and execution engine.
2. Explain why Origin validation, authentication, authorization, capability discovery, compatibility, composition, and execution are different claims.
3. Trace the current AnyApp WebApp bridge protections to source and tests without overstating what they guarantee.
4. Recognize missing controls and distinguish source-verified behavior from recommended hardening.
5. Design a narrow, read-only discovery path that preserves provenance and cannot turn model output or retrieved text into host authority.

## 1. The problem: connection is not trust

A desktop host often needs to coordinate with a browser, retrieve remote material, load modular artifacts, and eventually ask an execution subsystem to perform work. Each connection crosses a boundary. A successful connection proves only that a communication path exists.

A browser being able to reach a loopback port does not make it trusted. A valid browser Origin does not identify a user. A valid launch token does not grant every possible operation. A catalog result does not prove that its artifact is available. A loaded MicroBundle does not prove that it can execute. A model response does not become policy merely because it is fluent.

The design task is therefore not to create one universal notion of trust. It is to preserve several independent decisions and make each owner responsible for the decision it is qualified to make.

## 2. Vocabulary: keep the claims separate

| Term | Question answered | What it does not prove |
|---|---|---|
| Transport binding | Where can a process connect? | Who is connecting or whether it is allowed |
| Origin validation | Which browser origin initiated this request? | User identity or permission |
| Authentication | Does the caller possess the expected credential/session proof? | Permission for every operation |
| Authorization | May this caller perform this operation on this resource in this context? | That a dependency exists or is executable |
| Discovery | What identities or declarations match this query? | Retrieval, compatibility, or activation |
| Retrieval | Can the exact selected source/artifact revision be obtained? | That its contents are safe or executable |
| Integrity verification | Do bytes match the claimed immutable identity/digest? | That the artifact is benign |
| Compatibility | Does the artifact satisfy a declared host/runtime contract? | Permission to activate it |
| Composition | Can FSM_COS construct a RuntimeAssembly from the validated inputs? | That the assembly has an execution contract |
| Execution authorization | May this runtime perform this action now? | That the action will succeed |
| Execution | Did the owning execution subsystem perform the authorized work? | That the result should be trusted without validation |

These are not synonyms and should not be collapsed into a single boolean such as `trusted`.

## 3. The AnyApp trust-boundary map

```text
                 User / Windows account
                          |
                    Native AnyApp host
             host policy, UI, lifecycle, scope
                /          |           \
               /           |            \
        WebApp browser   Catalog/API   Model provider
              |              |             |
        Origin + token   authored data   model output
              |              |             |
        Loopback bridge  immutable refs  untrusted text
              \              |             /
               \             |            /
                 Host-controlled adapters
                          |
                 validated exact inputs
                          |
                       FSM_COS
               resolve / load / arbitrate
                          |
                    RuntimeAssembly
                          |
                    FSM_Execution
                typed execution contract
                 (upstream handoff pending)
```

This is a conceptual map, not a claim that every arrow is already implemented. The current AnyApp bridge is a concrete browser-to-host presentation channel. The LLM-facing discovery adapter remains a separately tracked design/integration task. The typed RuntimeAssembly-to-FSM_Execution handoff remains an upstream contract dependency.

### Responsibility allocation

- **AnyApp:** native process/window lifecycle, host policy, caller/context scope, browser bridge, native presentation, and host-side orchestration.
- **WebApp:** browser manifestation and authoring/presentation surface; it does not acquire desktop runtime authority.
- **Experiences:** authored Experience definitions, manifests, semantic requirements, and source-owned authored metadata.
- **MicroBundleRepository:** immutable artifact identity, delivery, and byte verification.
- **FSM_COS:** dependency resolution, loading, arbitration, and RuntimeAssembly construction.
- **FSM_Execution:** execution lifecycle and scheduling contracts it explicitly supports.
- **HeadlessAi:** provider-neutral model invocation; it does not grant access or authorize actions.
- **Experience/domain MicroBundles:** authoritative domain behavior and state transitions.

A host adapter may coordinate these owners. It must not silently absorb their responsibilities.

## 4. A concrete example: the WebApp bridge

The bridge binds to an HTTP loopback endpoint. Its implementation rejects endpoint configuration unless the address parses as a loopback IP address. The bridge does not treat loopback as a browser trust signal: privileged requests also require an allowed Origin and the per-launch token.

The current source implements these controls:

- Origin values are parsed as absolute HTTP or HTTPS origins; user information, non-root paths, query strings, and fragments are rejected.
- Allowed origins are exact values from `ANYAPP_ALLOWED_ORIGINS`; when unset, the Workshop WebApp origin is the default.
- Loopback browser origins are not implicitly trusted.
- The launch token is compared with `CryptographicOperations.FixedTimeEquals`.
- HTTP routes are restricted to `GET /state` and `POST /command`.
- The command switch accepts only `return-hub`, `split-moniker`, `whole-moniker`, and `flip-moniker`.
- WebSocket messages must be text, fit within 64 KiB, and carry the expected protocol version.
- The bridge does not accept shell commands, assembly paths, arbitrary executable content, or filesystem operations.

Source: [AnyAppWebBridge.cs](../../AnyAppWebBridge.cs). Tests: [AnyAppWebBridgeTests.cs](../../tests/TheSingularityWorkshop.AnyApp.Tests/AnyAppWebBridgeTests.cs). Protocol overview: [WebApp bridge protocol](../WEBAPP_BRIDGE_PROTOCOL.md).

### What the tests establish

The current bridge tests cover loopback endpoint enforcement, exact-origin matching, rejection of unconfigured loopback origins, rejection of malformed origins and invalid allowlist entries, and the configured Workshop default. These are useful regression tests for the Origin policy.

They do not, by themselves, prove every HTTP/WebSocket runtime path, denial behavior under load, credential secrecy in diagnostics, request-body bounds, or end-to-end browser interoperability.

## 5. Source-audit findings: protections and gaps

A textbook should teach the actual implementation, including its unfinished edges. The following findings are from the `development` source and its current bridge tests.

| Area | Source-verified behavior | Limit or follow-up |
|---|---|---|
| Listener binding | Requires a loopback IP endpoint | Binding is not authentication |
| Origin policy | Exact configured origin; malformed/non-origin values rejected | Origin is not user identity or per-resource authorization |
| Launch token | Required on HTTP and WebSocket handshake; fixed-time byte comparison | Token is carried in the URL query; query values can enter diagnostics/history and deserve a safer transport review |
| HTTP commands | Four named commands; unknown commands rejected | Request body is read to completion without an explicit size cap in this handler |
| WebSocket payload | Text only; message accumulation is capped at 64 KiB | No visible per-client rate limit or global client-count bound |
| Request concurrency | Listener accepts requests and starts handlers asynchronously | No explicit handler concurrency gate is visible here |
| Request duration | Shutdown cancellation is passed to async operations | No explicit per-request deadline is visible in this bridge |
| CORS response | Allow-Methods and Allow-Headers are emitted | The current response helper does not emit Access-Control-Allow-Origin; browser cross-origin access therefore needs end-to-end verification and likely a deliberate response-policy change |
| WebSocket protocol | Version checked for received envelopes | Protocol versioning is not a substitute for operation authorization |
| Command authority | Commands map to a small host presentation switch | This is a shell-specific command surface, not a portable Experience action/execution contract |

The missing bounds matter because a loopback listener is still reachable by local processes and, depending on browser behavior and origin policy, can be targeted through browser-mediated requests. Exact Origin checks and tokens reduce exposure but do not eliminate resource-exhaustion risks.

The query-string token deserves special attention. Fixed-time comparison addresses one narrow comparison concern; it does not prevent the credential from appearing in URLs, browser tooling, telemetry, crash reports, or request logs. A future protocol revision should evaluate a header-based credential for HTTP and an appropriate WebSocket handshake mechanism, while preserving the existing explicit Origin policy. Any migration must be coordinated with WebApp and tested as a protocol change.

The CORS observation is an interoperability finding, not a claim that the listener is publicly exposed. The bridge currently emits no `Access-Control-Allow-Origin` response header. If the intended WebApp calls are cross-origin browser fetches, the browser's CORS enforcement will prevent the response from being exposed to script. Confirm the actual browser integration and preflight behavior before changing headers; if CORS is required, return the exact validated Origin rather than a wildcard, and test allowed and rejected origins.

## 6. Discovery and model orchestration: read-only by construction

AnyApp's tracked discovery proposal deliberately separates two query families:

1. **Authored-content discovery:** documentation, theory, design, tutorials, and other authored material, each result pinned to a source path and immutable revision.
2. **Experience/capability discovery:** Experience and MicroBundle identities, authored declarations, manifest/artifact references, maturity, and known availability.

The two families answer different questions. A content result should lead to bounded retrieval of the exact revision that was selected. An Experience result should distinguish an authored definition from its published manifest and immutable artifact references. Neither result should be promoted into an activation instruction.

A safe model-assisted flow is:

1. The host establishes caller identity and the relevant user/session/tenant scope.
2. The host authorizes the specific query and permitted content scope.
3. A bounded query returns stable IDs, short excerpts, source paths, immutable revisions, and provenance.
4. The host retrieves only the selected exact revision, with byte/size/time limits.
5. Retrieved content is presented to the model as untrusted reference material, not executable instructions.
6. The model returns an explanation with source citations and explicit uncertainty.
7. Any proposed action is separately interpreted by host policy and, where applicable, the domain and execution owners.
8. The host requires an explicit authorized action path; model text cannot grant itself permission.

The model should not receive unrestricted repository credentials, arbitrary filesystem access, or a general command tool merely to answer questions about Workshop content. Search, retrieval, artifact verification, composition, activation, and execution remain separate operations.

The current cross-repository acceptance record is [AnyApp #62](https://github.com/TrentBest/AnyApp/issues/62). The authored-content and provider-discovery prerequisite is [Experiences #64](https://github.com/TrentBest/TheSingularityWorkshop.Experiences/issues/64). Treat these as tracked work, not as proof that the complete discovery system already exists.

## 7. Threat model and abuse cases

| Threat or mistake | Boundary that must contain it | Safe response |
|---|---|---|
| Untrusted website attempts to call the local bridge | Origin policy and launch authentication | Reject before processing the request |
| Local process guesses or reuses a token | Launch credential lifecycle and host authorization | Reject invalid credentials; scope and rotate credentials; avoid URL leakage |
| Browser sends an oversized command body | HTTP parser/host resource policy | Reject before unbounded buffering; enforce a byte limit |
| Client floods connections or requests | Listener admission and resource policy | Bound concurrent handlers, clients, and request rate |
| Retrieved document says “ignore previous rules” | Host/model trust boundary | Treat it as quoted data; never as policy |
| Model invents a capability or action | Host authorization and typed domain contract | Reject undeclared or unauthorized operations |
| Catalog advertises an unavailable artifact | Repository retrieval and integrity verification | Report unavailable; do not substitute an unpinned revision |
| Artifact bytes do not match their digest | Repository/materialization boundary | Fail closed and preserve diagnostic provenance |
| MicroBundle is loaded but has no execution adapter | FSM_COS/FSM_Execution boundary | Keep it composable/presentable only; do not infer executability |
| Runtime is composed but not authorized for an action | Host and execution policy | Do not activate or execute it |
| Browser event is acknowledged but not semantically applied | Domain interaction contract | Distinguish transport acknowledgement from domain outcome |

## 8. Failure semantics

A secure host should make rejection and uncertainty observable without converting them into success.

- **Origin rejected:** do not process the request or disclose host state.
- **Token invalid or absent:** reject the privileged request; do not fall back to loopback trust.
- **Malformed/oversized request:** reject with a bounded error path; do not continue parsing partial input as a valid command.
- **Rate/concurrency limit reached:** reject or defer according to a documented policy; do not allow unbounded task creation.
- **Catalog or repository unavailable:** report unavailable; do not silently substitute a different revision.
- **Digest mismatch:** reject the artifact and preserve the expected/observed identity for diagnosis.
- **Untrusted content conflicts with host policy:** preserve it as content, but do not follow it as an instruction.
- **Model output is ambiguous or asks for an unauthorized operation:** ask for clarification or refuse that operation; do not infer consent.
- **Composition succeeds but execution contract is missing:** report a composed assembly, not a running Experience.
- **Transport acknowledgement arrives without a domain outcome:** report receipt only, not successful state transition.

## 9. Design invariants

1. A network address is never an authorization decision.
2. Origin validation, authentication, and authorization remain distinct.
3. Every operation is scoped to a caller, resource, and host context.
4. Read operations have explicit bounds on input, output, bytes, concurrency, and duration.
5. The host owns policy; retrieved content and model output cannot override it.
6. Every retrieved result retains its exact source revision and provenance.
7. No newer revision is silently substituted for a previously selected immutable identity.
8. Discovery does not imply retrieval; retrieval does not imply integrity; integrity does not imply safety; compatibility does not imply authorization.
9. Composition does not imply execution.
10. Transport acknowledgement does not imply domain success.
11. Domain behavior remains in its owning Experience/MicroBundle, not in AnyApp's presentation shell.
12. Missing or ambiguous authority fails closed and remains observable.

## 10. Worked example: “Show me the Experience that teaches orbital mechanics”

A user asks the desktop assistant to find an Experience about orbital mechanics and explain what it teaches.

- AnyApp determines the caller's permitted discovery scope.
- The content query independently searches theory/tutorial material; the Experience query independently searches Experience definitions and declared capabilities.
- Results include stable IDs, owning repository, exact source revision, and concise excerpts.
- AnyApp retrieves the chosen theory page at that exact revision and verifies the retrieval identity.
- The model summarizes the page and cites its source. It may describe declared MicroBundle relationships, but it cannot claim that an artifact is currently retrievable unless repository evidence says so.
- If the user later asks to launch the Experience, that is a new operation. AnyApp checks host policy, retrieves and verifies the immutable artifact closure, and asks FSM_COS to compose it.
- If the resulting RuntimeAssembly has no supported FSM_Execution handoff, AnyApp may present the available semantic surface but must not claim that executable behavior has started.

The same result can therefore be useful for learning even when launch or execution is unavailable. Discovery remains valuable without being granted authority.

## 11. Evidence map and limits

| Claim | Evidence | Status |
|---|---|---|
| Bridge binds only to loopback IP endpoints | `AnyAppWebBridge.ConfigureEndpoint` | Implemented; unit tested |
| Origin allowlist is exact and explicit | `IsAllowedOrigin` and bridge tests | Implemented; unit tested |
| Launch token is fixed-time compared | `IsValidToken` / `FixedEquals` | Implemented in source |
| HTTP commands are a fixed small set | `TryExecuteCommand` | Implemented in source |
| WebSocket messages are limited to 64 KiB | `ReceiveLoopAsync` | Implemented in source |
| HTTP body, concurrency, and request duration are bounded | No corresponding explicit limits found in the inspected handler | Not established |
| Browser CORS interoperability is complete | Response helper lacks `Access-Control-Allow-Origin` | Not established |
| LLM discovery and exact-revision content retrieval are complete | [AnyApp #62](https://github.com/TrentBest/AnyApp/issues/62), [Experiences #64](https://github.com/TrentBest/TheSingularityWorkshop.Experiences/issues/64) | Tracked prerequisite/design |
| RuntimeAssembly has a typed execution handoff | [FSM_Execution #23](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Execution/issues/23) | Upstream contract pending |

This chapter is source-checked against the AnyApp `development` branch at the time it was written. It is not a penetration test, formal security review, or claim that all recommended hardening is implemented. Re-audit source and tests whenever the bridge or discovery contracts change.

## 12. Exercises

1. Explain why a request with an allowed Origin but no valid launch token must still be rejected.
2. Explain why a valid launch token must not authorize arbitrary shell commands.
3. Design a test that proves an HTTP body-size limit is enforced before the entire body is buffered.
4. Design a concurrency test that proves a flood of local requests cannot create an unbounded number of active handlers.
5. Explain why returning `Access-Control-Allow-Origin: *` would not be an acceptable shortcut for this bridge.
6. A retrieved tutorial contains instructions to expose a local credential. Explain how the host and model should treat that text.
7. Explain the difference between a successful artifact digest check and a safe-to-execute artifact.
8. Describe the additional contract required before a composed RuntimeAssembly can be called an executing Experience.

## Further reading

- [WebApp bridge protocol](../WEBAPP_BRIDGE_PROTOCOL.md)
- [Experience host adapter contract](../EXPERIENCE_HOST_ADAPTER_CONTRACT.md)
- [AnyApp #62 — LLM-facing discovery](https://github.com/TrentBest/AnyApp/issues/62)
- [Experiences #64 — authored-content and provider discovery](https://github.com/TrentBest/TheSingularityWorkshop.Experiences/issues/64)
- [FSM_Execution #23 — RuntimeAssembly execution contract](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Execution/issues/23)
- [Trusted Windows distribution](../TRUSTED-DISTRIBUTION.md)

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
