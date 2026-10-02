# WebApp ↔ AnyApp Bridge Protocol

## Purpose

AnyApp and the Workshop WebApp are two manifestations of the same Experience environment. The bridge lets an explicitly connected browser observe and communicate with the desktop host without moving runtime authority into the browser.

## Boundaries

```text
WebApp
  |
  | bridge messages
  v
AnyApp local bridge
  |
  v
Experience / FSM_COS
```

The WebApp is a manifestation and authoring surface. AnyApp is a host. FSM_COS remains the composition boundary.

## Bootstrap

A browser may request desktop execution with a registered URI scheme:

`anyapp://experience/{experienceId}/{version}/{contentHash}`

The URI identifies an immutable published Experience. It is not an Experience definition and must not contain arbitrary executable code.

After launch, the browser establishes a local WebSocket session with AnyApp.

## Message envelope

Every bridge message is versioned and carries a session identifier:

```json
{
  "protocol": 1,
  "type": "hello",
  "sessionId": "...",
  "sequence": 1,
  "timestampUtc": "...",
  "payload": {}
}
```

Unknown message types are ignored or rejected according to the protocol version policy. Unknown payload fields are forward-compatible.

## Initial messages

### hello

Browser -> AnyApp. Contains browser origin and capability summary.

### welcome

AnyApp -> browser. Contains bridge protocol, host identity/version, session ID, and accepted capabilities.

### experience.request

Browser -> AnyApp. Requests an immutable Experience identity.

### experience.state

AnyApp -> browser. Reports requested/active identity and lifecycle state.

### heartbeat

Both directions. Used to measure connection health and latency.

### event

Either direction. Carries an explicitly defined Experience event. It is not a general JavaScript execution channel.

### error

Either direction. Reports a protocol or Experience boundary error.

## Capability record

Capture only data useful to the shared environment:

- protocol version
- WebApp origin
- browser/platform capability summary
- viewport dimensions
- device-pixel ratio
- visibility/focus state
- WebXR availability
- supported XR session modes
- session ID
- requested/active Experience identity
- bridge connection state
- explicit runtime lifecycle state
- latency/heartbeat measurements
- explicit Experience events

Do not collect credentials, cookies, browsing history, arbitrary DOM contents, or unrelated browser data.

## Security boundary

The bridge binds to loopback only. The server validates the browser `Origin` against an explicit allowlist and requires a per-session launch token before accepting privileged Experience requests.

The bridge never accepts arbitrary assembly, code, filesystem paths, command lines, or shell commands from the browser.

## XR

WebXR is another manifestation boundary. XR capability information may be reported through the same capability handshake, but XR rendering remains browser-side.

The intended future path is:

```text
Experience identity
      |
      +---- WebApp / desktop manifestations
      |
      +---- WebXR manifestation
```

The shared identity/session is the connection; the renderer is not.
