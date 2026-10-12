# MageDuel 2 Unity asset structure

First-party Unity assets follow these roots:

- `Art` — source models, textures, terrain data, props, and VFX source textures.
- `Animations` — imported animation clips, controllers, and reaction defaults.
- `Audio` — music, ambience, voice, and sound effects.
- `Editor` — Unity editor-only builders, validators, and setup tools, grouped by feature.
- `Fonts` — source font files and their licenses.
- `Materials` — runtime materials and shaders, grouped by feature.
- `Prefabs` — reusable scene objects, grouped by feature.
- `Resources` — only assets that are intentionally loaded through `Resources` or must keep their current runtime path.
- `Scenes` — Unity scenes.
- `ScriptableObjects` — definitions, configuration types, and serialized instances.
- `Scripts` — runtime code grouped by system.
- `Settings` — render-pipeline and project configuration assets.
- `Tests` — Unity EditMode and PlayMode tests.
- `UI` — UI sprites, generated art kits, portraits, and HUD source assets.

## Feature subfolders

- Environment assets use `Art/Environment`, `Materials/Environment`, and `Prefabs/Environment`.
- Character assets use `Art/Characters`, `Materials/Characters`, and `Prefabs/Characters`.
- VFX assets use `Art/VFX`, `Materials/VFX`, and `Prefabs/VFX` or `Prefabs/Projectiles`.
- Props use matching `Art/Props`, `Materials/Props`, and `Prefabs/Props` paths.
- Deprecated but still referenced UI belongs in `UI/LegacyBattle` until its dependencies are removed.
- Editor automation uses `Editor/Battle`, `Editor/MainMenu`, `Editor/Settings`, and `Editor/Project`.

## Rules

1. Move or rename Unity assets through `AssetDatabase` so their `.meta` files and GUIDs remain intact.
2. Do not add general assets directly under `Assets`.
3. Do not put non-phone materials in the phone material folder or mix VFX textures with UI sprites.
4. Keep editor scripts under an `Editor` directory and runtime scripts outside it.
5. Keep `Resources` small and intentional; prefer serialized references for new assets.
6. Keep third-party package layouts intact unless a migration has been explicitly tested.
7. Do not delete an asset only because it appears unused; verify GUID dependencies and code-based loading first.

Use `Mage Duel > Project > Organize Assets` for the controlled one-time migration and
`Mage Duel > Project > Validate Asset Organization` to verify the resulting paths.
