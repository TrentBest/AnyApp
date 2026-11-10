<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# CiAi Evidence Integration

## Purpose

AnyApp is the first practical consumer of CiAi and the first planned self-building CODE RED host. GitHub Actions remains the authoritative build/test executor; CiAi evaluates provider evidence for an exact AnyApp commit. The producer workflow now emits the provenance and TRX artifacts required for structured diagnostics.

This is still an **evidence-production and observation integration**, not a live repair loop. It does not grant an LLM repository-write authority, replace the .NET workflow, merge code, publish NuGet packages, create GitHub Releases, or deploy.

## What runs

On each development push, the [CiAi evidence workflow](../.github/workflows/ciai-evidence.yml):

1. Resolves the exact 40-character AnyApp source commit SHA.
2. Waits for the existing [.NET workflow](../.github/workflows/dotnet.yml) to finish for that commit.
3. Resolves the exact workflow run and attempt when there is one unambiguous completed run.
4. Checks out CiAi at its configured immutable source commit.
5. Runs CiAi's existing ObserveRun sample against TrentBest/AnyApp and the exact commit SHA.
6. Uploads JSON and Markdown evidence reports as a 30-day GitHub Actions artifact.

The observer retains read-only GitHub permissions. It does not dispatch workflows or write repository contents. The .NET workflow remains the executor.

## Evidence produced by the .NET workflow

The build job now produces two run-attempt-scoped artifacts:

| Artifact | Identity | Contents |
|---|---|---|
| Workflow provenance | **ciai-workflow-provenance-{runId}-{runAttempt}** | **ciai.workflow-provenance.v1** JSON with repository, run/attempt, workflow-definition ref/SHA, event ref/SHA, and actual checkout ref/SHA |
| Test diagnostics | **ciai-test-results-{runId}-{runAttempt}-build** | TRX output from the AnyApp test project |

The test artifact is uploaded with **if: always()**, so a failing dotnet test step does not suppress its diagnostics. The test command itself remains authoritative and keeps its nonzero exit code; artifact upload does not convert a failed job into a pass. If the test step never runs or produces no TRX, the artifact is absent and must be treated as incomplete evidence—not success.

The provenance record is generated from GitHub's workflow/event context and the actual checked-out commit. The producer does not assert that the workflow definition is trusted; the receiving host must compare workflow ref/SHA against its protected control-plane policy.

## Evidence lanes

| Lane | Authoritative workflow | Current meaning |
|---|---|---|
| Build and tests | .github/workflows/dotnet.yml | Required Windows source-graph build and AnyApp tests; also attempts a diagnostic Windows executable publish after a test failure |
| Publish-size baseline | .github/workflows/baseline.yml | Separate baseline comparison; master/manual/PR-to-master scoped and may be not triggered for a development commit |
| Windows release candidate | .github/workflows/release-executable.yml | Release-published trigger only; not triggered during ordinary development CI |

A lane that did not run is not reported as a success. The baseline and release workflows remain separate from the development build/test lane.

## Trusted ingestion sequence

The receiving host's provenance-bound TRX contract requires it to:

1. Resolve the exact workflow run and attempt, then collect the complete job inventory.
2. Select the provenance and test artifacts from that exact run/attempt. Match the test artifact's **build** suffix to the observed YAML job key and independently resolve the numeric GitHub job ID and observed job name.
3. Verify each provider-reported SHA-256 over the complete downloaded ZIP before extraction.
4. Safely extract only the expected JSON/TRX payload, reject unexpected or duplicate entries, and compute the extracted content digest and byte length.
5. Validate workflow-definition trust against protected host policy before passing provenance into CiAi.
6. Bind the test report to the exact repository, run, attempt, candidate checkout SHA, workflow path, job key, observed job ID/name, and artifact digests.
7. Preserve the normalized CiAi verdict and diagnostics as untrusted context for the next bounded HeadlessAi turn.

The parser does not download artifacts, authenticate to GitHub, verify provider ZIP digests, or extract archives. Those remain trusted-host responsibilities. Foundry must correlate the report to the same task and enforce lifecycle, budget, and authorization rules. HeadlessAi may propose a repair but cannot authorize or apply repository changes.

## First observed result

The first complete read-only CiAi observation was produced for AnyApp commit **4d40f584fa4011179a731962afdd755fa85e8063**:

- [CiAi observer run](https://github.com/TrentBest/AnyApp/actions/runs/41281306728)
- [Authoritative AnyApp .NET run](https://github.com/TrentBest/AnyApp/actions/runs/41281306660), attempt 1

CiAi reported the required workflow complete and unsuccessful, with **complete: false**. It preserved the exact source SHA, run ID/attempt, job ID, job conclusion, step outcomes, and authoritative run/job links. This was a successful observation of a failed build lane—not a successful AnyApp build.

That run compiled AnyApp but failed two integration tests with the manifest content-hash exception. The new producer is intended to preserve future TRX output for this kind of failure. It has not yet been exercised by a completed run.

## Current limits and next steps

This producer change does not yet connect artifact retrieval to CiAi's parser. In particular:

- CiAi PR #30 and PR #36 remain draft dependencies; this producer PR does not merge or activate them.
- Foundry still needs to verify provider ZIP digests, safely extract artifacts, bind provider artifacts to observed numeric job IDs/job names, and establish workflow-definition trust.
- CiAi's normalized diagnostic report still needs delivery into the same correlated Foundry task.
- HeadlessAi repair turns, CommandAi proposal composition, host-side authorization, isolated candidate application, and exact-SHA revalidation remain unproven.
- The full AnyApp red → bounded repair → green cycle has not yet been demonstrated.

## Producer validation (2026-11-10)

The provenance/TRX producer was exercised by the development push for commit `5b2f167a9ffdfc8a99f2983ca9c678a4de1b3fd0`:

- [Authoritative .NET run](https://github.com/TrentBest/AnyApp/actions/runs/42737219041) — completed successfully; source-graph build and AnyApp tests passed.
- [CiAi exact-run observer](https://github.com/TrentBest/AnyApp/actions/runs/42737219075) — completed successfully for the same exact source commit.
- `ciai-workflow-provenance-42737219041-1` — uploaded, 424 bytes.
- `ciai-test-results-42737219041-1-build` — uploaded, 15,697 bytes.
- `CiAi-AnyApp-evidence-5b2f167a9ffdfc8a99f2983ca9c678a4de1b3fd0` — observer report artifact uploaded.

This verifies that the authoritative producer emits both run-attempt-scoped artifacts and that the separate observer resolves the exact .NET run. It does **not** yet prove that the observer or a trusted host downloads, digest-verifies, extracts, and binds those producer artifacts into the CiAi report.

The next acceptance milestone is trusted host ingestion of the provenance and TRX artifacts, followed by a CiAi **ActionableFailure** or **CompletePass** report bound to that exact run/attempt. No merge, dispatch, release, deployment, branch deletion, or NuGet publication is part of this change.

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="200" /></p>
