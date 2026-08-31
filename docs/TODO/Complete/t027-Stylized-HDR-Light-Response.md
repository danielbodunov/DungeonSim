# t027 — Stylized HDR Light Response

## Tracking

- **ID:** t027
- **Status:** Complete
- **Milestone:** Strategic Construction
- **Depends On:** t024 — Rotation-Safe Tile Textures
- **Related:** Propagated dungeon-light HDR source controls
- **Branch:** `feature/lighting-updates`

## Goal

Turn propagated local HDR energy into an expressive pixel-art hot-core response
while preserving quantized normal illumination, multiplicative atlas contrast,
world-stable light sampling, and shared materials.

## Implemented scope

- Normal ambient/local illumination remains quantized from `_MinLight` through
  authored full-light brightness (`1.0`).
- Local-only energy above `_OverbrightThreshold` is shaped by
  `_OverbrightResponse` and remapped up to `_MaxOverbright`.
- Restrained `_LightColorInfluence` remains responsible for normal local tint.
- `_OverbrightColorInfluence` applies stronger normalized local hue in
  proportion to the actual hot response.
- Local-only excess energy also drives an additive source-colored wash masked by
  original atlas luminance. `_HotWashBlackPoint` protects near-black pixels and
  `_HotWashFullPoint` identifies pixels receiving the full wash.
- Expansion suppresses atmospheric lighting, overbright, and hot tint. Exploring
  exposes the complete response.
- The old `_OverbrightStrength` property was removed from the production shader
  and shared material. No propagation, texture, or source-control changes were
  required for this presentation refinement.
- Overbright remains smooth. Separate hot-band quantization was deferred until
  visual validation demonstrates that it improves the existing pixel-snapped
  and quantized presentation.
- Visible lighting pixels per cell has no enforced upper cap. The manager now
  publishes propagation samples per cell separately, and snapped visible-block
  centers resolve to exact propagation texel centers before temporal blending.
  This is the first-line stability fix for high densities including 32; no
  geometry, depth-state, or screen-space workaround was introduced.
- Exact point reconstruction proved too coarse because visible density appeared
  capped by propagation resolution. The shader now manually reconstructs each
  temporal RGB field from four exact texel-center samples, then temporally blends
  the reconstructed results. This uses eight light-texture reads per fragment
  and allows 8/16/32 visible blocks to remain distinct from Smooth2x/Smooth4x.
  The taps use integer texture loads rather than the bilinear sampler so filtering
  precision cannot introduce differences before brightness quantization.

## Current defaults

- Active material `_OverbrightThreshold`: `1.37` (preserved author tuning)
- Active material `_OverbrightResponse`: `1.07` (preserved author tuning)
- Active material `_MaxOverbright`: `1.127` (preserved author tuning; shader
  default is `1.75`)
- Active material `_LightColorInfluence`: `0` (preserved author tuning)
- Active material `_OverbrightColorInfluence`: `0.333` (preserved author tuning)
- `_HotWashStrength`: `0.75`
- `_HotWashBlackPoint`: `0.05`
- `_HotWashFullPoint`: `0.3`
- `_HotWashColorInfluence`: `0.9`

## Acceptance criteria

- Core Boost and source intensity create visibly distinct hot regions once local
  energy crosses the threshold.
- Normal illumination remains capped at authored full-light brightness.
- Ambient alone cannot produce overbright or hot tint.
- Normal tint remains restrained while the hot core clearly reflects source hue.
- Dark atlas details retain their relative contrast under powerful sources.
- Powerful sources visibly wash receptive midtones toward their accumulated
  local hue while protected near-black pixels remain substantially darker.
- Animated and overlapping colored sources use the existing propagated RGB
  field without per-source shader loops.
- Expansion/Exploring behavior, world-stable pixel sampling, shared materials,
  rotation-safe atlas behavior, AO, topology, and traversal remain unchanged.

## Unity validation

- [x] Compare Core Boost `0`, `2`, `4`, and `8` with other settings fixed.
- [x] Compare intensity `0.5`, `1`, `2`, and `4` near the source.
- [x] Test `_MaxOverbright` at `1`, `1.5`, `2`, `2.5`, and `3`.
- [x] Tune threshold and response independently and confirm their roles differ.
- [x] Test normal color influence at `0`, `0.25`, `0.5`, and `1`.
- [x] Test hot color influence at `0`, `0.5`, `0.75`, and `1`.
- [x] Test hot wash strength at `0`, `0.25`, `0.5`, `1`, and `2`.
- [x] Test black point at `0`, `0.05`, `0.1`, and `0.2`, and full point at
  `0.2`, `0.3`, `0.4`, and `0.5`.
- [x] Compare white, orange, red, blue, and violet HDR sources.
- [x] Validate an animated colored torch and overlapping red/blue hot sources.
- [x] Confirm dark cracks, holes, mortar, and vertex AO retain relative contrast.
- [x] Confirm Expansion suppresses hot response and Exploring enables it.
- [x] Confirm representative renderers retain the shared material without
  `(Instance)` creation.
- [x] Validate visible lighting pixels per cell at `2`, `4`, `8`, `16`, and `32`
  without hatching on static and moving lights.
- [x] At `32`, compare LegacyCell, Smooth2x, and Smooth4x and Light Steps
  `4`, `5`, `6`, and `8`.
- [x] Pan/zoom the camera and inspect R0/R1/R2/R3 tiles at `32`; confirm the
  visible grid remains world-stable and hot wash/color remain coherent.

Do not mark this ticket Complete until the checklist has been manually validated
in Unity.

## Out of scope

- Unity point/spot lights or realtime shadows
- Directional or normal-based lighting
- Bloom, post-process exposure, emission, or volumetrics
- Per-light shader loops or material instances
