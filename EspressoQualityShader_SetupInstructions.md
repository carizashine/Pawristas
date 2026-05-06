# Espresso Quality Reveal — Setup Instructions

After the espresso minigame finishes, the camera now pans down over the cup
while rotating to look straight down, lands on a flat disc rendered with a
custom shader, and waits for the player to click before loading the Cafe.

The disc shows three visual tiers based on your shot accuracy:
- **Bad pour** (`0/4` to `1/4`): pale, watery, uneven brown surface, dull crema.
- **Alright pour** (`2/4` to ~`2.5/4`): medium espresso brown with a faint warm crema ring.
- **Great pour** (`3/4` to `4/4`): rich dark espresso, golden tiger-striped crema, subtle warm shimmer.

The shader is procedural — no textures needed. It uses centered UV coordinates,
animated value noise / FBM, and angular tiger-stripe math.

---

## What's been added to the project

1. **`Assets/Shaders/EspressoQuality.shader`** — the custom shader. Has both a URP
   subshader (HLSL) and a Built-in fallback (CG) so it works in either pipeline.
2. **`Assets/Scripts/EspressoMiniGameController.cs`** — modified. New fields under
   the **"Quality Reveal Pan"** header in the Inspector:
   - `Reveal Camera`, `Pan Target`, `Quality Disc Renderer`, `Quality Shader Property`,
     `Pan Start Delay`, `Pan Duration`.
3. New helper methods inside the controller: `PanAndRevealQuality()`, `ApplyQualityToShader()`,
   `GetQualityLabel()`, `LoadCafe()`.

---

## Setup Steps (in Unity)

### Step 1 — Create the material from the shader
1. In the **Project** window, navigate to `Assets/Shaders/`.
2. You should see **EspressoQuality.shader**. Right-click in the folder → **Create > Material**.
3. Name the new material **EspressoQuality_Mat**.
4. Select the new material. In the Inspector, click the **Shader** dropdown at the top
   and choose **Custom > EspressoQuality**.
5. You should now see four shader properties:
   - **Quality** — slider 0 to 1. Drag it around to preview the three tiers.
   - **Surface Noise Scale** — how dense the noise pattern is. Default 6.
   - **Crema Ring Width** — fraction of the disc occupied by the crema ring. Default 0.18.
   - **Shimmer Speed** — how fast the warm shimmer pulses on great pours. Default 1.0.

### Step 2 — Open the EspressoMinigame scene
1. Open `Assets/Scenes/EspressoMinigame.unity`.

### Step 3 — Create the disc
1. In the **Hierarchy**, right-click → **3D Object > Quad**.
2. Rename it **EspressoQualityDisc**.
3. Position it directly above the espresso cup, just barely above the surface where the
   coffee would be. Example values, adjust to your cup:
   - Position: `(cup.x, cup.y + 0.05, cup.z)`
   - Rotation: `(90, 0, 0)` — flat, facing up.
   - Scale: roughly the diameter of the cup interior, e.g. `(0.18, 0.18, 0.18)`.
4. Drag **EspressoQuality_Mat** from the Project window onto the disc to apply the material.
5. In Play mode, the disc should appear as a colored circle. (Outside the disc shape it's
   discarded by the shader.)

### Step 4 — Create the camera pan target
1. Right-click in Hierarchy → **Create Empty**.
2. Rename it **EspressoCamPanTarget**.
3. Position it directly above the cup, far enough back that the disc fits comfortably
   in frame when the camera looks straight down. Example: `(cup.x, cup.y + 1.2, cup.z)`.
4. The camera will rotate to `Rotation: (90, currentY, 0)` automatically — you only need
   to set the **position**.

### Step 5 — Wire everything up on the Espresso Controller
1. Select the GameObject that has the **EspressoMiniGameController** script attached
   (probably called `EspressoController` or similar).
2. In the Inspector, find the new **"Quality Reveal Pan"** section and fill in:

| Field                      | Drag this in                                                   |
|----------------------------|----------------------------------------------------------------|
| Reveal Camera              | The scene's **Main Camera** (or leave blank for `Camera.main`) |
| Pan Target                 | **EspressoCamPanTarget**                                       |
| Quality Disc Renderer      | **EspressoQualityDisc** (its Mesh Renderer is auto-detected)   |
| Quality Shader Property    | Leave as `_Quality`                                            |
| Pan Start Delay            | `0.7` (pause before camera starts moving)                      |
| Pan Duration               | `1.6` (camera move + rotate time)                              |

### Step 6 — Build settings
Make sure both `EspressoMinigame` and `Cafe` scenes are in **File > Build Settings >
Scenes In Build**, otherwise the click-to-load-Cafe step will fail.

---

## Testing

1. Open `EspressoMinigame.unity` and press **Play**.
2. Without `GameSessionManager` running (testing in isolation), `requiredShots` defaults to
   1 and the game runs with that setting.
3. Make all shots → quality reveal disc shows the rich dark espresso with golden crema.
4. Miss all shots → disc shows pale washed-out brown.
5. Verify camera pans smoothly down to the disc, then click anywhere — the Cafe scene loads.

---

## Tuning the look

If the disc looks off, tweak in the **EspressoQuality_Mat** Inspector:

| Want to change...                  | Adjust this property      |
|------------------------------------|---------------------------|
| Crema ring is too thick/thin       | Crema Ring Width          |
| Surface looks too smooth/blobby    | Surface Noise Scale       |
| Shimmer pulses too fast/slow       | Shimmer Speed             |
| Want to preview all three tiers    | Drag Quality slider 0→1   |

If the colors themselves feel off, open the shader file and tweak the six color constants
near the top of the `frag` function (`badColor`, `okColor`, `greatColor`, `badCrema`,
`okCrema`, `greatCrema`).

---

## How quality maps to score

```
quality = successfulShots / requiredShots   // clamped 0..1
```

Examples (with required = 4):
- 0/4 → 0.0  → bad
- 1/4 → 0.25 → between bad and alright
- 2/4 → 0.5  → exactly alright
- 3/4 → 0.75 → between alright and great
- 4/4 → 1.0  → great

The shader smoothly interpolates between the three tier palettes — there are no hard
cutoffs, so 1/4 looks meaningfully better than 0/4 even though both are "bad".

The on-screen text label (`Bad`, `Alright`, `Great`) uses these thresholds:
- `>= 0.75` → Great
- `>= 0.40` → Alright
- otherwise → Bad
