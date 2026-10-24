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

- **Phase 1 (setup)** — done: Quest/Android build configuration, passthrough + scene permission, the Core/Placement/Room/Interaction/UI assembly layout, a `FurnitureItem` + `PlacementValidator` core model with tests, and a base scene with a passthrough-enabled camera rig.
- **Phase 2 (load the room)** — done in the Editor (see "What is tested" below): on start the app loads your room from Space Setup through MRUK and draws the floor (teal), walls (amber) and ceiling (faint blue) as a translucent overlay on passthrough. A small panel in front of you says `Room loaded: W x D m, N walls`, or tells you to run Space Setup / allow the spatial data permission.

- **Phase 3A (furniture placement, floor items)** — done in the Editor (see "What is tested"): after the room loads, a catalog panel appears to your left with five items (bed, sofa, table, chair, wardrobe). Pick one with the controller ray, aim the right controller at the floor, and a ghost of the item follows the ray: green where it fits, red where it would go through a wall, sit closer than 5 cm to a wall, or overlap furniture you already placed. Pull the trigger to place it; push the right thumbstick left or right to turn it in 15° steps.

Wall items, measurements/save-load, and polish/release are still ahead.

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
- **Not yet tested by hand**: aiming and pulling the trigger with a real controller, clicking catalog buttons with the controller ray, the thumbstick rotation, and the "trigger over UI does not place" rule. These need the headset (over Link or as a build).
- **Not yet tested as a standalone build on the headset**: the runtime permission prompt (accept and deny), loading Space Setup data without Link, and frame rate with the Interaction SDK rig.

## Tests

94 NUnit Edit Mode tests, all passing: furniture catalog and labels, `OverlapChecker` (separate, overlapping, touching, rotated 45°, one inside another, walls), `PlacementValidator` (every reason, margins, corners, L- and U-shaped rooms, slanted walls), `FloorRay`, `YawStepper`, `GridSnapper`, `AxisStepDetector`, `FloorFrame`, `RoomFootprint`, `RoomStatusFormatter` and `FurnitureItem`. Run via the Test Runner as above.

## AI-use note

This project is built with [Claude Code](https://claude.com/claude-code) as a coding assistant — writing C# against my specs, wiring up Unity Editor/Meta SDK settings, and running tests. I design the architecture, review and integrate every change, and do all testing (Editor, simulator, and on-device where noted). Nothing here ships without me having read and understood it.
