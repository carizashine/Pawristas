# Syrup Station Trigger — Cafe Scene Setup

After this is wired up, the player flow becomes:

1. Espresso minigame → Cafe → cup spawns at the espresso machine.
2. Player drags the cup onto the **syrup stand snap point** → the cup snaps in
   place and the syrup minigame loads automatically.
3. Syrup minigame → back to Cafe → the cup respawns at the syrup stand snap
   point (still fully draggable).
4. Player drags the cup to the pickup counter as before.

If the player drops the cup somewhere that isn't the syrup stand or the
counter, it resets to its spawn position — same as the existing espresso
behavior.

If the player ever drops the espresso cup back on the syrup stand after syrup
is already done, the cup just snaps there and does nothing — no second trigger.

---

## Files added / changed

- **NEW:** `Assets/Scripts/SyrupStationDropZone.cs` — drop zone component with
  a snap point and screen-space accept radius. Triggers
  `GameSessionManager.GoToSyrupMinigame()` on first valid drop.
- **MODIFIED:** `Assets/Scripts/CafeCupDraggable.cs` — added a
  `syrupStation` reference and the `StopDrag` flow now checks the syrup
  station before falling back to the counter.
- **MODIFIED:** `Assets/Scripts/CafeEspressoCupSpawner.cs` — added a
  `syrupStation` reference. Spawner now passes it through to the cup it
  instantiates, AND respawns the cup at the syrup stand once
  `HasCompletedSyrup` is true.

---

## Unity setup steps (Cafe scene)

### Step 1 — Create the syrup stand drop zone

1. Open `Assets/Scenes/Cafe.unity`.
2. In the Hierarchy, right-click → **Create Empty**, name it
   **SyrupStation**. Position it next to your syrup stand model on the
   counter (wherever feels natural for the player to drop the cup).
3. Right-click on **SyrupStation** → **Create Empty**, name the child
   **SyrupStation_SnapPoint**. Position it at the exact spot where the cup
   should sit when snapped — this is where it visually rests.
4. Select **SyrupStation** in the Hierarchy. In the Inspector,
   **Add Component** → **SyrupStationDropZone**.

### Step 2 — Configure the SyrupStationDropZone

On the **SyrupStation** GameObject:

| Field                  | Value                                                                          |
|------------------------|--------------------------------------------------------------------------------|
| Snap Point             | Drag **SyrupStation_SnapPoint** here                                           |
| Screen Accept Radius   | `200` (start here — same default as the counter)                               |
| Place Audio            | Drag any AudioSource you want to play on snap (or leave blank)                 |

The screen accept radius is in **pixels** — when the player releases the
mouse, if the cursor is within this many pixels of the snap point's
screen-space position, the drop counts. Increase it if the snap feels too
strict, decrease it if accidental drops are happening.

### Step 3 — Wire the spawner

1. In the Hierarchy, find the GameObject that has the
   **CafeEspressoCupSpawner** component (it's whatever spawns the cup near
   the espresso machine).
2. In the Inspector, find the new **Syrup Station** field.
3. Drag the **SyrupStation** GameObject (the one with the
   `SyrupStationDropZone` component) into that field.

That's it — at runtime the spawner will pass the reference through to the
spawned cup automatically. You do **not** need to manually set anything on
the cup prefab.

### Step 4 — Build settings

Verify all three scenes are in **File > Build Settings > Scenes In Build**:
- Cafe
- EspressoMinigame
- SyrupMinigame

If `SyrupMinigame` isn't in build settings, the scene change in step 2 of
the player flow will silently fail.

---

## Testing

Quickest manual test path:

1. Start from **Cafe.unity** (or **TakingOrder.unity** if you have a full
   flow). Make sure `GameSessionManager` is active in the scene so it has
   a `CurrentOrder`.
2. Walk over to the espresso machine, do the espresso minigame, return to
   Cafe — the cup spawns at the espresso machine.
3. Drag the cup → release near the **SyrupStation_SnapPoint** → cup snaps
   there → SyrupMinigame loads.
4. Play the syrup minigame → return to Cafe.
5. Verify: the cup has respawned at the syrup stand snap point and is
   still draggable.
6. Drag it to the pickup counter — it should snap there (existing
   behavior, unchanged).
7. Drag it back to the syrup stand after step 6 — it should snap there
   silently and NOT trigger the minigame again.

---

## Edge cases handled

- **Pastry on the syrup stand** — ignored. `SyrupStationDropZone.ReceiveCup`
  checks `itemType == CafeItemType.Espresso` and bails out otherwise.
- **Drop missed both zones** — `ResetCup()` snaps the cup back to its
  spawn position. Same as the existing espresso-on-counter fallback.
- **Re-entry after syrup is done** — `SyrupStationDropZone.ReceiveCup`
  checks `OrderProgressTracker.HasCompletedSyrup` and skips the scene load
  if it's already true.
- **`GameSessionManager` not in scene (testing in isolation)** — logs a
  warning, scene load is skipped, cup is still snapped.
