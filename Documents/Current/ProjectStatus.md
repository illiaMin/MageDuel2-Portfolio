# Mage Duel 2 — Project Status

Snapshot date: **2026-10-12**

Milestone: **advanced playable online prototype / vertical slice in development**

Verification baseline: **`main` working tree based on commit `84945e0`**, including the uncommitted development changes present during the audit.

This document separates designed features from verified implementation. “Implemented” does not automatically mean production-ready; “not complete” does not invalidate the demonstrated core game design.

## 1. Executive status

Mage Duel 2 has a working server-authoritative combat core substantially beyond its original prototype documents. The territorial match, final duel, cards, reconnect, block, bots, spectator broadcast and guided tutorial foundation exist in code. The project is suitable for demonstrating the central design and its implementation relationship.

It should not yet be presented as a shipped online game. The remaining risks are concentrated in client acceptance, tutorial verification, audio, production account delivery and deployment rather than in the central territorial rule set.

## 2. Verification snapshot

Latest full audit results:

| Check | Result |
|---|---|
| Server automated tests | **83/83 passed** |
| BotClient build | **passed**, 0 errors, 0 warnings |
| Unity batch compilation | **passed** |
| Unity asset/scene/prefab integrity validator | **passed** |
| Unity PlayMode suite | **66/74 passed** |

Of the eight non-passing Unity PlayMode cases:

- five are reproducible UI/layout acceptance regressions;
- three integration cases are inconclusive in `NullGfx` headless mode because render textures cannot be created, rather than proven authentication/battle defects.

The detailed evidence and exact failing cases are retained in [../Development/ImplementationAudit.md](../Development/ImplementationAudit.md).

## 3. Feature matrix

| Area | Status | Current evidence | Remaining work |
|---|---|---|---|
| Territorial regular match | Implemented and server-tested | Multiple hits per round, moving front, timeout scoring | Live balance and presentation acceptance |
| Full ring-out | Implemented and server-tested | Terminal match result waits for decisive flight | Visual/feel review |
| Referee positioning | Implemented and server-tested | Three windows, spotlights, cards, illegal releases | UX clarity review |
| Final duel | Implemented | Only after level scheduled rounds | End-to-end session review |
| Movement/energy | Implemented | Server simulation and snapshot state | Balance iteration |
| Cast/segmented attack | Implemented | 1–3 projectile charge flow | VFX/HUD polish |
| Directional block | Implemented | Raise, sector, drain, impact and shield break | Balance and animation review |
| Roll profiles | Implemented | Ammo-based distance/i-frames/recovery | Balance iteration |
| Projectile ricochet/cleanup | Implemented | Rope response and 10 m exit margin | Load/performance smoke |
| Reconnect | Implemented, needs graphical E2E | 20 s window, auto-defence, restart countdown | Re-run integration with graphics |
| Random/password matchmaking | Implemented | Server and menu flows | Full two-client acceptance |
| Main-menu match broadcast | Implemented, needs graphical E2E | Read-only spectator snapshots and broadcast HUD | Re-run integration with graphics |
| Bot personalities | Implemented | Adaptive, Ricochet, Roll Punisher | Behaviour-specific tests and tuning |
| Fight the Teacher | Implemented | Normal server match in training flow | Reaction imperfection/tuning |
| Guided lessons | In progress, largely implemented | Movement, Block, Roll, Cast, Attack flow | Completion checklist below |
| Battle HUD | In progress | Dynamic server-driven HUD and cursor | User visual/layout acceptance |
| Athletes screen | In progress | Nine athletes and working Back navigation | Two current layout/test regressions |
| Controls settings | Accepted for current milestone | Rebind/apply/reset/capture isolation | Do not redesign without new requirement |
| Gameplay settings | Implemented | Held-projectile layout selection | Manual acceptance |
| Audio settings UI | Implemented | Persistent percentage/mute controls | Connect an AudioMixer and content |
| Audio content | Not implemented | No production audio assets/mixer | Full audio design and production pass |
| Authentication server | Implemented foundation | Email/password/OTP/reset/session tests | Production secret and email provider |
| Authentication UI | Functional, structurally immature | Runtime-created screen | Convert to authored layout/prefab later |
| External login providers | Not implemented | Interface foundation only | Decide launch providers and implement |
| Profile/notifications/tournaments | Deferred | Menu placeholders/disabled entries | Separate product design and implementation |
| Windows build | Primary target, not final-tested | Windows-specific secure session storage | Distributable build smoke/signing |
| macOS | Not supported at current milestone | Secure storage is Windows-only | Keychain, build and QA, or remove from scope |
| CI/deployment/operations | Not implemented | Local scripts only | Pipeline, secrets, database, monitoring, backups |

## 4. Guided tutorial completion checklist

The tutorial design is current, but the following acceptance work remains before marking it complete:

- automated coverage for every lesson step and every forbidden action;
- teacher reaction delay and rare mistakes;
- confirmation that standard online battle is unchanged by tutorial filtering;
- authoritative pause/resume timer verification;
- exit verification from every guided lesson and Fight the Teacher;
- persistence verification across logout/login;
- final screenshot and hierarchy acceptance in TrainingGym.

## 5. Current Unity UI verification gap

The latest headless PlayMode run contains five reproducible UI/layout acceptance regressions. They concern the Athletes screen and main-menu presentation rather than the territorial server rules. Exact test names and technical evidence are kept in the [development audit](../Development/ImplementationAudit.md) so the public status page remains focused on product readiness.

## 6. Content and presentation status

### Strong current presentation work

- arena stone/water composition;
- waterfall, foam, splash and wet-stone material work;
- fireball core/shell/trail/smoke direction;
- custom UI blur and centre diamond cursor;
- responsive menu navigation language;
- dynamic zone-based battle camera;
- server-driven battle and broadcast HUD foundations.

### Still under art/UX review

- final battle HUD layout;
- athletes screen responsive polish;
- final training gym environment and lighting;
- fireball disappearance and trail balance;
- waterfall variation/performance;
- complete audio identity.

## 7. Account and production readiness

The account server is more mature than the visible product shell, but checked-in defaults intentionally do not send real email and do not contain a production signing secret. Before a public deployment the project needs:

- secret injection and rotation policy;
- a configured email delivery provider;
- production database/migration/backups;
- proxy/TLS and rate-limit validation;
- observability and incident handling;
- privacy/security review;
- external provider implementation only if retained in launch scope.

## 8. Recommended next milestones

### Milestone A — portfolio stabilisation

1. Keep the current documentation and archive structure.
2. Fix the five current UI PlayMode regressions.
3. Capture current screenshots/video from MainMenu, Battle, Training and Athletes.
4. Run the graphical two-client auth/match/reconnect/spectator smoke.
5. Create a clean reviewed commit; push only after confirming no private configuration is included.

### Milestone B — complete the vertical slice

1. Close the guided tutorial acceptance checklist.
2. Complete Battle HUD and TrainingGym visual review.
3. Implement the first coherent audio pass and connect AudioMixer settings.
4. Produce and test a Windows build.

### Milestone C — production foundation

1. Deployment and CI.
2. Production email/secrets/database.
3. Load, latency and reconnect testing.
4. Security review and operational documentation.

## 9. Portfolio claim boundary

Safe and accurate claims:

- designed and implemented a server-authoritative territorial alternative to health-based combat;
- translated spatial rules into referee states, penalties, comeback logic and readable presentation;
- iterated beyond an early first-hit prototype and maintained the old documents as design history;
- connected design rules to configurable balance, automated server tests and client UI;
- built an advanced playable online prototype with bots, spectating and guided-lesson flows.

Claims to avoid until later milestones:

- “release-ready online service”;
- “complete cross-platform support”;
- “finished audio/accessibility/localisation”;
- “production authentication/deployment”;
- “all client tests passing”.

## 10. Authorship and playtest context

Mage Duel 2 is a solo, AI-assisted project developed over approximately three months as of this snapshot. The sole creator owns design direction, specifications, integration, tuning, testing and final approval; a substantial part of code production was performed with AI tools under that direction.

Informal validation currently includes approximately 100–150 matches against each bot profile, around ten tutorial completions and feedback on combat/UI/presentation. One observed first-time player was defeated by the teacher bot in roughly 20 seconds, so newcomer survivability and teacher difficulty remain explicit validation risks.
