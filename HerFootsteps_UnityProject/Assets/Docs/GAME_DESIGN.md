# Her Footsteps - Game Design

## Working Title

**Her Footsteps**\
Alternate working title: **When the Forest Falls Silent**

## Genre

3D first-person psychological horror.

Subgenres: - Exploration horror - Survival horror

## Quick Pitch

The player searches a cursed forest for their missing younger sister. As
the player's composure deteriorates, the environment becomes
increasingly unreliable and distorted, leading the player deeper into
the influence of an ancient cryptid.

## Design Intent

The intended player experience is tension and vulnerability while
searching an unfamiliar forest. Psychological horror should come
primarily from uncertainty, unreliable perception, resource pressure,
sound, and loss of control rather than traditional combat.

## Narrative Premise

-   The player's younger sister has been afraid of the woods behind
    their home since the family moved there.
-   She has struggled to sleep and claims to hear noises and dream about
    an ominous beast.
-   The player character, her older brother, camps in the woods with her
    to show her there is nothing to fear.
-   The night initially goes peacefully.
-   The player falls asleep and later discovers that the sister is gone.
-   A torn piece of her sweater near the campsite indicates something
    happened.
-   The player feels responsible and enters the forest to find her.

## Core Gameplay Loop

1.  Explore the forest.
2.  Search for clues about the sister.
3.  Manage composure and limited resources.
4.  Avoid unnecessary noise.
5.  Survive a hunt when the cryptid becomes active.
6.  Regain control and continue the search.

## Primary Mechanics

Initial target: Windows with keyboard/mouse controls.

### Movement

-   Walking
-   Stamina-limited running.
-   Crouching is excluded from the current prototype.

### Searching

-   Inspect clues by holding the interaction input.
-   Use a tap for standard interactions.
-   Gather supplies.
-   Discover the sister's belongings.

### Flashlight

-   Illuminate the environment.
-   Serve as a defensive/reality-check tool against hallucinated
    threats.

### Hiding

-   Enter hiding locations.
-   Hold breath during dangerous moments.

### Inventory

-   Pick up items.
-   Store a limited number of items.
-   Use supplies.

## Gameplay States

### Exploration

-   Navigate forest pathways.
-   Avoid noisy environmental obstacles such as leaves, branches, broken
    glass, and trash.
-   Use the flashlight to search for clues and the sister's belongings.

### Survival

-   Manage light sources.
-   Conserve supplies.
-   Search campsites and the environment for resources.
-   Avoid excessive noise.

### Hiding / Hunt

-   Excessive noise can trigger a cryptid hunt.
-   Reaching broken composure can also trigger a hunt.
-   The player must use careful movement, darkness, hiding, and breath
    control to survive.

## Progression

The timings below describe the larger vertical-slice direction, not the
duration of the first deliverable, which is a compact gameplay test.

### Early Forest - approximately 1-5 minutes

-   A configurable safe period prevents all hunts so the player can learn
    the objective, controls, and basic systems before facing the cryptid.
-   The safe-period timer's starting event and composure/hallucination
    behavior during this period remain TBD.
-   Mostly navigation.
-   Supplies are available.

### Mid Forest - approximately 5-15 minutes

-   More false trails.
-   More noise obstacles.
-   Supplies become limited.
-   Hallucinations appear occasionally.

### Deep Forest - approximately 15-30 minutes

-   Frequent hunts.
-   Very limited supplies.
-   Composure deteriorates faster.
-   The cryptid's presence is known.

## Inspirations

### Friday the 13th: The Game

Design references: - Hide-and-seek tension during hunt sequences. -
Hiding spots. - Breath-control mechanics.

### Don't Starve

Design references: - A mental-state system that changes perception. -
Visual/gameplay distortions as the mental state deteriorates. - Innocent
creatures appearing threatening.

### Blair Witch

Design references: - Becoming lost in a supernatural forest. - The
forest itself functioning as an antagonist.

## Creative Principles

-   No conventional combat loop.
-   The forest should become less trustworthy as composure falls.
-   Sound should matter mechanically, not only atmospherically.
-   The flashlight should have more purpose than simple illumination.
-   The cryptid should feel threatening because the player must evade it
    rather than defeat it.
-   Navigation and environmental readability can intentionally degrade,
    but the experience should remain playable rather than arbitrary.

## Open Design Questions

These should not be decided by the AI without approval: - Exact cryptid
visual design. - Exact detection rules and ranges. - Exact composure
values/rates. - Exact noise thresholds and values. - Stamina rules and
values. - Safe-period timer start and composure/hallucination behavior
during it. - Final item list and resource balance. - Exact
ending/resolution of the prototype.

Temporary Inspector tuning values may be proposed for designer review
and playtesting; they are not final design decisions.
