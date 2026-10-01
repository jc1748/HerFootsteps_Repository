# Milestone 2 - Flashlight, Resources, and Noise Test

## Start and scope

Open `Assets/Scenes/Milestone2_Test.unity` and press Play. All references
are assigned. F toggles the flashlight; E uses the existing interaction
system for batteries and debris. WASD, mouse look, Left Shift sprint,
Escape/click cursor handling, and stamina are unchanged.

This scene copies the current saved Milestone 1 blockout, preserving
designer edits. It uses a variant of the existing player prefab. The
original scene and prefab are not rewritten by the Milestone 2 setup.
The new scene has dimmer lighting, two battery pedestals/pickups, and one
debris patch; the layout and art remain temporary and designer-controlled.

No composure, hallucinations, reality checks, cryptid AI, hunts, hiding,
clues, progression, or full inventory are implemented. No imported audio
clips or tree assets are used. Mechanical noise events are separate from
audio playback.

## Provisional Inspector tuning

| Component / location | Setting | Value |
| --- | --- | --- |
| PlayerFlashlight / Milestone2Player | Battery capacity | 100 units |
| PlayerFlashlight | Drain while on | 2 units/second (50 seconds from full) |
| PlayerFlashlight | Starts on | False; starts with full charge |
| BatteryPickup / TestBattery | Restore amount | 40 units (up to 20 seconds of light) |
| Flashlight Beam / player camera child | Range | 18 m |
| Flashlight Beam | Intensity | 8 Unity Light intensity units |
| Flashlight Beam | Outer / inner spot angle | 55 / 30 degrees |
| Flashlight Beam | Shadows | Soft |
| MovementNoiseEmitter / player | Walking intensity / range | 0.2 / 4 m |
| MovementNoiseEmitter | Sprinting intensity / range | 0.6 / 10 m |
| MovementNoiseEmitter | Walk / sprint stride | 1.5 / 2 m |
| MovementNoiseEmitter | Minimum emission interval | 0.15 seconds |
| MovementNoiseEmitter | Minimum actual horizontal speed | 0.1 m/s |
| NoiseObstacle / TestDebris | Intensity / range | 1.0 / 18 m |
| NoiseObstacle | Shared traversal/interaction cooldown | 2 seconds |
| NoiseObstacle | Minimum traversal speed | 0.1 m/s |
| Milestone2Hud / player | History size / lifetime | 5 events / 10 seconds |
| Milestone2Hud | Range gizmos | Enabled |
| Test Directional Light / scene | Intensity | 0.12 |
| Scene ambient light | RGB | 0.08, 0.10, 0.13 |

These are playtest values, not final design decisions. Walking at 3 m/s
normally emits about twice per second; sprinting at 6 m/s about three
times per second. Footsteps use actual CharacterController displacement,
so idle input, gravity alone, and pushing against a wall do not generate
footsteps. Once sprint stamina runs out, moving footsteps become walking
events. Partial stride distance persists across brief stops.

Flashlight battery only drains while active, including while the cursor
is released with Escape (Escape does not pause the game). At zero charge
the light switches off; refilling leaves it off until F is pressed.
Battery pickups are single-use after any successful refill. A full charge
does not consume a pickup; partial refills discard charge above capacity.

## Noise architecture and debugging

`PrototypeNoise.asset` is the explicit shared NoiseChannel reference for
emitters and HUD. Future systems subscribe to `Emitted` and unsubscribe
when disabled. NoiseEvent carries category, position, intensity, range,
timestamp, source, and instigator. No global scene lookup or singleton is
required. Range is a signal radius for future consumers; no hearing
threshold, occlusion, attenuation, noise accumulation, or hunt triggering
has been decided or implemented here.

FirstPersonMotor now publishes movement samples after collision resolves.
MovementNoiseEmitter observes those samples without changing movement or
stamina rules. NoiseObstacle can emit through E or trigger traversal;
these paths share a cooldown. Its trigger and visible, interactable surface
are independent of final models. PlayerFlashlight exposes IsActive,
Battery, Capacity, Toggle, and RestoreBattery for later integrations.

The bottom-left HUD shows battery/on-off state and recent noise events:
cyan = Walking, yellow = Sprinting, orange = Environment. Each row gives
intensity, range, and age. Range spheres are visible in Scene view with
Gizmos enabled during Play Mode. No Console spam is enabled.

## Scene additions

- Milestone2Player prefab variant: preserves the original player components
  and designer edits; adds PlayerFlashlight, MovementNoiseEmitter, and
  Milestone2Hud. Player Camera gains a Flashlight Beam child.
- Milestone 2 Test Props: parent for the following additions.
- Battery Pedestal A and Battery Pickup A: near the start, at x=-2, z=-7.
- Battery Pedestal B and Battery Pickup B: x=7, z=4.
- Noise Debris Patch: x=0, z=-2; 3 x 3 m trigger area, with Debris Surface child.
- TestBattery and TestDebris are reusable prefabs. The patch includes a
  kinematic Rigidbody to support trigger callbacks with the player capsule.

Existing boundaries, ramp, steps, slalom blocks, and toggle targets remain.
The light intensity and camera background are dimmed only in this scene.

## Playtest checklist

1. Press F repeatedly and hold it: each press should toggle once. Assess
   beam reach, cone width, shadow quality, and visibility with the light off.
2. Leave the light on until empty (~50 seconds); check HUD and shutoff.
   Toggle attempts at empty should leave it off. Switching it off earlier
   should stop drain.
3. Try a battery at full charge, then after draining. Check the +40 refill,
   capacity clamp, single-use behavior, and explicit F press after depletion.
4. Walk and sprint around the obstacles: compare HUD categories and radii.
   Sprint to exhaustion and confirm footsteps return to Walking.
5. Stand still and push directly into a wall: verify no new footsteps.
6. Walk across the orange debris patch, stop on it, then move again; also
   aim at its surface and press E. Check significant Environment events and
   the 2-second shared cooldown without stationary spam.
7. Stop/restart Play Mode: battery should be full/off, pickups restored,
   and noise history empty. Recheck Milestone 1 movement and interaction.

No manual component wiring or package installation is required. No build
profile changes or packaged Windows build are included. Use Her Footsteps >
Milestone 2 > Open Test Scene to open the saved scene. The Milestone 2
Run All Milestone Edit Mode Tests / Run All Milestone Play Mode Tests menus
run both milestone test classes in their shared assemblies. The existing
Milestone 1 test menus do the same; do not use its scene-creation command.

## Verification and file inventory

### Interrupted setup recovery

The interrupted editor session imported Milestone2Setup.cs but retained a
compiler source list containing only Milestone1Setup.cs in the editor
assembly. Reimporting the script/assembly did not repair that session.
A clean Unity restart rebuilt the source list, compiled the existing
setup, and successfully generated the missing assets. No C# error in the
Milestone 2 systems was found as the cause. The underlying reason Unity's
source list became stale is unknown.

The Milestone 1 creation error was its intentional overwrite protection.
The Play Mode path error was caused by the missing scene; the existing
project-relative Assets path is valid. The test now checks for the scene
first and reports the exact creation menu if it is missing.

Recovery changed Milestone2Setup.cs (open-scene and test menus),
Milestone2PlayTests.cs (missing-scene diagnostic and input-test routing),
Milestone1PlayTests.cs (input-test routing and current Unity object lookup
APIs), and this guide, in addition to generating the assets below.
Existing gameplay components were retained. The saved Milestone 1 scene and PrototypePlayer prefab
were verified byte-for-byte unchanged from the start of recovery.

The initial background Play Mode run passed both saved-scene integration
tests but failed both simulated-keyboard tests. InputSettings.IgnoreFocus
does not override the separate editor Game-view routing policy. Those
tests now temporarily select AllDeviceInputAlwaysGoesToGameView and
restore both input policies in finally blocks. This affects test execution
only, not player focus/cursor behavior or saved input settings.

### Milestone 2 file inventory

New files from the original implementation, retained during recovery:

- Assets/HerFootsteps/Runtime/PlayerFlashlight.cs
- Assets/HerFootsteps/Runtime/BatteryPickup.cs
- Assets/HerFootsteps/Runtime/NoiseChannel.cs
- Assets/HerFootsteps/Runtime/MovementNoiseEmitter.cs
- Assets/HerFootsteps/Runtime/NoiseObstacle.cs
- Assets/HerFootsteps/Runtime/Milestone2Hud.cs
- Assets/HerFootsteps/Editor/Milestone2Setup.cs
- Assets/HerFootsteps/Tests/EditMode/Milestone2Tests.cs
- Assets/HerFootsteps/Tests/PlayMode/Milestone2PlayTests.cs
- Assets/Docs/MILESTONE_2_PLAYTEST.md

Generated during recovery:

- Assets/Scenes/Milestone2_Test.unity
- Assets/HerFootsteps/Prefabs/Milestone2Player.prefab
- Assets/HerFootsteps/Prefabs/TestBattery.prefab
- Assets/HerFootsteps/Prefabs/TestDebris.prefab
- Assets/HerFootsteps/Settings/PrototypeNoise.asset
- Assets/HerFootsteps/Materials/M2_Battery.mat
- Assets/HerFootsteps/Materials/M2_Debris.mat

Each new file has matching Unity .meta metadata; the new Settings folder
also has Assets/HerFootsteps/Settings.meta. Keep these files with assets.

Existing files modified in the original Milestone 2 implementation:

- Assets/InputSystem_Actions.inputactions: Player/Flashlight on F.
- Assets/HerFootsteps/Runtime/PlayerInputReader.cs: reads that action.
- Assets/HerFootsteps/Runtime/FirstPersonMotor.cs: publishes resolved movement.
- Assets/HerFootsteps/Runtime/PrototypeHud.cs: generic test-space heading.
- Assets/HerFootsteps/Editor/HerFootsteps.Editor.asmdef: root namespace.
- Assets/HerFootsteps/Editor/Milestone1Setup.cs: shared-test-assembly comment.
- Assets/Docs/DEVELOPMENT_PLAN.md and GAMEPLAY_SYSTEMS.md: approved scope and behavior.

Pre-existing designer changes to PrototypePlayer.prefab and
Milestone1_Test.unity were preserved and are not Milestone 2 edits.

### Verified results (2026-09-30)

- Unity 6000.4.10f1 compiled the setup and runtime components successfully.
- Generated all five requested scene/prefab/channel assets plus two materials.
- Confirmed the Milestone 2 menu appears, including Create Test Scene (once),
  Open Test Scene, and the two all-milestone test commands.
- Edit Mode: 20 passed, 0 failed (12 Milestone 1, 8 Milestone 2).
- Final interactive-editor Play Mode: 4 passed, 0 failed (2 per milestone).
- Verified battery active-only drain/depletion/refill, existing interaction
  compatibility, walk/sprint event differences, idle/wall silence, event
  payload/subscription behavior, obstacle cooldown, and prefab wiring.
- Both saved scenes load in Play Mode. The Milestone 2 integration test
  verifies battery drain/refill and real physics traversal of the debris
  trigger, including no repeated environmental events while stationary.
- Input tests verify WASD, mouse delta, sprint, tap E, and F once per press.
  They exercise synthetic actions; focus/cursor feel still needs manual review.
- Visual editor inspection confirmed the saved blockout, battery/debris props,
  and both temporary HUD panels render. OS-injected desktop-automation keys
  did not reliably drive in-game controls; a hands-on F/beam check is therefore
  not claimed. Perform the manual checklist above. The scene is left open
  outside Play Mode with no unsaved gameplay changes.

Local evidence (Logs is ignored by Git): Milestone2-generation.log,
Milestone2-setup.txt, Milestone2-editmode.xml/.log,
Milestone2-playmode-final.xml/.txt, and Milestone2-editor.log. The earlier
Milestone2-playmode.xml/.log retains the background-input failure for
diagnosis; use the -final files for the corrected result. Menu test runs
also write the shared legacy Milestone1-tests/playtests filenames.

No remaining milestone compile errors or failing tests. Existing Unity AI
NoSubscription / entitlement-404 diagnostics are unrelated to these systems.
Unity startup also reports skipped invalid package test assemblies; the
HerFootsteps assemblies compile, load, and run successfully. The obsolete
Milestone 1 test API warnings were removed by updating those test lookups.
No imported audio clips are used. No manual scene wiring is needed.
