# MAPS_ARCHITECTURE_SPEC

## Core Entities

### WorldMap

* WorldMapId
* WorldId
* Name
* BasemapBlobKey
* BasemapContentType
* BasemapOriginalFilename
* CreatedUtc / UpdatedUtc

### MapLayer

* MapLayerId
* WorldMapId
* ParentLayerId (nullable)
* Name
* SortOrder
* IsEnabled

Default layers created on map creation:

* World
* Campaign
* Arc

Layers are hidden in MVP.

### MapFeature (MVP: Point only)

* MapFeatureId
* WorldMapId
* MapLayerId
* X (float, normalized 0..1)
* Y (float, normalized 0..1)
* LinkedArticleId
* GeometryBlobKey (reserved for future)
* GeometryETag (reserved for future)

### Join Tables

* WorldMapCampaign (WorldMapId, CampaignId)
* WorldMapArc (WorldMapId, ArcId)

---

## Blob Storage

Container: `chronicis-maps`

Paths:

* maps/{mapId}/basemap/{filename}
* maps/{mapId}/layers/{layerId}/features/{featureId}.geojson.gz

Blobs are private.
Access via SAS issued by API.

Maps are NOT WorldDocuments.

---

## Routing

* `/world/{worldId}/maps`
* `/world/{worldId}/maps/{mapId}`

---

## Map Discovery Rules

Maps Detail shows all maps in the world.
Grouping logic:

* No campaign/arc rows = world-scoped
* Campaign rows = campaign-scoped
* Arc rows = arc-scoped

Sorted by Name ascending.

---

## Pin Layer Selection Rule

When creating a pin:

* Use Arc default layer if present.
* Else Campaign default layer.
* Else World default layer.

---

## Security

All endpoints must enforce world membership.
Blob access must use short-lived SAS URLs.

---

## Separation Rule

* Reuse low-level blob utilities.
* Do NOT reuse WorldDocument services.
* Implement a dedicated Map storage service abstraction.
