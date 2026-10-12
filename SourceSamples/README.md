# Selected source samples

These files are copied from the private Mage Duel 2 development repository to support portfolio review. They demonstrate the translation of the territorial design into authoritative server state, tutorial orchestration, automated tests and client presentation.

They are intentionally **not** a standalone build: supporting infrastructure and third-party project assets are excluded from this public repository.

## Server

- `Server/TerritoryState.cs` — ordered seven-zone model, centre-distance comparison and territorial-front state.
- `Server/TerritorySimulation.cs` — zone loss, assigned positions, full ring-out handling and territorial transitions.
- `Server/RoundFlowSimulation.cs` — authoritative regular-round, positioning and final-duel flow.
- `Server/TutorialDirectorSimulation.cs` — server-directed lesson steps, allowed-action filtering and tutorial progression.

## Unity client

- `Unity/BattleCameraController.cs` — stable contested-zone camera presets with local-athlete longitudinal following.
- `Unity/TrainingSceneController.cs` — training presentation, lesson transitions, blackout flow and local input restrictions.

## Tests

- `Tests/TerritoryDomainTests.cs` — domain-level territory invariants.
- `Tests/TutorialDirectorSimulationTests.cs` — tutorial simulation behaviour and progression coverage.

## Authorship context

The project is solo and AI-assisted. The creator authored the systems, rules, specifications and acceptance criteria, directed implementation, integrated and tested the results, tuned the game and made all final product decisions. A substantial part of code production was performed with AI tools under that direction.

