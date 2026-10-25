# Room Planner (MR)

A mixed-reality app for Meta Quest 3S. You see your real room through passthrough and place virtual furniture that snaps to the real floor and walls.

Solo portfolio project. Design, integration and testing by me; built with [Claude Code](https://claude.com/claude-code) assistance.

## Goal

Walk around your actual room in passthrough, drop in virtual furniture (sofas, tables, shelves), and see how it fits before you buy or move anything — placement is validated against the real room's floor footprint so items can't end up floating or clipping through walls.

## Tech

- Unity 6000.3.5f2, Universal Render Pipeline (URP), OpenXR
- Meta XR SDK 207.0.1 — Core, Interaction SDK (+ Essentials), MR Utility Kit (MRUK)
- NUnit Edit Mode tests (Unity Test Framework)
- Target device: Meta Quest 3S, 72 FPS

## Project status

- **Phase 1 (setup)** — done: Quest/Android build configuration, passthrough + scene permission, the Core/Placement/Room/Interaction/UI assembly layout, a first core model with tests, and a base scene with a passthrough-enabled camera rig.
- **Phase 2 (load the room)** — done in the Editor (see "What is tested" below): on start the app loads your room from Space Setup through MRUK and draws the floor (teal), walls (amber) and ceiling (faint blue) as a translucent overlay on passthrough. A small panel in front of you says `Room loaded: W x D m, N walls`, or tells you to run Space Setup / allow the spatial data permission.

- **Phase 3A (furniture placement, floor items)** — done in the Editor (see "What is tested"): after the room loads, a catalog panel appears to your left with five items (bed, sofa, table, chair, wardrobe). Pick one with the controller ray, aim the right controller at the floor, and a ghost of the item follows the ray: green where it fits, red where it would go through a wall, sit closer than 5 cm to a wall, or overlap furniture you already placed. Pull the trigger to place it; push the right thumbstick left or right to turn it in 15° steps.
- **Phase 3B (edit furniture)** — done in the Editor (see "What is tested"): with no catalog item selected, point at placed furniture to highlight it and pull the trigger to pick it up. The item turns into the green/red ghost; move and turn it, pull the trigger to drop it, or squeeze the grip to put it back. B deletes the highlighted or held item; X undoes and Y redoes any place, move or delete (up to 50 steps). Click the selected catalog item again, or squeeze the grip, to stop placing and go back to picking up.
- **Phase 4 (save/load and measurements)** — done in the Editor (see "What is tested"): your layout is saved automatically about a second after every change and comes back the next time the app starts, re-checked against the room. The catalog panel has Save, Load and Clear All buttons (Clear All needs a second press within 3 seconds). A measurements panel to your right shows the room size, floor area, area used by furniture, free area and item count.

Wall items and polish/release are still ahead.

### Controls

| Button | Action |
|---|---|
| Right trigger | Place the selected item / pick up the pointed item / drop the held item |
| Right grip | Put a held item back, or stop placing |
| Right thumbstick left/right | Turn the ghost 15° per push |
| B (right) | Delete the highlighted or held item |
| X (left) | Undo (while holding an item: put it back) |
| Y (left) | Redo |

While the controller ray is over the catalog, the right-hand buttons operate the catalog instead. The catalog's Save / Load / Clear All buttons are clicked with the ray and trigger like the furniture buttons.

### How room loading works

- `RoomLoader` (Room assembly) starts the MRUK load itself; MRUK's own *Load Scene On Startup* is off so permission-denied and no-room cases can be reported.
  - On the headset it requests the spatial data permission (`com.oculus.permission.USE_SCENE`), then loads Space Setup data.
  - In the Editor it tries device data first (Meta XR Simulator or Quest Link), then falls back to an MRUK sample room prefab.
- It turns the floor outline and ceiling height into a `RoomFootprint` (Core) and raises `RoomLoaded`, or raises `RoomLoadFailed` with a reason. It never throws on a failed load.
- Three MRUK `EffectMesh` objects draw the surfaces with unlit, transparent URP materials (no shadows, no extra lights).
- `RoomStatusPanel` (UI assembly, depends only on Core) shows the text from `RoomStatusFormatter`. The scene wires the loader's events to the panel.
- Non-rectangular rooms keep their real outline: area and "is this point inside" use the exact polygon; width and depth are the bounding box in the room's own floor frame.
- The footprint also carries a `FloorFrame` (floor height, origin and yaw of the floor anchor), so placement code can map between room coordinates and the world without touching MRUK.

### How furniture placement works

- **Core (plain C#, tested):** `FurnitureCatalog` (sizes in metres), `PlacedItem` (item + position + yaw), `OverlapChecker` (separating axis theorem; touching is not overlapping), `PlacementValidator.Validate` (returns `Valid`, `OutsideRoom`, `TooCloseToWall` or `OverlapsItem`, with a 5 cm wall margin measured as the true distance), `FloorRay`, `YawStepper` (15° steps), `GridSnapper` (5 cm) and `AxisStepDetector` (one rotation step per thumbstick push).
- **Placement:** `GhostPreview` casts the right controller's ray onto the floor plane, snaps to 5 cm in the room's own frame (so items line up with the walls), validates and turns green/red. `PlacementController` holds the selection, yaw and placed items and spawns the prefab on a valid trigger press. The ghost update does not allocate memory.
- **Interaction:** `ControllerPlacementInput` reads the right Touch controller (trigger, thumbstick) and raises events. While the Interaction SDK ray is over the catalog, the trigger clicks the UI instead of placing furniture.
- **UI:** `CatalogPanel` builds its button labels from the catalog and highlights the selection. Ray input uses the Interaction SDK's comprehensive OVR rig (added with Meta's quick action, locomotion disabled so the thumbstick only turns furniture) and the "Add Ray Interaction to Canvas" quick action.
- Furniture models are placeholders made from scaled cubes with simple URP Lit materials, sized exactly like the catalog.

### How editing works

- **Core:** every placed item has a stable `Id` (from a Guid) that survives moves and undo/redo, ready for save/load. `RayVsOrientedBox` tests the controller ray against each item's box (footprint plus height), so the prefabs need no colliders. `PlacementHistory` keeps undo/redo stacks of place, move and remove commands (50 max, a new action clears redo). A moving item is validated against the room and all *other* items.
- **Placement:** `SelectionController` highlights the item the ray points at by swapping in a highlight material. `PlacementController` applies every history change to the scene (spawn, destroy or move one prefab by Id) and hides the held item's prefab while the ghost shows its new spot. Selection and ghost updates allocate no memory per frame.
- **Interaction:** `ControllerPlacementInput` also raises grip, B, X and Y events (button names checked in the installed `OVRInput`).

### How saving and measurements work

- **File:** `layout.json` in `Application.persistentDataPath` (on the Quest that is the app's own data folder; in the Editor, `%USERPROFILE%\AppData\LocalLow\<company>\room-planner-mr`). Plain, indented JSON with a `version` and one line per item: `id`, `definitionId`, `posX`, `posZ`, `yawDegrees`. Positions are in the room's own floor frame, so the layout follows the room.
- **Core:** `LayoutSerializer` (exact round-trip; unknown furniture is dropped and counted; malformed, empty or wrong-version files give an empty layout with an error instead of crashing), `LayoutRevalidator` (re-checks saved items against the current room), `RoomMetrics` + `MetricsFormatter`, `LayoutStatusFormatter`, `ConfirmGate`.
- **Saving:** automatic, one second after the last place, move, delete, undo or redo (so a burst of edits writes once), and when the app is paused or closed with a save pending; plus the Save button. Each save writes a temporary file, flushes it and swaps it in, so a crash mid-save cannot corrupt the previous layout.
- **Loading:** once, automatically, after the room has loaded; and with the Load button. Items that no longer fit the current room (outside it, too close to a wall, or overlapping an earlier item) are skipped and the measurements panel says how many. Loading does not rewrite the file, so skipped items stay saved until the next save. Loading and Clear All start a fresh undo history.
- **Clear All:** empties the room and deletes the file.
- **Measurements:** recomputed only when something changes (never per frame): room width and depth, floor area (exact outline), used area (sum of item footprints), free area (never below 0) and item count, rounded to 0.1.

## How to open and run

1. Install Unity **6000.3.5f2** via Unity Hub.
2. Clone this repo and open it as a Unity project (`Open` → select the repo root).
3. Let Unity resolve packages on first open.
4. **Edit Mode tests**: `Window > General > Test Runner` → `EditMode` tab → `Run All`.
5. **Play Mode (no headset)**: open `Assets/Project/Scenes/01_RoomPlanner.unity` and press Play. With no XR runtime the loader falls back to MRUK's sample room `Bedroom00` (Room Index on the `Managers/MRUK` object; set it to `-1` for a random sample room).
   - **Meta XR Simulator**: `Window > Meta > Meta XR Simulator > Activate`, then Play. The room comes from the Simulator's environment.
   - **Quest Link**: with *Spatial Data over Meta Quest Link* enabled in the Link app, Play loads the room you scanned on the headset.
   - **Failure path**: on `Managers/RoomLoader`, untick *Use Device Data In Editor* and *Use Prefab Fallback In Editor*, then Play. The panel shows the "No room found" message.
   - **Placement**: needs a right Touch controller, so use Quest Link. The Meta XR Simulator's input emulation may also work but has not been tried. There is no mouse fallback in this project.
6. **On-device**: switch to the `Meta Quest` Build Profile (already set as active) and Build & Run with a Quest 3S connected over USB/ADB, developer mode enabled. Run Space Setup on the headset first.

## What is tested

- Edit Mode (NUnit): the Core logic below.
- Play Mode in the Editor: MRUK sample room prefabs, the Meta XR Simulator, a real Space Setup room over Quest Link, and the no-room failure path. Surfaces appear, the panel shows the right text, no exceptions.
- Placement in Play Mode (MRUK rooms `Bedroom00` and `OtherRoom04_empty`, and a real room over Quest Link): all five items placed; items through a wall, too close to a wall, in corners, in an L-shaped cut-out, over an inner corner and on top of other furniture are rejected with the right reason; placed prefabs land exactly on the validated footprint. These runs aimed the ghost with a scripted ray and called the place/rotate/select methods directly, because no one was holding the controllers.
- Editing in Play Mode (a real room over Quest Link, and MRUK rooms `Bedroom00` and `OtherRoom04_empty`): place three items, pick one up, try to drop it into a wall (rejected, still held), drop it somewhere valid, delete it with B, undo the delete (same Id comes back), undo the move, redo it. Also grip-cancel, B while holding, and grip while placing. Driven by a scripted ray and by invoking the input component's events, not by pressing real buttons.
- On the headset (my own test after Phase 3B): placing, moving, deleting and undo work with the real controllers.
- Save/load in Play Mode (a real room over Quest Link, and MRUK rooms `Bedroom00` and `OtherRoom04_empty`): placed items autosave about a second after an edit; after stopping and starting Play Mode they come back with the same ids at exactly the same spots; a layout saved in one room and loaded into a smaller sample room places what fits and reports the rest ("Loaded 1 item. 3 did not fit this room."); loading does not change the file; the Save button, Clear All (first press arms, second within 3 s clears the room and deletes the file, a late press only re-arms) and Load with no file work; unknown furniture in a hand-edited file is dropped and reported. The measurements panel matched the placed items each time. Buttons were clicked through their `onClick`, not with the controller ray.
- **Not yet tested by hand**: the new Save / Load / Clear All buttons with the controller ray.
- **Not yet tested as a standalone build on the headset**: the save file in `persistentDataPath` on the Quest (writing, surviving an app restart, the temp-file swap on Android), autosave when the headset is taken off (app pause), the runtime permission prompt (accept and deny), loading Space Setup data without Link, and frame rate with the Interaction SDK rig.

## Tests

154 NUnit Edit Mode tests, all passing: furniture catalog and labels, item ids, `OverlapChecker` (separate, overlapping, touching, rotated 45°, one inside another, walls), `PlacementValidator` (every reason, margins, corners, L- and U-shaped rooms, slanted walls, moves excluding themselves), `RayVsOrientedBox` (hit, miss, rotated, nearest of several), `PlacementHistory` (undo/redo order, redo cleared, 50-entry cap, exact move poses, delete restoring the same Id, clear), `LayoutSerializer` (exact round-trip of 0, 1 and many items, version field, unknown and invalid items dropped, empty/malformed/wrong-version files), `LayoutRevalidator`, `RoomMetrics` and `MetricsFormatter` (rectangular and L-shaped rooms, free area never negative), `LayoutStatusFormatter`, `ConfirmGate`, `FloorRay`, `YawStepper`, `GridSnapper`, `AxisStepDetector`, `FloorFrame`, `RoomFootprint` and `RoomStatusFormatter`. Run via the Test Runner as above.

## AI-use note

This project is built with [Claude Code](https://claude.com/claude-code) as a coding assistant — writing C# against my specs, wiring up Unity Editor/Meta SDK settings, and running tests. I design the architecture, review and integrate every change, and do all testing (Editor, simulator, and on-device where noted). Nothing here ships without me having read and understood it.
