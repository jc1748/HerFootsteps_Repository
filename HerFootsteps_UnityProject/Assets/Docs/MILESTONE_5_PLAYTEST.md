# Milestone 5 - Composure, inventory and hallucinations

## Start

Open `Assets/Scenes/Milestone5_Test.unity`, or use Her Footsteps > Milestone 5 >
Open Test Scene, and press Play. This is a separate copy of the working M4
environment with its own player variant and baked NavMesh. M1-M4 scenes/prefabs
are preserved. No manual component wiring or package installation is required.
Do not run Create Test Scene again: its overwrite protection is intentional.
For a standalone build, include M5 as the startup scene in the active build
profile; this milestone does not change the project's build profile.

## Architecture and integration

- `PlayerComposure` owns a numerical resource and four states. `Apply(amount,
  source)` returns the actual signed change. Local Changed/StateChanged/
  BrokenEntered events and the reusable `ComposureChannel` publish the player,
  previous/current value, previous/current state and source. Broken is emitted
  once per transition into Broken, including a later re-entry after recovery.
  Neither component calls the cryptid or starts a hunt. The future Hunt
  Controller must enforce the configurable early-game safe period itself.
- `ComposureRateSource` applies a signed rate with an optional oriented box,
  enable switch, recovery ceiling and lifetime-in-session budget. Leaving or
  disabling a source does not replenish its budget. `CryptidComposureSource`
  observes the existing AI's distance, detection and Chase state. Sources can
  stack when deliberately enabled. Proximity currently uses distance regardless
  of occlusion; this is provisional. `ComposureInteractable` supplies finite
  E-triggered changes with cooldown. No hiding-based composure recovery exists.
- `PlayerInventory` owns slots and atomic acceptance/use/removal. `ItemDefinition`
  ScriptableObjects define names, descriptions, maximum stacks, consumability,
  removal permission and an optional `ItemUseEffect`. `InventoryPickup` uses
  the existing PlayerInteractor E contract. A pickup stays in the world if its
  entire quantity cannot fit. No partial pickup, weight, crafting or equipment.
  A successful effect consumes one only when marked consumable. Remove discards
  an item without creating a world drop. Reducing capacity at runtime retains
  existing occupied overflow slots instead of deleting items; new acceptance
  uses the reduced capacity.
- `BatteryItemEffect` calls the existing flashlight's clamped RestoreBattery.
  Full batteries do not waste inventory items; partial recharge may discard
  excess charge. It does not turn the light on. The original BatteryPickup
  component/prefab and previous scenes remain direct-refill historical tests;
  every battery in M5 uses InventoryPickup.
- `ComposureItemEffect` is an optional consumable effect. The separate
  `DiscoveryComposureRecovery` adapter grants one recovery on the first addition
  of the configured keepsake. Inventory does not depend on composure. Discovery
  at full composure still counts as discovery, with no deferred reward.
- `InventoryView` provides temporary IMGUI selection, descriptions, Use/Remove,
  empty slots and debug controls. Input's modal state gates movement, look,
  interactions, flashlight toggling and hold-breath input. It does not clear
  hiding's separate movement lock. **The world is not paused**: AI, rates,
  flashlight drain and breath recovery continue; opening inventory releases
  held breath. Tab/Esc/Close returns cursor capture when the app is focused.
- `HallucinationEvent` owns composure gating, automatic/manual activation,
  duration, cooldown, an overridable activation condition and end reasons.
  Future types derive from it. Each event is internally identifiable as a
  hallucination and owns only a separate visual root. The test visuals have no
  colliders and are outside the baked NavMesh hierarchy.
- `FalseTrailHallucination` shows misleading markers. `WildlifeHallucination`
  displays a capsule threat, requires view and clear sight to start, and checks
  active flashlight, beam distance/cone and occlusion for dismissal. Continuous
  illumination must meet the reaction time requirement. Failure emits one
  NoiseKind.Scream at the player's position, with the hallucination as Source
  and player as Instigator. Existing cryptid hearing responds normally; it
  receives a sound snapshot, not scripted knowledge of the player/hiding spot.
  Recovery above minimum severity immediately removes either event without a
  scream. Every end, including recovery/disable, starts cooldown. No real
  environment destruction, automatic hunt, attack, death or reward on dismissal.

## Provisional Inspector tuning

All values below are test defaults, not final balance. Existing M1-M4 tuning is
unchanged; consult their playtest guides for movement/noise/breath/AI values.

| Component/source | Default |
| --- | --- |
| Composure capacity / starting value | 100 / 100 |
| High lower bound | 70 (70-100) |
| Medium lower bound | 35 (35 to below 70) |
| Broken upper bound | 0; Low is above 0 and below 35 |
| Passive danger source | OFF, -0.5/s, unlimited budget, no volume |
| Cryptid proximity | OFF, within 6 m, -2/s |
| Cryptid pursuit | OFF, -4/s while Chase |
| Cryptid detection | OFF, -8 on first observed detection/reacquisition |
| Danger volume | ON, -3/s, unlimited budget; 3 x 2 x 3 m box |
| Safe volume | ON, +5/s, stops at 75, total recovery budget 30; 3 x 2 x 3 m |
| Fright interaction | -25, four uses, 1 s cooldown |
| Recovery interaction | +25, two uses, 1 s cooldown |
| Generic ComposureInteractable default | -25, three uses, 1 s cooldown |
| Generic ComposureRateSource default | -3/s, ceiling 75, budget -1 = unlimited |
| First keepsake discovery | +20 once per play session |
| Recovery token | +15/use, kept at full composure |
| Inventory | 5 slots, configurable; starts empty |
| Battery | Stack 3, consumable, removable, +40 charge/use |
| Keepsake | Stack 1, persistent, cannot remove, no use effect |
| Stone | Stack 1, removable, no use effect |
| Recovery token | Stack 2, consumable, removable |
| Both hallucinations | Automatic ON, minimum Low, range 12 m, duration 8 s, cooldown 20 s |
| Wildlife activation | View required, 40-degree half-angle, clear camera ray |
| Wildlife reaction | 4 s deadline, 0.4 s continuous flashlight illumination |
| Wildlife light check | Existing beam's range/spot angle, active charged light, clear ray |
| Wildlife occlusion mask | Physics.DefaultRaycastLayers; triggers and self/player ignored |
| Failure noise | Scream, intensity 1.2, range 18 m, at player position |
| Optional failure playback | Existing woman-scream clip, volume 0.35, one-shot at player |

The flashlight's existing 100 capacity, 2/s active drain, F toggle and beam
configuration remain unchanged. Debug +/-25 buttons are deliberately unlimited
and not gameplay recovery resources. Lower-threshold values are clamped into
order by the resource; tune valid High > Medium > Broken values in Inspector.
Hallucination debug triggers still require state/range/view/cooldown; they do
not bypass composure control. Automatic toggles let each example be tested alone.

## Scene contents and debug feedback

M4 blockout, movement/interactions, batteries, debris, hiding spot and cryptid
remain. New test area is on the west (negative X) side near the spawn:

- Bench centered (-6, 0.45, -10): battery pack x4, battery x1, keepsake x1,
  stone bundle x3 (requires three empty slots), recovery token stack x2.
- Fright console (-4, 0.8, -13); recovery console (-9, 0.8, -10).
- Orange danger floor at (-7, 0, -3); cyan safe floor at (-7, 0, -13).
- False trail event origin (-7, 0, 0); wildlife origin (-7, 0, -6).

New HUD shows value/state, currently applying rate sources, last source,
transition, Broken flag/count, inventory status and hallucination timers/status.
Tab contains source switches, +/-25 and independent event controls. Existing
cryptid state/FOV/path/memory gizmos and NoiseChannel history/range gizmos remain;
Scream is red. Select volume BoxColliders to see their bounds. Placeholder event
visuals and noise/debug feedback are temporary. Failure uses the already supplied
`Assets/Audio/618112__nachtmahrtv__woman-scream.wav` by NachtmahrTV as placeholder
audio (Freesound 618112, Creative Commons 0 per the unchanged
`Assets/Audio/_readme_and_license.txt`). No new downloaded audio or final art is
introduced. Playback is optional and independent of the mechanical noise event;
no full hallucination VFX/audio system is implemented.

## Input

WASD move; mouse look; Left Shift sprint; E interact/hide/exit; F flashlight;
Left Ctrl hold breath while hidden. Tab opens/closes inventory. Mouse clicks
select slots, Use, Remove and debug controls. Esc closes inventory (or releases
gameplay cursor normally); click Game view to recapture when necessary.
F7 requests false trail, F8 requests wildlife while gameplay control is captured.

## Manual playtest sequence

1. Open M5 and Play. Confirm 100/High, no active depletion, empty five-slot
   inventory and normal walking/sprinting/stamina, E, F, debris noise and AI.
2. Walk west to the bench. E the four-battery pack. Tab: expect quantities 3
   and 1 in two slots, three empty slots, battery description and Use button.
   Use at full charge: count stays four. Close, turn F on and wait; reopen and
   Use: one consumed, charge clamped to 100. Full charge refuses the next use.
3. Pick up three stones to fill the remaining slots. Attempt another non-stack
   pickup: it stays in the world with a failure status. Add a battery into an
   existing partial stack if it still fits. Remove a stone and retry a pickup.
   Restart if needed to test rejecting the whole three-stone bundle when fewer
   than three slots are free. Select every item and inspect its description.
4. While inventory is open, hold WASD/Shift, move mouse and press E/F/Ctrl.
   Player/look/world interaction should be blocked, cursor free; existing light
   and AI continue. Close and confirm control resumes. Hide, open/close Tab and
   confirm player stays hidden and movement locked until E exits.
5. At a clear position, use Tab's -25 buttons or the fright console. Observe
   100 -> 75 High -> 50 Medium -> 25 Low -> 0 Broken. Broken count rises once;
   further loss at zero does not repeat it. No new hunt or AI state override.
   Recover, then return to zero to see a new Broken entry.
6. Enable passive loss alone and verify -0.5/s; turn it off. Enter/leave the
   orange danger area: only inside applies -3/s. Disable automatic hallucinations
   while isolating these checks if desired.
7. At low composure, enter the cyan safe area: +5/s up to 75 or its 30-point
   budget, whichever comes first. Leave/re-enter: budget does not refill. Test
   the recovery console twice (+25 each, 1 s apart), then exhaustion. A full
   resource should not consume an interaction use. Hiding alone restores none.
8. At reduced composure, pick up the keepsake: +20 once and persistent inventory
   item, no Remove. Use a recovery token: +15 and consume one. Full composure
   refuses token use. This is an integration sample, not clue progression.
9. Enable cryptid proximity alone; within 6 m observe -2/s even behind cover.
   Then test pursuit and detection separately: sight/Chase apply their selected
   effects; loss of sight stops pursuit loss. Existing investigation/search/
   disengagement and hiding/breath-noise behavior should remain intact.
10. Disable automatic wildlife. Set Low (e.g. 25), approach within 12 m of trail
    origin and press F7. Markers appear for 8 s then vanish. Repeated requests
    during 20 s cooldown fail. Recover to at least 35 while active: markers
    vanish immediately; real blocks remain. High/Medium reject activation.
11. Disable automatic trail and enable/request wildlife while Low. Stand south
    of the wildlife origin (-7, 0, -6), within range, looking toward it with a
    clear view. With F off, let it appear; toggle F and aim continuously for
    0.4 s within its 4 s deadline. It dismisses with no Scream. Check away-facing,
    blocked and empty-battery beams do not count as successful illumination.
12. Wait cooldown (or restart), repeat with F off or aim away until deadline.
    Exactly one red Scream event should appear at your location. If in hearing
    range, cryptid investigates that snapshot. Move to cover, hide and manage
    breath; it must not acquire special knowledge from the hallucination.
13. During active wildlife, recover to Medium using debug: it cancels without
    Scream. Repeated F8 presses cannot bypass state/range/view/cooldown.
14. Reopen M1-M4 scenes for a short movement, interaction, direct-battery,
    flashlight/noise, hiding/breath and cryptid regression smoke test.

## Verification

Unity generated the scene and assets successfully. Edit Mode suite: 39 passed,
0 failed (34 existing and 5 new). Initial Play Mode suite: 11 passed, 1 failed.
All three M5 tests passed: stored battery/full-charge protection with modal hiding
lock, flashlight dismissal/recovery without scream, and failure producing one
Scream and normal cryptid snapshot investigation. All M2-M4 tests passed.
The M1 test failed on an unrelated installed Unity AI Assistant `NoSubscription`
log (same known M4 harness issue), not a gameplay assertion. No package removal
or broad log suppression was introduced. Final gameplay/balance evaluation
remains manual.

The menus Run All Milestone Edit Mode Tests / Play Mode Tests run the shared
assemblies. The runner retains legacy output names `Logs/Milestone1-tests.*`
and `Logs/Milestone1-playtests.*`; milestone-specific copies are kept locally
under `Logs/Milestone5-*`. Logs/Library/Temp test caches are ignored by Git.

## Design review

No approval is needed to use this authorized prototype. Designer review is
still needed before making these test values final: depletion/recovery balance,
slot/stack economy, inventory remaining live/releasing breath, the distance-only
proximity rule, hallucination severity/timing/reaction, and any final audio/art.
Future safe-period timer start/trigger handling and composure/hallucination
behavior during it remain unresolved. No timer policy is silently selected here.

## File inventory

New runtime files under `Assets/HerFootsteps/Runtime/`:
PlayerComposure.cs, ComposureChannel.cs, ComposureRateSource.cs,
CryptidComposureSource.cs, ComposureInteractable.cs, ItemDefinition.cs,
ItemUseEffect.cs, BatteryItemEffect.cs, ComposureItemEffect.cs, PlayerInventory.cs,
InventoryPickup.cs, DiscoveryComposureRecovery.cs, InventoryView.cs,
HallucinationEvent.cs, FalseTrailHallucination.cs, WildlifeHallucination.cs,
Milestone5Hud.cs. Each has a Unity .meta file.

New editor/tests: `Assets/HerFootsteps/Editor/Milestone5Setup.cs`,
`Assets/HerFootsteps/Tests/EditMode/Milestone5Tests.cs`,
`Assets/HerFootsteps/Tests/PlayMode/Milestone5PlayTests.cs`, and their .meta files.

New saved content: `Assets/Scenes/Milestone5_Test.unity`,
`Assets/HerFootsteps/Prefabs/Milestone5Player.prefab` (M3 player variant),
`Assets/HerFootsteps/Prefabs/InventoryBattery.prefab`, and their .meta files.
New `Assets/HerFootsteps/Settings/Milestone5/` folder/.meta contains:
ComposureChannel.asset, BatteryEffect.asset, RecoveryEffect.asset, Battery.asset,
SisterKeepsake.asset, TestStone.asset, RecoveryToken.asset, Milestone5NavMesh.asset
and each .meta. Areas, test consoles and hallucination visual/event objects are
scene instances; reusable components do not depend on their layout or art.

Modified runtime: PlayerInputReader.cs (optional actions/modal input),
FirstPersonMotor.cs (modal movement lock), PlayerInteractor.cs (modal interaction
guard), PrototypeHud.cs (hide capture prompt during inventory), NoiseChannel.cs
(append Scream category), Milestone2Hud.cs (Scream color).
Modified `Assets/InputSystem_Actions.inputactions` (Tab/F7/F8 actions/bindings).
Modified docs: GAME_DESIGN.md, GAMEPLAY_SYSTEMS.md, PROTOTYPE_SCOPE.md,
AI_DESIGN.md, DEVELOPMENT_PLAN.md. Added this guide and its .meta.
