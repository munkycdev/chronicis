# Map Session Linking Product Specification

This document describes how users interact with map features from session notes.

The goal is to allow locations to accumulate campaign history.

---

# UX Patterns

Two interaction patterns are supported initially.

Pattern 2
Inline map feature references inside the editor.

Pattern 3
Map-driven feature selection using a map picker.

Pattern 1 (structured linking panel) will also exist for visibility and editing.

---

# Session Note UI

Each session note displays a panel:

Linked Locations

Example

Linked Locations

Blackroot Ford  
Ruined Watchtower  
South Gate

Each item displays:

feature name  
map name  
optional article badge

---

# Feature Navigation

Selecting a linked location must:

open the map viewer  
center the map on the feature  
highlight the feature

---

# Map Picker Workflow

Users can link locations by:

Add Location  
Open map picker  
Click one or more features  
Save

---

# Inline Editor References

The editor supports feature chips.

Typing

@location

opens a feature search.

Example result inside notes:

The party crossed [Blackroot Ford] at dawn.

The chip represents a MapFeature reference.

---

# Backlinks

Map features may display:

Referenced in session notes

Example

Blackroot Ford

Referenced in

Session 3  
Session 8  
Session 11

---

# Non Goals

This feature does not include:

coordinate tagging
freehand spatial notes
polygon selection for notes
map drawing tools

Those belong to other map phases.

---

# Future Enhancements

Future capabilities may include:

timeline visualization for features  
location heatmaps  
map-driven campaign summaries
