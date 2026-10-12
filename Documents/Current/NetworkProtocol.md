# Mage Duel 2 — Network Protocol

Document revision: **1.0**

JSON envelope version: **1**

Binary battle protocol version: **3**

Status: current implementation contract summary

Updated: 2026-10-12

This document supersedes the archived JSON-only specification. Mage Duel 2 now uses versioned JSON envelopes for low-frequency control messages and binary protocol v3 for live battle input/state. The two version numbers describe different wire formats and must not be conflated.

## 1. Transport

- One WebSocket connection per client session.
- UTF-8 text frames carry JSON envelopes.
- Binary frames carry player input or battle snapshots.
- Maximum accepted message size: 16,384 bytes.
- Server simulation: 60 Hz.
- Snapshot publication: 30 Hz.
- Client input heartbeat: 400 ms.
- Stale input timeout: 1,000 ms.

All multi-byte binary values use little-endian order.

## 2. JSON envelope

Client control messages use an envelope conceptually equivalent to:

```json
{
  "protocolVersion": 1,
  "type": "startMatchmaking",
  "requestId": "client-generated-correlation-id",
  "payload": {}
}
```

`protocolVersion` is mandatory and currently must equal `1`. Server messages carry a message type and payload, with correlation data where applicable. Validation failures produce a structured protocol error containing a stable code, readable message and original request ID when supplied.

## 3. Client JSON message families

| Family | Message types |
|---|---|
| Authentication | `registerWithEmail`, `verifyEmailCode`, `resendEmailCode`, `completeProfile`, `loginWithEmail`, `refreshSession`, `logout`, `requestPasswordReset`, `verifyPasswordResetCode`, `resetPassword` |
| Identity | `setPlayerName` (legacy/development compatibility) |
| Matchmaking | `startMatchmaking`, `cancelMatchmaking` |
| Training | `startTraining`, `exitTraining`, `tutorialContinue`, `setTrainingPaused`, `configureTraining` |
| Battle lifecycle | `battleReady`, `reconnectBattle`, `requestBattleResync` |
| Input fallback | `playerInput` JSON form |
| Spectating | `watchRandomBattle`, `stopWatchingBattle` |

Unknown types return `unknownMessageType` without being interpreted as gameplay input.

`tutorialContinue` is retained as an internal/compatibility acknowledgement for tutorial barriers. Current lesson presentation does not require a visible Continue button for every step.

## 4. Important server JSON messages

The current flows include:

- `authenticationSucceeded`, `authenticationFailed`, `emailVerificationRequired`;
- matchmaking started/cancelled and opponent-found notifications;
- `battleStarted`, which supplies battle identity, participant metadata, arena quantisation metadata and `binaryProtocolVersion = 3`;
- `roundEnded`, `duelStarted`, `reconnectBreak`, `battleEnded`;
- tutorial state/event messages;
- spectator attachment/state messages;
- structured `error` messages.

Binary snapshots remain the source for high-frequency state even when a lifecycle event is also sent as JSON.

## 5. Binary header

Every binary packet begins with:

| Offset | Size | Field | Value |
|---:|---:|---|---|
| 0 | 1 | Magic 0 | ASCII `M` |
| 1 | 1 | Magic 1 | ASCII `D` |
| 2 | 1 | Version | `3` |
| 3 | 1 | Type | `1` input, `2` snapshot |

Packets with the wrong magic, version, type or length are rejected.

## 6. Player input packet

The preferred live input is a fixed 11-byte packet:

| Offset | Size | Field |
|---:|---:|---|
| 0 | 4 | Common header (`MD`, v3, type 1) |
| 4 | 4 | Unsigned input sequence |
| 8 | 1 | Button bit field |
| 9 | 2 | Quantised aim angle |

### 6.1 Button bits

| Bit | Value | Meaning |
|---:|---:|---|
| 0 | 1 | Move forward |
| 1 | 2 | Move backward |
| 2 | 4 | Move left |
| 3 | 8 | Move right |
| 4 | 16 | Attack held |
| 5 | 32 | Cast held |
| 6 | 64 | Roll pressed |
| 7 | 128 | Space/Block held |

The roll command is represented alongside its held block input according to the current semantic binding model.

### 6.2 Aim quantisation

The X/Z aim direction is converted to a 0–65535 angle over a complete turn. A zero-length direction encodes as zero. Movement is reconstructed from directional bits rather than sending arbitrary analogue magnitudes.

### 6.3 Sequence handling

Input sequences are monotonically increasing unsigned values. The server ignores stale/out-of-order input and snapshots acknowledge the latest accepted sequence.

## 7. Battle snapshot header

Snapshot type 2 continues after the common header:

| Field | Type | Notes |
|---|---|---|
| Snapshot flags | byte | bit 0 means full snapshot |
| Server tick | uint32 | lower 32 bits of authoritative tick |
| Server time | uint32 | lower 32 bits of server elapsed milliseconds; wraps modulo 2^32 |
| Acknowledged input | uint32 | last accepted sequence for recipient |
| Global mask | byte | changed global groups |
| Global data | variable | fields selected by mask |
| Blue player mask | byte | changed player groups |
| Blue player data | variable | fields selected by mask |
| Red player mask | byte | changed player groups |
| Red player data | variable | fields selected by mask |
| Projectile create count | byte | number of complete projectile records |
| Projectile update count | byte | number of changed projectile records |
| Projectile remove count | byte | number of IDs removed |
| Projectile records | variable | create, update, then removed IDs |

Full snapshots are sent initially, at round changes, after resync requests and periodically at least every five simulation seconds.

## 8. Global groups

| Mask bit | Group | Contents |
|---:|---|---|
| 0 (`1`) | Phase | phase, round, duel flag, duel kind |
| 1 (`2`) | Timer | round time in 10 ms units |
| 2 (`4`) | Score | blue and red round score |
| 3 (`8`) | Zones | blue and red authoritative zone code |
| 4 (`16`) | Reconnect | connection flags and reconnect/restart timers |
| 5 (`32`) | Territory | positioning stage/time, cards, hits, end reason and winner |

### 8.1 Phase codes

| Code | Meaning |
|---:|---|
| 0 | waiting/unknown |
| 1 | waiting before battle |
| 2 | moving to round start |
| 3 | ready/countdown |
| 4 | active regular round or duel (use duel flag) |
| 5 | pending/round ending |
| 6 | round reaction/result |
| 7 | match reaction |
| 8 | finished |
| 9 | reconnect break |
| 10 | territorial positioning |

### 8.2 Zone codes

`0..6` correspond to `blue3`, `blue2`, `blue1`, `neutral`, `red1`, `red2`, `red3`. `255` is unknown.

### 8.3 Positioning stage codes

| Code | Meaning |
|---:|---|
| 0 | none |
| 1 | first instruction |
| 2 | second instruction |
| 3 | final instruction |
| 4 | waiting for hit recovery |

### 8.4 End reason codes

| Code | Meaning |
|---:|---|
| 0 | none |
| 1 | full ring-out |
| 2 | disqualification |
| 3 | both disqualified |
| 4 | final duel |
| 5 | regular score |
| 6 | disconnect |
| 7 | both disconnected |

Winner code: `0` none, `1` blue, `2` red.

## 9. Player groups

| Mask bit | Group | Contents |
|---:|---|---|
| 0 (`1`) | Position | quantised X and Z |
| 1 (`2`) | Facing | body facing and head/aim angle |
| 2 (`4`) | Action | current action-state code |
| 3 (`8`) | Energy | current, current maximum and initial maximum |
| 4 (`16`) | Inventory | held projectile count |
| 5 (`32`) | Progress | cast, attack and roll progress |
| 6 (`64`) | Flags | invulnerability, block, reconnect and auto-defence flags |

### 9.1 Quantisation

- Position precision is 0.001 metres.
- X is an unsigned 16-bit offset from arena minimum X.
- Z is an unsigned 32-bit offset from minimum configured zone Z.
- Facing/head use one byte over a complete turn.
- Energy and projectile speed use 0.01 units.
- Normalised progress uses one byte from 0 to 255.
- Time fields use 10 ms units.

### 9.2 Action codes

| Code | Action |
|---:|---|
| 0 | free |
| 1 | casting |
| 2 | attack charging |
| 3 | attack release |
| 4 | rolling |
| 5 | roll recovery |
| 6 | round loss pending |
| 7 | zone exit pending |
| 8 | block raising |
| 9 | blocking |
| 10 | shield-broken fall |
| 11 | getting up |
| 12 | round winner |
| 13 | round loser |
| 14 | round draw |
| 15 | match winner |
| 16 | match loser |
| 17 | match draw/double result presentation |
| 18 | round get-up |

### 9.3 Player flags

| Bit | Meaning |
|---:|---|
| 0 | invulnerable |
| 1 | block raising |
| 2 | blocking |
| 3 | disconnected during reconnect break |
| 4 | attack charging |
| 5 | automatic defence active |

## 10. Projectile records

A projectile creation record contains:

- compact unsigned 16-bit projectile ID;
- owner code (`0` blue, `1` red);
- quantised X and Z;
- 16-bit direction;
- speed in 0.01 units.

An update omits the owner because it is immutable. A removal contains only the compact ID. A full snapshot clears the recipient's prior projectile map before applying creates.

## 11. Resynchronisation

The client may send `requestBattleResync` after detecting missing/invalid delta state. The server marks recipient snapshot state for a forced full update. Clients must not attempt to repair competitive state by inventing projectiles or results locally.

## 12. Compatibility policy

- JSON control additions should be backwards-compatible when possible.
- Any binary field/layout change requires a new binary protocol version.
- `battleStarted` must advertise the version before the first snapshot.
- Unity and BotClient reject unsupported versions rather than decoding them heuristically.
- Archived protocol v1.0 is retained only as history and must not be used to build a current client.

## 13. Security and trust

- Authentication/session state is validated before protected operations.
- Battle participation is resolved from the authenticated connection, not a client-claimed player ID.
- A supplied battle ID does not grant access to that battle.
- Input is treated as intent; the server validates phase, action restrictions, energy and timing.
- Spectators receive state but cannot become participants through spectator messages.
