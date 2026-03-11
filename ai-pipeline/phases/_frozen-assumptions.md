# Frozen Assumptions

- This feature is being implemented inside an existing repository, not a greenfield redesign.
- Existing authentication remains unchanged unless a phase explicitly changes it.
- Existing authorization patterns remain unchanged unless a phase explicitly changes them.
- Existing client transport and token attachment patterns remain unchanged unless a phase explicitly changes them.
- Existing repo-wide architecture and hosting choices remain unchanged unless a phase explicitly changes them.
- Do not propose repo-wide redesigns while planning or reviewing a narrow feature phase.
- Preserve existing public contracts unless the phase explicitly changes them.

## Map Feature Constraints

Map feature implementation must extend the existing map architecture.

Do not redesign the map system.

Do not replace the existing basemap + overlay rendering approach.

Do not introduce GIS libraries or mapping frameworks.

Do not introduce spatial databases or SQL spatial features.

Geometry operations should remain minimal and feature-focused.

## Geometry Format Constraints

Polygon geometry must use the GeoJSON-inspired structure defined in the architecture document.

Do not introduce alternate geometry formats.

Do not invent custom coordinate systems.

Coordinates remain normalized image-space values.

## UI Guardrails

New UI must follow existing styling and component patterns.

Do not introduce new visual design systems.

Prefer extending existing components and patterns over introducing new ones.

New UI should appear as a natural continuation of the existing interface.

## Scope Guardrails

Each implementation phase should deliver only the capabilities defined in that phase.

Do not implement future phases early.

Do not add speculative capabilities not required by the current phase.


