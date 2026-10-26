# Milestone 7 - Editable forest blockout

Scene: `Assets/Scenes/Milestone7_ForestBlockout.unity`.
Source design: `LevelReferences/HerFootsteps_Mockup.png` and `HerFootsteps_MockupKey.png`.
This is the designer's map translated into a playable blockout, not a final art or narrative pass.
Earlier scenes, prefabs, input assets and gameplay implementations remain unchanged.

## Translation and assumptions

- North is Unity +Z, east is +X. The 1920x1080 reference was hand-traced at a provisional 0.14 metres per image pixel: X=(u-960)*0.14, Z=(1000-v)*0.14.
- The native Unity Terrain is 196x154 metres, with a 513 heightmap and 512 texture map. The irregular playable outline occupies roughly 170x130 metres.
- Fourteen route guides preserve the main loops, western optional exploration, northern skeleton branch and eastern approach. Trail paint is approximately 3-5.4 metres wide; conifers are excluded within 3.4 metres of route centres.
- The map does not specify scale, elevations, exact structures or individual tree positions. Gentle uneven terrain, flattened landmark clearings and a raised southeast tower clearing are provisional implementation choices.
- The blue eastern fill is interpreted as a corruption/fog region, not water. No river or swimming mechanic was added.
- The tower uses three walkable switchback ramps because the existing controller has no jumping/climbing. Its root is shifted 5 metres north within its southeast clearing to keep its entrance clear of the boundary.
- Route B retains the northern skeleton as an explorable branch; it is not made a mandatory choke point. Both approaches join the northeastern outer approach.
- Toolboxes currently contain batteries. Flares, cabin NPC dialogue, the skeleton's future real scream and the sister rescue are named reservations only. No final narrative outcome or progression gates were invented.
- Trees and a modular rock ridge define the boundary. Some off-trail travel remains possible between trunks: this is a semi-open forest, not an invisible corridor network. Collider geometry is included in navigation.

## Hierarchy and points of interest

The environmental parent is `M7 Forest Blockout`. Existing player, cryptid, hunt, ambience and hallucination roots remain separate.

| Location | Child hierarchy beneath M7 Forest Blockout | Map image centre (u,v) |
|---|---|---|
| Player spawn / initial supplies | `01 South - starting area` / `Starting fire and supplies`, `Battery cache`, `Camp calm - finite recovery` | 790,940 |
| Hunter cabin / NPC reservation | `02 West - cabin and abandoned tent` / `Hunters cabin - future NPC` | 510,365 |
| Abandoned tent / flare reservation | `02 West - cabin and abandoned tent` / `Abandoned tent - flare reservation` | 700,680 |
| Central campsite | `03 Central - campsite and branches` / `Central campsite fire`, `Camp calm - finite recovery` | 880,720 |
| Skeleton reservation | `04 North - skeleton and noise route` / `Human skeleton - future scripted scream reservation` | 810,150 |
| Accessible tower | `05 Southeast - watchtower` / `Watchtower - accessible ramps` | 1065,920, plus 5 m north |
| Corruption pressure volumes | `06 East - corrupted forest and territory` / `Route A - corrupted composure pressure` | 1290,720; 1430,700; 1380,540 |
| Sister / cryptid territory | `06 East - corrupted forest and territory` / `Territory - future sister rescue` | 1340,390 |
| Route guides | `07 Design guides - select routes for gizmos` / numbered routes 01-14 | Hand-traced nodes |
| Outer boundary | `08 Natural boundary - editable rock ridge` | Map silhouette |

The player is `Milestone5Player` (inherited name) at the southern spawn. `TestCryptid` begins in the northeastern territory; HuntController retains control over its activation.
Each geographic region contains its own `Trees` group and nearby supplies, cover, clue and noise objects. Regions are organizational, not gameplay gates.

Distributed content: 597 imported conifer instances, additional modular dead trees,
7 hiding shelters, 11 dry-branch noise patches, 5 sister discoveries,
7 supply caches (2 batteries each), 2 dead-animal proxies, 2 calm areas,
3 corrupted pressure volumes and 3 phantom-footstep eligibility areas.

## Reused systems and provisional tuning

The M6 player, HUD/hotbar, stamina, flashlight, inventory, interactions, hiding,
breath, NoiseChannel, composure, cryptid senses/navigation/brain and HuntController
are reused. There is no second noise, inventory, hiding or hunt system.

| Setting | M7 value / behavior |
|---|---|
| Passive composure | -0.5/s; suppressed inside either camp's calm box |
| Calm boxes | 9x5x9 m; +3/s; 20-point budget per box per session; ceiling 75 |
| Corrupted boxes | 13x8x16 m; additional -0.5/s inside; normally total -1/s with passive loss |
| Clues | Hold E for 1.5 s; +20 once per clue; no inventory slot; five unique IDs |
| Supplies | Seven caches x2 batteries; stack 3; five inventory slots; +40 charge per use; no consumption at full charge |
| Phantom areas | 16x8x18 m; call existing TryActivate while inside; Low/Broken gate, 8 s event, 25 s cooldown retained |
| Phantom automation | Disabled on the event in M7 so area eligibility controls normal activation; F7 retains existing debug behavior |
| Wildlife | Existing flashlight-reactive hallucination at the Route B animal area; real failure Scream still uses NoiseChannel |
| Cryptid travel timeout | 90 s scene override, increased from 15 s for the larger map |
| Navigation | Surface collects child physics colliders; voxel size 0.18 m; saved ForestNavMesh asset |
| Visibility | Exponential-squared cold fog density 0.0145; player camera far clip 240 m; inherited restrained moon/colour grading |
| Tower | Deck 10 m above its clearing; three 14 m ramp flights; 2.4 m walking width |

All numeric values are provisional. Existing M6 tuning remains documented in
`MILESTONE_6_PLAYTEST.md`, including the 60 s safe period, 4 s warning and 30 s cooldown.
Camp calm is not hunt immunity. Hiding does not restore composure. Noise supplies
an evidence position; sight alone allows live chase tracking. Broken activation
does not reveal a concealed player. Fog does not secretly change sight distances.

`HallucinationArea` is a small reusable box-eligibility adapter; the existing
HallucinationEvent still owns composure conditions, cooldown, duration and audio.
Phantom audio emits no gameplay noise. The existing wildlife failure does.
`BlockoutRoute` only draws selected-node gizmos; it never generates terrain.

## Editing the level

1. Open the saved M7 scene and work outside Play Mode. Save a copy before large layout experiments.
2. Select `Editable Unity Terrain - sculpt and paint`. Use Unity Terrain sculpt/smooth tools for heights and Paint Texture for `Forest floor`, `Worn trail` and `Corrupted ash`. These are native terrain layers, not a runtime mesh.
3. Move trees individually or as geographic groups. Imported conifers retain their original visual prefab references; scene trunk collider overrides define physical cover. Keep a clear walking lane and avoid placing trunks in hiding exits.
4. Move landmark prefab roots as a unit. Cabin, tent, campfire, tower, shelter, corrupted tree, noise patch and animal proxy are separate reusable prefabs in `Assets/HerFootsteps/Milestone7/Prefabs`.
5. Keep tower ramp ends adjoining their landings without a landing slab overhanging the approach: overhead overlap blocks the character even when NavMesh finds a route. Preserve a clear southern entrance and test walking to the deck after edits.
6. For hiding spots, move both Hidden position and Exit position with the shelter; keep both capsules clear. Cover must physically conceal the player's head/body. A hiding flag alone does not grant invisibility.
7. Resize/move calm, pressure and hallucination BoxColliders in the Inspector. They are triggers excluded from physical navigation. The player passive source's `suppressInside` list explicitly references the two calm boxes.
8. Move clue/supply/noise roots with their visible props. Clues have unique IDs; duplicate with a new ID. Supply quantities and all component values are editable.
9. Route nodes are visual design guides only. Moving a node does not repaint, move trees or rebuild anything. Sculpt/paint and move nearby props manually.
10. After collider/terrain changes, select `M7 Forest Blockout` and Bake its NavMeshSurface through the Inspector. Keep collection as Children / Physics Colliders. Save the scene and navigation asset, then run `Her Footsteps > Presentation > Validate Milestone 7`.

Nothing regenerates on scene load, Play Mode or compilation. The explicit
`Build Milestone 7` editor command refuses to run if the scene or output folder
already exists. Do not delete the scene/output folder to rebuild over designer edits.

## Review images

`Her Footsteps > Presentation > Export Milestone 7 Overview` renders the current
saved-layout scene to `Assets/Docs/LevelReviews`. The top-down raw and labelled
views share the original map's north-up orientation. Review lighting temporarily
disables fog and brightens ambient light so paths can be compared; normal gameplay
settings are restored afterward. These overview conditions are not gameplay lighting.

## Validation

Play Mode regression/integration: **17 passed, 0 failed**, including M1-M6 and
`Milestone7PlayTests.ForestMapNavigationAndIntegratedMechanics`.
The M7 integration checks saved-scene loading, HUD presence, safe-period gating,
noise publication, corrupted drain, clue recovery without inventory use, all seven
hiding entries/exits, complete routes from cryptid origin, physical character
ascent of the watchtower and Broken-to-investigation without teleporting the cryptid.
Navigation validation checks 92 route/landmark destinations, seven shelter clearances
and missing scripts. Final report: `Logs/Milestone7-navigation.txt`.
Play Mode XML/text: `Logs/Milestone7-playtests.*`.

These checks establish connectivity and component integration. The designer's
manual review still determines pacing, route readability, pressure balance,
resource placement and atmosphere across the whole forest.

## Manual playtest checklist

1. Open M7, press Play and click Game view. Verify the southern spawn, readable trail entrance, hotbar and composure dial. Collect the starting cache with E.
2. Walk/sprint with WASD/Left Shift; check stamina. Toggle F, let charge drain, select 1-5 or mouse wheel and use R. Verify full charge does not consume a battery.
3. During the first 60 seconds, make noise and lower composure using F3 debug controls; verify no hunt starts. Timer expiry alone should not start a hunt.
4. Follow the western loop to the tent and cabin. Enter the cabin through its doorway, inspect the NPC reservation and return via a different branch.
5. Find the central fire. At composure below 75, observe limited recovery and passive-loss suspension. Leave and verify passive loss resumes. The fire is not protection from an active hunt.
6. Hold E on a sister clue. Verify one recovery and discovery feedback, no hotbar item and no repeat recovery.
7. Reach the southeast tower and walk all three ramp flights without jumping. From the deck face northeast: identify the dead-tree region through fog without a clear view of the sister/cryptid at ground level.
8. Complete eastern Route A through the corrupted trees; compare drain inside/outside pressure boxes. Reach the northeastern outer approach and territory.
9. Return and follow Route B past the animal remains and branches. Test careful avoidance versus stepping on noise patches. Explore both northern skeleton branches; the skeleton has no scripted scream yet.
10. At Low/Broken composure, enter a phantom eligibility area and listen for directional footsteps. Check cooldown and recovery dismissal. Phantom footsteps must not wake the cryptid through NoiseChannel. F7 is the existing debug trigger; F8 tests wildlife near its location.
11. After the safe period, make significant noise within the cryptid's actual hearing range. Confirm warning, investigation at the sound location, sight-driven chase, loss of sight and bounded search. Noise across the entire map is not automatically audible.
12. Use each shelter with E, look around and hold Left Ctrl. Check concealed versus exposed angles and forced-breath consequences. Remain undetected for hunt disengagement; verify atmosphere returns.
13. During an active chase, inspect navigation around trees, cabin, tower and branches. Existing capture/restart should reload M7 at its southern spawn.
14. Stop Play Mode. Move a landmark/tree or edit terrain, save, reopen/recompile and verify your edits persist. Rebake navigation manually after geometry edits.

## File inventory

- Scene: `Assets/Scenes/Milestone7_ForestBlockout.unity`.
- Runtime additions: `Assets/HerFootsteps/Runtime/BlockoutRoute.cs`, `HallucinationArea.cs`.
- Editor additions: `Assets/HerFootsteps/Editor/Milestone7Setup.cs`, `Milestone7Landmarks.cs`, `Milestone7Review.cs`.
- Test addition: `Assets/HerFootsteps/Tests/PlayMode/Milestone7PlayTests.cs`.
- New asset folder: `Assets/HerFootsteps/Milestone7`: native terrain, terrain material, three terrain textures/layers, saved NavMesh, bone placeholder material and eight modular prefabs.
- Review PNGs: `Assets/Docs/LevelReviews/Milestone7_*`.
- Documentation: this file plus GAME_DESIGN, PROTOTYPE_SCOPE, GAMEPLAY_SYSTEMS, AI_DESIGN, LEVEL_DESIGN and DEVELOPMENT_PLAN.
- `ProjectSettings/EditorBuildSettings.asset`: M7 appended so existing restart loads the new scene.
- Unity-generated `.meta` files accompany the additions. The designer's `LevelReferences` folder is supplied input.

Stop at M7. No M7B, dialogue, rescue, scripted skeleton scream, final cryptid art,
new progression system or detailed environmental decoration is included.
