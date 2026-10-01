# metaDev — Runtime Intelligence Boundary

metaDev is the development/performance intelligence layer above FSM_COS.

It is deliberately **not** part of the composition kernel. FSM_COS composes a runtime from a manifest; metaDev observes the development and runtime behavior surrounding that composition and supplies measured configuration data.

## Position in the stack

```text
metaDev
   |
   | publish calibration / runtime observations / configuration evidence
   v
AnyApp / WebForge / other hosts
   |
   | RuntimeManifest + configuration
   v
FSM_COS
   |
   v
RuntimeAssembly
   |
   v
MicroBundles
   |
   v
Singularity DataWarehouse
```

The important boundary is:

- **FSM_COS** answers: "What do we assemble?"
- **The manifest/configuration** answers: "What resource envelope does this assembly request?"
- **The warehouse** answers: "How is that envelope physically laid out?"
- **metaDev** answers: "What did we observe, and what should the next configuration consider?"

## Publish-time calibration

A publish is a measurement opportunity.

The development machine provides a known execution environment. The application is exercised against representative workloads and metaDev records measurements such as:

- memory consumption;
- allocation/high-water behavior;
- population counts;
- cleanup/reclamation behavior;
- relevant execution timings;
- machine characteristics used to normalize those measurements.

The goal is not to copy one developer machine's absolute numbers into every installation. The goal is to derive a deterministic configuration equation from measured behavior and the published workload envelope.

For bounded populations, the configuration may deliberately use a small cardinality ceiling. A population whose practical maximum is below 100 can, for example, be represented with a 0..99 capacity domain rather than carrying an unnecessarily large general-purpose structure.

That is a **measured design constraint**, not a universal rule. metaDev supplies the evidence that justifies the chosen envelope.

## Warehouse slicing

At runtime, the published manifest and configuration determine the requested resource envelope.

That information is available before the warehouse is sliced, so the warehouse allocator can derive the required shelves/pages/pools deterministically rather than discovering the shape through trial allocation.

```text
Published Manifest
       +
Published Configuration
       +
Local Override (optional)
       |
       v
Resource Envelope
       |
       v
Warehouse Layout
       |
       v
Pages / Shelves / Pools
```

The local override is applied **after** canonical configuration. It is installation-specific and must not silently mutate the published manifest.

## Passive runtime observation

metaDev remains a passive observer during normal execution.

A useful first policy is a high-water threshold around 75% of the configured capacity:

```text
usage < 75%       -> observe
usage >= 75%      -> record pressure / recommend expansion
usage > capacity  -> expand locally and record the event
```

The exact threshold belongs in configuration so it can be measured and changed without changing FSM_COS.

When an allocation is actually exceeded, a simple first expansion policy can be:

```text
nextCapacity = currentCapacity + ceil(currentCapacity / 4)
```

The point is not that one-quarter is mathematically optimal. It is that the first implementation has a deterministic, explainable expansion rule while metaDev gathers evidence about whether that rule is appropriate.

## Cleanup is data

Cleanup is also an observation point.

When resources are reclaimed, the runtime can report the observed peak, retained population, reclaimed population, and resulting utilization back to metaDev.

Over time, metaDev can distinguish:

- allocations that were genuinely needed;
- allocations that were consistently oversized;
- temporary spikes;
- sustained pressure;
- workloads that repeatedly cross the configured envelope.

This creates a feedback loop without making the runtime dependent on a learning service.

## Manifest evolution

Canonical configuration belongs to the published/source-of-truth side of the system.

When the aggregated evidence justifies changing a default:

```text
runtime observations
        |
        v
      metaDev
        |
        v
configuration evidence
        |
        v
SST manifest/configuration update
        |
        v
new manifest version
```

Changing the canonical manifest/configuration creates a new manifest version.

Existing users may continue operating against their current manifest and local override. New users, or users whose update policy permits it, resolve the newer manifest version and receive the revised resource envelope.

This gives us a controlled distinction between:

1. **published defaults** — reproducible and versioned;
2. **local overrides** — installation-specific and immediately actionable;
3. **runtime observations** — evidence collected by metaDev;
4. **future configuration** — changed only when accumulated evidence warrants it.

## AnyApp's role

AnyApp is the first concrete host where this can be demonstrated.

The intended Forge path becomes:

```text
AnyApp
  |
  +-- runs The Forge experience
          |
          +-- exposes metaDev
                  |
                  +-- development measurements
                  +-- publish calibration
                  +-- runtime observations
                  +-- configuration evidence
          |
          +-- consumes the resulting manifest/configuration
                  |
                  v
                FSM_COS
                  |
                  v
             RuntimeAssembly
```

This keeps AnyApp useful as the baseline desktop host while allowing The Forge to demonstrate why the surrounding ecosystem exists.

## First implementation boundary

The first implementation should remain intentionally small.

metaDev should initially provide:

- a machine/workload measurement record;
- a capacity observation record;
- a publish-calibration result;
- a local override representation;
- high-water and overflow observations;
- aggregation suitable for producing a proposed configuration value.

It should **not** initially:

- modify FSM_COS;
- embed warehouse allocation policy into FSM_COS;
- mutate the canonical manifest during a running application;
- require a network service for ordinary execution;
- make GUI.WPF, GUI.Blazor, or other platform adapters aware of metaDev internals.

The architecture should allow the same evidence to be consumed by AnyApp, WebForge, Unity, or another host without changing the composition kernel.

## Baseline measurements

AnyApp should establish the first reproducible physical baseline before metaDev begins optimizing it.

At minimum we should record:

- Release build size;
- framework-dependent publish directory size;
- self-contained win-x64 publish size;
- single-file self-contained size;
- trimmed single-file result, if the WPF application supports it safely;
- startup time;
- working-set/high-water memory under the baseline experience;
- representative warehouse population/capacity measurements.

The measurements belong beside the versioned build so later metaDev work has an actual baseline to improve against.
