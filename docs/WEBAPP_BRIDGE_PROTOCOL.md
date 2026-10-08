# WebApp ↔ AnyApp Bridge Protocol

## Purpose

AnyApp and the Workshop WebApp are two manifestations of the same Experience environment. The bridge lets an explicitly connected browser observe and communicate with the desktop host without moving runtime authority into the browser.

## Boundaries

```
WebApp
  |
  | authenticated bridge messages
  v
AnyApp local bridge
  |
  v
Experience / FSM_COS
```

The WebApp is a manifestation and authoring surface. AnyApp is a host. FSM_COS remains the composition boundary.

## Immutable launch identity

A desktop launch is identified by:

`anyapp://experience/{experienceId}/{version}/{contentHash}?token={launchToken}`

The URI identifies an immutable published Experience. It is not an Experience definition and does not contain executable code.

AnyApp must retrieve the published manifest from the configured repository/catalog and verify that its Experience identity and version match the requested identity before composition. The content hash remains part of the requested immutable identity.

## Bridge authentication

The bridge binds to loopback only.

Privileged HTTP and WebSocket requests require:

1. a valid browser `Origin`;
2. the per-launch token;
3. the expected bridge protocol version for WebSocket messages.

The token is compared with a fixed-time comparison. The token is never stored in the Experience manifest.

Allowed non-loopback browser origins are supplied through `ANYAPP_ALLOWED_ORIGINS`. Without that setting, the Workshop WebApp origin is the only configured non-loopback origin.

## HTTP compatibility surface

Existing browser integrations may use:

- `GET /state?token=...`
- `POST /command?token=...`

Supported commands remain:

- `return-hub`
- `split-moniker`
- `whole-moniker`
- `flip-moniker`

The bridge rejects unknown commands and never accepts arbitrary executable content, assembly paths, shell commands, or filesystem operations.

## WebSocket surface

WebSocket connections use the same endpoint and token:

`ws://127.0.0.1:{port}/?token={launchToken}`

Every message carries protocol versioning and an envelope type. Initial protocol messages include:

- `hello`
- `welcome`
- `heartbeat`
- `heartbeat.ack`
- `command`
- `command.ack`
- `event`
- `event.ack`
- `error`

Experience semantics remain above the transport layer.

## Browser capability boundary

The bridge may carry information needed to coordinate manifestations:

- protocol version
- WebApp origin
- viewport and device capabilities
- WebXR availability
- requested/active Experience identity
- runtime lifecycle state
- bridge connection state
- latency/heartbeat measurements
- explicit Experience events

It must not collect credentials, cookies, browsing history, arbitrary DOM contents, or unrelated browser data.

## Principle

The bridge connects manifestations; it does not become another runtime.

```
Canonical Experience identity
        |
        +---- WebApp manifestation
        |
        +---- AnyApp manifestation
        |
        +---- future WebXR manifestation
```

The renderer remains native to each manifestation and FSM_COS remains the composition boundary.
