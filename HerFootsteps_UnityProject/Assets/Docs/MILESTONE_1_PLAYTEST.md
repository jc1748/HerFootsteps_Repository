# Milestone 1 - Player Movement and Interaction Test

## Playing the test

Open `Assets/Scenes/Milestone1_Test.unity` in Unity 6000.4.10f1 and press
Play. Click the Game view if the cursor is not captured. References and
materials are already assigned; no packages or tree imports are needed.
This is a temporary testing space, not the designer's final level.

- WASD (existing arrow-key alternatives also remain): movement.
- Mouse delta: look.
- Left Shift: hold to sprint while moving on the ground.
- E: activate the target under the crosshair once per press.
- Escape: release the cursor and suspend movement/look/interaction input.
- Left click: recapture the cursor. This is not a pause menu; stamina
  recovery and gravity continue while the cursor is released.
- No jumping or crouching. Existing template actions remain unused.

Orange targets turn green when activated and back to orange on the next
press. Target B is behind a wall for testing occlusion. No supplied audio
or tree assets are used in this milestone.

## Provisional Inspector values

On the PrototypePlayer prefab or scene instance:

| Component | Setting | Initial value |
| --- | --- | --- |
| FirstPersonMotor | Walk speed | 3 m/s |
| FirstPersonMotor | Sprint speed | 6 m/s |
| FirstPersonMotor | Stamina capacity | 100 |
| FirstPersonMotor | Stamina drain | 20/second |
| FirstPersonMotor | Stamina recovery | 15/second |
| FirstPersonMotor | Mouse sensitivity | 0.1 degrees/pixel |
| FirstPersonMotor | Pitch limit | +/-85 degrees |
| FirstPersonMotor | Gravity | -20 m/s squared |
| PlayerInteractor | Interaction range | 2.5 m |
| CharacterController | Height / radius | 1.8 m / 0.3 m |
| CharacterController | Step offset / slope limit | 0.3 m / 45 degrees |
| CharacterController | Skin width / minimum move distance | 0.03 m / 0 |
| Player Camera | Eye height / field of view | 1.65 m / 75 degrees |
| Player Camera | Near / far clip | 0.05 m / 150 m |

Stamina starts full. Five seconds of sustained sprint consumes a full
meter; recovery from empty takes approximately 6.67 seconds. Recovery
occurs when not sprinting, including walking. After exhaustion, release
Shift to re-arm sprint; recovered stamina alone does not automatically
restart sprint while Shift stays held. Stationary Shift does not drain
stamina. Moving into an obstruction can consume stamina while sprint is
requested. These are initial test behaviors, not finalized balance.

Mouse delta is not multiplied by frame time. Diagonal movement is clamped
to prevent faster diagonal travel. Gravity and collision use Unity's
CharacterController; no Rigidbody or model-specific movement is required.

## Interaction architecture

PlayerInputReader uses a runtime copy of the existing
`Assets/InputSystem_Actions.inputactions`, restricted to Keyboard&Mouse.
Only Player/Move, Look, Sprint, Interact and UI/Cancel, Click are enabled.
The original action IDs and bindings are retained. Interact's global Hold
interaction was removed for immediate standard presses.

PlayerInteractor casts from the camera, respecting the nearest solid
collider and range. Interactable is a reusable base class independent of
models. A target's HoldDuration defaults to zero (tap). Future targets can
override it; releasing E or losing the target cancels hold progress, and
completion requires a new press before repeating. No clue behavior,
inspection UI, or final clue-hold duration is implemented. The 1-second
hold stub exists only in automated tests.

The player capsule uses the built-in Ignore Raycast layer. Keep blockers
and interactables in the interaction raycast mask; excluding walls would
allow interacting through them. Target colliders must be non-trigger.

PrototypeHud and TestToggleInteractable are temporary test feedback.

## Created scene GameObjects and prefabs

`Milestone1_Test` contains:

- Temporary Blockout
  - Floor
  - North Boundary, South Boundary, East Boundary, West Boundary
  - Occlusion Wall
  - Slalom Block A, Slalom Block B
  - 15 Degree Ramp
  - Step 1, Step 2, Step 3
- Test Directional Light
- PrototypePlayer (prefab instance)
  - Player Camera
- Tap Target A (TestToggle prefab instance)
- Tap Target B (behind wall) (TestToggle prefab instance)

Prefab assets: PrototypePlayer (with Player Camera child), and TestToggle
(single cube with a renderer, collider, and test interaction component).
The blockout floor is 24 x 32 m; boundaries are 3 m high. No hidden runtime
scene builder is needed to play: geometry and references are saved.

## Verification and manual playtest

Automated tests can be rerun from Her Footsteps > Milestone 1 > Run Edit
Mode Tests / Run Play Mode Tests. Results are written under `Logs/`.
The setup menu creates assets once and refuses to overwrite existing ones.
An editor-only request file under `Library/HerFootsteps/` supports the same
explicit build/test commands; no asset generation runs without a request.

Manual checks:

1. Walk forward, backward, sideways, and diagonally; compare speed.
2. Sprint until exhausted, keep Shift held, then release and try again.
3. Walk into walls/blocks and traverse the ramp and low steps.
4. Look up/down to the pitch limits; assess sensitivity and field of view.
5. Approach a target, aim at it, and tap E. Hold E to confirm no repeats.
6. Test the range boundary and attempt to use Target B through the wall.
7. Press Escape, click to resume, and switch focus away/back.
8. Stop and restart Play Mode; stamina and target state should reset.

No changes to the build scene list were requested. To make a standalone
test build later, include Milestone1_Test as the startup scene in the
build profile. This milestone does not include a packaged Windows build.

## File inventory

The implementation file inventory and final verification results are
recorded below after validation. Unity also creates matching `.meta`
files for every new asset and folder; keep these with their assets.

Created project files (including Unity-generated metadata):

- Assets/Docs/MILESTONE_1_PLAYTEST.md
- Assets/Docs/MILESTONE_1_PLAYTEST.md.meta
- Assets/HerFootsteps.meta
- Assets/HerFootsteps/Editor.meta
- Assets/HerFootsteps/Editor/HerFootsteps.Editor.asmdef
- Assets/HerFootsteps/Editor/HerFootsteps.Editor.asmdef.meta
- Assets/HerFootsteps/Editor/Milestone1Setup.cs
- Assets/HerFootsteps/Editor/Milestone1Setup.cs.meta
- Assets/HerFootsteps/Materials.meta
- Assets/HerFootsteps/Materials/M1_Boundary.mat
- Assets/HerFootsteps/Materials/M1_Boundary.mat.meta
- Assets/HerFootsteps/Materials/M1_Floor.mat
- Assets/HerFootsteps/Materials/M1_Floor.mat.meta
- Assets/HerFootsteps/Materials/M1_Indicator.mat
- Assets/HerFootsteps/Materials/M1_Indicator.mat.meta
- Assets/HerFootsteps/Materials/M1_Obstacle.mat
- Assets/HerFootsteps/Materials/M1_Obstacle.mat.meta
- Assets/HerFootsteps/Prefabs.meta
- Assets/HerFootsteps/Prefabs/PrototypePlayer.prefab
- Assets/HerFootsteps/Prefabs/PrototypePlayer.prefab.meta
- Assets/HerFootsteps/Prefabs/TestToggle.prefab
- Assets/HerFootsteps/Prefabs/TestToggle.prefab.meta
- Assets/HerFootsteps/Runtime.meta
- Assets/HerFootsteps/Runtime/FirstPersonMotor.cs
- Assets/HerFootsteps/Runtime/FirstPersonMotor.cs.meta
- Assets/HerFootsteps/Runtime/HerFootsteps.Runtime.asmdef
- Assets/HerFootsteps/Runtime/HerFootsteps.Runtime.asmdef.meta
- Assets/HerFootsteps/Runtime/Interactable.cs
- Assets/HerFootsteps/Runtime/Interactable.cs.meta
- Assets/HerFootsteps/Runtime/PlayerInputReader.cs
- Assets/HerFootsteps/Runtime/PlayerInputReader.cs.meta
- Assets/HerFootsteps/Runtime/PlayerInteractor.cs
- Assets/HerFootsteps/Runtime/PlayerInteractor.cs.meta
- Assets/HerFootsteps/Runtime/PrototypeHud.cs
- Assets/HerFootsteps/Runtime/PrototypeHud.cs.meta
- Assets/HerFootsteps/Runtime/SprintStamina.cs
- Assets/HerFootsteps/Runtime/SprintStamina.cs.meta
- Assets/HerFootsteps/Runtime/TestToggleInteractable.cs
- Assets/HerFootsteps/Runtime/TestToggleInteractable.cs.meta
- Assets/HerFootsteps/Tests.meta
- Assets/HerFootsteps/Tests/EditMode.meta
- Assets/HerFootsteps/Tests/EditMode/HerFootsteps.Tests.EditMode.asmdef
- Assets/HerFootsteps/Tests/EditMode/HerFootsteps.Tests.EditMode.asmdef.meta
- Assets/HerFootsteps/Tests/EditMode/Milestone1Tests.cs
- Assets/HerFootsteps/Tests/EditMode/Milestone1Tests.cs.meta
- Assets/HerFootsteps/Tests/PlayMode.meta
- Assets/HerFootsteps/Tests/PlayMode/HerFootsteps.Tests.PlayMode.asmdef
- Assets/HerFootsteps/Tests/PlayMode/HerFootsteps.Tests.PlayMode.asmdef.meta
- Assets/HerFootsteps/Tests/PlayMode/Milestone1PlayTests.cs
- Assets/HerFootsteps/Tests/PlayMode/Milestone1PlayTests.cs.meta
- Assets/Scenes/Milestone1_Test.unity
- Assets/Scenes/Milestone1_Test.unity.meta
- ProjectSettings/SceneTemplateSettings.json

Modified existing files:

- Assets/InputSystem_Actions.inputactions: removed Interact Hold requirement.
- Assets/Docs/DEVELOPMENT_PLAN.md: linked this milestone and its approved scope.
- ProjectSettings/URPProjectSettings.asset: Unity added m_ProjectSettingFolderPath while creating URP materials; no pipeline switch.

Unity generated SceneTemplateSettings.json while creating the test scene. No existing scene or package manifest was modified. The externally removed Conifers installer launcher and its metadata were left untouched.

Generated local verification artifacts (ignored by Git): Logs/Milestone1-setup.txt, Logs/Milestone1-tests.txt, Logs/Milestone1-tests.xml, Logs/Milestone1-playtests.txt, Logs/Milestone1-playtests.xml. Library/HerFootsteps/Milestone1.request was a temporary command file consumed by the editor. Unity also updated its normal Library, Temp, log, and test-runner caches.
