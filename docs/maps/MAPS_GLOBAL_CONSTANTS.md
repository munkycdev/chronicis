# MAPS_GLOBAL_CONSTANTS

## Routes

* Maps Detail: `/world/{worldId}/maps`
* Map Page: `/world/{worldId}/maps/{mapId}`

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
