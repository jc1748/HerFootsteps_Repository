# Milestone 4 - Cryptid AI behavior test

## Start and scope

Open `Assets/Scenes/Milestone4_Test.unity`, or Her Footsteps > Milestone 4 >
Open Test Scene, and press Play. The scene copies the saved Milestone 3 blockout,
retains the original Milestone3Player prefab, and adds a primitive cryptid and
an already baked NavMesh. No manual component wiring or package installation.
The cryptid starts near (0, 0, 5), facing north (+Z), away from the initial player.
The existing shelter is at (3, 0, -8), with its opening facing south (-Z).

Controls remain WASD, mouse look, Left Shift sprint, E interact/hide/leave,
Left Ctrl hold breath while hidden, F flashlight, Escape/click release/recapture.
Escape does not pause gameplay: the AI and resource timers continue.

This scene is an always-active behavior test. Idle means standing and sensing,
not a Hunt Controller's inactive cryptid. No hunt activation, warning, safe-period
timer, attacks, damage/death, composure, hallucinations, clues or progression.
The existing planned Hunt safe-period gate remains unimplemented and must be
integrated when that later system is authorized. No final models, animations,
audio, VFX or environment art; no imported audio clips are used.

## Components and knowledge boundaries

- **CryptidSenses:** explicit player/head/eyes references, geometry-based sight,
  hearing eligibility, and read-only access to existing hiding/breath/light state.
  Only the sensor reads live player transforms, and only a successful visual
  observation exports the current position to the brain. Debug state reads are
  not knowledge used to navigate or inspect cover.
- **CryptidBrain:** small enum-based state machine, subscribes/unsubscribes to
  the existing PrototypeNoise asset, remembers information positions and owns
  search/travel timers. No global player lookup, scripted hiding-spot knowledge,
  or dependence on current map coordinates.
- **CryptidNavigation:** wraps NavMeshAgent sampling/path requests and reports
  arrival/failure. Samples use the agent's type/area mask. Partial paths approach
  their reachable endpoint; invalid/unmapped destinations fall back to bounded
  search. Movement does not teleport through walls. Travel timeouts prevent
  permanently stuck investigation/return states.
- **CryptidDebugView:** temporary HUD and gizmos. The replaceable capsule and
  forward-indicator children are visual-only and have no colliders or AI logic.
  Eyes remain a separate root child when the visual is replaced.

### States

1. **Idle:** stand still, listen and look forward. New valid noise starts
   Investigate; confirmed sight immediately starts Chase.
2. **Investigate:** move toward a stored noise/last-seen position. Reaching the
   target or reachable edge, path failure, or travel timeout starts Search.
3. **Search:** bounded search around the evidence location. Initially stop and
   turn; then sample surrounding points at a golden-angle sequence and scan when
   stopped. A new audible event can restart investigation; sight starts Chase.
   Waypoint changes do not reset the total search budget.
4. **Chase:** update the destination from confirmed visual observations, at a
   bounded repath interval. No attack or death on reaching the player.
5. **Disengage:** after search expires, walk toward the position where the AI
   started. Continue sensing; new evidence can interrupt the return. Arrival,
   invalid route or travel timeout ends at Idle and clears active location memory.

Losing sight immediately changes Chase to Investigate at the last confirmed
position. There is no through-wall grace period or hidden live-position tracking.
The cryptid can still receive subsequent footsteps/breath/debris events, which
legitimately supply newer positions. Without new evidence, it searches and leaves.

## Hearing

All existing NoiseKind categories use the same rule: intensity must meet the
minimum, and 3D distance from cryptid root to the event position must be within
`min(event.Range * hearingRangeMultiplier, maximumHearingRange)`. Equality at the
boundary is accepted. No hearing occlusion, attenuation or final noise balance.
The event's source/instigator is not used to follow the player's current transform.

Latest audible information wins while the player is not visually confirmed.
Direct sight takes priority over audible distractions. Inaudible events appear
as rejected in debug feedback but do not change state/memory/destination. Own
noise sources are ignored. Repeated relevant sounds may extend an investigation;
this is new evidence, not omniscience.

Walking (4 m), sprinting (10 m), debris (18 m), ordinary hidden breathing (2 m)
and forced breath (12 m) retain their M2/M3 emitter values. These are not final
cryptid hearing distances; the multiplier, cap and threshold are tunable.

## Sight, hiding and flashlight

Two samples are used: the player's head/camera position and body offset. Each
must be within the sight range and a 3D forward cone (full angle is FOV). Either
unobstructed sample confirms sight. Raycasts ignore triggers and the cryptid/player
hierarchies, and respect the configured occlusion mask. The existing player on
Ignore Raycast is supported because sight checks points rather than requiring
a hit on its capsule.

IsHidden is queried, but grants no automatic invisibility or range discount.
Cover must block both head and body rays. The shelter's rear/sides conceal the
player; its open front may expose them. An AI that naturally walks around cover
may see the player. No explicit spot inspection, occupant query, or hidden-player
position shortcut exists in the brain.

Holding breath suppresses the existing breathing events. Exhaustion emits the
existing stronger ForcedBreath event, which can redirect an investigation when
audible without guaranteeing visual detection. Releasing breath resumes ordinary
low-range breathing. You cannot hold indefinitely: manage the existing resource
and cooldown. Normal breathing can also be heard when close enough.

Flashlight remains usable and is never switched off by AI/hiding. Its IsActive
state is available through CryptidSenses and displayed for future integration.
There is deliberately no beam detection, visibility bonus, or darkness rule.

## All provisional AI tuning and saved configuration

These defaults are temporary playtest values, not final creative decisions.
Existing M1-M3 player values are unchanged.

| Component | Setting | Provisional value |
| --- | --- | --- |
| CryptidSenses | Sight range / full FOV | 12 m / 100 degrees |
| CryptidSenses | Body sample offset | Local (0, 0.9, 0) |
| Eyes marker | Position | Local (0, 1.7, 0) |
| CryptidSenses | Occlusion layers | Default raycast layers (all except Ignore Raycast) |
| CryptidSenses | Hearing range multiplier | 1 |
| CryptidSenses | Maximum hearing range | 20 m |
| CryptidSenses | Minimum noise intensity | 0.05 |
| CryptidBrain | Investigate speed | 2.5 m/s |
| CryptidBrain | Chase speed | 4.5 m/s |
| CryptidBrain | Search speed | 1.8 m/s |
| CryptidBrain | Return speed | 2.5 m/s |
| CryptidBrain | Search duration / radius | 8 sec / 3 m |
| CryptidBrain | Time per search point | 2 sec |
| CryptidBrain | Stationary search scan | 90 degrees/sec |
| CryptidBrain | Investigation / return travel timeout | 15 sec each |
| CryptidBrain | Chase repath interval | 0.2 sec |
| CryptidNavigation | Destination sampling radius | 2 m |
| CryptidNavigation | Arrival tolerance beyond agent stopping distance | 0.2 m |
| NavMeshAgent | Type / area mask | Existing Humanoid (0) / all areas |
| NavMeshAgent | Radius / height / base offset | 0.5 m / 2 m / 0 |
| NavMeshAgent | Initial speed / acceleration / angular speed | 2.5 m/s / 12 m/s² / 240 degrees/sec |
| NavMeshAgent | Stopping distance | 0.8 m |
| NavMeshAgent | Auto braking / auto repath / off-mesh traversal | On / on / off |
| NavMeshAgent | Avoidance quality / priority | High quality / 50 (Unity defaults) |
| NavMeshSurface | Agent / collection / geometry | Humanoid / children / physics colliders |
| NavMeshSurface | Layer mask / default area | Default raycast layers / Walkable |
| NavMeshSurface | Voxel override / minimum region area | 0.1 m / 2 m² |
| NavMeshSurface | Tile size / height mesh / generated links | Default 256 / off / off |
| Existing Humanoid bake settings | Radius / height / slope / climb | 0.5 m / 2 m / 45 degrees / 0.75 m |
| CryptidDebugView | Show HUD / draw gizmos | Both on |

The cryptid spawn is sampled onto the baked floor near (0, 0, 5). Its initial
position is captured at runtime as its return point; no AI code contains those
scene coordinates. The prefab's player references are intentionally unassigned;
the saved scene assigns them. When adding another cryptid instance, assign the
player, player head, hiding, breath and flashlight references in CryptidSenses.

## Navigation and changing the map

The installed AI Navigation package (2.0.12) and existing Humanoid agent settings
are reused. `Milestone 4 Navigation Geometry` parents the copied environment and
contains NavMeshSurface. Player and cryptid stay outside that hierarchy so they
are not baked as static obstructions. No project-wide NavMesh settings changed.

For future blockout edits, keep solid environment colliders under that root and
rebake its NavMeshSurface in the Inspector, then save **only the M4 scene/data**.
New maps should have their own surface/data. Regeneration is not automatic, and
the scene-creation menu refuses to overwrite M4 assets. The bake is static;
runtime moving obstacles and navigation links are not part of this milestone.

## Debugging

The top-right panel shows state/reason, current visual result and sight reason,
range/FOV, last received noise category/effective radius and acceptance result,
investigation target, active last-known position, search time and navigation
status. Hiding, holding breath and flashlight rows are designer diagnostics only.
CryptidBrain also serializes current runtime state/memory/timers for the Inspector.

During Play with Scene Gizmos enabled: cyan sphere and edge rays = sight range
and horizontal FOV bounds (actual sight test is a 3D cone); yellow = last noise
and effective hearing radius; red = active remembered location; blue = search
area; white = current NavMesh path. The existing bottom-left noise HUD still
shows source events, including green breathing and magenta forced breath.
The latest received sound may be rejected: use the acceptance text and red memory
marker to distinguish it from the sound currently being investigated.

## Manual playtest checklist

Restart Play Mode between scenarios when useful; there is no reset/death system.
Use a large/maximized Game view for the temporary HUD and Scene view for gizmos.

1. **Baseline/navigation:** open M4 and Play. Check Idle and no initial chase.
   Locate the orange capsule north of the start; the protrusion marks forward.
   Verify no off-NavMesh errors. Leave M1-M3 open only in separate test sessions.
2. **Hearing bounds:** approach the cryptid from behind without entering its FOV.
   Walk farther than 4 m: events should be rejected. Walk within 4 m: Investigate
   should begin. Repeat with sprint noise outside/inside 10 m. Later sight may
   legitimately take priority; check the reason text. Use Scene gizmos to measure.
3. **Debris snapshot:** cross/interact with the existing debris at (0, 0, -2).
   The cryptid starts about 7 m away, inside its 18 m event range. Move quietly
   away behind cover. The target should stay at the sound position unless another
   audible event or sight updates it. Test outside 18 m after repositioning via
   normal movement or temporarily changing the scene-instance spawn before Play.
4. **Hide and survive:** generate debris noise, then get into the shelter at
   (3, 0, -8) before being seen. Hold Left Ctrl during close passes. The rear wall
   should conceal you from the north. Watch Investigate → Search → Disengage →
   Idle if no new evidence arrives. Hold breath strategically (capacity is only
   5 sec); recovery/quiet breathing and timing matter. An exposed front view or
   audible new breath can legitimately prevent disengagement.
5. **Breath failure:** with the cryptid within 12 m and cover between you, hold
   until exhaustion. Check a ForcedBreath event redirects the target to that
   event position without automatically claiming visual contact. Beyond the
   effective range it should be rejected. Compare normal 2 m breathing and silence
   during a successful hold.
6. **Sight:** stand still outside hearing influence, then enter the forward cone
   within 12 m. Expect Chase. Try behind the cryptid, beyond range and behind the
   occlusion wall: no visual contact. A head visible above/around cover can count.
7. **Chase/loss:** run around the existing occlusion wall and slalom blocks.
   Check path following and the 4.5 m/s chase. Break LOS and stop making audible
   noise. Expect pursuit of the last-seen location, then search, then return.
   Move silently elsewhere: the red location marker must not follow you through
   walls. Reappear in the cone and confirm reacquisition. Reaching you does not kill.
8. **Hiding is not invisibility:** allow an unobstructed look through the shelter's
   open front while hidden. Expect visual detection. Compare the fully blocked
   rear/side view. No automatic hiding-spot inspection should occur.
9. **Flashlight:** enter/exit with F on; verify it stays on and drains. Toggle F
   inside. Its debug state should change, but sight/hearing rules should not.
10. **Regression:** verify WASD/look/Shift/stamina, E switches and batteries,
    walking/sprint/debris noise, hidden movement lock, breath release/exhaustion,
    cooldown, Escape/click focus, and stop/restart initialization. Open M1-M3
    separately and test their previous checklists; those saved assets are retained.
11. **Map iteration:** move a block in an M4 working copy, rebake the surface and
    verify paths go around it. Test an inaccessible noise point: search must time
    out and return to Idle rather than pursue forever or teleport.

Judge the provisional speeds, FOV, search behavior and hiding escape opportunities
manually. This prototype does not promise a safe hiding spot or final stealth balance.

## File inventory

Created (each with .meta):

- `Assets/HerFootsteps/Runtime/CryptidSenses.cs`
- `Assets/HerFootsteps/Runtime/CryptidNavigation.cs`
- `Assets/HerFootsteps/Runtime/CryptidBrain.cs`
- `Assets/HerFootsteps/Runtime/CryptidDebugView.cs`
- `Assets/HerFootsteps/Editor/Milestone4Setup.cs`
- `Assets/HerFootsteps/Tests/EditMode/Milestone4Tests.cs`
- `Assets/HerFootsteps/Tests/PlayMode/Milestone4PlayTests.cs`
- `Assets/HerFootsteps/Prefabs/TestCryptid.prefab`
- `Assets/HerFootsteps/Settings/Milestone4NavMesh.asset`
- `Assets/Scenes/Milestone4_Test.unity`
- `Assets/Docs/MILESTONE_4_PLAYTEST.md`

Modified: `Assets/HerFootsteps/Editor/HerFootsteps.Editor.asmdef` (existing
Unity.AI.Navigation assembly reference for baking); `Assets/Docs/AI_DESIGN.md`,
`GAMEPLAY_SYSTEMS.md`, and `DEVELOPMENT_PLAN.md` (confirmed M4 behavior/scope).
No existing runtime player/noise/breath scripts, old scenes/prefabs, input actions,
packages, project navigation settings or build profiles are modified.

## Verification

Unity compiled the runtime/editor scripts and generated the saved scene, cryptid
prefab and NavMesh data. Edit Mode: 34 passed, 0 failed across M1-M4. Includes all
five noise categories inside/outside range, intensity/cap checks, sight distance,
FOV/occlusion, exposed hiding, and saved asset checks.

Play Mode: **8 passed, 1 failed**. All four new M4 saved-scene integration tests
passed: actual traversal around a wall and unmapped-target fallback; noise
snapshot → investigation → search → disengage → idle with a concealed player;
held-breath suppression and forced-breath redirection via the shared channel;
and visual loss freezing memory followed by reacquisition. M2 and M3 integration
and existing synthetic input tests also passed. The M1 saved-scene test was
interrupted by an unexpected Unity AI package `NoSubscription` log, not a failed
gameplay assertion. This is the previously observed external package diagnostic;
no package changes or log suppression were introduced. The full suite is not
green and final M1 regression remains a manual check. No prolonged harness work.

Tests temporarily shorten search/travel timers in isolated test instances; the
saved scene retains the provisional values above. The real-time concealed search
test uses 3 sec rather than the saved 8 sec. All M1-M3 gameplay files, scenes and
prefabs are unchanged in the working diff. Local evidence (ignored by Git):
`Logs/Milestone4-setup.txt`, `Milestone4-editmode.xml/.txt`, and
`Milestone4-playmode.xml/.txt`. Final keyboard/gameplay evaluation is manual;
no packaged Windows build was created or tested.

Visual editor preview confirmed the baked navigation surface and sight gizmos,
and the new AI HUD alongside the three existing panels in Full HD Game view.
The scene starts Idle without immediate visual detection. `git diff --check`
passed. The M4 scene is left open outside Play Mode for the designer's manual test.
