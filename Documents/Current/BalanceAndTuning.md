# Mage Duel 2 — Balance and Tuning Baseline

Version: **1.0**

Status: current development values, not final launch balance

Updated: 2026-10-12

This document records values currently loaded by the authoritative server. Its purpose is to make design intent reviewable and to prevent obsolete spreadsheets from being mistaken for live balance.

The runtime source of truth is `MageDuel2Server/MageDuel2Server/appsettings.json`. A configuration change must update this document in the same change set.

## 1. Match and territory

| Parameter | Value | Design intent |
|---|---:|---|
| Regular rounds | 3 | Makes round score meaningful while preserving a final-ring-out comeback |
| Active round duration | 60 s | Allows multiple territorial exchanges without making one round exhausting |
| Final duel limit | Unlimited | Sudden death must resolve a level match |
| Countdown | 1 s | Short local-development pacing; likely needs presentation review |
| Intermission | 1 s | Short local-development pacing; not assumed final |
| Pending resolution | 1 s | Leaves room for result presentation |
| Zone width | 20 m | Shared unit for territory and hit knockback |
| Valid territorial knockback | 20 m | One standard hit corresponds to one territorial step |
| Knockback duration | 0.65 s | Fast enough to feel forceful while remaining readable |
| Positioning window | 5 s | Time allowed per referee stage |
| Positioning skip threshold | 5 s | Avoids starting a longer procedure at the end of a round |
| Yellow cards for disqualification | 2 | First card warns; second ends the match |
| Reconnect window | 20 s | Gives a brief recovery opportunity without stalling the opponent indefinitely |
| Reconnect restart countdown | 3 s | Restores mutual readiness after return |

## 2. Arena geometry

| Element | Range/value |
|---|---|
| Lateral playable range | X = -30 to +30 m |
| Player collision radius | 1 m |
| Blue 3 | Z = -70 to -50 m |
| Blue 2 | Z = -50 to -30 m |
| Blue 1 | Z = -30 to -10 m |
| Neutral | Z = -10 to +10 m |
| Red 1 | Z = +10 to +30 m |
| Red 2 | Z = +30 to +50 m |
| Red 3 | Z = +50 to +70 m |
| Projectile cleanup margin | 10 m beyond the arena |

The seven equal-width zones make spatial advantage easy to reason about and allow the knockback distance to communicate one complete territorial loss.

## 3. Energy

| Parameter | Value |
|---|---:|
| Initial maximum | 100 |
| Base action coefficient | 4 |
| Regeneration | 5 per second |
| Run multiplier | 1.0 |
| Roll multiplier | 1.5 |
| Attack multiplier | 2.25 |
| Cast multiplier | 3.375 |
| Penalty divisor | 14 |
| Maximum unavailable fraction | 90% |
| Penalty recovery between rounds | 1% |

Spending energy also accumulates a capped maximum-energy penalty. The HUD therefore distinguishes:

- current usable energy;
- spent but recoverable energy;
- temporarily unavailable maximum energy.

Current design consequence: casting is the most expensive standard commitment, attack is next, and movement is the cheapest continuous pressure. The very low between-round penalty recovery intentionally makes accumulated exertion persist through the match; this value requires continued playtesting.

## 4. Movement and encumbrance

| Parameter | Value |
|---|---:|
| Base run speed | 10 m/s |
| Minimum low-energy speed multiplier | 0.5 |
| Speed multiplier with 0–1 projectile | 1.0 |
| Speed multiplier with 2 projectiles | 0.7 |
| Speed multiplier with 3 projectiles | 0.4 |

Actual speed is the product of base speed, the current energy factor and the ammunition factor. Carrying more offensive potential therefore gives up positioning strength before an attack is even released.

## 5. Cast and attack

| Parameter | Value | Meaning |
|---|---:|---|
| Cast duration | 1.25 s | Time to create one projectile |
| Maximum held projectiles | 3 | Ammunition and charge-segment cap |
| Attack segment duration | 0.25 s | Charge time contributed by each visible segment |
| Release animation | 0.2 s | Committed release state |
| Minimum charge | 0 | Release handling accepts the current segment logic without an extra floor |
| Multi-shot parallel offset | 1.75 m | Separates simultaneous paths while keeping a coherent volley |

The key relationship is more important than any single number: carrying three projectiles creates the highest attack ceiling but also the worst run and roll profiles.

## 6. Projectile

| Parameter | Value |
|---|---:|
| Initial speed | 70 m/s |
| Collision radius | 0.5 m |
| Rope rebound speed multiplier | 0.9 |
| Destroy speed threshold | 56 m/s |
| Cleanup distance beyond arena | 10 m |

A rope rebound keeps 90% of speed, making one planned ricochet useful without supporting an indefinitely bouncing projectile. The ricochet bot explicitly demonstrates this option.

## 7. Block

| Parameter | Value |
|---|---:|
| Raise time | 0.2 s |
| Defensive sector | 70° |
| Hold cost | 2 energy/s |
| Projectile impact cost | 40 energy |
| Broken-shield fall | 0.8 s |
| Get-up | 1.2 s |
| Threat look-ahead | 1.5 s |
| Defensive roll distance | 3 m |
| Trajectory safety margin | 0.5 m |

Block is strong but not passive: the player must face the attack, raise before impact and retain enough energy to absorb it. A depleted shield creates a long punishable state.

## 8. Roll profiles

All roll profiles use a 0.7-second action. Carried ammunition changes distance, invulnerability and recovery.

| Held projectiles | Distance | Invulnerable interval | Recovery | Energy multiplier |
|---:|---:|---:|---:|---:|
| 0–1 | 5 m | 0.05–0.60 s | 0 s | 1.5 |
| 2 | 4 m | 0.10–0.60 s | 0.05 s | 1.5 |
| 3 | 2 m | 0.20–0.50 s | 0.20 s | 1.5 |

This makes a fully loaded roll a prediction tool rather than a universal escape. It also enables the Roll Punisher bot to create meaningful second-shot pressure.

## 9. Network cadence affecting feel

| Parameter | Value |
|---|---:|
| Server simulation | 60 Hz |
| Battle snapshots | 30 Hz |
| Input heartbeat | 400 ms |
| Input timeout | 1000 ms |

These are technical values, but they directly influence responsiveness and must be reviewed alongside combat tuning. The archived 20 Hz balance template is no longer current.

## 10. Matchmaking

| Parameter | Value |
|---|---:|
| Password length limit | 8 characters |
| Ready timeout | 15 s |

## 11. Guided tutorial

| Parameter | Value |
|---|---:|
| Player start Z | -20 m |
| Teacher start Z | +20 m |
| Spotlight reach radius | 1.75 m |
| Movement exercise distance | 6 m |
| Direction threshold | 0.25 |
| Exit fade | 1000 ms |
| Target radius | 1.5 m |
| Resume delay | 400 ms |
| Lesson transition | 900 ms |
| Attack targets | (-10, 8), (0, 14), (10, 8) |

The spotlight objects and final target placement remain scene-authorable. Server numbers define validation, not the visual size of every light.

## 12. Teacher baseline

| Parameter | Value |
|---|---:|
| Desired projectiles | 2 |
| Attack hold | 700 ms |
| Aim prediction | 0.18 s |
| Defensive distance | 11 m |
| Strafe half-width | 7 m |

The teacher is intended to be credible but beatable. Reaction delay and occasional non-optimal choices should be tuned explicitly rather than achieved by client-side cheating or hidden stat reductions.

## 13. Tuning risks and questions

The next structured balance sessions should answer:

1. Does 60 seconds produce enough exchanges without making recovery from B3/R3 trivial?
2. Is the 20-metre knockback readable at all camera zone presets?
3. Does a three-projectile 0.4 speed multiplier create an interesting commitment or excessive frustration?
4. Is a 40-energy block impact cost sufficient to prevent indefinite defence?
5. Does 1% penalty recovery create the intended long-match exhaustion, or merely punish the losing player twice?
6. Can the 0:2 full-ring-out route occur through skill often enough to remain credible without becoming common?
7. Are five-second referee windows strict enough once players understand spotlights?

Changes should be tested as hypotheses with a short written observation, not adjusted only by visual preference.
