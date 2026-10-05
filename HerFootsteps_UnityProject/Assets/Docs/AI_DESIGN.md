# Her Footsteps - AI Design

## Milestone 4 implementation status

The approved AI behavior test now uses CryptidSenses, CryptidNavigation and
CryptidBrain with Idle, Investigate, Search, Chase and Disengage states. It reuses
NoiseChannel events, the existing player/hiding/breath/light states, and the
installed AI Navigation package. Sound supplies a snapshot; confirmed sight
supplies current position. Losing sight stops live tracking immediately and
returns to bounded investigation/search of the last-known location.

Sight requires provisional range/FOV and an unobstructed head or body ray.
Hiding is not invisibility: geometry must conceal both samples. No flashlight
detection rules or special hiding-spot inspection have been added. The scene is
an always-active AI test, not a Hunt Controller or safe-period implementation.
All tuning is provisional; see `MILESTONE_4_PLAYTEST.md` for exact values,
architecture, scene assets and manual validation. Later design sections remain
direction for future work, not a claim that the complete hunt is implemented.

## Cryptid Role

The cryptid is the primary physical threat. It should create fear
through investigation, searching, pursuit, and the player's inability to
defeat it through conventional combat.

## Required Prototype Behaviors

### Navigation

Foundation for all cryptid behavior. - Navigate forest paths and
encounter spaces. - Support changing level geometry during
development. - Avoid dependence on a single final map layout.

### Investigation

Purpose: make player noise meaningful. - Receive information from the
Noise System. - Move toward or investigate relevant sound locations. -
Exact hearing radius, priority, memory, and investigation duration are
TBD.

### Hunt

The Hunt System activates the cryptid during a hunt. - Cryptid becomes
an immediate gameplay threat. - Behavior should support searching,
detection, chase, and disengagement.

Hunt activation must respect the Hunt System's configurable early-forest
safe period. No AI activation path may bypass that protection. Timer
start and composure/hallucination behavior during the period remain TBD.

### Search

-   Search nearby areas after a hunt/noise event.
-   Interact logically with hiding gameplay.
-   Search behavior should create tension without guaranteeing player
    discovery.
-   Exact hiding-spot inspection rules are TBD.

### Detection

Determines whether the cryptid detects the player. Potential inputs may
include: - Visibility. - Distance. - Player movement. - Noise. - Hiding
state. - Breath-control failure.

Exact rules are explicitly TBD.

### Chase

After successful detection: - Pursue the player. - Use level
navigation. - Support failure/death conditions. - Exact speed and escape
rules are TBD.

### Disengage

Determines when the cryptid stops hunting/searching. - Allows the hunt
to resolve. - Returns game to exploration. - Exact timers/conditions are
TBD.

## Hallucinated Creature Behavior

Prototype priority: medium.

At low composure, harmless wildlife can be perceived as threatening. - A
creature can perform a simple hostile/jump-scare approach. - Flashlight
reaction can reveal/revert the hallucination. - Failed player reaction
can cause a scream/noise spike.

Keep this behavior simple for the prototype.

## Ambient Wildlife

Low priority / optional. Primarily supports atmosphere unless required
by the hallucination test.

## AI Architecture Principles

-   Keep sensing, navigation, hunt state, and presentation as separable
    as practical.
-   AI should consume gameplay signals from Noise, Composure, Hiding,
    and Hunt systems rather than duplicating those systems.
-   Expose important tuning parameters.
-   Avoid coupling AI logic to one cryptid model/animation set.
-   Avoid coupling navigation logic to the current temporary forest
    layout.
-   Animation and audio should respond to AI state rather than define AI
    state.

## Suggested State Model

This is an implementation interpretation of the documented behaviors,
not a new design requirement:

Inactive -\> Investigate -\> Search -\> Detect/Chase -\> Disengage

The exact state machine/behavior-tree implementation is a technical
choice and should be proposed before major implementation if it affects
project architecture.
