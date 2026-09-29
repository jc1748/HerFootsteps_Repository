# Her Footsteps - Level and Environment Design

## Environment Role

The forest is both the main playable environment and a psychological
antagonist. It should initially be readable enough to explore, then
become increasingly unreliable as composure and progression change.

## Required Prototype Spaces

### Starting Campsite

-   Opening location.
-   Establishes the sister's disappearance.
-   Contains initial narrative evidence such as the torn sweater clue.
-   Should orient the player before deeper exploration.

### Forest Paths

-   Primary navigation structure.
-   Guide the player through the prototype.
-   Should support recognizable landmarks and controlled branching.

### Dense Forest / Off-Path Areas

-   Restrict or complicate navigation.
-   Create natural boundaries.
-   Help prevent the level from feeling like invisible walls.

### Noise Obstacles

Examples: - Branches. - Leaves. - Broken glass. - Trash.

Placement should create route decisions rather than unavoidable random
punishment.

### Hiding Locations

Examples: - Hollow logs. - Bushes. - Small structures/cover where
appropriate.

Hiding locations should be placed deliberately around hunt encounter
spaces.

### Major Landmarks

Recognizable locations help the player orient themselves.

This is especially important because the hallucination system can later
make navigation unreliable.

### False Trails

High-priority psychological/navigation feature. - Appear as composure
deteriorates. - Misdirect the player. - Must be designed so confusion
creates tension without making progress impossible.

### False Landmarks

Optional/medium-priority prototype feature. Hallucinated versions of
familiar landmarks can undermine player trust.

### Clue Locations

Contain the sister's belongings or narrative information. - Drive
exploration. - Can support composure recovery. - Help control
progression.

### Supply Locations

Locations for batteries and other approved resources. Resource placement
should support survival pressure without making the prototype
unwinnable.

### Hunt Encounter Areas

Critical. Must support: - Multiple movement choices. - Cryptid
navigation. - Hiding. - Search behavior. - Potential chase. - Readable
escape/survival decisions.

### Forest Boundaries

Natural barriers such as: - Dense vegetation. - Rocks. -
Cliffs/terrain. - Other believable environmental blockers.

### Deep Forest

Later, more threatening area. - Higher danger. - Stronger cryptid
presence. - Increased psychological distortion. - Reduced resource
availability.

## Level Progression

### Early

Teach navigation, flashlight, clue searching, and noise awareness.

A configurable safe period prevents hunts while the player learns the
objective, controls, and basic systems. When the timer begins and how
composure/hallucinations behave during it remain TBD.

### Mid

Increase branching, noise hazards, resource pressure, and
hallucinations.

### Deep

Deliver the major hunt/cryptid experience and strongest psychological
pressure.

## Iteration Rules

The map layout is expected to change frequently.

Environment blockouts created during development are temporary testing
spaces for the compact gameplay test. They do not establish the final
level layout or environment design, which remain designer-controlled.

Therefore: - Gameplay code should not rely on exact world coordinates
unless clearly necessary. - Use reusable markers/components for clues,
hiding spots, noise hazards, encounter areas, spawn points, and
progression gates. - Models and props should be swappable without
rewriting system logic. - Prefer blockout geometry while testing
navigation and encounters. - Validate gameplay flow before committing to
final art placement.
