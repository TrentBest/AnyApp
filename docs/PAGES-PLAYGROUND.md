<!-- The Singularity Workshop animated moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# AnyApp Pages Playground — learning and experimentation architecture

**Status:** design direction; not an implementation claim.  
**Target:** the AnyApp GitHub Pages experience, developed alongside the native AnyApp host.  
**Primary objective:** teach the Workshop ecosystem by letting visitors observe, change, and experiment with real Experiences.

## 1. The product idea

The Pages site should become an interactive Workshop playground rather than remain primarily a download page.

A visitor should be able to:

1. enter a working Experience;
2. observe its behavior and presentation;
3. change meaningful inputs and configuration;
4. inspect the semantic and runtime data behind what is displayed;
5. reset the Experience and compare outcomes;
6. follow the explanation from the underlying theory to the implementation and owning repository.

The site is both a showcase and a learning environment. A game is not merely an advertisement for the engine: it is a concrete system that visitors can take apart, tune, and understand.

The native AnyApp host and the Pages host should demonstrate shared Workshop composition where their capabilities permit it. The browser must remain a distinct host with its own security and lifecycle constraints; it must not become a second implementation of FSM_COS or silently inherit native-only capabilities.

## 2. Teach the ecosystem through visible behavior

The playground should make the ownership boundaries tangible:

| Layer | Responsibility | What a visitor should be able to discover |
|---|---|---|
| Experience | A composed, usable activity | What the activity does and which parts can be changed |
| MicroBundles | Reusable behavior and capability | Which independent components contribute behavior |
| FSM_COS | Runtime composition | How selected components become a runtime |
| Renderer | Representation and render-command data | What the system has decided should be represented |
| Visualize | Visualization/presentation | How representation becomes visible output |
| Host | Platform lifecycle and interaction | What the browser or desktop host contributes |

Do not collapse these into one generic "engine" explanation. Each demo should identify which layer owns the behavior being explored and link to the corresponding theory and implementation.

## 3. The paired Renderer and Visualize view

A flagship teaching surface should present two synchronized perspectives of the same frame.

### 3.1 Visible perspective — Visualize

Show the actual presentation produced by the Visualize path. This is the user's observable result: geometry, layout, motion, color, and other supported visual output.

### 3.2 Inspectable perspective — Renderer

Show a readable text projection of the Renderer frame/command data associated with that exact presentation. The intent is to expose the structured representation an AI-assisted tool or developer could inspect, not to fabricate a second rendering result.

The two views must share an explicit frame identity or equivalent correlation key. Selecting an entity, command, or region in either view should highlight the corresponding item in the other view when the underlying data supports that mapping.

Potential inspection controls include:

- pause/resume and single-step frame progression;
- select an entity or command;
- filter command categories;
- expand structured command data;
- show transforms, geometry/material identifiers, and entity identity when those fields are actually present;
- compare before/after values when a visitor changes an input;
- reset to a documented baseline.

The inspector must display the actual data available from the Renderer contract. It must not invent fields, imply that a diagnostic text projection is a GPU backend, or claim that a CPU/WPF proof is production GPU rendering.

### 3.3 Why the pairing matters

The user sees the pixels; the inspector exposes the structured decisions that lead to those pixels. Together they make the translation from representation to presentation understandable.

This is also a practical AI transparency surface: a person can compare the visual result with the machine-readable description instead of being asked to trust an opaque visual pipeline.

## 4. Experiences as editable laboratories

Games and other Experiences should be the primary teaching material. Each should expose safe, meaningful controls rather than only a play button.

A demo may expose controls such as speed, gravity, spawn rate, palette, layout, difficulty, or component selection—but only when those parameters are genuinely supported by that Experience. Each control should explain:

- what the parameter represents;
- its units and valid range;
- the default value;
- which behavior or subsystem consumes it;
- what the visitor should expect to change;
- whether the change is temporary or saved.

A good experiment follows a repeatable loop:

`observe → change one input → run → inspect → compare → reset`

Where useful, include guided experiments that teach one concept at a time, followed by an unrestricted sandbox. Do not hide important system behavior behind unexplained presets.

## 5. Sandbox safety and predictable behavior

"Mess with all of it" means meaningful freedom inside a deliberate sandbox—not unrestricted execution of arbitrary visitor code on the hosting origin.

The playground should provide:

- bounded parameter ranges and validated inputs;
- deterministic reset and reproducible starting states where possible;
- resource/time limits for expensive simulations;
- isolation between demos so one experiment cannot corrupt another;
- clear distinction between local transient state and saved/shareable configurations;
- no access to secrets, host credentials, private repository data, or privileged APIs;
- explicit consent before any external persistence or network-backed action;
- graceful recovery when a demo fails or exceeds its budget.

If custom scripts or user-authored MicroBundles are introduced later, they require a separate threat model and isolation boundary. A browser sandbox must not be treated as a security boundary merely because the UI is hosted on Pages.

## 6. A consistent page for every demo

Each Experience page should provide the same basic learning structure:

1. **Try it** — the working Experience, with accessible controls.
2. **See it** — the visible Visualize result.
3. **Inspect it** — Renderer or domain data relevant to the demo.
4. **Understand it** — theory first, then the implementation and ownership boundaries.
5. **Experiment** — guided challenges and a resettable sandbox.
6. **Go deeper** — source, tests, contracts, and related repositories.

Keep evidence labels honest: implemented, verified, source-checked, experimental, or planned. A screenshot, mocked visualization, or design sketch must never be presented as a running Experience.

## 7. Incremental implementation plan

The playground should grow from working vertical slices, not a large speculative front-end rewrite.

### Phase 1 — Establish the Pages information architecture

- Replace the download-only framing with a Workshop playground entry point.
- Keep download and release information available, but make Experiences and learning the primary navigation.
- Establish a consistent demo-page structure and a theory/implementation link pattern.
- Identify which current demos can be run in a browser today and which require native AnyApp.

**Exit evidence:** published Pages navigation, a clear capability matrix, and no links that imply unavailable browser functionality.

### Phase 2 — Publish one honest interactive Experience

- Select an existing Experience with a complete browser-compatible execution path.
- Run it through the correct browser host and supported composition contracts.
- Expose a small set of real, validated parameters and a deterministic reset.
- Record the exact source/dependency revisions used by the published demo.

**Exit evidence:** a visitor can interact with the Experience, change a supported parameter, reset it, and follow the implementation links.

### Phase 3 — Build the Renderer/Visualize inspector

- Establish the actual Renderer frame/command contract available to the browser host.
- Correlate inspected data with the exact visible frame.
- Add selection/highlighting and readable command inspection.
- Clearly label any diagnostic or CPU/WPF path and distinguish it from GPU execution.

**Exit evidence:** one frame can be traced from Renderer data to its Visualize presentation without invented metadata or mismatched frames.

### Phase 4 — Turn games into learning sandboxes

- Add further games and Experiences as independent entries.
- Expose supported domain parameters and guided experiments.
- Add safe configuration export/import only after validation and versioning are defined.
- Keep the demo host isolated from the internals and lifecycle of neighboring demos.

**Exit evidence:** multiple independently resettable Experiences, each with working controls, explanatory theory, and implementation evidence.

### Phase 5 — Open deeper creation capabilities

- Evaluate user-authored configurations and MicroBundle composition.
- Define capability permissions, resource budgets, compatibility/version rules, and isolation.
- Only then consider custom executable content or user-published extensions.

**Exit evidence:** a reviewed security and compatibility design plus tests proving the isolation and validation boundaries.

## 8. Non-goals and architectural guardrails

- Pages is not the authority for NuGet package publication.
- Pages deployment is not permission to create a GitHub Release or publish desktop binaries.
- A browser demo must not claim native WPF, HWND, or GPU capabilities it does not execute.
- AnyApp must not absorb FSM_COS composition semantics, Renderer contracts, or Visualize implementation.
- The inspector must consume authoritative data rather than maintain a parallel, hand-authored model.
- Do not make every demo a bespoke application. Reuse the host, navigation, control, inspection, and learning patterns where their contracts fit.
- Do not describe future phases as implemented until code, tests, and published behavior prove them.

## 9. First implementation investigation

Before building the first browser demo, inventory:

- the current Pages deployment workflow and published artifact;
- the browser host's actual composition path;
- which Experience manifests and MicroBundles are browser-compatible;
- the current Renderer frame/command contract and its Visualize adapter;
- which data can be correlated across the rendered frame and inspector;
- existing demos that can be adapted without duplicating domain behavior.

Use that inventory to select the smallest complete vertical slice. Prefer one genuinely interactive, inspectable Experience over several static showcase cards.

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
