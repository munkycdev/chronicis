# MAPS_FUNCTIONAL_SPEC

## Purpose

Provide a spatial interface for Chronicis that allows users to attach maps to a world and tag locations on a 2D plane.

---

## MVP Scope

### MVP 0

* Create maps at the World level.
* Upload a basemap image (PNG, JPG, WebP).
* Upload supports click-to-browse and drag/drop into the basemap drop zone.
* Prevent browser "open file in new tab" behavior during map upload drag/drop interactions.
* View basemap on a dedicated map page.
* Maps appear under a new top-level **Maps** tree category.
* Map Listing page lists all maps in the world.
* Rename map from the map page header (world owner only).
* Delete map from Map Listing (world owner only) with destructive confirmation.

### MVP 1

* Add point pins to maps.
* Pins link to Articles (including SessionNote articles).
* Pins persist and can be moved or deleted.

---

## Navigation

* New top-level tree category: **Maps**.
* Clicking the **Maps** tree group opens `/world/{worldId}/maps`.
* Clicking a map opens `/world/{worldId}/maps/{mapId}`.
* Tree displays map name only.
* Maps sorted by name.
* On Map Listing load, the world and Maps group are expanded in the tree and Maps is selected.
* On Map Detail load, the world and Maps group are expanded and the active map is selected.
* When map name changes, the tree label updates immediately.
* The Maps tree group does not offer an "Add Item" action.

---

## Map Listing Page (World Context)

Displays all maps in the world grouped by:

* World-scoped maps
* Campaign-scoped maps (grouped by Campaign)
* Arc-scoped maps (grouped by Campaign then Arc)

Maps sorted by name within each grouping.

Page behavior:

* Breadcrumbs: `Dashboard / {world name} / Maps`
* Header uses shared detail-page style conventions.
* Create section includes basemap drag/drop + browse upload.
* Each map row includes delete action with typed-name confirmation.

---

## Map Detail (Single Map Context)

* Route: `/world/{worldId}/maps/{mapId}`
* Breadcrumbs: `Dashboard / {world name} / Maps / {map name}`
* Header map name is editable for world owner.
* Save pattern matches Session Detail behavior:
  * Dirty tracking
  * Save status indicator
  * Save button
  * Enter key save
* Basemap image is constrained to container width (no overflow blowout).

---

## Explicit Non-Goals (MVP)

* No layer UI.
* No polygons, regions, routes, or lasso tools.
* No contextual display within Campaign or Arc pages.
* No reuse of WorldDocument feature.

---

## User Stories

1. As a GM, I can create a map and upload an image.
2. As a GM, I can view that map.
3. As a world owner, I can rename a map from the map page.
4. As a world owner, I can permanently delete a map after explicit confirmation.
5. As a GM, I can place a pin and link it to an article.
6. As a user, I can click a pin and open its linked article.

