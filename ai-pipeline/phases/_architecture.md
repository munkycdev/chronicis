# Architecture And System Rules

## Data Model Description

The existing `MapLayer` entity includes:

- `MapLayerId`
- `WorldMapId`
- `ParentLayerId` (nullable)
- `Name`
- `SortOrder`
- `IsEnabled`

Layers form a tree structure.

Tree cardinality:

- a layer may have 0 or 1 parent
- a layer may have 0..N children
- a root layer has `ParentLayerId = null`

Layer identity is always `MapLayerId`.

Never use array index or `SortOrder` for identity.

## Layer Rules

`SortOrder` applies only within a sibling group.

Example:

```text
World
 ├ Cities
 │   ├ Capital
 │   └ Ports
 └ Terrain
```

`SortOrder` is only meaningful among:

- Cities vs Terrain
- Capital vs Ports

It is not global.

## Visibility Inheritance Rules

Effective visibility is:

`layer.IsEnabled AND all ancestors.IsEnabled`

A hidden parent hides descendants without mutating child `IsEnabled` values.

## Deletion Rules

A layer cannot be deleted if:

- pins reference it
- it has child layers
- it is a protected default layer

Protected default layers:

- World
- Campaign
- Arc

## Tree Integrity Rules

The system must never allow:

- self parent assignment
- cycles
- cross-map parent references

## Map Features

Map content is represented as **MapFeatures**.

A MapFeature belongs to exactly one MapLayer.

Feature cardinality:

- a layer may have 0..N features
- a feature belongs to exactly one layer

Feature identity is always `MapFeatureId`.

Never derive feature identity from array position, render order, or client-side indexes.

## Feature Types

MapFeatures support multiple geometry types.

Initial supported feature types:

- Point (existing map pins)
- Polygon (new region features)

Future feature types may include:

- LineString (rivers, borders)
- MultiPolygon

Feature type is explicit and must not be inferred from geometry payload shape.

## Geometry Storage Rules

Point features store normalized coordinates directly on the feature record.

Polygon features store geometry in blob storage.

Geometry payload format:

- GeoJSON-inspired structure
- Stored as compressed JSON blob
- Blob path follows the existing map feature blob convention

Coordinates are **normalized image coordinates** relative to the basemap.

Coordinate rules:

- X and Y range: 0.0 – 1.0
- (0,0) represents the top-left of the basemap
- (1,1) represents the bottom-right of the basemap

These coordinates do NOT represent geographic coordinates.

Do not introduce map projections or geographic coordinate systems.

The system operates entirely in normalized image space.

## Polygon Rules

Polygon geometry represents a single closed ring.

Initial polygon constraints:

- only one outer ring
- no holes
- no multi-polygons
- Advanced topology validation, including self-intersection detection, is out of scope for the initial implementation.
- vertex order must form a closed loop

The client drawing workflow is responsible for constructing a valid ring.

The backend stores geometry as provided unless it violates coordinate bounds.

## Feature Layer Integration

All MapFeatures participate in the existing layer architecture.

Rules:

- a feature must always belong to a MapLayer
- features inherit visibility from their layer
- hidden layers hide all features within them
- feature visibility must not bypass layer visibility rules

Layer visibility rules remain authoritative.

## Rendering Model

Map rendering uses the existing basemap image with overlay rendering.

Feature rendering rules:

- features render as overlays above the basemap
- polygons render within the same overlay system as pins
- layer visibility controls polygon visibility
- feature render order must remain consistent with layer ordering

Do not introduce alternate rendering systems.

Examples of disallowed changes:

- replacing the existing overlay system
- introducing map libraries
- introducing GIS rendering engines

## Polygon Editing Model

Initial editing support is intentionally limited.

Supported actions:

- create polygon via click-to-add vertices
- complete polygon via double-click
- cancel draft via Escape
- remove last vertex via Backspace
- move existing vertices
- delete polygon

Unsupported in initial implementation:

- polygon holes
- freehand drawing
- polygon splitting
- polygon merging
- topology validation

## Future Geometry Features (Not In Scope)

The following capabilities are planned but not part of the initial polygon implementation:

- magnetic lasso / coastline tracing
- automatic terrain following
- hole support
- multi-polygons
- spatial queries
- geometry simplification

## Feature Interaction Rules

Feature selection and editing operate on rendered geometry within the active map view.

Initial interaction rules:

- point features are selected by existing pin interaction behavior
- polygon features are selected by clicking within the rendered polygon area
- vertex handles are only shown for the currently selected polygon
- only one polygon may be actively edited at a time

Do not introduce multi-select behavior in the initial implementation.

## Feature Validation Rules

Initial polygon validation is intentionally minimal.

Polygon requirements:

- at least 3 distinct vertices
- all coordinates must remain within normalized bounds
- persisted polygon rings must be closed
- polygons with invalid coordinate data must be rejected

Do not add advanced topology validation in the initial implementation.


