# Her Footsteps - AI Development Instructions

## Purpose

This repository contains the Unity project for **Her Footsteps**
(working alternate title: **When the Forest Falls Silent**), a 3D
first-person psychological horror game.

Before changing gameplay code, scenes, prefabs, project settings,
packages, or important assets, read the documents in `Docs/`.

## Read Order

1.  `Docs/GAME_DESIGN.md`
2.  `Docs/PROTOTYPE_SCOPE.md`
3.  `Docs/GAMEPLAY_SYSTEMS.md`
4.  `Docs/AI_DESIGN.md`
5.  `Docs/LEVEL_DESIGN.md`
6.  `Docs/DEVELOPMENT_PLAN.md`

## Development Rules

-   Treat the documentation as the current design direction, not as
    permission to invent missing design decisions.
-   If a required behavior is marked **TBD**, ask before making a major
    design decision.
-   Do not expand scope without approval.
-   Do not silently replace established mechanics with alternatives.
-   Preserve working systems unless a change is necessary and explain
    significant architectural changes.
-   Prefer modular, readable C# components with clear responsibilities.
-   Expose gameplay tuning values in the Unity Inspector where
    practical.
-   Avoid hard-coding scene-specific references when a reusable
    reference or configuration is more appropriate.
-   Use placeholder art/audio when final assets are unavailable;
    gameplay functionality comes first.
-   Do not assume current models, props, materials, lighting, terrain,
    or map layout are permanent.
-   Keep gameplay systems as independent as practical from specific art
    assets and exact level geometry.
-   Prefer interfaces, events, configurable references, tags/layers,
    ScriptableObjects, or other maintainable patterns where appropriate
    rather than tightly coupling systems to a particular model.
-   Existing models, map layout, environment art, and content may change
    frequently during development.
-   New systems may be added as the design evolves. Check these
    documents before beginning a new feature.
-   After implementation, check for compile errors and Unity Console
    errors when tooling permits.
-   Work on one clearly defined feature or milestone at a time unless
    explicitly instructed otherwise.
-   Never delete or broadly restructure unrelated project content merely
    to solve a local issue.
-   Before large or destructive changes, explain the plan and affected
    files.

## Design Authority

The human designer owns creative direction. The AI is an implementation
and prototyping partner.

When documentation conflicts: 1. Follow the newest explicit instruction
from the designer. 2. Follow `PROTOTYPE_SCOPE.md` for prototype scope.
3. Follow the relevant specialized design document. 4. Flag unresolved
conflicts rather than guessing.

## Documentation Maintenance

These documents are living project documentation.

When an approved implementation materially changes how a documented
system works, propose the corresponding documentation update. Do not
assume a temporary implementation or placeholder asset represents a
permanent design decision.
