# MAPS_FUNCTIONAL_SPEC

## Purpose

Provide a spatial interface for Chronicis that allows users to attach maps to a world and tag locations on a 2D plane.

---

## MVP Scope

### MVP 0

* Create maps at the World level.
* Upload a basemap image (PNG, JPG, WebP).
* View basemap on a dedicated map page.
* Maps appear under a new top-level **Maps** tree category.
* Maps Detail page lists all maps in the world.

### MVP 1

* Add point pins to maps.
* Pins link to Articles (including SessionNote articles).
* Pins persist and can be moved or deleted.

---

## Navigation

* New top-level tree category: **Maps**.
* Clicking Maps opens `/world/{worldId}/maps`.
* Clicking a map opens `/world/{worldId}/maps/{mapId}`.
* Tree displays map name only.
* Maps sorted by name.

---

## Maps Detail Page (World Context)

Displays all maps in the world grouped by:

* World-scoped maps
* Campaign-scoped maps (grouped by Campaign)
* Arc-scoped maps (grouped by Campaign then Arc)

Maps sorted by name within each grouping.

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
3. As a GM, I can place a pin and link it to an article.
4. As a user, I can click a pin and open its linked article.
