# Render Definitions

This directory contains JSON files that control how external resource content is rendered in the detail panel.

## Structure

```
render-definitions/
  {source}/
    _default.json          # Fallback for all categories in this source
    {category}.json         # Definition for a specific top-level category
    {category}/{sub}.json   # Definition for a specific subcategory
```

## Resolution Order

For a resource at `ros/bestiary/Cultural-Being/ambrian-warrior`:
1. `render-definitions/ros/bestiary/cultural-being.json`
2. `render-definitions/ros/bestiary.json`
3. `render-definitions/ros/_default.json`
4. Generic renderer (no definition)

## Schema

See `Chronicis.Client.Models.RenderDefinition` for the full model.

```json
{
  "version": 1,
  "displayName": "Bestiary Entry",
  "titleField": "name",
  "subtitleField": "race",
  "sections": [
    {
      "label": "Overview",
      "fields": [
        { "path": "description", "render": "richtext" },
        { "path": "race", "label": "Race" }
      ]
    },
    {
      "label": "Abilities",
      "path": "abilities",
      "render": "list",
      "itemFields": [
        { "path": "name", "render": "heading" },
        { "path": "description", "render": "richtext" }
      ]
    }
  ],
  "hidden": ["pk", "model"],
  "catchAll": true
}
```

## Render Types

- `text` (default) — Label: value inline display
- `richtext` — Rendered as HTML block
- `heading` — Sub-section heading
- `chips` — Array of strings as tags
- `hidden` — Suppressed from display
- `list` — Array of objects rendered as cards
- `table` — Array of objects rendered as table rows

## Generating Definitions

Use the Definition Generator tool at `/dev/render-definition-generator` to analyze
a JSON blob and generate a starter definition file.
