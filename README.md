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

Furniture placement, measurements/save-load, and polish/release are still ahead.

### How room loading works

- `RoomLoader` (Room assembly) starts the MRUK load itself; MRUK's own *Load Scene On Startup* is off so permission-denied and no-room cases can be reported.
  - On the headset it requests the spatial data permission (`com.oculus.permission.USE_SCENE`), then loads Space Setup data.
  - In the Editor it tries device data first (Meta XR Simulator or Quest Link), then falls back to an MRUK sample room prefab.
- It turns the floor outline and ceiling height into a `RoomFootprint` (Core) and raises `RoomLoaded`, or raises `RoomLoadFailed` with a reason. It never throws on a failed load.
- Three MRUK `EffectMesh` objects draw the surfaces with unlit, transparent URP materials (no shadows, no extra lights).
- `RoomStatusPanel` (UI assembly, depends only on Core) shows the text from `RoomStatusFormatter`. The scene wires the loader's events to the panel.
- Non-rectangular rooms keep their real outline: area and "is this point inside" use the exact polygon; width and depth are the bounding box in the room's own floor frame.

## How to open and run

1. Install Unity **6000.3.5f2** via Unity Hub.
2. Clone this repo and open it as a Unity project (`Open` → select the repo root).
3. Let Unity resolve packages on first open.
4. **Edit Mode tests**: `Window > General > Test Runner` → `EditMode` tab → `Run All`.
5. **Play Mode (no headset)**: open `Assets/Project/Scenes/01_RoomPlanner.unity` and press Play. With no XR runtime the loader falls back to MRUK's sample room `Bedroom00` (Room Index on the `Managers/MRUK` object; set it to `-1` for a random sample room).
   - **Meta XR Simulator**: `Window > Meta > Meta XR Simulator > Activate`, then Play. The room comes from the Simulator's environment.
   - **Quest Link**: with *Spatial Data over Meta Quest Link* enabled in the Link app, Play loads the room you scanned on the headset.
   - **Failure path**: on `Managers/RoomLoader`, untick *Use Device Data In Editor* and *Use Prefab Fallback In Editor*, then Play. The panel shows the "No room found" message.
6. **On-device**: switch to the `Meta Quest` Build Profile (already set as active) and Build & Run with a Quest 3S connected over USB/ADB, developer mode enabled. Run Space Setup on the headset first.

## What is tested

- Edit Mode (NUnit): the Core logic below.
- Play Mode in the Editor: MRUK sample room prefabs, the Meta XR Simulator, a real Space Setup room over Quest Link, and the no-room failure path. Surfaces appear, the panel shows the right text, no exceptions.
- **Not yet tested as a standalone build on the headset**: the runtime permission prompt (accept and deny), and loading Space Setup data without Link.

## Tests

33 NUnit Edit Mode tests, all passing: `RoomFootprint` (rectangular and L-shaped rooms, zero walls, margins), `RoomStatusFormatter` (every message, decimal separator), `PlacementValidator` and `FurnitureItem`. Run via the Test Runner as above.

## AI-use note

This project is built with [Claude Code](https://claude.com/claude-code) as a coding assistant — writing C# against my specs, wiring up Unity Editor/Meta SDK settings, and running tests. I design the architecture, review and integrate every change, and do all testing (Editor, simulator, and on-device where noted). Nothing here ships without me having read and understood it.
