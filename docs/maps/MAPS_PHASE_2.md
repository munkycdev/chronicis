# MAPS_PHASE_2 — Session Note Map Chips + Modal Viewer

## Objective

Integrate world maps into the Session Note editor.

* Typing `[[maps/` triggers autocomplete for maps in the current world.
* Selecting a map inserts a chip.
* Clicking the chip opens a modal viewer.
* Viewer shows basemap only (layer 0).
* Modal closes via Esc or top-right close button.

---

## Scope

In scope:

* Autocomplete suggestions from world maps
* Chip insertion and persistence in Session Notes
* Modal basemap viewer using existing SAS read URL behavior

Out of scope:

* Pins, features, or overlays
* Layer selection UI
* Editing tools inside the modal
* Campaign or Arc contextual map discovery

---

## Requirements

### Autocomplete

* Trigger: `[[maps/`
* Suggestions: maps in the current world
* Sort: name ascending
* Display: map name

### Chip

* Shows map name
* Click opens modal

### Modal

* Title: map name
* Content: basemap and pins. May be zoomed and panned in the same manner as the map page.
* Size: fills the browser window with enough margin to make it clear that it is a modal box
* Close: Esc and top-right close button
* Loading and error states

---

## API Requirements

1. World map search for autocomplete

* Input: worldId, optional query string
* Output: list of `{ mapId, name }`
* Authorization: world membership required
* Sorting: name ascending

2. Basemap read URL

* Use existing P0-11 basemap read URL behavior

---

## Client Architecture Notes

* Prefer representing the chip as a structured editor node or token with stable round-trip behavior.
* Do not refactor the editor globally. Add the smallest extension point needed.
* Modal must fetch basemap read URL via map client service.

---

# Atomic Agent Prompts

Execute prompts sequentially. Do not combine them.
Always follow MAPS_AGENT_CONTRACT.md.
If unrelated improvements are noticed, list them under "Deferred" and stop.

---

## P2-01 API: Map Autocomplete Endpoint

Task:

* Add endpoint to search or list maps for a world for autocomplete.
* Return minimal DTO: mapId, name.
* Sort by name ascending.

Constraints:

* Do not add new unrelated endpoints.
* Enforce world membership.
* Stop after build succeeds.

---

## P2-02 Client: Map Autocomplete Service Method

Task:

* Add client method to fetch autocomplete suggestions.
* Use the new API endpoint.

Constraints:

* Follow client service folder conventions.
* Do not duplicate services.
* Stop after build succeeds.

---

## P2-03 Editor: Trigger Detection and Suggestion UI

Task:

* Detect typing `[[maps/` in Session Note editor.
* Show suggestion list using P2-02 results.

Constraints:

* Minimal integration, no editor refactors.
* Stop after build succeeds.

---

## P2-04 Editor: Insert Map Chip and Persist

Task:

* Selecting a suggestion inserts a map chip.
* Chip persists through save and reload.

Constraints:

* Ensure stable round-trip serialization.
* Stop after build succeeds.

---

## P2-05 UI: Map Modal Viewer (Basemap Only)

Task:

* Implement modal that opens with worldId and mapId.
* Fetch basemap read URL.
* Render basemap image.
* Close with Esc and top-right close button.

Constraints:

* Basemap only.
* No pins or layers.
* Stop after build succeeds.

---

## P2-06 Wiring: Chip Click Opens Modal

Task:

* Clicking chip opens the modal viewer.
* Modal shows correct map.

Constraints:

* Minimal UI.
* Stop after build succeeds.

---

## P2-07 Tests

Must test:

* API authorization and sorting
* Client service route and DTO handling
* Editor insertion and persistence of chip
* Modal open and close behavior

Constraints:

* Keep tests focused.
* verify.ps1 must pass.
* Stop after tests pass.

---

## Manual Validation Checklist

* In a Session Note, type `[[maps/` and confirm autocomplete list.
* Select a map and confirm chip inserted.
* Save and reload the Session Note and confirm chip persists.
* Click chip and confirm modal opens.
* Confirm basemap renders.
* Press Esc and confirm modal closes.
* Click close button and confirm modal closes.

---

## Guardrails

* No pins or map overlays.
* No layer UI.
* No map editing in modal.
* No WorldDocument reuse.

---

## Definition of Done

* All tests pass.
* verify.ps1 passes.
* Manual checklist satisfied.
