# MAPS_GLOBAL_CONSTANTS

## Routes

* Map Listing: `/world/{worldId}/maps`
* Map Detail: `/world/{worldId}/maps/{mapId}`

## Tree Behavior Constants

* Maps virtual group type: `VirtualGroupType.Maps`
* Maps virtual group is selectable and navigable to Map Listing.
* Map node type: `TreeNodeType.Map`
* Map nodes are selectable and navigable to Map Detail.
* Maps virtual group has no tree add-child action.

## Blob Container

* chronicis-maps

## Blob Prefix Schema

* maps/{mapId}/basemap/{filename}
* maps/{mapId}/layers/{layerId}/features/{featureId}.geojson.gz

## Default Layers

* World
* Campaign
* Arc

## Pin Geometry

* X, Y normalized 0..1 relative to basemap.

## Sorting Rules

* Maps sorted by Name ascending.

## Layer Selection Rule

* Arc layer > Campaign layer > World layer.

## Supported Basemap Types

* image/png
* image/jpeg
* image/webp

## Basemap Upload Limits

* Max basemap upload size: `50 * 1024 * 1024` bytes (50 MB)

## Map Name Constraints

* Name required.
* Max length: 200 characters.

## Delete Semantics

* Delete is permanent.
* Deletes map metadata and all blobs under `maps/{mapId}/`.

