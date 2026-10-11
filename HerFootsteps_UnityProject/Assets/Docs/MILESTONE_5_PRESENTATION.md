# Milestone 5 presentation refinement

## Open and play

Open `Assets/Scenes/Milestone5_Presentation.unity` and press Play. References,
URP assets, Canvas, forest ground and NavMesh are saved; no manual setup is needed
for Editor play. Original M1-M5 test scenes and prefabs remain unchanged.
The original M5 test keeps its historical modal inventory; this new scene uses
the current hotbar design. For a standalone build, explicitly add this scene to
the desired build profile. Project-wide pipeline/build settings were not changed.

## What changed

The presentation scene derives from the saved M5 test scene. It reuses the player,
inventory, flashlight, noise, hiding/breath, composure, cryptid and hallucination
systems. It replaces only its copied test-room geometry and player-facing HUD.
There is no full Hunt Controller, safe-period timer, map, death or progression.
The cryptid remains the M4 always-active AI test and is initially farther along
the trail, screened by trees and fog. Broken composure still publishes events
without starting a hunt. A future controller must gate hunts behind its safe period.

`Assets/Visual_References/Forest_Atmosphere_Reference` and
`Cryptid_Atmosphere_Reference` were inspected before implementation. They establish
layered conifer silhouettes, cold fog, restricted views and an uncertain creature
outline. They were not used as textures or altered. The map/art direction remains
designer-owned and provisional.

## Controls and presentation

| Input | Behavior in presentation scene |
| --- | --- |
| WASD / mouse / Left Shift | Existing movement, look and stamina sprint |
| E | Existing world interaction, supplies, hiding entry/exit |
| Hold E | Discover sister clue, 1.5 seconds; release/look away cancels |
| F | Flashlight toggle |
| Left Ctrl while hidden | Existing hold-breath behavior |
| 1-5 | Select hotbar slot without interrupting movement/look |
| R | Use one selected item through PlayerInventory.TryUse |
| Tab | Reserved Map action; intentionally does nothing yet |
| Esc / click Game view | Existing release/recapture mouse control |
| F3 | Development overlay toggle (Editor/development builds only) |
| F4 / F5 while debug visible | -25 / +25 composure |
| F7 / F8 while debug visible | Existing gated trail/wildlife test requests |

The five-slot bottom-center Canvas hotbar uses reusable `HotbarSlotView`
components, muted translucent backgrounds, thin selection borders, a battery
Sprite and quantity text. Optional icons live on existing ItemDefinition assets;
unassigned icons use a short text placeholder. The selected item and R hint are
shown. Movement/camera stay active and the world never pauses for selection/use.
Mouse wheel selection was optional and is not included. No modal inventory,
discard control or map window is added to this scene; inventory removal remains
available through the existing API/historical test UI.

Gameplay HUD includes a tiny crosshair, contextual E/hold-E prompt, hold progress,
fading pickup/use/clue messages and relevant flashlight/stamina/breath values.
Composure transitions produce a brief subtle color wash and message instead of
a permanent numerical composure HUD. F3 enables the existing developer panels
and their event information. Its key binding and F4/F5 bindings are Inspector
strings on PresentationDebugMode. Debug does not pause AI or world time.
PresentationInput is a separate action asset: the shared historical input asset
is preserved. PlayerInputReader still exposes its modal-control hook for a future
map implementation, but the hotbar never uses it.

## Composure and clues (all values provisional)

- Capacity/start 100; High >=70, Medium >=35, Low >0, Broken at 0, unchanged.
- Exactly one global player ComposureRateSource: enabled, -0.5/s. No additional
  global passive source is generated.
- Campsite calm box: 8 x 4 x 7 m, center near (0, -30) in X/Z. Its reference
  suspends the passive source while the player's 0.9 m body sample is inside.
  Recovery: +3/s, ceiling 75, total session budget 20. Suspension continues when
  that budget is exhausted; leaving resumes -0.5/s. Other losses are not blocked.
- Cryptid proximity enabled: -2/s within 6 m, distance-only as in M5. Detection
  enabled: -8 on detection/reacquisition. Chase enabled: -4/s. Sources can stack.
  They read AI state and never change it. Scene values are Inspector overrides.
- Hiding neither restores composure nor suspends passive loss outside calm areas.
- `ClueDiscovery`: hold E for 1.5 s; immediately restore +20 once. It has a clue ID,
  C# Discovered event (clue/player payload) and Inspector UnityEvent. At full
  composure the clue still becomes discovered; no repeat/deferred reward.
  It works without an inventory component and cannot be blocked by a full bag.
- The presentation scarf replaces the inventory keepsake concept in this scene.
  DiscoveryComposureRecovery is disabled here, so there is no duplicate inventory
  reward. Historical keepsake definitions/tests remain available.
- The existing signed Apply API, environmental rate sources, frightening
  interactions and recovery effects remain reusable; no extra global danger
  volume is silently added to this forest.

## Environment and assets

Imported the **supplied** `Conifers_URP.unitypackage` using its intended Unity
import API. Existing BIRP assets/materials remain untouched. Added package content:

- `Assets/Forst/Conifers [BOTD]/Render Pipeline Support/URP/`
- `Assets/Forst/CTI Runtime Components/CTI Runtime Components URP 14plus/`

The scene uses all four `PF Conifer ... BOTD URP` types: Tall, Medium, Bare and
Small. CTI URP bark/leaves shaders and source textures are reused. Small conifers
scaled down supply understory. Existing package LODs are retained. No pipeline
conversion or new custom tree shader was performed. The package's known issue
document mentions old Unity 2022 billboard shadow binding problems; shader and
scene validation in this project's Unity 6000.4.10f1 are recorded below. A final
standalone performance/art pass remains manual.

New ground is a saved uneven mesh (about 76 x 80 m, 100 x 100 cells) with a
subdued soil texture and lighter worn-route tint. Natural edge stones form the
perimeter. These ground, rock, camp and silhouette props are placeholders because
the supplied content contains conifers, not a complete ground/rock/camp/animal kit.
No runtime world generation is introduced: the editor uses seed 510 once and
saves editable placements. Delete/move/add objects normally to redesign it.

Main route X/Z anchors: (0,-30), (-3,-18), (0,-5), (5,10), (0,26).
Optional western loop: (-3,-18), (-15,-8), (-16,9), (-8,17), (5,10).
Starting campsite: canvas shelter, bedroll, seat and cold fire ring. There is no
bright artificial camp light. Battery packs (two batteries each) near (2,-30),
(-15,-5), (2,20). Sister scarf near (-15,9). Hiding root/rock shelter at (-12,7),
exit near (-12,4). Dry-branch NoiseObstacle hazards near (-2,-11) and (3,8).
Cryptid begins near (5,21). Actual Y values follow the ground.

Trunk colliders and solid rocks supply physical/visual occlusion. Undergrowth
without colliders is visual dressing and does not grant magical hiding. Rock
mesh colliders match stretched visuals. The shelter uses the existing HidingSpot
and has a free capsule position and exit. Navigation uses an independent saved
NavMeshSurface, collider geometry and 0.12 m voxel size. After layout changes,
rebake this scene's surface and recheck paths/hiding clearance. No other scene's
NavMesh is replaced.

## Lighting and fog (provisional)

`ForestAtmosphere` on the Atmosphere root exposes fog color/density, flat ambient
color and moon intensity. Scene Light/Volume/flashlight controls remain directly
editable. The component applies only while its scene is loaded; the new scene
must be the active lighting scene when editing additive setups.

| Setting | Initial value |
| --- | --- |
| Fog | Exponential Squared, density 0.045 |
| Fog/background RGB | 0.085, 0.125, 0.14 |
| Ambient RGB | 0.16, 0.21, 0.23 |
| Moon intensity/color | 0.38; 0.55, 0.72, 0.77; soft shadows |
| Color grading | Saturation -24, contrast +12, exposure +0.1 |
| Vignette | Intensity 0.28, smoothness 0.6 |
| Flashlight scene override | Starts on; range 19 m, outer cone 46°, inner 28°, intensity 35 |
| Flashlight color | 0.85, 0.9, 0.82; soft shadows |
| Flashlight resource | Unchanged 100 capacity, 2/s active drain, +40 per inventory battery |

Fog is ordinary URP-compatible distance fog, not a volumetric fog system.
The flashlight reveals a local corridor and does not affect cryptid sight rules.
Fog is visual atmosphere; the AI continues using its explicit M4 sight ranges.

Quiet looping ambience uses the existing
`Assets/Audio/333221__hdfreema__night-crickets-back-porch.aiff` by hdfreema,
volume 0.12, 2D loop. Wildlife failure retains
`618112__nachtmahrtv__woman-scream.wav` by NachtmahrTV at 0.35 volume.
Both are CC0 according to the preserved `Assets/Audio/_readme_and_license.txt`.
No new audio was downloaded and no source attribution/license file was altered.

## Hallucinations and cryptid visuals

Existing FalseTrailHallucination now owns weathered stakes and faded marker
planks near (3,0), pointing away from the real route. Wildlife at (-7,13) is a
rough, dark quadruped with a branch-like crown in forest cover. Its reaction
target remains a distinct child transform, independent of art. The cryptid has
a narrow, long-limbed branch-crowned silhouette instead of the visible capsule;
the original visual renderer is disabled only on this scene instance. No mesh
asset determines AI state, and no final creature model/animation was created.

Existing gates are preserved: minimum Low, 12 m activation range, 8 s duration,
20 s cooldown. Wildlife still requires a clear view within 40° half-angle to
activate, then 0.4 s continuous flashlight illumination within a 4 s reaction
deadline. Failure emits one Scream through the original NoiseChannel at the
player's location (intensity 1.2, range 18). Recovery above Low removes active
hallucinations without failure. No permanent geometry changes or scripted
knowledge is given to the cryptid. These values and silhouettes remain provisional.

## Manual playtest

1. Open the presentation scene, Play, and maximize Game view if needed. Verify
   no giant debug panels/technical labels, five empty hotbar slots, subtle opening
   hint and flashlight on. Check readable ground/trunks with F both on and off.
2. Walk around camp and locate the seat supply. Tap E: two batteries stored,
   icon/count appears. Press 1-5 while walking/looking. Selection changes without
   cursor release or movement lock. R recharges and consumes one only if needed;
   another R at full charge keeps the item. Tab opens nothing.
3. Follow the main path and western branch. Check footing, edge barriers, tree
   collision and flashlight range. Step on/around dry branches; F3 should show
   noise when visible, and nearby cryptid hearing should respond normally.
4. Use F3 to inspect composure. In camp passive is suspended. Outside camp it
   drains -0.5/s; return at reduced value and observe limited +3/s up to 75.
   Exhaust the 20-point recovery budget, leave/re-enter and verify it does not
   refill; passive remains suspended inside. Hide outside camp: no free recovery.
5. At the western fallen log, aim at the scarf. Hold E briefly/release or look
   away: discovery does not complete. Hold for 1.5 s: one subtle confirmation and
   +20 composure, no inventory slot used. Revisit: no repeated reward. Test again
   in a new session with a full inventory.
6. Enter/leave the root shelter with E; manage Left Ctrl breath as before. Check
   terrain and cover do not block entry/exit. Generate noise then hide and let
   the cryptid investigate/search; it must not gain scripted hiding knowledge.
7. Approach the cryptid: verify silhouette/occlusion, normal sight/Chase behavior
   and configured pressure. Lose sight: last-known investigation/search still
   works. There is no death or automatic hunt when Broken.
8. Use F3, then F4 to reach Low. Near the false-trail origin press F7. Confirm
   environmental markers appear/expire; F5 recovery removes them and cooldown
   prevents rapid retriggering. Debug requests keep state/range gates.
9. Near wildlife, face it and press F8 (or allow automatic activation at Low).
   Aim F within four seconds: apparition dismisses. Fail on a later attempt:
   one scream/noise snapshot, normal cryptid response. Cover/empty light should
   prevent dismissal. No floating capsules or technical text should appear.
10. F3 off: all developer panels disappear. Continue moving, using batteries,
    hiding and exploring. Check screen readability at your normal resolution.
11. Smoke-test the preserved original M5 scene and earlier milestone scenes.
    They retain their historical controls/presentation and mechanics.

## Verification and file report

Scene generated successfully with a separate ground/NavMesh/player variant.
Material validation: no shader errors; zero missing scripts; exactly one player
passive source. Automated tests on October 10, 2026: **41 Edit Mode passed and
14 Play Mode passed, zero failures**. This includes all existing milestone tests,
safe-volume suspension/resumption, one-time discovery without inventory, and a
saved-forest integration test covering default gameplay/debug state, five slots,
Tab reservation, battery use, hold cancellation/completion, free hiding entry/exit
and complete NavMesh paths to the campsite, branch and main trail anchors.
Local test evidence is in ignored `Logs/Presentation-*` and the existing shared
runner's legacy `Logs/Milestone1-tests.*` / `Milestone1-playtests.*` filenames.

The complete created/modified path list is `MILESTONE_5_PRESENTATION_FILES.md`.
Key new runtime components: ClueDiscovery, HotbarSlotView, PresentationHud,
PresentationDebugMode, ForestAtmosphere. Targeted shared extensions:
ComposureRateSource safe-volume references, optional ItemDefinition icon,
optional hotbar/Map actions in PlayerInputReader, configurable NoiseObstacle
prompt and Milestone5Hud control hint. Editor construction/validation is separate
from runtime. UI assembly references use the already installed Unity UI package.
New prefabs: PresentationPlayer (variant) and HotbarSlot. New content is under
`Assets/HerFootsteps/Presentation/`; imported URP content stays in its vendor folders.

No final asset/design approval is implied by this pass. Missing final creature,
wildlife, ground/rock and campsite art limits fidelity to the references. This is
a playable presentation test whose layout, fog, lighting and balance remain
available for designer iteration; it stops before the next milestone.
