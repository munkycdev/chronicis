MAP_LAYER_HIERARCHY_PLAN.md
# Chronicis Map Layer Hierarchy Design & Implementation Plan

This document defines the architecture and implementation phases for adding **nested map layers** to the Chronicis map system.

The existing system supports:

- flat map layers
- layer ordering
- layer visibility
- layer selection
- pin placement on layers
- rename/delete/custom layer management

This document introduces **hierarchical layers using ParentLayerId**.

The implementation is broken into **safe incremental phases for Codex execution**.

Each phase must compile and pass verification before moving forward.

Run after every phase:

.\scripts\verify.ps1
Architecture Overview
Data Model

The existing MapLayer entity already includes:

MapLayerId
WorldMapId
ParentLayerId (nullable)
Name
SortOrder
IsEnabled

Layers form a tree structure.

Rules:

a layer may have 0 or 1 parent

a layer may have 0..N children

a root layer has ParentLayerId = null

Core System Rules

These rules apply across all hierarchy phases.

Layer Identity

Layer identity is always determined by:

MapLayerId

Never use array index or SortOrder for identity.

SortOrder Semantics

SortOrder only applies within a sibling group.

Example:

World
 ├ Cities
 │   ├ Capital
 │   └ Ports
 └ Terrain

SortOrder is only meaningful among:

Cities vs Terrain
Capital vs Ports

It is not global.

Visibility Inheritance

Visibility is determined by:

layer.IsEnabled AND all ancestors.IsEnabled

A hidden parent hides descendants without mutating child IsEnabled values.

This matches standard hierarchical UI patterns.

Deletion Safety

A layer cannot be deleted if:

pins reference it

it has child layers

it is a protected default layer

Default layers:

World
Campaign
Arc
Tree Integrity

The system must never allow:

self parent assignment

cycles

cross-map parent references

Implementation Strategy

Nested layers are introduced incrementally.

We intentionally avoid full drag/drop tree editing until the hierarchy model is stable.

Phases:

L15 hierarchical rendering
L16 parent assignment API
L17 nesting UI
L18 inherited visibility
L19 sibling reorder
L20 child layer creation
L21 hierarchy-safe deletion

Goal

Render nested layers in the map page and modal UI.

No hierarchy editing yet.

Target files
MapPage.razor
MapPageTests.cs
SessionMapViewerModal.razor
SessionMapViewerModalTests.cs
Implementation

Build a tree projection from flat layer list.

Pseudo logic:

roots = layers where ParentLayerId == null

for each root:
  attach children where ParentLayerId == root.MapLayerId

Recursive expansion allowed.

Render with indentation.

Example UI:

World
  Cities
    Capital
    Ports
  Terrain

Sort siblings by SortOrder.

Tests

Add tests verifying:

children render beneath parent

sibling order follows SortOrder

root layers render first

Constraints

Do NOT:

modify reorder logic

add parent editing

change visibility logic

change selection logic

Run verify script.

Phase L16 — Parent Assignment API
Goal

Allow a layer to be assigned a parent.

Backend Files
MapDTOs.cs
IWorldMapService.cs
WorldMapService.cs
MapsController.cs
WorldMapServiceTests.cs
MapsControllerCoverageSmokeTests.cs
DTO
SetLayerParentRequest
{
  Guid? ParentLayerId
}
Service Method
SetLayerParent(worldId, mapId, userId, layerId, parentLayerId)
Validation

Reject when:

layer == parent

parent not in map

cycle detected

default layer modified if disallowed

Cycle detection:

walk parent chain
if layer encountered → reject
Controller Endpoint
PUT /world/{worldId}/maps/{mapId}/layers/{layerId}/parent
Tests

Service:

valid parent assignment

self parent rejected

cross-map parent rejected

cycle rejected

Controller:

success

bad request

forbidden

Run verify.

Phase L17 — Nest / Unnest UI
Goal

Allow users to nest layers.

Target files
MapPage.razor
MapPageTests.cs
UI Actions

Custom layer actions:

Nest under...
Move to root

Optional:

Create child layer
Behavior

On nest:

MapApi.SetLayerParentAsync(...)

Update local _layers.

Rebuild tree projection.

Preserve:

SelectedLayerId
visibility
Tests

Verify:

nest action calls API

child appears under parent

move to root removes indentation

Phase L18 — Inherited Visibility
Goal

Parent visibility affects descendants.

Rendering rule

Effective visibility:

visible = layer.IsEnabled AND allAncestorsEnabled

Do NOT modify stored IsEnabled values.

Files
MapPage.razor
SessionMapViewerModal.razor
MapPageTests.cs
SessionMapViewerModalTests.cs
Tests

Verify:

hidden parent hides descendants

re-enabling parent restores descendants

explicitly disabled child remains hidden

Phase L19 — Hierarchical Reorder
Goal

Preserve reorder behavior in a hierarchy.

Rule

Reorder only among siblings.

same ParentLayerId required

Cross-parent drag is rejected.

Files
MapPage.razor
MapPageTests.cs
WorldMapService.cs
WorldMapServiceTests.cs
Backend validation

Reject reorder where:

layerIds include mixed parent groups
Tests

sibling reorder persists

cross-parent reorder rejected

Phase L20 — Child Layer Creation
Goal

Create layers directly under a parent.

API
POST /world/{worldId}/maps/{mapId}/layers

Body:

CreateLayerRequest
{
  Name
  ParentLayerId (optional)
}
Behavior

If ParentLayerId provided:

SortOrder = max sibling order + 1
ParentLayerId = given
IsEnabled = true
UI

Add action:

Add Child Layer
Tests

Verify:

child created under parent

child appears nested

correct SortOrder

Phase L21 — Hierarchy-Safe Delete
Goal

Prevent dangerous deletes.

Service rules

Reject delete if:

layer has child layers
layer has pins
layer is protected default

Return ArgumentException.

Files
WorldMapService.cs
WorldMapServiceTests.cs
MapPage.razor
MapPageTests.cs
Tests

Verify:

delete rejected when children exist

delete rejected when pins exist

delete succeeds otherwise

SortOrder normalization must still occur.

Testing Requirements

Every phase must maintain:

100% line coverage
100% branch coverage

Tests must remain focused:

backend logic in service tests

status codes in controller tests

UI behavior in page tests

transport contracts in client tests

Validation

After each phase run:

.\scripts\verify.ps1

Stop if validation fails.
