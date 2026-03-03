# MAPS_PHASE_1 — Pins (Point Features)

## Objective

Allow users to place, move, delete pins linked to Articles.

---

# Atomic Agent Prompts

Execute sequentially. Do not combine prompts.
Always follow MAPS_AGENT_CONTRACT.md.

---

## P1-01 Add MapFeature Entity

Task:

* Implement MapFeature (Point only).
* Fields: MapFeatureId, WorldMapId, MapLayerId, X, Y, LinkedArticleId.

Constraints:

* No geometry blobs.
* No layer UI.
* Stop after build succeeds.

---

## P1-02 Add Pin Service Logic

Task:

* CreatePin.
* ListPinsForMap.
* UpdatePinPosition.
* DeletePin.

Layer rule:

* Arc > Campaign > World default layer.

Constraints:

* Enforce membership.
* Stop after build succeeds.

---

## P1-03 Add Pin API Endpoints

Task:

* POST create pin.
* GET list pins.
* PATCH update pin.
* DELETE pin.

Constraints:

* Return minimal linked article metadata.
* Stop after build succeeds.

---

## P1-04 Render Pins (Read-Only)

Task:

* Render pins positioned using normalized coords.
* Clicking pin opens linked article.

Constraints:

* No drag yet.
* Use simple positioned elements over image.
* Stop after build succeeds.

---

## P1-05 Add Pin Creation UI

Task:

* Click map to create pin.
* Select article to link.
* Persist via API.

Constraints:

* Minimal UI.
* Stop after build succeeds.

---

## P1-06 Add Drag to Move

Task:

* Allow dragging pin.
* Persist position on drag end.

Constraints:

* Throttle updates.
* Stop after build succeeds.

---

## P1-07 Add Delete Pin

Task:

* Allow deleting selected pin.
* Remove from UI and DB.

Constraints:

* Minimal confirmation.
* Stop after build succeeds.

---

## P1-08 Add Tests

Must test:

* Deepest layer rule.
* Authorization.
* CRUD happy path.

Constraints:

* verify.ps1 must pass.
* Stop after tests pass.
