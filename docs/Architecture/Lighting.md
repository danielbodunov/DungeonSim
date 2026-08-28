# Dungeon Lighting Architecture

## Purpose

`DungeonLightingManager` maintains a low-resolution world-space light field for dungeon materials. Lighting is a downstream consumer of dungeon topology rather than part of placement ownership.

## Inputs

- `TileGridGenerator` — grid dimensions, placed topology, material application, and `LayoutChanged`.
- `DungeonLightSource` — static/dynamic light sources and `SourcesChanged`.

## Runtime flow

```mermaid
flowchart LR
    TileGridGenerator -->|LayoutChanged| DungeonLightingManager
    DungeonLightSource -->|SourcesChanged| DungeonLightingManager
    DungeonLightingManager -->|chunked propagation| CurrentTexture[Current dungeon light texture]
    CurrentTexture -->|previous/current interpolation| Materials[Dungeon materials]
    DungeonLightingManager -->|shader globals| Materials[Dungeon materials]
```

The manager supports legacy cell sampling and smoother sub-cell sampling presets. Dynamic sources refresh on an interval rather than forcing a full per-frame rebuild.

Dynamic presentation uses two shared textures. Before a dynamic propagation
upload, the current target texture is copied to the previous texture. The new
field is uploaded to the current texture, and `_DungeonLightTextureBlend`
advances from 0 to 1 every rendered frame using unscaled time over the configured
dynamic update interval. Full/static rebuilds synchronize both textures
immediately. The default dynamic update interval is 0.05 seconds (20 Hz).

`RotationSafeTileAtlas.shader` samples `_DungeonLightTexture` with the manager's
grid-origin, grid-step, and grid-size globals. Before sampling, it snaps the
world-derived grid coordinate to `_DungeonLightingPixelsPerCell` blocks per
dungeon cell; the default is 2. This grid-space snapping is stable under camera
movement and tile rotation and is independent of the underlying LegacyCell,
Smooth2x, or Smooth4x propagation resolution.

CPU chunk storage uses floating-point `Color` buffers, and the paired GPU fields
use `RGBAHalf` when supported (`RGBAFloat` is the explicit HDR fallback). Source
and overlapping-light values accumulate without a 0-1 ceiling. Platforms
supporting neither HDR format fail lighting initialization rather than silently
falling back to an 8-bit saturated field.

The shader interpolates the previous/current local RGB fields, then combines
that local result with `_DungeonAmbientColor` and converts the total to
non-negative Rec.709 luminance. HDR energy is compressed through
`1 - exp(-energy * _LightExposure)` before quantization with `_LightSteps`. It
remaps the quantized result through `_MinLight` to 1 and multiplies the authored
atlas rather than adding light color to it. Local-only luminance above
`_OverbrightThreshold` contributes the bounded multiplier
`1 + (1 - exp(-excess)) * _OverbrightStrength`; ambient cannot produce this
effect. The separate
phase-controlled `_GlobalLightIntensity` presentation multiplier is applied
afterward. The
`_DungeonLightingInitialized` global prevents an uninitialized/default texture
from lighting tiles before a manager has established its grid.

Ambient and local color have separate responsibilities. Ambient contributes to
brightness but not local hue. Local RGB is normalized by its maximum channel,
falling back to white when no local light is present, and blended from white by
the material's `_LightColorInfluence` (default 0.35). Overlapping colored sources
remain accumulated in the propagated RGB field, so their combined field color
produces the tint without a per-source shader loop. Tint and illumination remain
multiplicative, preserving near-black atlas detail.

## Source controls and animation

`DungeonLightSource` owns source behavior. Its effective contribution is
`CurrentColor × CurrentIntensity × shaped falloff × core boost`.

The base falloff is `pow(1 - saturate(distance / radius), falloffPower)`. Within
the inner radius it is multiplied by
`1 + saturate(1 - distance / innerRadius) * coreBoost`. This is continuous at the
inner-radius boundary and reaches zero at the outer radius.

Current defaults and safe ranges are:

- intensity: `1`, non-negative and HDR-capable;
- radius: `6` cells, minimum `0.25`;
- falloff power: `2`, range `0.1-8`;
- inner radius: `0`, clamped to `0-radius`;
- core boost: `0`, range `0-8`;
- intensity flicker: opt-in, amount `0.12` (`0-1`), speed `2` (`0.01-10`);
- color animation: opt-in, amount `0.25` (`0-1`), speed `1` (`0.01-10`), an
  authorable HDR gradient, and Noise or Loop mode.

Noise animation uses deterministic Perlin samples derived from the source's
integer seed. Intensity and color use different salted coordinates so they do
not move in lockstep. Loop mode is the deliberate predictable gradient option.
Animation uses unscaled presentation time, so cosmetic flicker continues during
selective simulation pause. Animated sources are treated as dynamic; the manager
samples their current state only at its normal dynamic refresh, and temporal
texture interpolation smooths the visible result.

Useful starting points (tuning examples, not hardcoded presets):

- Torch: radius `4-6`, power `2-3`, inner radius `1-1.5`, core boost `2-4`,
  warm HDR color, intensity `1.5-3`, noise flicker amount `0.08-0.18`, and a
  restrained warm gradient.
- Soft ambient source: radius `6-10`, power `0.5-1`, no core boost, low intensity,
  and animation disabled.
- Strong magical source: radius `5-8`, power `1-2`, inner radius `1-2`, core
  boost `1-3`, intensity `2-4`, and an authored blue/violet/cyan gradient using
  Noise or intentional Loop mode.

## HDR memory

For the current 15×15 grid, each `RGBAHalf` texture uses 8 bytes per sample
instead of 4 for `RGBA32`. With paired temporal textures, approximate GPU
storage is 3.6 KB at LegacyCell, 14.4 KB at Smooth2x, and 57.6 KB at Smooth4x
(2× the previous paired-RGBA32 storage). The three CPU chunk arrays use 16-byte
`Color` entries instead of 4-byte `Color32`: approximately 10.8 KB, 43.2 KB, and
172.8 KB respectively (4× previous CPU storage). These figures exclude small
texture/object and managed-array overhead.

The production tile receiver is visually unlit and has no main directional-light
or realtime main-light-shadow dependency. Normal data is used for rotation-safe
surface-role selection only, not diffuse lighting. Dungeon Sim therefore does
not rely on a sun/directional light for normal dungeon rendering.

The production `Placeholder_NPC` carries a dynamic `DungeonLightSource`, so its
light field contribution follows traversal automatically and refreshes at the
manager's configured dynamic interval.

## Phase presentation modes

`GameplayLoopController` selects the lighting presentation through
`DungeonLightingManager.SetPresentationMode` whenever the dungeon phase changes:

- `ExpansionUniform` uses uniform material illumination. It bypasses both the
  quantized dungeon light response and the propagated dungeon light texture so
  construction remains clearly readable.
- `ExploringAtmospheric` enables ambient dungeon darkness and quantized
  propagated static/dynamic sources such as NPC lights.

The manager blends `_DungeonLightingModeBlend` between these responses using
unscaled time. The default transition duration is 0.3 seconds, so pausing the
game does not strand the presentation between modes. Disabling or unloading the
manager resets the global to the uniform expansion response.

This blend is independent of `_GlobalLightIntensity`. The presentation mode
chooses which lighting model is visible; `DungeonVisualLightingController`
controls the separate phase-brightness treatment.

The runtime debug lighting override controls both layers. Enabling it uses the
configured debug brightness and forces the uniform presentation response,
bypassing quantized propagated dungeon cell/NPC lighting. Disabling it blends
back to the response for the current gameplay phase.

Current `DungeonLightSource` illumination is transported through the propagated
light field; it does not use Unity point/spot lights and does not cast realtime
Unity shadows. The shader retains its `ShadowCaster` pass for future
compatibility, but realtime stylized point/spot-light shadow reception requires
a separate implementation.

## Independent tuning controls

- Propagation resolution (`LightQualityPreset`) controls samples calculated per
  dungeon cell.
- Dynamic update interval controls how often moving sources are propagated.
- Visible lighting pixels per cell controls only the world-space block size used
  for shader sampling.
- `_LightSteps` controls the number of brightness bands.
- `_LightExposure` maps HDR energy into the normal 0-1 illumination response.
- `_OverbrightThreshold` and `_OverbrightStrength` control bounded local-only
  illumination above authored full-light brightness.
- `_LightColorInfluence` controls restrained continuous local hue independently
  from quantized brightness.

## Ownership rule

Do not add light-state persistence to individual tile placements unless the light is itself authoritative gameplay content. The light field is derived from the current layout and active sources and should be rebuilt when those inputs change.

## When a generation change affects lighting

Check lighting if you change:

- what constitutes an open/closed connection;
- grid dimensions or coordinate mapping;
- material assignment for placed tiles;
- how layout-change events are emitted;
- topology that should block or transmit light.

## Extension guidance

- New lamp/torch content: prefer `DungeonLightSource` configuration/component work.
- New material response: shader / `DungeonLightReceiver` or material-side work.
- New propagation rule: `DungeonLightingManager` architecture work.
- Purely decorative emissive art that does not illuminate the dungeon does not need to enter this system.

## Related docs

- [`Dungeon_Generation.md`](Dungeon_Generation.md)
- [`System_Map.md`](System_Map.md)
- [`../Design/Visual_and_Interaction_Design.md`](../Design/Visual_and_Interaction_Design.md)
