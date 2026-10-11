# Her Footsteps - Prototype Scope

## Prototype Goal

Create a playable first-person horror prototype that proves the core
experience before production scope expands.

The first deliverable is a **compact gameplay test**, not the full
15-30 minute vertical slice. Scope may expand only with designer approval
after evaluating development progress and playtesting.

Initial target: **Windows with keyboard/mouse**.

The larger vertical-slice target is approximately **15-30 minutes** and
should introduce: - Clue tracking - Composure - Hallucinations - One
complete hunt sequence - A cryptid reveal

## Primary Experience Question

Does managing light, noise, composure, and movement while navigating a
dark forest create tension and make the player alter their behavior when
the cryptid becomes a threat?

## Prototype Must-Haves

### Player

-   First-person player controller.
-   Walking.
-   Stamina-limited running.
-   Flashlight.
-   Standard interactions use a tap; clue inspection requires holding
    the interaction input.
-   Hiding.
-   Hold breath.
-   Simple inventory.
-   Crouching is excluded from the current prototype.

### Core Systems

-   Noise.
-   Composure.
-   Flashlight.
-   Hunt.
-   Hallucination.
-   Clue tracking.
-   Progression.
-   Death/restart.
-   A configurable early-forest safe period blocks all hunt triggers so
    the player can learn the objective, controls, and basic systems.
    Timer start and composure/hallucination behavior during this period
    remain TBD.

### Cryptid

-   Navigation.
-   Investigation of player-generated noise.
-   Search behavior.
-   Player detection.
-   Chase behavior.
-   Disengagement/end-of-hunt behavior.

### Environment

-   Prototype forest.
-   Starting campsite.
-   Navigable forest paths.
-   Noise obstacles.
-   Hiding locations.
-   Landmarks.
-   Clue locations.
-   At least one hunt encounter area.
-   Natural boundaries.

### Feedback

-   Essential interaction prompts.
-   Flashlight/battery feedback.
-   Inventory feedback.
-   Objective/progression feedback.
-   Essential audio cues, especially hunt warning/silence.

## Explicit Scope Limits

The prototype should remain intentionally small.

### Inventory

-   Approximately 3-5 slots maximum.
-   No weight system.
-   No crafting.
-   No item combining.

### Combat

-   No traditional weapon/combat system.
-   The cryptid cannot be killed.
-   Light and limited special resources may protect or temporarily stun
    threats if later approved.

### Content

-   Do not add additional enemy families, large quest systems, skill
    trees, crafting, procedural generation, dialogue trees, or other
    major systems unless explicitly approved.
-   Ambient wildlife is optional/low priority except where needed to
    test hallucination behavior.
-   False landmarks are optional for the prototype.
-   Abandoned campsites are optional unless needed for
    progression/resource testing.

## Definition of Prototype Success

The prototype is successful when a player can: 1. Enter the forest and
understand the immediate search objective. 2. Move through the
environment and recognize that different actions create different risk.
3. Use the flashlight for navigation and at least one meaningful
gameplay interaction. 4. Discover clues/resources. 5. Experience
composure-driven perceptual change. 6. Trigger and understand a hunt. 7.
Use hiding and breath control to survive. 8. Resume exploration after
the hunt or fail and restart cleanly.

## Not a Final-Art Milestone

The designer separately authorized an atmospheric M5 presentation refinement:
existing Conifers URP assets, compact editable forest, persistent hotbar, independent
sister discoveries, passive composure pressure/calm areas and optional debug UI.
This extends presentation within M5 and does not begin M6, a full map, Hunt
Controller, death/restart, final creature art or final narrative progression.

The separately approved Milestone 5 test combines composure, lightweight
inventory and a modular hallucination framework with two playable examples:
false trail markers and flashlight-reactive hallucinated wildlife. The saved
M4 environment is retained in a separate M5 scene. Test recovery/loss objects
and debug controls are temporary. Broken only publishes a reusable event;
failed wildlife reactions feed existing noise investigation. Complete hunts,
safe-period timing, final psychological presentation, clue progression, death,
restart and Milestone 6 remain outside this implementation.

Models, materials, animations, sounds, lighting, vegetation, terrain,
and map layout may all be replaced.

Development environment blockouts are temporary testing spaces. Final
level layout and environment design remain designer-controlled.

Temporary Inspector tuning values may be proposed for designer review
and playtesting. They do not establish final design decisions.

Systems should therefore be designed so content can be swapped without
requiring major gameplay rewrites.
