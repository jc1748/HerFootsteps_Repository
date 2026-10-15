# Milestone 6 — gameplay hunt prototype

## Open and play

Open `Assets/Scenes/Milestone6_Gameplay.unity` (or Her Footsteps > Presentation >
Open Milestone 6), then Play and click Game view. The saved scene derives from
Milestone5_Presentation. Its forest, navigation, lighting, supplies, sister clue,
hiding shelter and wildlife are retained. Prior scenes/prefabs and the recovery
folder are preserved. M6 is added to Build Settings for scene-path restart;
existing entries retain their order. No manual component wiring is required.

## Hotbar diagnosis and controls

Two defects caused the reported problem: the saved M5.1 scene's input override
still referenced the historical InputSystem_Actions asset, so Slot/UseItem actions
were absent despite the presentation prefab having a newer asset. Also, its
`<Keyboard>/digit1`–`digit5` paths were invalid: the installed Input System names
these controls `1`–`5`. M6 explicitly binds a separate GameplayInput asset on the
scene instance, with corrected paths and the Keyboard&Mouse binding group.
Historical scenes and assets remain unchanged. This fixes the new gameplay scene;
the historical M5.1 scene is retained as delivered.

| Input | M6 behavior |
| --- | --- |
| WASD / mouse / Left Shift | Existing walking, look, stamina-limited sprint |
| 1–5 | Select corresponding hotbar slot, including empty slots |
| Mouse wheel | Up selects previous; down selects next; wraps 1–5 |
| R | Use selected item through existing PlayerInventory.TryUse |
| F | Existing flashlight toggle |
| E / hold E | World interaction / sister clue discovery |
| Left Ctrl while hidden | Existing hold breath |
| Tab | Reserved for future map; no modal UI |
| F3 | Toggle development panels |
| F4 / F5 with F3 visible | -25 / +25 composure |
| F7 / F8 with F3 visible | Gated phantom footsteps / wildlife request |
| Enter on failure screen | Restart current scene; button also works |

Hotbar selection never releases cursor lock or disables movement/look. Wheel
input uses the existing Input System and one selection step per nonzero input
update, independent of platform wheel-delta units. Empty use is a silent no-op.
Battery behavior remains +40, clamped to capacity 100, consuming one only when
charge actually changes. With the light on, even a small amount of intervening
drain permits another battery; turn F off when testing the exact-full safeguard.

## Composure indicator

`ComposureIndicator` is a small original vector brain-and-ring UI graphic near
bottom-left, 88 reference pixels in the existing 1920×1080-scaled Canvas. The
circular meter shows the current fraction with smooth response. High uses muted
green, Medium ochre, Low rust, Broken red with a diagonal fracture mark. Loss and
recovery gently tint the meter. No numerical HUD panel was added; exact values,
sources and transitions remain in F3. It observes PlayerComposure without changing
that resource. The two folded lobes are authored UI geometry, not borrowed artwork.

Existing values remain provisional: 100 capacity/start, High >=70, Medium >=35,
Low >0, Broken at0. Outside camp passive loss is -0.5/s. Camp suspends only passive
loss and restores +3/s up to75 with a20-point session budget. Sister discovery
holds E1.5s and restores20 once without a slot. Hiding does not restore composure.
Cryptid proximity/detection/pursuit losses remain -2/s within6m, -8 per detection,
-4/s pursuit; the observer naturally ignores the inactive cryptid between hunts.

## Phantom footsteps

`PhantomFootstepsHallucination` derives from the existing HallucinationEvent.
It uses Low/Broken state eligibility, automatic activation, duration and cooldown,
and cancels immediately on recovery above the minimum state. A separate world-space
AudioSource begins to one side of the player's position when the event starts,
then drifts along that initial heading. It does not follow subsequent player
movement or rotation. There is no visible person, persistent geometry or footprint
object. Optional footprint impressions were omitted for this audio-first example.

The component intentionally has no NoiseChannel reference or emission code. These
sounds cannot trigger or redirect a cryptid. The old FalseTrailHallucination root
is inactive in M6; its code and earlier scenes remain available. Wildlife behavior,
flashlight reaction and real Scream emission are unchanged.

Provisional phantom settings: Low minimum,8s duration,25s cooldown, automatic on,
4.5m lateral offset plus2m forward,0.35m/s drift, source volume0.45, full3D spatial
blend, linear falloff2–16m. The event component is player-relative for eligibility
(base activation range12m); its audio origin is independent world space.

## Hunt architecture

`HuntController` listens to the existing NoiseChannel and PlayerComposure's
BrokenEntered event. States: **SafePeriod → Exploration → Warning → Active →
Cooldown → Exploration**, with **Failed** from Active capture. C# StateChanged is
available for presentation. `HuntPresentation` handles ambience/warning/failure;
`HuntDebugView` observes timers and state, and invokes gated phantom debug requests.

The controller disables the existing CryptidBrain outside Active. This prevents
its independent sight/hearing listener from bypassing safe/warning/cooldown gates.
It does not replace its internal Idle/Investigate/Search/Chase/Disengage logic.
`CryptidBrain.InvestigateEvidence` is the small new integration entry point:
one location snapshot starts normal investigation, never forced Chase or teleport.

- Safe timer starts on the first frame with PlayerInputReader.HasControl. It then
  counts gameplay time even if the cursor is later released. Initial lack of Game
  focus does not consume it. Composure, noise and hallucinations continue normally.
- Safe-period, Warning, Active, Cooldown and Failed trigger attempts are discarded,
  never queued. Safe/cooldown expiry merely permits a later fresh event. Remaining
  Broken after expiry does not silently retrigger; recover and re-enter Broken.
- Noise must be attributable to this player, meet intensity threshold0.8, and pass
  the cryptid's existing range/intensity hearing check. Current walking0.2 and
  sprint0.6 do not initiate hunts; branches1.0, forced breath0.8 and scream1.2 can
  when in hearing range. During Active, ordinary audible sounds still redirect
  the AI normally, including walking/sprinting. No noise accumulation is added.
- A noise hunt freezes the initiating sound position throughout Warning. Broken
  searches a configurable search-origin transform, or the cryptid's own current
  position when unassigned. The saved scene uses that fallback: no hidden-player
  position or scripted hiding knowledge is supplied by Broken.
- After Warning, the brain investigates that snapshot. Only its normal distance,
  FOV and clear head/body ray can transition into Chase. Losing sight immediately
  stops live tracking and starts normal last-known investigation/search.
- Hunt resolves after Search and Disengage return the brain to Idle. No arbitrary
  active-hunt timeout interrupts a visible chase. AI travel timeout still provides
  its existing inaccessible-destination fallback. Exploration returns with cooldown.
- Capture requires Active, AI Chase, distance<=1.4m and a fresh successful existing
  visual observation. Hiding state is checked by the senses: solid cover protects;
  an exposed hiding flag does not confer immunity. No capture through walls.

Provisional hunt settings: safe60s, warning4s, cooldown30s, noise threshold0.8,
capture1.4m. Hearing remains range multiplier1/cap20m/minimum intensity0.05;
sight12m/FOV100°. Existing search8s/radius3m, travel timeout15s, investigate2.5,
chase4.5, search1.8 and return2.5m/s remain unchanged.

Warning fades ambient sources toward5% of original volume over1.5s and plays a
nonlocal2D growl, volume0.25/pitch0.8, stopped when Active begins. The cue is not
positioned at the cryptid, so it does not disclose its location. Ambience returns
on Cooldown. Failure shows a simple caught/restart overlay, releases the cursor,
blocks player gameplay input and pauses simulation/audio. Enter/button reloads
the current scene and restores time/audio. There are no checkpoints or save data;
supplies, clues, composure, cooldowns and the safe period reset.

## Audio attribution and limitations

Reuses supplied clips; no downloaded or purported final sound assets:

- Phantom: `75266__garybran__heavyfootstompsinforest4.wav`, garybran, CC0,
  https://freesound.org/s/75266/ — looped provisionally as a footsteps sequence.
- Warning: `869866__fusagawi__beast-growling.wav`, fusagawi+, CC BY4.0,
  https://freesound.org/s/869866/ — playback pitch0.8, volume0.25, truncated to
  warning duration; original file unchanged. License:
  https://creativecommons.org/licenses/by/4.0/ . Keep this attribution in distribution.
- Existing hdfreema CC0 night ambience and NachtmahrTV CC0 wildlife scream remain.

Original `Assets/Audio/_readme_and_license.txt` is unchanged. Final cryptid roar,
footstep mixing and creature art are still missing; current clips/silhouettes are
prototype assets. Spatial audio/reaction readability needs designer headphone
evaluation. No forest expansion, final narrative progression or final art was added.

## Manual playtest checklist

1. Play M6, click Game view. Confirm compact brain dial, five slots, no debug
   panels. Verify WASD/look/F/E and Tab doing nothing. Pick up camp batteries.
2. While cursor locked and while moving, press each1–5; verify the matching border.
   Wheel both directions through boundaries. Select battery, R: charge increases,
   quantity falls byone. Use empty slot: nothing. Turn F off, recharge to100, R
   again: no consumption. Turn F on: drain resumes normally.
3. Walk out of camp: ring drains slowly. F3/F4 crosses High/Medium/Low/Broken;
   F5 and clue recovery fill it. Verify state colors, Broken fracture and restrained
   feedback. F3 off must leave only the compact gameplay UI.
4. Before safe timer expires, create loud noise and reach Broken: no hunt. Low
   phantom/wildlife may still happen. Wait for expiry: no queued hunt. Recover then
   cause a fresh Broken entry, or create a significant audible gameplay noise.
5. At Low, F3 then F7: hear steps from one side without visible markers/person.
   Turn/move and listen for the fixed-world origin. Confirm no new NoiseChannel
   event or AI response. Recover with F5: sound stops. Low again during cooldown:
   no immediate repetition. Test automatic activation after cooldown.
6. Near wildlife (western forest around -7,13), keep Low, face it/F8. Illuminate
   continuously to dismiss; later fail a reaction. Confirm one scream event. After
   safe period and within hearing range, it can trigger Warning through HuntController.
7. After safe period, step on branches near(3,8) within18m of cryptid at(5,21).
   Watch Warning4s, ambience dip and growl. Move away during warning: initial target
   must stay at the noise position. Walk/sprint alone should not initiate a hunt
   with current0.8 threshold; adjust Inspector for alternate balance tests.
8. During Active, approach into sight: Chase. Break LOS around trees/rocks and move
   away: AI investigates last seen spot, searches and disengages. Verify ambience
   restoration and30s cooldown; loud events during cooldown must not queue.
9. Generate noise then use root shelter(-12,7), E/Left Ctrl. Concealed and quiet:
   no scripted discovery/capture. Exhaust breath: a real audible event may redirect
   investigation. Leaving cover in sight should permit normal Chase.
10. During an active chase allow close contact: failure overlay, no movement/use.
    Restart with Enter; repeat via button. Confirm fresh scene, empty inventory,
    reset resources/clue/safe timer, restored audio and cursor capture.
11. Smoke-test earlier milestones separately. M5.1 retains its historical saved
    input bug; use M6 for corrected hotbar controls. Original mechanics/geometry
    have not been replaced. Evaluate headphone mix and capture fairness manually.

## Verification

Automated validation on October14,2026: **43/43 Edit Mode passed; 15/16 Play Mode
passed**. Both new M6 integration tests passed, covering safe/cooldown trigger
discard, fresh Broken/noise activation, frozen evidence, existing search/disengage,
correct saved input reference, hotbar battery/full/empty behavior, capture occlusion,
failure input lock and actual scene restart/reset. The remaining older M1 test was
interrupted by the installed Unity AI Assistant's unrelated `NoSubscription` log
from generators.ai.unity.com. It is reported rather than hidden/suppressed; package
settings were not changed. A real scene-unload AudioSource cleanup error found in
the first run was fixed and the M6 restart test passed on rerun.

The saved M6 scene was opened in Play Mode and the compact brain meter/default
hotbar visually inspected. Windows automation keypresses did not reach gameplay,
including unchanged F3, despite the reader reporting control and cursor Locked.
The designer subsequently confirmed through physical keyboard input in Play Mode:
"Selection and battery use work." This confirms number-key slot selection and R
battery use independently of the automation limitation. Mouse-wheel cycling and
the remaining hunt/audio checklist still require manual evaluation.
The local runner outputs `Logs/Milestone1-tests.*` and `Milestone1-playtests.*`;
M6 copies are retained under `Logs/Milestone6-*`. Editor-only Milestone6LiveCheck
can record runtime input/selection/inventory state on an explicit local request
and position the player at a battery for keyboard checking; it does not synthesize
key input or alter saved scene content. All tuning above is provisional.

## Files created or modified

Paths are relative to the Unity project; new Unity assets also have their generated
`.meta` files. No imported tree/audio files or historical scenes/prefabs changed.

Created:

- `Assets/Scenes/Milestone6_Gameplay.unity`
- `Assets/HerFootsteps/Milestone6/GameplayInput.inputactions` (plus folder meta)
- `Assets/HerFootsteps/Runtime/ComposureIndicator.cs`
- `Assets/HerFootsteps/Runtime/PhantomFootstepsHallucination.cs`
- `Assets/HerFootsteps/Runtime/HuntController.cs`
- `Assets/HerFootsteps/Runtime/HuntPresentation.cs`
- `Assets/HerFootsteps/Runtime/HuntDebugView.cs`
- `Assets/HerFootsteps/Editor/Milestone6Setup.cs`
- `Assets/HerFootsteps/Editor/Milestone6LiveCheck.cs`
- `Assets/HerFootsteps/Tests/EditMode/Milestone6Tests.cs`
- `Assets/HerFootsteps/Tests/PlayMode/Milestone6PlayTests.cs`
- `Assets/Docs/MILESTONE_6_PLAYTEST.md`

Modified:

- `Assets/HerFootsteps/Runtime/PlayerInputReader.cs` — optional wheel action.
- `Assets/HerFootsteps/Runtime/PresentationHud.cs` — wheel cycling, empty-use guard,
  item name taken directly at use time.
- `Assets/HerFootsteps/Runtime/CryptidBrain.cs` — evidence-only investigation API.
- `Assets/HerFootsteps/Runtime/Milestone5Hud.cs` — configurable Broken event label.
- `Assets/HerFootsteps/Runtime/PresentationDebugMode.cs` — configurable event hint.
- `ProjectSettings/EditorBuildSettings.asset` — append M6 for restart support.
- `Assets/Docs/GAME_DESIGN.md`, `GAMEPLAY_SYSTEMS.md`, `PROTOTYPE_SCOPE.md`,
  `AI_DESIGN.md`, `LEVEL_DESIGN.md`, `DEVELOPMENT_PLAN.md` — current M6 scope and
  confirmed integration, with historical milestone boundaries retained.

No new art prefabs are required: M6 composes the existing presentation player and
cryptid instances with the new components and UI child. Existing forest assets and
NavMesh are shared read-only. The user's `Assets/_Recovery/` content is untouched.
No additional design approval is needed to run this prototype. The listed pacing,
noise thresholds, Broken search-origin policy, capture distance and audio mix are
provisional choices for designer evaluation, not final balance.
