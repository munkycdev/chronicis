# MAPS_ARCHITECTURE_SPEC

## Core Entities

### WorldMap

* WorldMapId (Guid, PK)
* WorldId (Guid, FK → World)
* Name (string, required)
* BasemapBlobKey (string?, nullable until uploaded)
* BasemapContentType (string?, e.g. image/png)
* BasemapOriginalFilename (string?)
* CreatedUtc (DateTime)
* UpdatedUtc (DateTime)

Indexes:
* WorldId

### MapLayer

* MapLayerId (Guid, PK)
* WorldMapId (Guid, FK → WorldMap)
* ParentLayerId (Guid?, nullable self-reference)
* Name (string, e.g. "World", "Campaign", "Arc")
* SortOrder (int)
* IsEnabled (bool)

Default layers created on map creation:

* World
* Campaign
* Arc

Layers are hidden in MVP.

Indexes:
* WorldMapId

### MapFeature (MVP: Point only)

* MapFeatureId (Guid, PK)
* WorldMapId (Guid, FK → WorldMap)
* MapLayerId (Guid, FK → MapLayer)
* X (float, normalized 0..1)
* Y (float, normalized 0..1)
* LinkedArticleId (Guid?)
* GeometryBlobKey (string?, reserved for future)
* GeometryETag (string?, reserved for future)

Indexes:
* WorldMapId
* MapLayerId

### Join Tables

#### WorldMapCampaign

* WorldMapId (Guid, FK → WorldMap)
* CampaignId (Guid, FK → Campaign)

Indexes:
* Composite PK: (WorldMapId, CampaignId)
* CampaignId (reverse lookup)

#### WorldMapArc

* WorldMapId (Guid, FK → WorldMap)
* ArcId (Guid, FK → Arc)

Indexes:
* Composite PK: (WorldMapId, ArcId)
* ArcId (reverse lookup)

### Entity File Paths

EF entity models live in Chronicis.Shared/Models/ (matching Article.cs, Campaign.cs, World.cs convention):

* src/Chronicis.Shared/Models/WorldMap.cs
* src/Chronicis.Shared/Models/MapLayer.cs
* src/Chronicis.Shared/Models/WorldMapCampaign.cs
* src/Chronicis.Shared/Models/WorldMapArc.cs

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
