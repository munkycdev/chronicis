Map Layers Implementation Plan

This document defines the full implementation plan for the Map Layers system in Chronicis.

The goal is to provide a structured set of phases for Codex to execute safely and incrementally. Each phase must compile, pass tests, and pass the repository validation script before proceeding.

Run validation after each phase:

.\scripts\verify.ps1

Stop after validation passes.

System Overview

Maps support multiple layers. Each layer can contain pins.

Layers support the following capabilities:

• layers can be shown or hidden
• layers can be reordered
• one layer is selected at a time
• pins are created on the selected layer
• layer visibility affects pin rendering
• layer order affects rendering order

Layers are persisted server side.

Selected layer state is client side.

Existing Domain Model

MapLayer includes the following properties:

MapLayerId
WorldMapId
ParentLayerId
Name
SortOrder
IsEnabled

MapFeature includes:

MapFeatureId
MapLayerId
X
Y
Label
Description

Each feature belongs to exactly one layer.

Default layers created for a map:

World
Campaign
Arc

Default sort order:

World = 0
Campaign = 1
Arc = 2
Functional Requirements
Layer Visibility

Users must be able to show or hide layers.

When a layer is hidden:

• pins on that layer are not rendered
• layer remains selectable
• visibility persists server side

Layer Selection

Exactly one layer is selected at a time on the map page.

The selected layer determines where pins are created.

Selection is page state only and is not persisted.

Layer Ordering

Layers have an explicit order determined by SortOrder.

Users can reorder layers via drag and drop.

Ordering persists server side.

Rendering respects layer order.

Higher layers render above lower layers.

Pin Placement

When a user creates a pin:

• the pin must be assigned to the currently selected layer
• the selected layer must be sent in the create pin request

Pins are not created using the old Arc > Campaign > World rule when a layer is explicitly selected.

Map Page Behavior

The map page must include a layer panel.

The panel displays layers ordered by SortOrder.

Each layer row includes:

visibility toggle
layer name
selection state
drag handle

Behavior:

Clicking a row selects the layer.

Toggling visibility updates the server.

Dragging changes ordering.

Maps Modal Behavior

The maps modal must display the list of layers.

Modal capabilities:

• show or hide layers
• reflect current layer order

Drag and drop reordering is optional in the modal and not required.

API Requirements

The API must expose the following capabilities.

List Layers
GET /world/{worldId}/maps/{mapId}/layers

Returns layers ordered by SortOrder.

Update Layer Visibility
PUT /world/{worldId}/maps/{mapId}/layers/{layerId}

Request body

{
  "isEnabled": true|false
}

Behavior

• validate world membership
• ensure layer belongs to map
• update IsEnabled

Reorder Layers
PUT /world/{worldId}/maps/{mapId}/layers/reorder

Request

{
  "layerIds": [ Guid ]
}

Behavior

• validate membership
• verify all layer IDs belong to the map
• ensure no duplicates
• update SortOrder sequentially

Client API Requirements

The client API service must support:

GetLayers(worldId, mapId)
UpdateLayerVisibility(worldId, mapId, layerId, isEnabled)
ReorderLayers(worldId, mapId, layerIds)
Rendering Rules

Pin rendering must obey:

Only enabled layers render pins.

Layers render in SortOrder order.

Later layers render above earlier layers.

Phase Plan for Codex
Phase L1

Backend Layer Visibility

Goal

Allow layers to be enabled or disabled.

Tasks

Add service method:

UpdateLayerVisibility(worldId, mapId, layerId, isEnabled)

Add API endpoint:

PUT /world/{worldId}/maps/{mapId}/layers/{layerId}

Behavior

• verify membership
• verify layer belongs to map
• update IsEnabled

Tests

Service tests

• valid update
• wrong map rejected
• unauthorized rejected

Controller tests

• success response
• forbidden response
• bad request response

Validation

.\scripts\verify.ps1
Phase L2

Client API Visibility Support

Goal

Expose visibility management to the client.

Tasks

Add method to MapApiService:

UpdateLayerVisibility(...)

Tests

• correct route
• correct verb
• correct payload

Validation

Run verify script.

Phase L3

Map Page Layer Panel

Goal

Display and manage layers on the map page.

UI requirements

Layer panel displays:

[visibility toggle]  Layer Name

Behavior

• clicking row selects layer
• toggling visibility calls API
• hidden layers stop rendering pins

No drag and drop yet.

Phase L4

Selected Layer State

Goal

Allow a layer to be selected.

Add client state:

SelectedLayerId

Rules

• one selected layer
• default selected layer is Arc if available
• selection persists while page open

Visual state

Selected layer must be highlighted.

Phase L5

Pin Creation Uses Selected Layer

Goal

Pins are created on the selected layer.

Update pin creation logic.

Behavior

When creating a pin:

CreatePin(
   worldId,
   mapId,
   selectedLayerId,
   x,
   y
)

Remove implicit layer selection logic during manual pin creation.

Tests

• pin created on selected layer
• correct layer ID sent to API

Phase L6

Backend Layer Reordering

Goal

Persist new layer order.

Add service method

ReorderLayers(worldId, mapId, layerIds)

Add endpoint

PUT /layers/reorder

Behavior

• verify membership
• verify layers belong to map
• update SortOrder sequentially

Tests

• reorder persists
• invalid layer rejected
• duplicate rejected

Phase L7

Client API Reorder Support

Add method

ReorderLayers(worldId, mapId, layerIds)

Tests verify route and payload.

Phase L8

Drag and Drop Layer Reordering

Map page layer list must support drag and drop.

Behavior

• UI order updates immediately
• drop triggers reorder API call

Failure handling

• revert UI if API fails

Selection and visibility must remain unchanged.

Phase L9

Modal Layer Synchronization

Ensure modal layer list:

• uses SortOrder
• reflects updated visibility

No drag and drop required.

Phase L10

Rendering Order Enforcement

Ensure map renderer sorts layers by SortOrder.

Pins must render in layer order.

Later layers render above earlier ones.

Phase L11

UX Improvements

Optional improvements

• drag handle icon
• hover highlight
• reorder animation

No functional changes.

Validation Rules

For each phase:

Build must succeed

Tests must pass

verify.ps1 must pass

Do not refactor unrelated systems
