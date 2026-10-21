# Room Planner (MR)

A mixed-reality app for Meta Quest 3S. You see your real room through passthrough and place virtual furniture that snaps to the real floor and walls.

Solo portfolio project. Design, integration and testing by me; built with [Claude Code](https://claude.com/claude-code) assistance.

## Goal

Walk around your actual room in passthrough, drop in virtual furniture (sofas, tables, shelves), and see how it fits before you buy or move anything — placement is validated against the real room's floor footprint so items can't end up floating or clipping through walls.

## Tech

- Unity 6000.3.5f2, Universal Render Pipeline (URP), OpenXR
- Meta XR SDK 207.0.1 — Core, Interaction SDK (+ Essentials), MR Utility Kit (MRUK, added in Phase 2)
- NUnit Edit Mode tests (Unity Test Framework)
- Target device: Meta Quest 3S, 72 FPS

## Project status

Phase 1 of 5 (setup) is complete: Quest/Android build configuration, passthrough + scene permission, the Core/Placement/Room/Interaction/UI assembly layout, a `FurnitureItem` + `PlacementValidator` core model with tests, and a base scene with a passthrough-enabled camera rig.

Room loading (MRUK), furniture placement, measurements/save-load, and polish/release are still ahead.

## How to open and run

1. Install Unity **6000.3.5f2** via Unity Hub.
2. Clone this repo and open it as a Unity project (`Open` → select the repo root).
3. Let Unity resolve packages on first open.
4. **Edit Mode tests**: `Window > General > Test Runner` → `EditMode` tab → `Run All`.
5. **Play Mode (no headset)**: use the Meta XR Simulator with a synthetic room — `Meta > Simulator` in the Editor menu, then Play.
6. **On-device**: switch to the `Meta Quest` Build Profile (already set as active) and Build & Run with a Quest 3S connected over USB/ADB, developer mode enabled.

## Tests

12 NUnit Edit Mode tests, all passing (`RoomPlanner.Core` model + placement validation). Run via the Test Runner as above.

## AI-use note

This project is built with [Claude Code](https://claude.com/claude-code) as a coding assistant — writing C# against my specs, wiring up Unity Editor/Meta SDK settings, and running tests. I design the architecture, review and integrate every change, and do all testing (Editor, simulator, and on-device where noted). Nothing here ships without me having read and understood it.
