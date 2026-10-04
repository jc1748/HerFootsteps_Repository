# Milestone 3 - Hiding and hold breath

Open `Assets/Scenes/Milestone3_Test.unity` and press Play. This saved scene
copies the saved Milestone 2 environment. All component references are assigned.
The temporary blue shelter is near the start at (3, 0, -8), to the right.
Approach its front marker, aim at it, and tap E. Tap E again to leave,
even while looking away. Hold **Left Ctrl** to hold breath while hidden.
F still toggles the flashlight. WASD, mouse look, Shift and Escape/click retain
their established meanings. Escape releases input; it does not pause resources.

## Confirmed implementation

- HidingSpot extends the existing Interactable. Child colliders can belong to
  any replacement cover model. Assign a player-feet hiding marker and optional
  exit marker; without an exit marker, the player returns to their entry position.
- PlayerHiding exposes IsHidden, ActiveSpot and HiddenChanged. A spot has one
  occupant. Entry snaps the player root to the marker while preserving view
  orientation; the camera and flashlight follow their existing player hierarchy.
- Movement and gravity are locked while hidden; mouse look retains the existing
  85-degree pitch limit and unrestricted yaw. Sprint is stopped and stamina
  recovers normally. Teleports do not publish movement samples/footstep noise.
- Entry checks capsule clearance. Exit tries the assigned marker, then the entry
  position. If both are blocked, the player stays hidden with an exit-blocked
  message. Disabling the player hiding component or spot releases occupancy and
  restores the entry position, so lifecycle cleanup never leaves movement locked.
  Keep return points clear, including for this forced lifecycle cleanup.
- Markers are root/feet positions, not camera positions. This prototype assumes
  an upright player with unit root scale, matching the existing player prefab.
- While hidden, the interactor targets the occupied spot independently of view
  direction. E cannot also use a nearby pickup or switch while inside.
- The flashlight is never switched off by hiding; F and active-only battery
  drain continue. No renderers are disabled and IsHidden grants no invisibility.
- Holding breath drains its own resource, independently of sprint stamina.
  Releasing input, leaving cover or releasing cursor stops holding. Breath
  recovers inside or outside cover. Leaving/re-entering does not reset capacity
  or bypass exhaustion recovery.
- At zero breath, holding stops and one ForcedBreath event is emitted. Another
  hold requires the cooldown to expire, some recovered breath, and releasing the
  input at least once. Keeping Ctrl held through recovery cannot auto-restart.
- Normal hidden breathing emits periodic Breathing events. Successful holds
  suppress new breathing events; existing debug history remains visible until
  expiry. There is no extra gasp for an ordinary early release.
- Both categories use the existing PrototypeNoise asset / NoiseChannel event
  payload and Milestone2Hud history/range gizmos. Green = Breathing; magenta =
  ForcedBreath. These are gameplay signals, not literal audio volume. No audio
  clips, hearing rules, detection, cryptid AI or hunts are implemented.

## All new provisional Inspector values

All values below are temporary playtest defaults, not final design decisions.
Existing movement, light, stamina and noise values remain as documented in
`MILESTONE_1_PLAYTEST.md` and `MILESTONE_2_PLAYTEST.md`.

| Component | Setting | Provisional value |
| --- | --- | --- |
| PlayerBreath | Breath capacity | 100 units |
| PlayerBreath | Depletion per second | 20 units (5-second full hold) |
| PlayerBreath | Recovery per second | 25 units (4 seconds empty to full) |
| PlayerBreath | Forced recovery seconds | 3 seconds, plus input release requirement |
| PlayerBreath | Breathing interval | 2 seconds of continuous normal hidden breathing |
| PlayerBreath | Breathing intensity / range | 0.1 / 2 m |
| PlayerBreath | Forced intensity / range | 0.8 / 12 m |
| HidingSpot | Hiding position | Local (0, 0.05, 0) |
| HidingSpot | Exit position | Local (0, 0.05, -2) |
| TestHidingSpot scene instance | Position | World (3, 0, -8) |

New references: PlayerInteractor.hiding -> PlayerHiding (optional on earlier
milestone prefabs); PlayerBreath -> input, hiding, existing noise channel;
Milestone3Hud -> hiding and breath; HidingSpot -> hidden and exit transforms.
There are no new tuning fields on the motor, interactor or Milestone3Hud.

## New scene objects and prefabs

- `Milestone3Player.prefab`: variant of Milestone2Player, adding PlayerHiding,
  PlayerBreath and Milestone3Hud, plus the interactor's optional hiding reference.
- `TestHidingSpot.prefab`: HidingSpot root; Hiding Position and Exit Position
  marker children; Left Cover, Right Cover, Rear Cover, Roof and an E marker.
  Cover/marker cubes have renderers and solid BoxColliders and reuse the existing
  M1_Obstacle material. No new art, material, audio, layers or packages.
- `Milestone3_Test.unity`: copied Milestone 2 blockout with the new player variant
  and one TestHidingSpot instance. Existing battery and debris prefabs are reused.

## File inventory

Created gameplay scripts under `Assets/HerFootsteps/Runtime/`:
`HidingSpot.cs`, `PlayerHiding.cs`, `PlayerBreath.cs`, `Milestone3Hud.cs`.

Modified gameplay scripts in that directory:
`FirstPersonMotor.cs` (movement lock and teleport), `PlayerInteractor.cs` (occupied
spot targeting), `PlayerInputReader.cs` (hold action), `NoiseChannel.cs` (appended
noise categories), `Milestone2Hud.cs` (new category colors).

Also modified `Assets/InputSystem_Actions.inputactions` (Player/HoldBreath on
Left Ctrl; existing Crouch action remains unused). Created the scene and two
prefabs listed above. All new Unity assets/scripts include .meta files.

Editor/test files: new `Assets/HerFootsteps/Editor/Milestone3Setup.cs`, new
`Assets/HerFootsteps/Tests/EditMode/Milestone3Tests.cs`, new
`Assets/HerFootsteps/Tests/PlayMode/Milestone3PlayTests.cs`; adjusted
`Assets/HerFootsteps/Tests/EditMode/Milestone1Tests.cs` so the preserved M1
prefab may have a null optional hiding reference. Documentation: this guide,
`GAMEPLAY_SYSTEMS.md` and `DEVELOPMENT_PLAN.md`.

## Setup and manual playtest

No manual wiring or package installation is required. Open the saved scene or
use Her Footsteps > Milestone 3 > Open Test Scene. The Create Test Scene (once)
menu refuses to overwrite existing M3 assets. No build profile changes or
packaged executable are included.

1. Enter Play Mode; walk to the shelter at x=3, z=-8. Aim at its front marker
   from within 2.5 m and tap E. Confirm Hidden=True and a snap inside the cover.
2. Hold WASD and Shift: no translation, sprinting or new footstep events. Look
   around: mouse look still works. Tap E while facing away to exit; check walking
   and sprinting resume. Repeat entry/exit several times and check clearance.
3. Enter with the light ON: it must stay on and lose battery. Toggle F inside.
   Exit with the light OFF: it must stay off. Check empty-battery behavior.
4. While hidden, wait for green Breathing events (0.1 / 2 m every 2 seconds).
   Hold Ctrl for about 2 seconds: Holding=True, breath falls, no new breathing
   events. Release: Holding=False, recovery begins and periodic noise resumes.
5. From full breath, hold Ctrl for about 5 seconds. Confirm one magenta
   ForcedBreath event (0.8 / 12 m), zero breath, Holding=False and recovery timer.
   Keep Ctrl held past the 3-second cooldown: holding must not auto-restart.
   Release and press again. Also release/repress during cooldown: it must block.
6. Exhaust breath, exit/re-enter immediately: cooldown and remaining capacity
   must persist. Outside cover, Ctrl must not drain breath or emit breathing.
7. Hold breath and press Escape or switch focus: holding stops; return/click to
   resume input. Check actual keyboard feel and no accidental E/F on recapture.
8. Enable Gizmos in Scene view during Play. Compare green 2 m and magenta 12 m
   spheres and HUD event ages; old events remain briefly during a silent hold.
9. Recheck M2 battery pickups, walk/sprint noise, debris interaction/traversal,
   idle/wall silence, stamina exhaustion, and the original toggle targets.
   Open M1 and M2 separately and check their original gameplay remains intact.
10. Stop/restart Play Mode: player starts outside, full breath, no cooldown,
    full/off flashlight, restored pickups and empty event history. Assess shelter
    visibility, camera clearance, HUD readability and all provisional timings.

Final gameplay feel, real keyboard/focus behavior and visual concealment remain
designer playtest decisions. The shelter is placeholder geometry only. Stop at
Milestone 3: no Milestone 4 or cryptid behavior is authorized by this guide.

## Automated verification

Unity 6000.4.10f1 compiled and generated the saved scene/player/spot assets.
Edit Mode: 26 passed, 0 failed (all three milestones). The initial run exposed
two harness assumptions: all M1 object references required, and OnDisable running
in Edit Mode. These were corrected; actual lifecycle behavior is tested in Play
Mode. The M3 saved-scene integration test passed on both Play Mode runs.
The first complete Play Mode run was 4 passed / 1 failed; the one retry was
3 passed / 2 failed. Failures were the existing M1 saved-scene and (on retry)
M2 saved-scene tests reporting an unexpected Unity AI package NoSubscription
log, not a failed gameplay assertion. Both synthetic keyboard tests passed.
No suppression or package changes were added to hide that external diagnostic.
Final hands-on M1/M2 regression remains necessary; this is not a fully green
Play Mode suite. Per the requested test limit, no further harness debugging.

Evidence in ignored `Logs/`: `Milestone3-setup.txt`, `Milestone3-editmode.xml/.txt`,
`Milestone3-playmode-initial.xml`, `Milestone3-playmode-final.xml/.txt`.
`git diff --check` passed. Saved M1/M2 scenes and existing prefabs have no changes.
Visual editor preview confirmed the shelter and all three HUD panels render
without overlap at the existing Full HD Game-view setting. The M3 scene is left
open outside Play Mode. Real keyboard/focus interaction and gameplay feel remain
for manual validation; no packaged Windows build was tested.

Edit tests cover movement lock/look/light compatibility, breathing suppression,
recovery, forced noise payload, cooldown/release enforcement, exit/re-entry,
blocked exits, cleanup, prefab references and Left Ctrl binding. Play integration
loads the saved M3 scene, raycasts the marker, enters via interaction, verifies
locked movement and breath exhaustion, leaves while looking away, and disables
the spot to check movement restoration. Existing M1/M2 tests also run.
