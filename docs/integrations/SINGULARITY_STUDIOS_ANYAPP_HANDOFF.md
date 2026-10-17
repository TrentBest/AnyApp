# Singularity Studios — AnyApp Desktop Consumer Brief

**Audience:** AnyApp and Workshop runtime contributors  
**Status:** integration direction, not a claim that a studio simulator is implemented  
**Active branch:** `development`  
**Promotion and publishing:** not authorized by this document

## Purpose

AnyApp is the desktop proving ground for the same reusable production data that WebPage presents in the browser. The two hosts must not invent separate studio formats, gesture semantics, task graphs, or simulation rules.

Singularity Studios is intended to assemble large productions from reusable data: stage definitions, anchors, set pieces, performers, crew, camera rigs, shots, task dependencies, and versioned take inputs. The long-term target includes enormous indoor soundstages and battle sets. The first milestone is deliberately small: one repeatable 30-second take that can be inspected, reset, edited, and replayed.

For the browser workbench and user-facing interaction goals, see the [WebPageLLM integration handoff](https://github.com/TrentBest/WebPage/blob/development/docs/integrations/SINGULARITY_STUDIOS_WEBPAGELLM_HANDOFF.md). For reusable performer, crew, camera, and equipment procedures, see the [Gestures Singularity Studios plan](https://github.com/TrentBest/TheSingularityWorkshop.Gestures/blob/development/docs/SINGULARITY_STUDIOS_GESTURE_PLAN.md).

## Preserve the host boundary

AnyApp already demonstrates a manifest-driven desktop host, FSM_COS composition, a compiled MicroBundle catalog, Renderer representation selection, Visualize integration, and a WPF presentation surface. Build on those boundaries rather than introducing a studio-specific application loop.

- **AnyApp** owns native shell, user input, desktop presentation, and host-specific adapters.
- **FSM_COS** composes the manifest-declared MicroBundles into a RuntimeAssembly.
- **Gestures** owns reusable engine-neutral procedure definitions and reference evaluation.
- **Physics** owns physical contact, constraints, forces, collision, obstruction, breakage, and resulting physical events.
- **Renderer / Visualize** own observer-relative representation and GPU-facing presentation.
- **Singularity Studios production data/provider** owns stage anchors, shot/task composition, and studio-specific bindings.
- **Audio** owns sound rendering and mixing; physical sources must be grounded in physical events.

Do not add a second composition engine, duplicate browser behavior, or make AnyApp the canonical owner of reusable studio domain semantics. If a new shared contract is needed, define its owning package before building parallel WPF and Blazor models.

## First desktop proof

The first desktop view should expose the same versioned fixture and evaluator outputs as WebPage, not a separate demonstration with hand-authored animation.

1. Load a small stage/take fixture through the agreed Experience/provider boundary.
2. Display stage bounds, named marks, a prop table, a prop, a short dolly track, and two camera definitions.
3. Show a compact take timeline and inspector for selected entities, anchors, procedure parameters, task dependencies, and status.
4. Run, pause, reset, and replay only the operations that the reference evaluator or host actually supports.
5. Move one mark and rebind the affected procedure; report unresolved anchors or invalid paths.
6. Emit or consume the same fixture-backed living SVG as WebPage so documentation can compare views of the same data.

Use a clear capability ledger: **implemented**, **reference-evaluated**, **host-observed**, or **planned**. A sample timeline is not proof of physics. A procedure ending is not proof of contact. An SVG is not proof of runtime scale.

## Reusable take model

The names below are conceptual, not public APIs:

- A **StageDefinition** provides bounds, surfaces, zones, and semantic anchors.
- A **RigDefinition** describes equipment degrees of freedom, limits, home/docking anchors, and supported procedures.
- A **ProductionEntity** references a stable provider/type identity for a performer, prop, crew member, camera, or set piece.
- A **ProcedureBinding** references a reusable procedure and binds semantic anchor IDs plus explicit parameters.
- A **ShotDefinition** expresses camera coverage/target intent and references production tasks.
- A **ProductionTask** references prerequisites, requested procedure, start conditions, and required completion/failure observations.
- A **TakeDefinition** pins versions and stable seeds for deterministic replay.
- A **TakeObservation** identifies timestamped reference-evaluator output or authoritative host/Physics observation.

Reuse definitions by identity and binding. Do not clone the same procedure or stage description every time a shot changes. Version the referenced data so a replay can state which definitions it used.

## Determinism and LOD

For identical versioned definitions, bindings, stable identities, and seeds, reference output should replay deterministically. Per-identity variation must be bounded and stable, not re-rolled every frame.

Changing visual fidelity must not reset authoritative task state or the playhead. Close-up actors and camera equipment may need detailed presentation; distant background performers can use cheaper representations, but do not skip required authoritative physical outcomes merely because an entity is visually distant.

Measure before claiming scale: active task/procedure count, CPU time, allocations, state size, render uploads, GPU time, and representation transitions. Huge stage dimensions alone are not evidence that huge crowds or complex collisions are affordable.

## Audio and studio sound

Keep authored dialogue/music/cinematic cues separate from physical sound. Physical footsteps, prop contact, rolling dollies, rig latches, and breaking set pieces must originate from observed physical/material events and flow through the audio path. Do not attach arbitrary SFX lists to Gesture definitions as a substitute for physical sound generation.

## Acceptance gates

1. The desktop host uses the same versioned fixture and shared production semantics as WebPage.
2. Existing manifest → FSM_COS → RuntimeAssembly composition remains the composition path.
3. Reset/replay yields deterministic reference output for unchanged inputs.
4. Moving an anchor requires rebinding, not duplicating the procedure definition.
5. Missing bindings and failed prerequisites are visible.
6. The UI distinguishes intent, reference output, and host-observed physical outcomes.
7. The living SVG is generated from the same fixture/evaluator outputs and can be checked in CI.
8. Existing AnyApp startup and Moniker/Renderer/Visualize behavior remain intact.
9. No package publication, release, or deployment is implied.

## Immediate next steps

1. Inspect AnyApp's current development startup and semantic GUI surface before selecting an extension point.
2. Agree on the shared take-fixture/provider contract with WebPage and the owning domain packages.
3. Add the smallest read-only stage/timeline inspector before adding a full editor.
4. Prove one deterministic reset/replay cycle and generated SVG parity.
5. Add Physics observations only through an explicit tested adapter.
6. Profile the vertical slice before scaling the population or stage complexity.

The desktop and browser are two windows into the same reusable production—not two studios that happen to look alike.
