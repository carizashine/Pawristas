# Syrup Drop Shader & Jar Tinting — Setup Instructions

Two improvements to the syrup minigame visuals:

1. **`Custom/SyrupDrop` shader** — a self-shaded shader for the falling drops
   with a glossy specular highlight, fresnel rim glow, and per-drop animated
   shimmer. Drops look shiny and liquid instead of flat plastic spheres.

2. **Jar material-slot tinting** — the controller can now tint only one
   material slot on the jar renderer instead of replacing every material with
   the syrup color. This preserves the cap, accents, etc.

---

## What's been added / changed

- **NEW:** `Assets/Shaders/SyrupDrop.shader` — URP HLSL pass + Built-in CG
  fallback. Properties: `_Color`, `_Highlight`, `_Glossiness`, `_FresnelPower`,
  `_FresnelStrength`, `_ShimmerSpeed`, `_ShimmerStrength`.

- **MODIFIED:** `Assets/Scripts/SyrupMiniGameController.cs`
  - New field `dropMaterial` — assign a material that uses
    `Custom/SyrupDrop` and every spawned drop will copy from it (with the
    syrup color applied).
  - New field `jarMaterialIndex` — `-1` to tint every material slot (old
    behavior), or `0`, `1`, etc. to tint only the body's slot.
  - New helper `TintJar()` — uses `jarRenderer.materials` so it always
    operates on per-instance copies, never the shared asset.

---

## Part 1 — Set up the shiny drops

### Step 1: Create a material from the shader
1. In the **Project** window, navigate to `Assets/Shaders/`.
2. Right-click on `SyrupDrop.shader` → **Create > Material**.
3. Name the new material **SyrupDrop_Mat**.
4. Select **SyrupDrop_Mat**. The shader should already be set to
   **Custom/SyrupDrop**. If not, click the Shader dropdown at the top and
   choose it.

### Step 2: Tweak the look (optional)
With **SyrupDrop_Mat** selected, the Inspector shows:

| Property            | What it does                                              | Default |
|---------------------|-----------------------------------------------------------|---------|
| Drop Color          | Base body color. Overridden by the script per-syrup-type. | brown   |
| Highlight Color     | Color of the specular and fresnel highlights.             | white   |
| Glossiness          | Higher = tighter, more concentrated highlight.            | 0.85    |
| Fresnel Power       | Higher = thinner rim. Lower = more body lit by rim.       | 3.0     |
| Fresnel Strength    | Brightness of the rim.                                    | 1.2     |
| Shimmer Speed       | How fast the brightness pulses.                           | 2.0     |
| Shimmer Strength    | How strong the shimmer is. 0 = no shimmer.                | 0.35    |

The script overrides `_Color` per-drop with the syrup color, so don't worry
about the body color slot — it'll get replaced at runtime.

### Step 3: Assign the material to the controller
1. Open the **SyrupMinigame** scene.
2. Select the **SyrupController** GameObject in the Hierarchy.
3. In the Inspector, find the **Drop Appearance > Drop Material** field.
4. Drag **SyrupDrop_Mat** from the Project window into that slot.

That's it for drops. Press Play and the drops should now look glossy and
catch the light, with subtle pulsing.

---

## Part 2 — Fix the jar tinting

The previous behavior tinted **every** material on the jar, which wiped out
the yellow cap and made the whole bottle uniformly brown.

Two ways to fix it depending on how your jar prefab is built:

### Option A — Jar uses one Material with multiple slots

This is the most common setup. In Unity, a single `MeshRenderer` can have
multiple **Materials** (one per submesh). Look at the jar's Mesh Renderer in
the Inspector:

- If it shows multiple **Element 0**, **Element 1**, etc. under the
  **Materials** array, you have multiple slots.
- The body is probably **Element 0** but it depends on the model.

**Steps:**
1. Select the jar in the Hierarchy.
2. Look at the Mesh Renderer's **Materials** list in the Inspector. Note
   which element is the body (the part you want to color with syrup) — try
   selecting different elements until you can spot which submesh tints when
   you change its material color.
3. Open **SyrupController** in the Inspector.
4. Find **Jar > Jar Material Index** and set it to the index of the body
   slot (usually **0**).
5. Press Play. The body should tint to syrup color while the cap stays
   original.

If your jar has a single material covering everything (no separate body),
tinting the whole thing IS the only option — there's nothing to preserve.
In that case, leave `Jar Material Index = -1`.

### Option B — Jar is built from multiple child GameObjects

If the cap is its own child GameObject with its own renderer, just point
**Jar Renderer** at the body's renderer (not the parent's). The cap renderer
won't be touched at all because the script never sees it.

**Steps:**
1. Expand the jar in the Hierarchy and find which child has the colorable
   body mesh.
2. Open **SyrupController** in the Inspector.
3. Drag that body child into **Jar > Jar Renderer**.
4. Leave **Jar Material Index** at `-1`.

---

## Quick reference

| Symptom                                  | Where to fix                                             |
|------------------------------------------|----------------------------------------------------------|
| Whole jar turns brown                    | Set **Jar Material Index** to body slot (e.g. `0`)       |
| Cap is brown but body should also be     | Set **Jar Material Index** back to `-1`                  |
| Drops still look matte / not shiny       | Make sure **Drop Material** uses `Custom/SyrupDrop`      |
| Drops are pink/purple (broken material)  | The shader file is missing or compile-failed — check the console |
| Highlight is too sharp                   | Lower **Glossiness** on **SyrupDrop_Mat**                |
| Shimmer is too aggressive                | Lower **Shimmer Strength** or **Shimmer Speed**          |

---

## How it works (technical notes)

**The shader** uses a fixed-direction light (`(0.30, 0.85, -0.30)`) instead
of sampling URP's real `GetMainLight()`. This means:
- Drops always look the same regardless of scene lighting.
- No need to set up Directional Lights in the SyrupMinigame scene for them.
- The shimmer term uses world-space X and Z to vary per drop, so a burst
  doesn't pulse in lockstep — it looks alive.

**The jar tint** uses `Renderer.materials` (plural) which Unity guarantees
returns per-instance copies. We mutate those copies and write them back. The
shared material asset on disk is never modified, so this is safe to call in
`Start()` every play session.
