# Phase 0 Contract

This document locks the Phase 0 architectural decisions for the Chronicis.ResourceCompiler console application.

## Scope
- Phase 0 is scaffolding only. No compiler behavior is implemented beyond stubs.
- No YAML parsing logic, JSON loading logic, indexing, assembly, or output writing behavior is implemented.
- Program.cs remains thin and only wires orchestration.

## Key Canonicalization
- Keys accept only scalar JSON values: string, number, boolean.
- Null, object, and array are invalid and emit typed warnings.
- String keys use the exact value.
- Boolean keys are canonicalized to "true" or "false".
- Number keys use decimal normalization when possible:
  - Parse the JSON number token as decimal when it fits.
  - Serialize as an invariant decimal string with no exponent and no trailing zeros.
  - Normalize -0 to 0.
  - If decimal parsing fails, fall back to invariant raw token text normalization and still normalize -0.
- Equality is the combination of key kind and canonical string.
- Numeric keys treat 1, 1.0, and 1.00 as the same key.

## Ordering
- If orderBy is present, apply stable sort by orderBy.field and orderBy.direction.
- Missing order fields sort last and emit a warning.
- If orderBy is absent, preserve raw file array order exactly.

## Recursion And Max Depth
- Depth starts at 0 for root entities and increments by 1 per child relationship traversal.
- RecursionGuard tracks (entityName, key) along the current path for cycle detection.
- On cycle or max depth exceeded, stop descending for that branch and emit a typed warning.

## JsonElement Lifetime
- RawDataLoader loads each entity file into a JsonDocument owned by RawEntitySet.
- RawEntitySet keeps JsonDocument alive until after output writing.
- DocumentAssembler materializes compiled output into JsonNode or JsonObject and must not retain JsonElement beyond the raw stage.

## Output Layout
- OutputLayoutPolicy generates collision safe entity folder names: <slug>-<shortHash>.
- For each root entity type, write compiled documents to:
  - out/<rootEntitySlug-hash>/documents.json
- Write indexes to:
  - out/<entitySlug-hash>/indexes/by-pk.json
  - out/<entitySlug-hash>/indexes/fk/<childSlug-hash>__<fkFieldSlug-hash>.json
- The relationship index name includes child entity and fk field to avoid collisions.
