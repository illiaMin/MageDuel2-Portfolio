# Mage Duel 2 — Territorial Combat Design Case Study

Status: portfolio case study

Updated: 2026-10-12

## 1. The original problem

The first playable rules ended an ordinary round on the first hit. That was simple to explain and easy to implement, but it fought against the strongest parts of the project:

- seven arena zones existed visually but most were irrelevant to ordinary play;
- a successful exchange stopped action instead of changing it;
- one mistake had a binary result with little opportunity to adapt;
- a 0:2 score made the last round conventionally hopeless;
- intermediate tiebreak duels fragmented the match;
- one hit per round created too few pressure events for injury, replay or audience systems;
- the viewer had to read score rather than the athletes' physical situation.

The problem was not that the projectile combat lacked actions. It lacked a persistent spatial consequence that could connect those actions into a longer story.

## 2. Design objective

The redesign needed to:

1. keep the existing cast, charge, projectile, roll and energy mechanics;
2. make all seven zones part of normal play;
3. allow several hits without adding a health bar;
4. make pressure readable from the arena itself;
5. retain a meaningful comeback at 0:2 without making the lead worthless;
6. use server-authoritative rules suitable for online competition;
7. create more dangerous moments without turning every hit into a long reset.

## 3. Design alternatives and constraints evaluated

The following are the principal alternatives against which the current territorial solution can be evaluated. They document the design rationale; they should not be read as a claim that each option reached a separately implemented prototype.

### Add health

Rejected because it would make the project easier to categorise but less distinct. Health would shift attention from space to damage efficiency and require another HUD system to explain what the arena already communicates physically.

### Award points for every hit

Rejected because it encourages safe hit farming and makes early leads snowball numerically. It also leaves the zones as decoration.

### Reset after every hit

Rejected because it preserves interruption. The player sees a consequence, then the game erases the interesting position that consequence created.

### Permanent free movement with no referee phase

Rejected because a knocked-back athlete and advancing opponent could resume from ambiguous or unfair positions. Online latency would make “where the next exchange legally begins” difficult to understand.

## 4. Chosen solution: a moving territorial front

A legal hit pushes the victim one zone away from centre and lets the attacker advance. The round continues from that new front. Repeated success can move the same opponent through multiple zones.

This transforms a hit from a binary point into three things at once:

- immediate spectacle through knockback;
- persistent advantage through lost territory;
- increased future risk because the outside boundary is closer.

At time expiry, the athlete closer to centre receives the ordinary round point. The round therefore retains a clear result while allowing its internal story to reverse several times.

## 5. Why full ring-out overrides score

The system needed a comeback route at 0:2, but simply increasing third-round points would devalue the first two rounds. Full ring-out solves a different problem:

- the leader retains the much easier conventional path: protect territory and finish the match on score;
- the trailing player must achieve the hardest spatial outcome: push through every remaining zone;
- the audience can understand the danger without calculating a special multiplier;
- the match can end early through its most spectacular event.

The rule creates possibility, not equality. A 2:0 lead remains powerful because the opponent's comeback condition is intentionally demanding.

## 6. Referee positioning as anti-ambiguity design

Knockback alone does not guarantee that both players arrive at fair legal starting positions. The referee therefore freezes active round time and assigns target zones after a territorial change.

The important decision was to keep player agency:

- the server knocks the victim out of the lost territory;
- both athletes must move themselves into position;
- spotlights show the targets;
- failure escalates from instruction to warning, yellow card and disqualification.

This is slower than teleporting, but it makes territorial ownership visible and gives the competitive world an officiated sport identity.

## 7. Closing the positioning exploits

Allowing control during positioning immediately creates edge cases. The design treats them explicitly rather than hiding them:

- releasing a projectile is an immediate yellow-card offence;
- casting becomes an offence only if the player also fails the current occupation window;
- an illegal projectile remains physically real, so it can hit and knock back;
- that hit cannot move territory or produce a full ring-out;
- a projectile legally released before positioning remains legal after it begins.

The distinction between **physical consequence** and **competitive legitimacy** is the key. It prevents visual discontinuity while protecting the territorial state from an illegal action.

## 8. Removing intermediate duels

The previous structure needed intermediate duels because an ordinary round could fail to produce a clean first-hit winner. Territorial rounds already support prolonged exchange and can end in a legitimate positional draw.

Intermediate duels were therefore removed. Only a final sudden-death duel remains after all scheduled rounds when the match score is still level. This gives the final duel a unique purpose instead of using it as a patch for ordinary round logic.

## 9. Teaching the rule without a lecture

Referee target zones could have required a large explanatory overlay during a real match. Instead, the movement lesson uses spotlights from its first interaction. The player learns a simple motor association in a safe space: move into the light.

When the same light appears after a territorial hit, its meaning is already embodied rather than newly described. The tutorial is therefore part of the combat-system design, not a separate manual.

## 10. Presentation consequences

The territorial system changed other design requirements:

- the camera must keep a knocked-back local athlete visible across several zones;
- framing should react to the contested zone, not every opponent animation;
- the HUD timer must visibly stop with the authoritative positioning state;
- hit reactions must finish before a full-ring-out result interrupts simulation;
- the broadcast HUD must update score, round and athlete identity from live state;
- held projectile removal must be immediate and legible after impact.

The current camera uses stable zone-relative height/pitch presets while following the local athlete longitudinally. This emerged directly from territorial playtesting: following both animated characters continuously produced visible jitter.

## 11. Technical translation

The design was implemented as explicit server state rather than client convention:

- named seven-zone order and centre distance;
- territorial front and assigned zones;
- positioning stage and timer;
- yellow-card and hit counters;
- legal/illegal release provenance;
- terminal end reason and winning team;
- binary protocol v3 fields for all of the above;
- automated tests for domain rules and multi-step simulation.

This translation matters to the design: if clients independently infer territory from transforms, latency can produce different truths. The server instead publishes the decision that presentation should communicate.

## 12. Current validation

The server suite currently passes 83/83 automated tests, including territorial and tutorial simulation coverage. This proves deterministic rule handling for the tested cases, not final balance quality.

Informal hands-on validation to date:

- approximately 100–150 creator-played matches against each bot profile;
- approximately ten creator completions of the guided lessons;
- informal feedback from other people on the combat, UI and visual presentation;
- one observed first-time player attempt in which the teacher bot won in roughly 20 seconds.

The last observation is useful precisely because it is unfavourable: it suggests that the teacher currently demonstrates the game's threat but may overwhelm a new player before they can form a plan. These observations are iteration evidence, not a controlled usability study or proof of balance.

The next structured validation pass should record:

- average match and round duration;
- hits and territorial-front changes per round;
- reversal frequency after a player loses neutral territory;
- full ring-out frequency, including comeback ring-outs from 0:2;
- positioning-window failures and yellow cards per match;
- time to first successful cast, attack, block and roll for new players;
- tutorial retries and completion time per lesson;
- teacher-bot survival time and win rate for first-time players;
- whether a spectator can identify the pressured athlete without explanation.

Qualitative questions still requiring broader testing:

- frequency of reversals within a 60-second round;
- whether 20-metre knockback always reads as one zone;
- how often a full ring-out comeback is realistically achievable at 0:2;
- whether five-second positioning windows feel strict after tutorial completion;
- whether cards create tension without making honest navigation errors overly punitive;
- whether spectators understand the current front without explanation.

## 13. Resulting design identity

The redesign did not add a layer on top of the original duel. It made space the connective tissue of the entire game:

- projectiles create displacement;
- displacement changes territory;
- territory changes risk;
- risk changes how much ammunition is safe to carry;
- the referee formalises new positions;
- the camera and spotlights communicate them;
- score records the round, while full ring-out preserves the match's ultimate threat.

That chain is the current core identity of Mage Duel 2.
