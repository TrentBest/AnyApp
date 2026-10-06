# Trusted Windows Distribution

AnyApp is intended to be a real desktop host that ordinary users can download and run without being told to disable Windows security.

That means **building AnyApp is not the same thing as making AnyApp trusted**. Public Windows trust has several layers:

1. **Authenticode signing** — the executable/package is cryptographically signed.
2. **Trusted publisher identity** — the signing certificate chains to a certificate authority trusted by Windows.
3. **Consistent signing identity** — releases continue to use the same publisher identity so reputation can accumulate.
4. **Timestamping** — signatures remain verifiable after the signing certificate expires.
5. **Installer/package integrity** — the artifact delivered to users is the artifact that was signed.
6. **SmartScreen reputation** — Windows evaluates publisher and file reputation; a valid signature does not guarantee that a brand-new public binary will have zero warnings on its first downloads.
7. **Smart App Control compatibility** — Windows 11 protected devices require code to be signed by an appropriately trusted provider or otherwise have sufficient Microsoft security reputation.

## The practical options

### Option A — Microsoft Store

For an MSIX distribution through the Microsoft Store, Microsoft signs the package after certification.

This is the lowest-friction path for end users because the Store is already a trusted distribution channel.

It also means the Workshop does not need to purchase and protect a public code-signing certificate merely to get the application installed.

### Option B — Azure Artifact Signing

For direct distribution outside the Store, Microsoft's current recommendation is **Azure Artifact Signing** (formerly Trusted Signing).

This is particularly interesting for the Workshop because the private signing key does not have to live in the repository or on a developer workstation. The signing service can be integrated with CI/CD.

The signing identity must still be validated, and new releases still need to build SmartScreen reputation.

### Option C — Traditional OV code-signing certificate

An Organization Validation certificate from a CA in the Windows trust ecosystem can sign AnyApp directly.

This works, but it creates an ongoing certificate/key-management cost and operational burden.

### Option D — Open-source signing programs

Because AnyApp is open source, programs such as the **SignPath Foundation** may be worth investigating before spending money on a commercial certificate.

Eligibility is external to this repository and must be confirmed before relying on it.

## What AnyApp must not do

We should never solve the trust problem by telling users to:

- disable SmartScreen;
- disable Smart App Control;
- run an unsigned executable with security overrides;
- install a random certificate as a trusted root;
- accept a certificate distributed from the repository;
- or download an executable from an arbitrary browser-local path.

Those approaches defeat the purpose of building a trustworthy desktop host.

## Workshop release pipeline

The intended production path is:

~~~text
Source
  |
  v
Reproducible Windows build
  |
  v
Installer / MSIX / release artifact
  |
  +--> SHA-256 manifest
  |
  v
PUBLIC CODE SIGNING
  |
  +--> Microsoft Store
  |       OR
  +--> Azure Artifact Signing
  |       OR
  +--> trusted OV certificate
  |       OR
  +--> qualifying open-source signing service
  |
  v
Signed release artifact
  |
  v
GitHub release / Workshop download
  |
  v
Windows SmartScreen + Smart App Control
  |
  v
User
~~~

**The signing step must happen after the final artifact is produced and before distribution.** Nothing may modify the signed executable/package afterward.

## Cost is not the architecture

The Workshop should not design itself around the assumption that a small independent developer must buy an expensive certificate before users can have a legitimate desktop application.

The architecture should keep signing provider-neutral:

~~~text
AnyApp
  |
  +-- build
  +-- package
  +-- hash
  +-- sign <--- provider-specific adapter
  +-- verify
  +-- publish
~~~

That lets us start with the least expensive legitimate path available to us and change providers later without redesigning AnyApp.

## Current repository status

The current AnyApp development line is **not yet a publicly trusted release**.

It builds the desktop host and exercises the composition architecture, but there is currently no production signing identity attached to the repository's release process.

Therefore:

- local development may use unsigned binaries;
- CI may build and test unsigned binaries;
- production distribution must not be represented as trusted until the final artifact has a valid public signature;
- no signing secret, private key, PFX, or certificate credential belongs in Git.

## Next engineering step

Before spending money, establish the release artifact first:

1. Produce a deterministic Windows distribution.
2. Package it as the actual artifact users will install.
3. Verify the artifact.
4. Add a signing-provider adapter to CI.
5. Sign only from protected CI credentials.
6. Verify the signature in CI.
7. Publish the signed artifact.
8. Build reputation through consistent releases.

The goal is not to make Windows security disappear.

The goal is to make **The Singularity Workshop a legitimate publisher whose software can pass through Windows security without asking ordinary users to compromise their own machines.**
