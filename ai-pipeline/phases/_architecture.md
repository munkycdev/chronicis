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

