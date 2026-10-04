# Her Footsteps - Development Plan

## Current Production Intent

The first deliverable is a compact gameplay test on Windows with
keyboard/mouse, not the full 15-30 minute vertical slice. Expanding scope
requires designer approval after reviewing development and playtests.

Confirmed controls and movement: stamina-limited running, no crouching,
tap for standard interactions, and hold the interaction input for clue
inspection. The early forest includes a configurable safe period that
blocks all hunt triggers while the player learns the objective,
controls, and basic systems. Timer start and composure/hallucination
behavior during this period remain TBD.

Temporary Inspector tuning values may be proposed for designer review
and playtesting; they are not final design decisions. Development
blockouts are temporary testing spaces. Final level layout and
environment design remain designer-controlled.

The designer has supplied audio and environment assets. Supplied audio
may be used during approved implementation; report the exact clips used
and preserve their attribution/license information. Asset availability
does not authorize beginning gameplay implementation.

The project is being developed iteratively. Planning documents estimate
approximately **320 hours** across design, programming,
level/environment work, art, audio, animation, UI, playtesting, and
finalization.

The schedule is a planning estimate, not a requirement for the AI to
complete everything at once.

## Recommended Dependency-Driven Order

### Milestone 1 - Player Movement and Interaction Test

Approved scope: Windows keyboard/mouse first-person movement, WASD,
mouse look, stamina-limited Left Shift sprint, tap E interactions, and
a temporary movement/interaction blockout. No crouching or jumping.
The interaction contract supports future hold-based targets without
implementing the clue system. No later-milestone systems are included.

See `MILESTONE_1_PLAYTEST.md` for implemented assets, provisional values,
verification, and the playtest checklist. These values and the test
layout are provisional. Further milestones require designer approval.

### Phase 1 - Foundation

Milestone 1 has been playtested by the designer: movement, stamina-limited
sprinting, and interaction are working.

### Milestone 2 - Flashlight, Resources, and Noise Test

Approved scope: a toggleable battery-powered flashlight, direct battery
pickups through the existing interaction system, reusable spatial noise
events from walking/sprinting and a temporary environmental obstacle,
and lightweight test feedback. Extend the current architecture and
blockout; do not replace the working controller. No full inventory,
composure, hallucinations, AI, hunts, hiding, clues, or progression.

See `MILESTONE_2_PLAYTEST.md` for provisional tuning, assets, verification,
and playtest guidance. Stop after this milestone pending designer review.

### Milestone 3 - Hiding and Hold Breath Test

Approved scope: reusable hiding spots, E entry/exit, movement lock with retained
look and flashlight control, limited held breath on Left Ctrl, forced recovery,
and breathing noise through the existing NoiseChannel. A separate M3 scene and
player variant preserve the saved M1/M2 scenes and prefabs. All new tuning is
provisional. See `MILESTONE_3_PLAYTEST.md` for inventory and validation. Stop after
Milestone 3; cryptid AI, hunts and other later systems remain unimplemented.

### Foundation dependency checklist

1.  Inspect Unity project configuration.
2.  Establish project conventions/folder structure only where needed.
3.  Core first-person player movement, including stamina-limited running.
4.  Reusable interaction foundation.

### Phase 2 - Core Horror Inputs

5.  Noise System.
6.  Flashlight System.
7.  Basic inventory/resource support.
8.  Composure System.

### Phase 3 - Environment Interaction

9.  Noise obstacles.
10. Clue System.
11. Hiding System.
12. Hold Breath.

### Phase 4 - Cryptid

13. Cryptid navigation.
14. Noise investigation.
15. Hunt activation with the configurable early-forest safe-period gate.
16. Search behavior.
17. Detection.
18. Chase.
19. Disengagement.

### Phase 5 - Psychological Systems

20. Hallucination framework.
21. Simple hallucinated creature/reality-check interaction.
22. False trails and other approved perception changes.

### Phase 6 - Progression and Failure

23. Objective/progression system.
24. Early -\> Mid -\> Deep escalation.
25. Death/restart/checkpoint behavior.

### Phase 7 - Prototype Level

26. Forest blockout.
27. Exploration paths and landmarks.
28. Gameplay/hunt encounter areas.
29. Environmental clues/storytelling.
30. Lighting/atmosphere.

### Phase 8 - Feedback and Presentation

31. Interaction prompts.
32. Flashlight/battery feedback.
33. Inventory UI.
34. Objective/progression feedback.
35. Forest ambience and footstep/noise audio.
36. Hunt silence transition and cryptid audio.
37. Required animations/VFX.

### Phase 9 - Playtest and Iterate

38. Test core mechanics.
39. Test cryptid AI.
40. Evaluate navigation/exploration.
41. Evaluate horror pacing/tension.
42. Implement playtest feedback.

### Phase 10 - Demo Finalization

43. Fix critical bugs.
44. Balance/tune systems.
45. Prepare and test demo build.

## Original Planning Estimates

### Planning & Game Design - 22 hours

-   Prototype system priorities: 2h
-   Prototype level progression: 5h
-   Completion requirements: 3h
-   Resource lists: 7h
-   Dependencies/development order: 5h

### Programming - 88 hours

-   Core movement: 10h
-   Flashlight and inventory: 14h
-   Interaction and hiding: 14h
-   Cryptid AI: 24h
-   Objective/progression: 12h
-   Gameplay events/encounters: 14h

### Level Design & Environment - 55 hours

-   Forest blockout: 12h
-   Exploration paths/landmarks: 10h
-   Gameplay/encounter areas: 12h
-   Environmental clues/storytelling: 9h
-   Atmosphere/lighting: 12h

### Art & Visual Production - 36 hours

### Audio Production - 27 hours

### Animation - 29 hours

### UI & Player Feedback - 22 hours

### Playtesting & Iteration - 23 hours

### Finalization - 17 hours

## AI Work Protocol

For each requested implementation task: 1. Read the relevant
documentation. 2. Inspect existing implementation before changing it. 3.
State a short implementation plan. 4. Identify assumptions/TBD decisions
that would materially affect design. 5. Implement only the requested
scope. 6. Compile/test when possible. 7. Report files changed and what
was verified. 8. Flag any documentation that should be updated.

## Living Project Policy

Models, map layout, assets, tuning values, and even system requirements
may change.

New systems can be added later by: - adding a section to the appropriate
existing document, or - adding a new specialized Markdown document if
the system becomes large enough to justify it.

Do not create one document per tiny mechanic. Split documentation when a
topic becomes difficult to navigate or has substantially different
implementation concerns.
