# Mage Duel 2 — Game Design Document

Version: **0.2**

Status: current design baseline

Updated: 2026-10-12

## 1. High concept

Mage Duel 2 is a competitive 1v1 arena game in which magical athletes win by controlling space rather than depleting health. Players create a small number of projectiles, decide how much of that ammunition to commit to an attack, defend through directional blocking or a timed roll, and push an opponent through a sequence of territorial zones.

A normal hit does not stop the round. It changes the geography of the fight. This gives every exchange a persistent consequence while allowing retaliation, recovery and visible comebacks.

## 2. Player fantasy

The player is not an adventuring wizard. They are a professional magical athlete performing under rules, a referee and an audience. The intended fantasy combines:

- the commitment and spacing of a fighting game;
- the trajectory reading of a projectile duel;
- the territorial pressure of a court sport;
- the spectacle and identity of a public arena competition.

The arena is the place where characters pursue recognition, freedom, proof, applause or mastery. The training gym is deliberately functional and unattractive by comparison: it teaches the tools, but the arena gives those tools meaning.

## 3. Design pillars

### 3.1 Territory is the primary state

There is no health bar. The audience should understand who is under pressure by looking at the athletes' positions. A hit matters because it costs space and makes the next hit more dangerous.

### 3.2 Readable commitment

Casting creates ammunition but occupies time. Charging determines how many available projectiles will be released. Blocking protects a direction but costs energy. Rolling is powerful only when timed correctly and changes with carried ammunition.

### 3.3 Escalating danger

The same hit has different dramatic weight in each zone. A loss near the centre is recoverable; pressure in the third zone threatens the entire match. The most important moments emerge from spatial context rather than inflated damage numbers.

### 3.4 A difficult comeback remains possible

Round score creates a major advantage but never makes the last round meaningless. Even at 0:2, a trailing player can win the match by forcing a full ring-out. This route is intentionally harder than an ordinary round victory.

### 3.5 Competitive truth comes from the server

Movement, energy, casting, attacks, projectiles, hits, territory, time, cards and victory are server-authoritative. The client presents and predicts where appropriate but does not decide competitive outcomes.

## 4. Target audience and format

The current primary audience is players who enjoy short competitive 1v1 sessions built around spacing, prediction, commitment and adaptation. The design assumes a player willing to learn a small set of interacting actions rather than receive constant power growth or a PvE campaign.

A secondary audience is spectators and players attracted by the fantasy-sport presentation: named athletes, officiated rules, visible territorial pressure and an arena designed to make danger understandable from position.

Current format:

- platform focus: Windows PC;
- input: keyboard and mouse with remappable gameplay bindings;
- match format: online 1v1, three regular rounds plus a final duel only when required;
- additional play: guided lessons and one dedicated bot match, “Fight the Teacher”;
- spectator layer: an active match can be broadcast in the main-menu arena.

The immediate target is an advanced playable prototype and vertical slice in development, not a content-complete commercial release.

## 5. Core loop

1. Read the opponent's position, ammunition and intent.
2. Move to preserve space or create an angle.
3. Cast up to three projectiles.
4. Charge and release one, two or three projectiles.
5. Block or roll through the opponent's response.
6. Convert a hit into territorial pressure.
7. Take the referee-assigned position and continue fighting.
8. Win the round by position, or win the match immediately through a full ring-out.

## 6. Arena and territorial model

The playable length is divided into seven ordered 20-metre zones:

```text
Blue outer edge                                            Red outer edge
┌──────┬──────┬──────┬─────────┬──────┬──────┬──────┐
│  B3  │  B2  │  B1  │ Neutral │  R1  │  R2  │  R3  │
└──────┴──────┴──────┴─────────┴──────┴──────┴──────┘
                           centre
```

At the start of each regular round, both athletes contest the neutral territory from their first side zones. The active territorial front moves after valid losses. Losing the neutral contest sends the defeated athlete away from the centre and allows the winner to occupy neutral. Repeated losses can move the same athlete through the second and third side zones.

The front resets for the next regular round. Match-level score, cards and hit counters do not reset.

## 7. Match structure

### 7.1 Entry

Players select an athlete before matchmaking. The server creates a match, assigns team colours and validates both participants. A short countdown precedes active play.

### 7.2 Regular rounds

- Three regular rounds are scheduled.
- Current round duration is 60 seconds of active combat time.
- A legal hit can change territory multiple times in one round.
- The active timer pauses during referee-controlled positioning.
- A hit does not award a round point by itself.

When time expires, the server compares both assigned positions by distance from the centre:

- closer athlete: one round point;
- equal distance: drawn round;
- no intermediate tiebreak duel is created.

### 7.3 Terminal outcomes

The match can end before all regular rounds are completed when:

- an athlete is fully pushed beyond the third zone;
- an athlete receives the disqualifying second yellow card;
- both receive a disqualifying card simultaneously, producing a double loss;
- a terminal connection/forfeit rule is reached.

### 7.4 Final duel

If the scheduled regular rounds finish without a match winner, a sudden-death final duel begins. It has no active time limit in the current configuration and does not advance the territorial front. The first unblocked projectile hit or valid zone exit ends the duel. If both athletes lose simultaneously, the duel is restarted rather than recorded as a draw. Its purpose is to produce one final winner after an otherwise level match.

## 8. Player actions

### 8.1 Aim and facing

The world-space cursor controls intent. In idle and running states the athlete's head follows the aiming direction. Head tracking is suppressed when animation readability is more important, including cast/release, hit reactions, falling and rolling.

### 8.2 Movement

Movement is direct and responsive. There is no separate walk mode in the current combat loop. Low available energy and carried ammunition reduce effective speed, so positioning and resource use remain connected.

### 8.3 Cast

Holding the cast input creates ammunition over time, up to three projectiles. Casting is a vulnerable commitment and uses energy. Created projectiles remain visually associated with the athlete until fired or destroyed by a hit.

### 8.4 Attack

Holding attack charges through a number of segments equal to currently available projectiles. Releasing sends the number of projectiles reached by the charge. Multi-projectile attacks use a configurable lateral launch offset rather than occupying the same trajectory.

### 8.5 Projectile behaviour

Projectiles are server-simulated. They travel quickly, interact with the arena boundaries, can be used for direct or ricochet attacks, and are removed after travelling beyond the playable arena margin. All remaining projectiles are cleared when the match ends.

### 8.6 Block

Block is directional: the player must face the incoming threat. Raising the shield takes time and maintaining it drains energy. A correctly oriented active shield consumes energy on impact; insufficient defence produces a broken-guard fall and recovery.

### 8.7 Roll

Roll combines movement and block input. Its distance, invulnerability window and recovery change with carried ammunition. A lightly loaded athlete rolls farther and more safely; carrying three projectiles creates a short, punishable defensive option.

### 8.8 Energy and fatigue

Energy is the shared action budget. Baseline regeneration continues while gameplay simulation is active, while running, casting, attacking, blocking and rolling impose immediate or continuous costs on top of it. Sustained exertion therefore creates a net drain. Repeated exertion also reduces the usable maximum and affects mobility as well as action frequency. The HUD presents current, spent and temporarily unavailable portions as one continuous ring.

## 9. Hits and territorial loss

A legal hit during active combat:

1. starts the athlete's hit reaction;
2. applies a configurable knockback of one zone width (currently 20 metres);
3. removes the victim's held projectiles immediately;
4. preserves the attacker's held projectiles;
5. increments the victim's authoritative hit counter;
6. moves the territorial front one step away from the centre;
7. starts positioning unless a terminal outcome or end-of-round shortcut applies.

The server allows the visible reaction and displacement to finish before declaring a full ring-out result. This avoids ending the simulation while the decisive athlete is still in flight.

Hit count is retained for future injury design, but injuries do not affect the current match and no injury consequences are currently implemented.

## 10. Referee positioning procedure

After territory changes, the referee assigns both athletes new target zones. Players remain in control and must occupy those positions themselves. Arena spotlights communicate where to go and intentionally reuse the visual language introduced by the movement lesson.

The regular-round timer is frozen while positioning is unresolved. The procedure uses up to three five-second windows:

1. first window: instruction to take position;
2. second window: warning; failure produces a yellow card;
3. third window: final opportunity; failure produces the next yellow card and therefore disqualification when it is the second card.

Releasing a projectile during positioning is illegal and immediately produces a yellow card. Casting at least one projectile becomes an offence when the player also fails to occupy the target by the end of the current window.

An illegal projectile remains physically real: it may hit, knock back, clear the victim's ammunition and increment hit count. It cannot change the territorial front or create a valid full ring-out. After the reaction, the athletes must still occupy the previously assigned positions.

If fewer than five active seconds remain when positioning would begin, the server may resolve the round directly instead of starting another positioning sequence.

The complete decision table is normative in [Territorial Combat Rules](TerritorialCombatRules.md).

## 11. Scoring, victory and comeback

Regular-round points reward territorial position at time expiry. They are valuable but intentionally do not provide an early score-based match closure. A player leading 2:0 enters the third round with a decisive conventional advantage, while the trailing player retains one extraordinary route: a full ring-out overrides the score and wins the match.

This produces two simultaneous stakes:

- **round stake:** be closer to the centre when time expires;
- **match stake:** never allow pressure to carry you beyond the outer zone.

## 12. Bots and teacher fight

Ordinary players do not receive a generic “play against bot” button. PvE is framed as **Fight the Teacher**, a real match in the training gym under the normal combat rules.

Automated opponents use distinct tactical profiles:

- **Ricochet:** creates two projectiles, sends one through a rope rebound and follows with direct pressure.
- **Roll Punisher:** attacks directly, then aims the second projectile toward the player's likely roll destination.
- **Adaptive:** the general behaviour retained from the original bot and used as a broader baseline.

The teacher must be beatable. Human-like reaction delay and occasional mistakes remain tuning goals rather than permission to violate server rules.

## 13. Tutorial experience

Guided lessons are separate from Fight the Teacher. They teach one concept at a time in this order:

1. movement;
2. block;
3. roll;
4. cast;
5. attack targets.

Each completion unlocks and immediately advances to the next lesson. Lessons restrict server-authoritative actions instead of trusting the client to disable buttons. Instruction moments pause the lesson simulation through server-controlled barriers, not a local-only time scale.

The training gym is a full-size but cheap institutional sports hall. Its flat electric light and lack of spectacle intentionally contrast with the arena. Spotlights create a consistent learned command: **move into the light**.

Full lesson behaviour is defined in [Tutorial Design](TutorialDesign.md).

## 14. Camera and presentation

The battle camera keeps a stable authored height and pitch for the currently contested zone while following the local athlete along the arena's longitudinal axis. Zone-relative presets prevent an opponent's animation or running motion from continuously perturbing the frame.

The presentation priorities are:

1. local athlete remains controllable and visible after knockback;
2. the active territorial front is readable;
3. dangerous projectiles remain inside the useful frame;
4. camera motion does not react to minor animation movement.

## 15. Interface

### 15.1 Battle HUD

The HUD deliberately leaves the centre clear except for the gold diamond cursor.

- Upper left: local athlete portrait, athlete name, energy ring and attack charge.
- Upper right: opponent portrait, athlete name, team-colour outline and current-match round results.
- Bottom centre: authoritative round timer.
- Central and referee messages appear only when needed.

Athlete names, never account names, identify combatants. The local portrait is mirrored to face inward; the opponent portrait preserves its original inward-facing orientation.

### 15.2 Main menu

The main menu overlooks and broadcasts the arena. Its top match strip shows the live participants, team colours, score and round. Navigation provides play, athletes, training and future profile entry points. Matchmaking and lesson selection use contextual pop-ups attached to their navigation items.

### 15.3 Settings

- remappable controls with capture-state input isolation;
- gameplay selection for the arrangement of held projectiles;
- percentage audio controls prepared for the future mixer;
- reusable numeric progress controls.

## 16. Athletes and identity

The current roster contains nine server-recognised athlete IDs:

Alex, Brandon, Chloe, Danyal, Emil, Fia, Gustav, Hector and Ingrid.

Athlete selection affects the displayed identity and visual presentation. Account display names remain separate and are not shown in the combat HUD. Broader character-specific mechanics, career progression and injury consequences are not part of the present vertical-slice rules.

## 17. Online and reconnect experience

Random and password matchmaking lead to the same authoritative match. Password rooms accept a compact room code. If an active player disconnects, the match enters a limited reconnect window; the returning client receives current state and a short countdown. Temporary server-side defence prevents a brief transport failure from becoming an entirely unprotected target. Expiry or repeated failure resolves according to the server's forfeit rules.

## 18. Accessibility and readability

Current provisions include remappable gameplay controls, text instructions that substitute the assigned binding, numeric timers, high-contrast outlines and redundant colour/shape cues for results. Future accessibility work should include independent text scaling, colour-vision validation, reduced flashes/camera motion and localisation review.

## 19. Current balance baseline

The numbers in this document explain relationships rather than duplicate configuration. [Balance and Tuning](BalanceAndTuning.md) records the current server values and is the numeric source of truth for design review.

## 20. Scope boundaries

### Present prototype scope

- territorial online duel;
- full ring-out and referee penalties;
- final duel;
- block, roll, cast and segmented attacks;
- three bot profiles and teacher match;
- five guided-lesson flows in final acceptance;
- athlete selection, matchmaking, settings and battle HUD;
- account and session foundation;
- main-menu battle broadcast.

### Designed or prepared, but not complete

- final UI acceptance and polish;
- full audio implementation;
- production email delivery and operations;
- external sign-in providers;
- profile and notification product flows;
- injury consequences and career systems;
- macOS support;
- production deployment and CI pipeline.

These are tracked honestly in [Project Status](ProjectStatus.md) and must not be represented as completed features.
