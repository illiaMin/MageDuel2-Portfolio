# Mage Duel 2 — Tutorial Design

Version: **1.0**

Status: current intended experience; implementation completion is tracked separately

Updated: 2026-10-12

## 1. Purpose

The tutorial teaches the minimum physical vocabulary required to enjoy Mage Duel 2 without trying to explain the whole competitive game at once. It separates deliberate instruction from the ordinary bot match.

The intended result is not only that the player knows the controls, but that they acquire one important conditioned response before entering the arena:

> When a spotlight marks a position, move into the light.

The same visual language is used by the referee during territorial positioning.

## 2. Training menu structure

The Training navigation item opens a contextual menu with:

- **Fight the Teacher** — a normal server-authoritative match against a beatable bot;
- **Movement**;
- **Block**;
- **Roll**;
- **Cast**;
- **Attack Targets**.

Fight the Teacher is not a lesson and does not gate lesson progression. There is no separate generic “Play with Bot” product flow.

Movement is unlocked by default. Each guided lesson unlocks when the preceding lesson is completed. Completion automatically continues into the next unlocked lesson unless the player exits.

Until the sequence is completed, the Training navigation item may receive persistent attention styling in the main menu. That emphasis ends after completion.

## 3. Environment and tone

All training occurs in a separate full-size sports hall. It uses the same gameplay dimensions needed by combat but deliberately looks cheap, institutional and practical:

- electric interior lighting;
- wooden sports floor;
- functional walls and doors;
- no crowd, monumental stone or arena ceremony.

The contrast is motivational. Training provides safety and repetition; real players, spectacle, applause and freedom are found on the arena.

The current TrainingGym scene may temporarily reuse arena pieces while the final hall art is assembled. This is a production substitution, not a change to the intended setting.

## 4. Authority model

Guided lessons use the real networked movement and action model. Restrictions and step completion are server-authoritative.

The client is responsible for:

- displaying instructions and assigned bindings;
- fades, panels, spotlights and cursor presentation;
- sending ordinary input;
- presenting completion and transition.

The server is responsible for:

- deciding the active lesson and step;
- accepting only the actions allowed by that step;
- pausing/resuming lesson simulation barriers;
- spawning the teacher only where required;
- validating completion;
- saving unlocked/completed progression.

Local `Time.timeScale = 0` is not used to create instructional pauses. In a networked simulation, a client-only freeze would allow the server and projectiles to continue. Instead, the tutorial director holds the relevant server simulation until the required input is received.

## 5. Common lesson presentation

Every lesson:

1. begins behind a full-screen black image;
2. fades the black image out over a configurable duration, currently targeting one second;
3. presents instructions through the shared lesson panel;
4. substitutes the player's current input binding into the text;
5. keeps the gold cursor above other tutorial UI when aiming is available;
6. ends with a fade to black;
7. transitions to the next lesson or returns to the menu.

There is no permanent Exit button inside individual lesson content.

The guided-lesson battle HUD is intentionally partial: it shows the local athlete and local state but hides the opponent block when no opponent is part of the lesson. Fight the Teacher uses the complete battle HUD.

## 6. Pause and exit

Pressing Escape opens the dedicated `TrainingPausePanel`.

- Pressing Escape again closes it and resumes the lesson.
- **No** closes it and resumes the lesson.
- **Yes** fades to black, leaves the tutorial session and returns to the main menu.

The panel uses the same selected/hovered button language as Settings. It does not require a separate Continue button because Escape and No already serve that function.

## 7. Lesson 1 — Movement

### Goal

Teach four directional inputs and establish spotlight occupation as a command.

### Setup

- No teacher is spawned.
- All gameplay input begins locked.
- Four scene-authored spotlight targets are assigned in the Inspector: right, left, forward and backward.
- Designers can move the spotlight objects without changing tutorial code.

### Sequence

#### Step A: right

1. Show: `Hold {Move Right} to enter the spotlight.`
2. Permit only rightward movement.
3. Enable the right spotlight approximately 5–6 metres from the athlete.
4. Complete when the athlete occupies its target.

#### Step B: left

1. Freeze at the next instruction barrier.
2. Show: `Hold {Move Left} to enter the spotlight.`
3. Permit only leftward movement.
4. Switch to the left spotlight and complete on occupation.

#### Step C: forward

1. Show: `Hold {Move Forward} to enter the zone ahead.`
2. Permit only forward movement.
3. Enable the scene-authored forward spotlight and complete on occupation.

#### Step D: backward

1. Show: `Hold {Move Backward} to enter the zone behind you.`
2. Permit only backward movement.
3. Enable the scene-authored backward spotlight and complete on occupation.

After the final occupation, fade to black and advance to Block.

## 8. Lesson 2 — Block

### Goal

Teach the player to face a threat and raise the shield before release.

### Setup

- This is the only guided lesson that spawns the teacher.
- The teacher prepares a readable projectile attack.
- Other player actions are disabled.

### Sequence

1. Show: `Face the attacker and raise your shield before the projectile is released. {Block}`
2. Wait for the assigned Block input; do not wait for a removed Continue button.
3. Resume the authoritative sequence and let the teacher release.
4. If block orientation/timing succeeds, complete the lesson.
5. If it fails, reset the attempt and show: `Don't worry, just try again. Face the attacker and raise your shield before the projectile is released.`

The lesson does not use the redundant instruction `Aim at the attacker and hold Block.` The cursor and head tracking remain active so the player can correct facing naturally.

## 9. Lesson 3 — Roll

### Goal

Build roll from two already understandable inputs instead of presenting it as an unrelated ability.

### Sequence

1. **Block reminder:** show `Just raise your shield.` and accept only Block.
2. **Movement reminder:** show `Just move.` and accept movement.
3. **Combination:** show `Hold a movement direction and raise your shield to roll!`
4. Accept movement + Block and start the roll.
5. Wait for the roll animation/action and its authoritative completion; do not end the lesson on the initial button press.
6. Fade out and advance.

No numeric `Progress 0/3` counter is displayed. The three-stage structure is communicated by the instructions themselves.

## 10. Lesson 4 — Cast

### Goal

Teach ammunition creation as a single focused action.

### Sequence

1. Show `Create a projectile. {Cast}`
2. Permit only the assigned Cast action, plus aiming presentation.
3. When the player holds the correct cast input, hide the blocking instruction plane/panel so it does not cover the action.
4. Complete when the server confirms the required projectile has been created.

The lesson does not label Cast as Attack and does not display a numeric progress counter.

## 11. Lesson 5 — Attack Targets

### Goal

Combine ammunition creation with charge/release against non-player targets.

### Setup

- No teacher is spawned.
- Scene-authored targets provide safe readable goals.
- Cast and Attack are both enabled; unrelated combat actions remain disabled.

### Sequence

1. Prompt the player to create a projectile with the assigned Cast binding.
2. Hide the instruction plane while Cast is held so the action remains visible.
3. Prompt the player to charge and release toward a target using the assigned Attack binding.
4. Confirm hits authoritatively.
5. Continue until the designed target objective is complete.
6. Fade out, mark the guided sequence complete and return to the menu.

No raw progress digits are displayed in the lesson panel. Target state and concise instruction text communicate progress.

## 12. Fight the Teacher

Fight the Teacher:

- runs as an ordinary server-authoritative match;
- uses the complete battle HUD;
- follows the current territorial rules;
- does not unlock or complete guided lessons;
- is available independently from the lesson sequence;
- uses a capable but beatable automated opponent.

The final teacher behaviour should include human-readable reaction latency and rare mistakes. The objective is credible practice, not perfect input-reading.

## 13. Input binding text

Tutorial prompts never hardcode keys such as Space or mouse buttons. They request the semantic action from the normal input-binding service and format the current assigned binding.

Examples:

- Roll shows a combination derived from Movement + Block.
- Cast shows the binding for Cast, not Attack.
- Attack shows the binding for Attack, not Cast.

Rebinding in Settings therefore changes tutorial text without editing lesson content.

## 14. Progress persistence

The server stores enough account progression to determine:

- highest unlocked guided lesson;
- completed guided sequence;
- whether the main-menu Training emphasis remains active.

Progress must survive logout/login and must never be inferred only from local UI state.

## 15. Failure and retry principles

- Failure text is encouraging and immediately actionable.
- A failed attempt resets only the current exercise.
- The player is never punished with match cards during a guided lesson unless the lesson explicitly teaches that rule in a future revision.
- Instructions remain concise enough to read before action resumes.
- The teacher does not exist in lessons that do not need an opponent.

## 16. Completion criteria

The tutorial feature is ready for the vertical slice when:

- every lesson rejects every forbidden action on the server;
- all prompts use current assigned bindings;
- pause/resume does not advance authoritative timers;
- exit works from every lesson and Fight the Teacher;
- completion persists across a new authenticated session;
- lesson transitions do not expose the arena or stale HUD during fades;
- the teacher's required attack is repeatable but not frame-perfect;
- the final TrainingGym hierarchy and presentation pass visual review.

Implementation status for these checks is recorded in [Project Status](ProjectStatus.md), not implied by this intended-design document.
