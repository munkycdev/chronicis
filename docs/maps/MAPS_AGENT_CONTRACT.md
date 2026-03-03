# MAPS_AGENT_CONTRACT

## Implementation Boundaries

* Do not refactor unrelated systems.
* Do not modify existing tree grouping logic beyond adding one Maps group.
* Do not introduce layer UI in MVP 0 or MVP 1.
* Do not implement non-point geometry.
* Do not reuse WorldDocument services.

---

## Folder Structure Expectations

API:

* Features/Maps/*

Client:

* Pages/Maps/*
* Services/Maps/*

Shared:

* Shared/Maps/*

---

## Testing Requirements

* Add unit tests for:

  * Default layer creation
  * Map grouping rules
  * Pin layer selection rule
  * Authorization enforcement

All tests must pass `./scripts/verify.ps1`.

---

## Performance Constraints

* Do not load geometry blobs during map metadata listing.
* Keep map page rendering minimal (basemap + pins).

---

## Prohibited Changes

* No tree system rewrites.
* No new global abstractions unless required.
* No breaking changes to existing routes.

---

## Definition of Done (General)

* Builds clean.
* Tests pass.
* Manual test checklist in phase doc is satisfied.
