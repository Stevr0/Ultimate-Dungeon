# UI Windowing System — Ultimate Dungeon

Version: 0.1  
Last Updated: 2026-01-31  
Engine: Unity 6 (URP)  
Networking: NGO (client UI only)  

---

## Purpose

Provide a **clean, UO-style UI shell** where:
- **Multiple windows** can be open at the same time (Inventory + Paperdoll + Skills, etc.)
- Clicking a window **brings it to the front**
- A **modal stack** (Loot / Confirm / Vendor) can appear above all windows
- Modals can **block world + window input** with a single `ModalBlocker`

This is **UI plumbing only** — no gameplay wiring.

---

## Assumptions / Required Hierarchy

You said you now have this (good):

- `Canvas_HUD`
- `Canvas_Windows`
  - `WindowDock`
  - (window prefabs as children or spawned under it)
- `Canvas_Modals`
  - `ModalBlocker` (full-screen Image, Raycast Target ON)
  - `ModalRoot`

**Sorting Orders** (recommended):
- HUD: `0`
- Windows: `10`
- Modals: `20`

---

## Wiring Steps (Setup)

### 1) Add the managers

On `Canvas_Windows`, add:
- `UIWindowManager`

On `Canvas_Modals`, add:
- `UIModalManager`

Drag references:
- `UIWindowManager.WindowDock` → your `WindowDock` Transform
- `UIModalManager.ModalRoot` → your `ModalRoot` Transform
- `UIModalManager.ModalBlocker` → your `ModalBlocker` Image/GameObject

### 2) Add UIWindow to each window prefab

On each `Window_*` prefab root, add:
- `UIWindow`

Set (optional):
- `WindowId` (e.g., `Inventory`, `Paperdoll`, `Skills`, `Magic`, `Character`)
- `StartOpen` true/false

### 3) Add UIModal to each modal prefab (when you create them)

On each `Modal_*` prefab root, add:
- `UIModal`

Set:
- `ModalId` (e.g., `Loot`, `Confirm`, `Vendor`)

---

## Behavior Rules (Locked for this plumbing)

### Windows (UO-style)
- Multiple windows can be **open simultaneously**.
- Any window can be **brought to front** by clicking it.
- Closing one window does **not** affect others.

### Modals
- Modals appear **above** all windows.
- Modal manager supports a **stack**:
  - Open Confirm while Loot is open → Confirm becomes topmost.
  - Closing topmost returns focus to the one below.
- When **any** modal is open:
  - `ModalBlocker` is enabled (blocks clicks to windows/world)

---

## Optional Next Step (Still setup-only)

Add a tiny `UIHotkeyToggler` script (temporary) that calls:
- `WindowManager.Toggle("Inventory")` on `I`
- `Toggle("Paperdoll")` on `P`
- `Toggle("Skills")` on `K`
- `Toggle("Magic")` on `B`
- `ModalManager.CloseTop()` on `Esc`

This lets you **validate the shell** immediately before wiring to systems.

(If you want this, tell me and I’ll add it as a SPIKE script with the required exit plan header.)

