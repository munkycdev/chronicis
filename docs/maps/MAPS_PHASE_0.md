# MAPS_PHASE_0 — Basemap + Hidden Layers

## Objective

Implement map creation, basemap upload, hidden default layers, and Maps navigation.

---

# Atomic Agent Prompts

Execute prompts sequentially. Do not combine them.
Always follow MAPS_AGENT_CONTRACT.md.
If unrelated improvements are noticed, list them under "Deferred" and stop.

---

## P0-01 Repo Orientation (No Code)

Task:

* Identify where tree virtual groups are defined and rendered (file paths).
* Identify API feature folder conventions.
* Propose exact file paths for Maps MVP.

Constraints:

* No code changes.
* No refactors.
* Keep response under 30 lines.
* Stop after delivering file paths.

---

## P0-02 Define EF Entities (No Code)

Task:

* List EF entities: WorldMap, MapLayer, WorldMapCampaign, WorldMapArc.
* List fields for each entity.
* List required indexes.

Constraints:

* No implementation.
* No extra entities.
* Stop after listing schema.

---

## P0-03 Implement EF Models

Task:

* Add WorldMap, MapLayer, WorldMapCampaign, WorldMapArc.
* Add DbSet and configuration wiring.

Constraints:

* No endpoints.
* No UI.
* No WorldDocument reuse.
* Stop after compilation succeeds.

---

## P0-04 Create Migration

Task:

* Generate EF migration for maps tables only.

Constraints:

* No unrelated schema changes.
* Stop after build succeeds.

---

## P0-05 Add Map Blob Storage Abstraction

Task:

* Implement IMapBlobStore.
* Implement AzureBlobMapBlobStore.
* Include SAS generation for upload and read.

Constraints:

* Reuse low-level blob utilities only.
* Do NOT reuse WorldDocument services.
* Stop after compilation succeeds.

---

## P0-06 Implement WorldMapService

Task:

* Implement CreateMap (must create default layers: World, Campaign, Arc).
* Implement GetMap.
* Implement ListMapsForWorld.

Constraints:

* Enforce membership.
* No controllers yet.
* Stop after build succeeds.

---

## P0-07 Add API Controllers

Task:

* Add endpoints:

  * Create map
  * Get map metadata
  * List maps for world
  * Request basemap upload SAS
  * Confirm basemap upload

Constraints:

* No pin endpoints.
* No layer UI logic.
* Stop after build succeeds.

---

## P0-08 Add Maps Tree Group

Task:

* Add top-level Maps category.
* Display map names only.
* Sort by name.

Constraints:

* No tree refactor.
* Stop after build succeeds.

---

## P0-09 Implement Maps Detail Page

Task:

* Create page at /world/{worldId}/maps.
* Group maps by scope.
* Sort by name.

Constraints:

* No upload UI in this step.
* Stop after build succeeds.

---

## P0-10 Add Basemap Upload UI

Task:

* Add drag-and-drop or file picker.
* Upload via SAS flow.
* Confirm upload and create map record.

Constraints:

* Do NOT use WorldDocument services.
* Stop after build succeeds.

---

## P0-11 Render Basemap Page

Task:

* Implement /world/{worldId}/maps/{mapId}.
* Fetch SAS URL from API.
* Render basemap image.

Constraints:

* No pins.
* Stop after build succeeds.

---

## P0-12 Add Tests

Must test:

* Default layers are created on map creation.
* Map grouping rules work correctly.
* Authorization is enforced.

Constraints:

* Keep tests focused.
* verify.ps1 must pass.
* Stop after tests pass.

---

# Manual Test Checklist

* Create map with uploaded image.
* Map appears in Maps Detail.
* Map appears in tree sorted by name.
* Basemap renders.
* Default layers exist in DB.

---

# Guardrails

* No pins yet.
* No layer UI.
* No reuse of WorldDocument.
* No geometry other than basemap.

---

# Definition of Done

* Tests pass.
* verify.ps1 passes.
* Manual checklist satisfied.
