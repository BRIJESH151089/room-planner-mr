# Room Planner (MR)

A mixed-reality app for Meta Quest 3S. You see your real room through passthrough and place virtual furniture that snaps to the real floor and walls.

Solo portfolio project. Design, integration and testing by me; built with [Claude Code](https://claude.com/claude-code) assistance.

**Demo video:** https://youtu.be/DktR8WYW8kM

## Goal

Walk around your actual room in passthrough, drop in virtual furniture, and see how it fits before you buy or move anything. Placement is validated against the real room's floor outline so items can't end up floating or clipping through walls.

## Tech

- Unity 6000.3.5f2, Universal Render Pipeline (URP), OpenXR
- Meta XR SDK 207.0.1: Core, Interaction SDK (+ Essentials), MR Utility Kit (MRUK)
- NUnit Edit Mode tests (Unity Test Framework)
- Target device: Meta Quest 3S, 72 FPS

## Features (v1.0)

- **Room loading:** loads your Space Setup room through MRUK and draws the floor (teal), walls (amber) and ceiling (faint blue) as a light translucent overlay on passthrough. A status line says `Room loaded: W x D m, N walls` and fades out after about 4 seconds; if there is no room or the spatial data permission is denied, it says what to do and stays.
- **First-use hint:** in an empty room a short hint explains how to pick and place furniture. It stays until the first item is placed.
- **Placing:** a catalog panel to your left has five items (bed, sofa, table, chair, wardrobe). Pick one with the controller ray, aim at the floor, and a ghost follows: green where it fits, red where it would go through a wall, sit closer than 5 cm to a wall, or overlap other furniture. Snaps to 5 cm in the room's own frame and turns in 15° steps.
- **Editing:** point at placed furniture to highlight it, pick it up (the spot you grabbed stays under the ray), move and turn it, drop it or put it back. Delete, and undo/redo any place, move or delete (up to 50 steps).
- **Session autosave:** your current layout is saved automatically about a second after every change (and when the app is paused or closed), and comes back the next time the app starts.
- **Save / Load checkpoint:** the Save button stores the current layout as your saved version. Load brings that saved version back, replacing what is on the floor, even after Clear All or an app restart. Clear All (press twice within 3 seconds) empties the floor but keeps the saved version.
- **Measurements:** a panel to your right shows room size, floor area, area used by furniture, free area and item count.
- **Controllers only:** the Interaction SDK hand interactors are turned off; both Touch controllers drive the ray and the buttons.

### Controls

| Button | Action |
|---|---|
| Right trigger | Place the selected item / pick up the pointed item / drop the held item |
| Right grip | Put a held item back, or stop placing |
| Right thumbstick left/right | Turn the ghost 15° per push |
| B (right) | Delete the highlighted or held item |
| X (left) | Undo (while holding an item: put it back) |
| Y (left) | Redo |

While the controller ray is over the catalog, the trigger clicks the catalog instead. The Save / Load / Clear All buttons are clicked with the ray and trigger like the furniture buttons.

## How it works

Assemblies under `Assets/Project/Scripts`, with dependencies pointing toward Core:

- **Core** (plain C#, no MonoBehaviours, XR or Meta references; all unit-tested): `RoomFootprint` + `FloorFrame` (the room outline in its own floor frame), `FurnitureCatalog`, `PlacedItem` (with a stable Id), `PlacementValidator` and `OverlapChecker` (separating axis theorem), `RayVsOrientedBox`, `GrabOffset`, `PlacementHistory` (undo/redo), `LayoutSerializer`, `LayoutRevalidator`, `LayoutSlots`, `RoomMetrics`, `OnboardingHint`, `StatusFade`, `ConfirmGate` and the text formatters.
- **Room:** `RoomLoader` starts the MRUK load itself (MRUK's *Load Scene On Startup* is off, so permission-denied and no-room cases can be reported) and turns the MRUK room into a `RoomFootprint`. `QuestPerformanceSettings` turns on dynamic foveated rendering.
- **Placement:** `GhostPreview`, `PlacementController`, `SelectionController` and the save code. The ghost and selection updates allocate no memory per frame. Furniture prefabs need no colliders; the ray is tested against each item's box in Core.
- **Interaction:** `ControllerPlacementInput` reads the Touch controllers through `OVRInput` (button names checked in the installed SDK) and raises events.
- **UI:** world-space panels (`RoomStatusPanel`, `CatalogPanel`, `MetricsPanel`) that only know Core. The catalog and measurements panels sit at eye level, 1 m away and 35° to each side, so they stay above floor furniture. Ray input uses the Interaction SDK's comprehensive OVR rig (locomotion disabled) and its "Add Ray Interaction to Canvas" quick action.

### Saving

- **Two files** in `Application.persistentDataPath` (on the Quest, the app's own data folder; in the Editor, `%USERPROFILE%\AppData\LocalLow\<company>\room-planner-mr`):
  - `layout.json`, the **session**: written by autosave and resumed at startup.
  - `checkpoint.json`, the **saved version**: written only by Save and read only by Load.
- **Format:** indented JSON with a `version` and one line per item (`id`, `definitionId`, `posX`, `posZ`, `yawDegrees`), in the room's own floor frame so the layout follows the room.
- **Safe writes:** each write goes to a temp file, is flushed and then swapped in, so a crash mid-save cannot corrupt the previous file.
- **Re-validation:** whatever is loaded is re-checked against the current room. Items that no longer fit are skipped and counted in the status message.
- **History:** Load starts a fresh undo history and is then autosaved as the new session.

## How to open and run

1. Install Unity **6000.3.5f2** via Unity Hub.
2. Clone this repo and open it as a Unity project (`Open` → select the repo root).
3. Let Unity resolve packages on first open.
4. **Edit Mode tests**: `Window > General > Test Runner` → `EditMode` tab → `Run All`.
5. **Play Mode (no headset)**: open `Assets/Project/Scenes/01_RoomPlanner.unity` and press Play. With no XR runtime, the loader falls back to MRUK's sample room `Bedroom00` (Room Index on the `Managers/MRUK` object; set it to `-1` for a random sample room).
   - **Quest Link**: with *Spatial Data over Meta Quest Link* enabled in the Link app, Play loads the room you scanned on the headset. Placement needs the Touch controllers, so use Link (there is no mouse fallback).
   - **Failure path**: on `Managers/RoomLoader`, untick *Use Device Data In Editor* and *Use Prefab Fallback In Editor*, then Play. The panel shows the "No room found" message.
6. **On-device**: switch to the `Meta Quest` Build Profile and Build & Run with a Quest 3S connected over USB (developer mode on). Run Space Setup on the headset first.

## What is tested

- **Edit Mode:** 187 NUnit tests, all passing (see below).
- **Play Mode in the Editor:** MRUK sample rooms (`Bedroom00`, `OtherRoom04_empty`), the Meta XR Simulator, a real Space Setup room over Quest Link, and the no-room failure path.
  - **Placement and editing:** all five items; rejections for walls, wall margin, corners, L-shaped cut-outs and overlaps; pick up, move, drop, put back, delete, undo/redo.
  - **Saving:**
    - Autosave, then a Play Mode restart resumes the session.
    - Save, then move an item, then Load returns the saved version.
    - Clear All, then Load brings the saved version back; Clear All keeps `checkpoint.json`.
    - Load with no saved version says "No saved layout yet." and leaves the floor alone.
    - A restart resumes the session, not the saved version.
  - **Panels:** the status line fades after about 4 s while the hint stays; the hint goes on the first placement; the panels sit at eye level.
  - **How these were driven:** a scripted ray plus direct calls to the input events and button methods, not physical buttons.
- **On the headset (my own tests):** passthrough (Phase 1); placing, moving, deleting and undo with the real controllers (Phase 3B); the Phase 4 save/load build.
- **Needs a standalone headset build to confirm:**
  - The two-file save/load on the Quest's storage, including the restart and pause cases.
  - The controller ray on the catalog now that the hand interactors are off.
  - That the eye-level panels clear real furniture at real head height (in the Editor the head sits at floor level).
  - The permission prompt.
  - Frame rate against the 72 FPS target, which has not been measured yet.

## Tests

187 NUnit Edit Mode tests, all passing:

- **Placement:** furniture catalog and labels, item ids, `OverlapChecker`, `PlacementValidator` (every reason, margins, corners, L- and U-shaped rooms, slanted walls, moves excluding themselves), `RayVsOrientedBox`, `GrabOffset`, `PlacementHistory`.
- **Saving:** `LayoutSerializer` (exact round-trip, unknown and invalid items, empty/malformed/wrong-version files), `LayoutRevalidator`, `LayoutSlots` (session vs saved version are independent, Load returns the saved version, Clear All keeps it, none saved, unreadable file, re-validation, write failure).
- **Measurements and UI logic:** `RoomMetrics` and formatters, `OnboardingHint`, `StatusFade`, `ConfirmGate`.
- **Geometry and input:** `FloorRay`, `YawStepper`, `GridSnapper`, `AxisStepDetector`, `FloorFrame`, `RoomFootprint`, `RoomStatusFormatter`.

Run them through the Test Runner as above.

## AI-use note

This project is built with [Claude Code](https://claude.com/claude-code) as a coding assistant: writing C# against my specs, wiring up Unity Editor/Meta SDK settings, and running tests. I design the architecture, review and integrate every change, and do all testing (Editor, simulator, and on-device where noted). Nothing here ships without me having read and understood it.
