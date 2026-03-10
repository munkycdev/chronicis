# MAPS_AGENT_CONTRACT

## Implementation Boundaries

* Do not refactor unrelated systems.
* Do not modify existing tree grouping logic beyond adding one Maps group.
* Do not introduce layer UI in MVP 0 or MVP 1.
* Do not implement non-point geometry.
* Do not reuse WorldDocument services.
* Keep Maps tree behavior aligned with current UX contract:
  * Maps group navigates to Map Listing.
  * Maps group does not show "Add Item".
  * Maps and Map nodes must remain selectable for tree state sync.

---

## Folder Structure Expectations

API (flat layered layout — no Features/ folder exists):

* Controllers/MapsController.cs
* Services/IWorldMapService.cs
* Services/WorldMapService.cs
* Services/IMapBlobStore.cs
* Services/AzureBlobMapBlobStore.cs

Client:

* Pages/Maps/*
* Services/IMapApiService.cs
* Services/MapApiService.cs
* wwwroot/js/mapsDropGuard.js

Shared:

* Models/WorldMap.cs
* Models/MapLayer.cs
* Models/WorldMapCampaign.cs
* Models/WorldMapArc.cs
* DTOs/Maps/MapDTOs.cs

---

## Testing Requirements

* Add unit tests for:

  * Default layer creation
  * Map grouping rules
  * Pin layer selection rule
  * Authorization enforcement
  * Map rename endpoint/service behavior
  * Map delete endpoint/service behavior
  * Tree sync behavior on map routes
  * Tree label updates on map rename
  * Drag/drop upload helper behavior and guard interactions

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
* No restore/undo semantics for map delete (delete remains permanent by contract).

---

## Definition of Done (General)

* Builds clean.
* Tests pass.
* Manual test checklist in phase doc is satisfied.
* Maps route UX verified:
  * `/world/{worldId}/maps` expands/selects Maps in tree.
  * `/world/{worldId}/maps/{mapId}` expands/selects active map.
  * Renaming a map updates nav tree title without full refresh.

