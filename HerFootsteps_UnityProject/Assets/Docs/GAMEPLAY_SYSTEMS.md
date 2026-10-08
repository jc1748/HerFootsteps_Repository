# Her Footsteps - Gameplay Systems

## 1. Player Movement

Initial target: Windows with keyboard/mouse controls.

### Walking

-   Slow, quiet player-controlled movement.
-   Critical prototype mechanic.
-   Should communicate with the Noise System.

### Running

-   Faster movement.
-   Running is stamina-limited. Stamina capacity, drain, recovery, and
    exhaustion behavior remain TBD.
-   Produces more noise than walking.
-   High-priority prototype mechanic.
-   Should communicate with the Noise System.

### Crouching

Excluded from the current prototype.

## 2. Noise System

Purpose: make movement and environmental interaction mechanically
meaningful.

Noise can come from: - Running. - Footsteps/surface types. - Branches,
leaves, glass, trash, and other noise obstacles. - Player screams caused
by failed hallucination reactions. - Potentially humming/singing used to
regain composure.

Noise should be consumable by cryptid investigation/hunt logic. Exact
values and thresholds are TBD and should be tunable.

Milestone 2 implements a test event channel with category, world position,
intensity, range, timestamp, source, and instigator. Walking/sprinting
emit based on actual grounded travel. A temporary debris obstacle emits
on traversal or interaction with a cooldown. Range/intensity are data for
future listeners, not finalized hearing, attenuation, occlusion, or hunt
rules. A debug HUD and range gizmos expose these events without requiring
audio clips. See `MILESTONE_2_PLAYTEST.md` for provisional values.

Milestone 4 adds a cryptid listener that compares event position/range, a
configurable range multiplier/cap and intensity threshold. Accepted sounds
provide investigation snapshots, not continuous player tracking. These hearing
rules are provisional and do not implement noise-triggered hunts. See
`MILESTONE_4_PLAYTEST.md` and `AI_DESIGN.md` for confirmed behavior.

## 3. Composure System

Purpose: track the player's psychological state and change
gameplay/perception as it deteriorates.

### Regaining Composure

Major recovery can come from finding the sister's belongings, such as: -
Piece of sweater. - Drawing. - Shoe.

Possible smaller recovery sources: - Small campfires. - Benches. -
Humming/singing, with the tradeoff that it creates noise.

### Losing Composure

Composure decreases while wandering/exploring. As it falls, possible
effects include: - False trails. - Misleading sounds. - Disappearing
footprints. - Wildlife appearing monstrous. - Louder breathing. - False
landmarks. - Increased chance of dangerous hallucination reactions.

### States

**High** - Forest mostly behaves normally.

**Medium** - Hallucinations begin. - False trails may appear. -
Unsettling sounds become more frequent.

**Low** - Hallucinations become more aggressive. - Screams become more
likely.

**Broken** - Triggers a hunt subject to the Hunt System's safe-period
gate. No hunt can trigger while the early-forest safe period is active.

Exact thresholds and drain/recovery values are TBD.

Composure behavior during the early-forest safe period remains TBD.

Milestone 5 implements `PlayerComposure` (0-100) with High/Medium/Low/Broken
thresholds, signed `Apply` events, state transitions and a `ComposureChannel`
ScriptableObject for external listeners. Broken publishes once per entry; it
does not start a hunt or change cryptid AI. `ComposureRateSource` supports optional
box volumes, independently enabled rates, recovery ceilings and persistent
per-session budgets. `CryptidComposureSource` observes proximity, visual detection
and Chase through an optional adapter. Passive and all cryptid sources start off
for isolated tests. Interactable sources, a first-discovery keepsake bridge and
an optional inventory recovery consumable demonstrate independent integrations.
Hiding does not restore composure. Exact provisional values and controls are in
`MILESTONE_5_PLAYTEST.md`; these do not settle final safe-period design.

## 4. Flashlight System

Purposes: - Navigation. - Illumination. - Resource pressure. -
Reality-check/defense mechanic.

The flashlight should support battery/resource use.

Milestone 2 adds F to toggle a model-independent light, battery capacity
and active-only drain, and temporary battery HUD feedback. Other systems
can query whether it is active. Battery depletion switches it off; an
empty light cannot turn on. No hallucination/reality-check behavior is
implemented in that milestone. Initial values remain provisional. M5 adds the
separate wildlife flashlight reaction described below without changing the
flashlight resource/toggle implementation or cryptid sight rules.

### Hallucination Reaction

At low composure, harmless wildlife may be perceived as monstrous. - A
hallucinated creature may jump at the player. - The player reacts by
using the flashlight. - Successful reaction reveals/reverts the
perceived threat and can reward composure. - Failure can cause the
player to scream. - A scream creates a major noise event and may trigger
a hunt.

Exact input timing and flashlight interaction rules are TBD.

## 5. Inventory System

Simple backpack inventory. - Approximately 3-5 slots. - No weight. - No
crafting. - No item combining.

Potential items: - Batteries. - Flares. - Matches. - Medical supplies.

Only items required for the current prototype milestone should be
implemented.

Milestone 5 provides `PlayerInventory`, Inspector capacity 5, atomic pickups,
stack limits, selection/description inspection, use and removal. ScriptableObject
`ItemDefinition` assets distinguish stack size, consumability, removal permission
and optional reusable `ItemUseEffect`. Battery use restores up to 40 charge and
consumes one only if charge changed; full charge keeps the item. M5 pickups store
items using the existing E interaction. Earlier milestone scenes retain their
historical direct-refill battery pickups. A persistent test keepsake, removable
non-stackable stones and consumable recovery tokens exercise extension points.
No crafting, equipment, weight, save persistence or clue progression is included.

Tab opens a temporary inventory; mouse selects slots and clicks Use/Remove.
Tab/Esc/Close returns control. Modal input blocks walking, look, F, E and breath
input while open, without clearing hiding's movement lock. The world, AI, breath
recovery, composure and flashlight drain continue. Opening inventory therefore
releases held breath; it is not a safe pause. Removal discards without spawning
a world pickup. Old controls remain unchanged when inventory is closed.

## 6. Resource System

Controls limited supplies such as flashlight batteries.

Purpose: - Create survival pressure. - Encourage exploration. - Prevent
unlimited reliance on safety tools.

Exact resource economy is TBD.

Milestone 2 tests batteries as direct E-interaction refills, without an
inventory. A pickup restores its configured amount up to capacity and
is consumed only when some charge is restored. Excess charge is discarded;
full charge leaves the pickup available. Refilling does not automatically
switch an extinguished flashlight back on. These are provisional test
behaviors, not a finalized inventory or resource economy.

## 7. Interaction System

Standard interactions use a tap. Clue inspection requires holding the
same interaction input. Hold duration, release behavior, and inspection
presentation remain TBD.

Required foundation for: - Clue inspection. - Item pickup. - Hiding
locations. - Environmental interactions. - Potential resource/safety
locations.

Interaction behavior should remain reusable across changing
props/models.

## 8. Clue System

Tracks discoveries related to the missing sister and narrative
progression.

Clues can include: - Sister's belongings. - Drawings. - Environmental
evidence.

Clues should support progression through the forest and may restore
composure.

## 9. Hallucination System

Hallucination behavior during the early-forest safe period remains TBD.

Depends on Composure.

Potential effects: - False trails. - Misleading
audio. - Monstrous versions of harmless wildlife. - Environmental
uncertainty.

Hallucinations should escalate with composure loss. Individual
hallucination types should remain modular so they can be added/removed
during iteration.

M5 `HallucinationEvent` is the reusable lifecycle: composure state subscription,
minimum state, activation range/condition, active duration, cooldown and end
reason. Derived events own separate, collider-free visual roots; real geometry
and baked navigation are untouched. Debug triggers respect the same gates.
`FalseTrailHallucination` toggles misleading placeholder markers.
`WildlifeHallucination` activates within its view/occlusion condition and permits
flashlight dismissal using active charge, beam range/cone and line of sight.
Failure emits one Scream event at the player's position via the shared
NoiseChannel. Recovery above the configured severity immediately removes active
hallucinations without a failure scream. Timed/failed/dismissed/recovered events
all enter cooldown. No permanent world changes or full cinematic/VFX systems.
The test defaults both examples to Low/Broken; Medium behavior remains future
content. See `MILESTONE_5_PLAYTEST.md` for the provisional timings and ranges.

## 10. Hiding System

Valid environmental hiding locations may include: - Hollow logs. -
Bushes. - Structures or other approved environmental cover.

During a hunt, hiding should affect the cryptid's ability to detect/find
the player.

The system should not depend on one specific hiding-spot model.

Milestone 3 implements reusable HidingSpot markers and PlayerHiding.IsHidden.
E enters through the existing interaction system and exits regardless of view
direction. Hidden movement is locked while look, F flashlight control and battery
drain continue. IsHidden grants no universal invisibility; detection remains TBD.
See `MILESTONE_3_PLAYTEST.md` for scene setup and provisional behavior.

Milestone 4 tests geometric visual concealment: both head and body sight samples
must be blocked by cover. An exposed hidden player can be detected. Held breath
suppresses existing breathing events; audible forced breathing can redirect the
cryptid without granting automatic visual detection. No scripted spot inspection.

## 11. Hold Breath

Used while hiding during a cryptid search. - Player holds breath. - A
breath meter/limit increases or depletes over time. - Player should
remain still. - Releasing/failing breath control may create detectable
sound.

Exact failure rules are TBD.

Milestone 3 provisionally uses held Left Ctrl while hidden. A separate breath
resource drains, recovers after release, and forces a cooldown plus input release
after depletion. Successful holds suppress breathing noise; normal hidden breaths
and stronger forced breaths use the existing NoiseChannel and debug range gizmos.
Values and failure behavior are provisional; see `MILESTONE_3_PLAYTEST.md`.

## 12. Hunt System

### Early-Forest Safe Period

A configurable safe period prevents all hunts, including those normally
triggered by noise, screams, or broken composure. Its purpose is to let
the player learn the objective, controls, and basic systems before
facing the cryptid.

The timer's starting event and composure/hallucination behavior during
this period remain TBD. Duration is configurable, with no final value
selected. Treatment of accumulated triggers at expiry is also TBD;
no automatic hunt or trigger-discarding rule has been approved.

### Triggers

A hunt may begin outside the safe period when: - Player creates too much noise. - Player
screams. - Composure reaches the broken state.

### Warning

-   Normal forest ambience fades/stops.
-   Forest becomes unnaturally silent.
-   A distant cryptid roar or equivalent warning cue occurs.

### Search Phase

-   Cryptid becomes active.
-   Player should turn off or carefully manage the flashlight.
-   Player moves carefully.
-   Player seeks a hiding location.

### Hiding / Search

-   Cryptid searches nearby.
-   Player uses hold breath and remains still.

### Resolution

If the player survives: - Cryptid leaves/disengages. - Player regains
some control/composure. - Exploration resumes.

If detected: - Cryptid can chase the player. - Failure can lead to
death/restart.

## 13. Progression System

Controls escalation from early forest to mid forest to deep forest.

Progression can affect: - Hallucination frequency. - Noise obstacles. -
Supply availability. - Composure pressure. - Hunt frequency. - Cryptid
presence.

Exact progression triggers are TBD.

## 14. Death / Restart

Required for a playable prototype. - Handle player failure. - Restore a
valid playable state. - Restart from an appropriate checkpoint/start
point.

Exact checkpoint structure is TBD.

## System Dependency Summary

-   Player Movement -\> Noise.
-   Interaction -\> Clues, pickups, hiding.
-   Composure -\> Hallucinations and Hunt triggers.
-   Inventory -\> Resource usage.
-   Flashlight -\> Resource usage and hallucination reaction.
-   Noise -\> Cryptid investigation and Hunt.
-   Hiding -\> Cryptid search/detection and Hold Breath.
-   Cryptid AI + Noise + Composure -\> Hunt.
-   Clues -\> Progression.
-   Hunt -\> Death/Restart.
-   Safe-period gate -\> Hunt eligibility for every trigger source.

## Tuning Policy

Temporary Inspector values may be proposed for designer review and
playtesting. They are provisional and do not resolve final design TBDs
or authorize new behavior.
