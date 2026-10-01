# AnyApp

**AnyApp is the first native host for RuntimeAssemblies produced by FSM_COS.**

It is not an Experience, not a scheduler, and not a replacement for FSM_COS. Its purpose is to provide a native runtime/presentation substrate in which a composed RuntimeAssembly can exist and become observable.

## Position in The Singularity Workshop

The Workshop separates composition from hosting:

```
Human / Forge
      |
      v
Ontology + MicroBundle selection
      |
      v
Runtime Manifest
      |
      v
FSM_COS
      |
      v
RuntimeAssembly
      |
      +-------------------+
      |                   |
      v                   v
   AnyApp             WebForge
 native host        web presentation
```

> **FSM_COS constructs the composition. The host makes the composition real.**

FSM_COS is intentionally host-neutral. It may ultimately operate in a desktop application, a web/server process, a service, a simulation, a development tool, or another execution environment.

AnyApp is simply the first native destination.

## Native runtime

AnyApp provides the facilities required to turn an assembled composition into a native observable runtime:

- process/runtime lifetime
- native windowing
- input
- timing
- resource management
- rendering
- future audio and other presentation facilities

Experience-specific behavior should remain reusable MicroBundle capability rather than becoming hard-coded host behavior.

## Native rendering

Unity is no longer the intended foundation for AnyApp.

The Workshop will establish its own native rendering/runtime substrate incrementally. We should not begin by attempting to build a giant game engine.

The first renderer should answer a smaller question:

> What is the minimum host boundary required for a RuntimeAssembly to become visible?

The progression can be earned:

```
RuntimeAssembly
    -> native host
    -> presentation surface
    -> primitive rendering
    -> spatial composition
    -> GUI
    -> complete Experiences
```

## Server neutrality

FSM_COS must remain independent of any particular server or cloud provider.

A future server host may consume the same composition model while having different responsibilities from AnyApp:

```
                    FSM_COS
                       |
                RuntimeAssembly
                 /            \
                /              \
        AnyApp host        Server host
           |                   |
       native UI          service/API/data
       rendering          networking/etc.
```

The initial server ecosystem should remain broad and provider-neutral. Development should prefer free/open tooling and local execution. Provider-specific deployment belongs at the outer infrastructure boundary.

## WebForge

WebPage already demonstrates Experience composition and web presentation.

Its next architectural direction can add server-side functionality without making WebPage and AnyApp the same host:

```
WebForge presentation
        |
        v
   server host
        |
        v
     FSM_COS
        |
        v
 RuntimeAssembly
```

AnyApp provides the parallel native proof:

```
AnyApp
  |
  v
FSM_COS
  |
  v
RuntimeAssembly
```

## What does NOT belong here

AnyApp should not become the home for:

- a second scheduler
- Experience-specific state machines
- a duplicate MicroBundle composition system
- ontology interpretation
- publication/catalog logic
- cloud-provider assumptions
- WebForge-specific behavior
- one-off visual effects that should be reusable capabilities

The host should consume the architecture rather than silently replacing it.

## Why start nearly empty?

That is an advantage.

We can establish the host boundary before inheriting assumptions from an existing engine or framework.

The first implementation should be small enough that every dependency can be explained.

The goal is not to reproduce an established rendering engine feature-for-feature. The goal is to discover the smallest native substrate required by the Workshop's own execution model.

## Current architectural questions

1. What is the minimum host contract required by RuntimeAssembly?
2. Which facilities belong to the native runtime versus reusable MicroBundles?
3. What is the smallest renderer that proves the boundary?
4. Which capabilities should be shared by native and server hosts?
5. How can WebForge demonstrate server-side functionality locally before paid infrastructure is required?
6. Which future facilities should remain host-specific rather than becoming part of FSM_COS?

## Related theory

- Issue #1 — native RuntimeAssembly host theory
- Issue #2 — host-neutral server ecosystem boundary

## Guiding principle

> **FSM_COS constructs the composition. The host makes the composition real.**
