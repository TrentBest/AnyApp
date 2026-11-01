<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# CiAi Evidence Integration

## Purpose

AnyApp is the first practical consumer of CiAi. GitHub Actions remains the build/test/publish executor; CiAi observes the provider's authoritative evidence and correlates it to an exact AnyApp commit.

This integration is intentionally **read-only and advisory**. It does not replace the existing .NET workflow, block that workflow, merge code, publish NuGet packages, create GitHub Releases, or deploy.

## What runs

On each push to development, the [CiAi evidence workflow](../.github/workflows/ciai-evidence.yml):

1. Resolves the exact 40-character AnyApp source commit SHA.
2. Waits for the existing [.NET workflow](../.github/workflows/dotnet.yml) to finish for that commit.
3. Resolves the exact workflow run and attempt when there is one unambiguous completed run.
4. Checks out CiAi at the immutable source commit 94e0a13ae171de4fce7eb5989e1dea4fe72cc16c (not a moving branch and not an unpublished NuGet package).
5. Runs CiAi's existing ObserveRun sample against TrentBest/AnyApp and the exact commit SHA.
6. Uploads the JSON and Markdown evidence reports as a 30-day GitHub Actions artifact.

The observer uses the workflow's read-only GITHUB_TOKEN permissions (actions: read, contents: read). It does not receive write permissions or secrets beyond the token required to read this repository's Actions evidence. The .NET workflow also attempts a diagnostic executable publish after a test failure, when the source build and publish restore succeed; this does not turn the failed test lane green.

## Evidence lanes

| Lane | Authoritative workflow | Current meaning |
|---|---|---|
| Build and tests | .github/workflows/dotnet.yml | Required for this first advisory observation; includes AnyApp source-graph build, tests, and Windows executable publish |
| Publish-size baseline | .github/workflows/baseline.yml | Separate baseline comparison; currently master/manual/PR-to-master scoped and may be **not triggered** for a development commit |
| Windows release candidate | .github/workflows/release-executable.yml | Release-published trigger only; it is **not triggered** during ordinary development CI |

A lane that did not run is not reported as a success. The first vertical slice requires the exact .NET run and attempt; the other lanes remain separately identifiable and may be absent/not-triggered. The observer's raw run collection preserves what GitHub actually returned. Expanding the sample into a first-class typed lane declaration is follow-up CiAi work, not something this workflow pretends already exists.

## How to inspect it

Open **Actions → CiAi AnyApp Evidence** and select the run for the commit. Download the artifact named CiAi-AnyApp-evidence-<commit-sha>. It contains:

- ciai-anyapp-evidence.json — machine-readable run, attempt, job, step, and policy evidence.
- ciai-anyapp-evidence.md — human-readable rendering of the same observation.

The report links to authoritative GitHub Actions run/job evidence. Compare its source SHA to the AnyApp commit being investigated. A report for another SHA is not evidence for this build.

## First observed result

The first complete CiAi observation was produced for AnyApp commit `4d40f584fa4011179a731962afdd755fa85e8063`:

- [CiAi observer run](https://github.com/TrentBest/AnyApp/actions/runs/41281306728)
- [Authoritative AnyApp .NET run](https://github.com/TrentBest/AnyApp/actions/runs/41281306660), attempt 1
- Evidence artifact: `CiAi-AnyApp-evidence-4d40f584fa4011179a731962afdd755fa85e8063`

CiAi reported discovery complete, required evidence complete, workflow terminal, and workflow unsuccessful. Its required-workflow state was `failed`, with `complete: false`. It preserved the exact source SHA, run ID/attempt, job ID, job conclusion, step outcomes, and authoritative run/job links. This is a successful observation of a failed build lane—not a successful AnyApp build.

That .NET run compiled AnyApp but failed two integration tests with the manifest content-hash exception. The original run stopped before executable publishing, so it produced no EXE. Subsequent workflow revisions now attempt a diagnostic EXE publish after test failure, while retaining the overall failed CI verdict; that behavior still requires a completed run to verify.

## Trust and failure semantics

- GitHub Actions is authoritative for workflow state and conclusions.
- CiAi observes; it does not execute AnyApp's build.
- Exact run attempts remain distinct; the integration does not blend retries.
- Missing, ambiguous, non-terminal, unsuccessful, or incomplete evidence does not become success.
- The CiAi observer's exit code is recorded but does not change the AnyApp CI verdict.
- The CiAi checkout is pinned to an immutable commit so a later branch movement cannot silently change the consumer's observer.
- The observer is a source-based pilot. AnyApp has no runtime dependency on an unpublished CiAi package.

## Current limits and next steps

This proves the first source-based, read-only consumer path. It does not yet provide a reusable GitHub Actions host adapter, a generalized manifest of consumer lanes, or an automated correlation of every dependency checkout SHA. The next CiAi improvements should be driven by gaps encountered in this real AnyApp run, with any new contracts added to CiAi's reusable domain rather than hard-coded only in AnyApp.

No package publication, GitHub Release, deployment, merge, or branch deletion is part of this integration.

<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="200" /></p>
