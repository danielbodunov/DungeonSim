# RENDER-04 — Pixel-Lit Modular Character Shader

## Tracking
- **ID:** RENDER-04
- **Status:** Planned
- **Milestone:** Pixel Rendering / Characters
- **Depends on:** RENDER-01; RENDER-02 for rigid equipped-item rendering conventions

## Goal
Establish the rendering foundation for modular, rigged 3D pixel-art characters. Characters should use ordinary skinned 3D meshes, low-resolution point-filtered textures, and the shared Pixel-Lit lighting model while allowing runtime appearance variation without creating one material asset per character.

The character source and runtime representation remain 3D. Generated sprite sheets, billboarding, and frame-by-frame character rendering are not part of this pipeline.

## Rendering Model
The intended visual stack is:

1. Simple/stylized low-poly character geometry.
2. Shared skeletal animation where anatomy permits.
3. Low-resolution authored character textures.
4. Point-filtered sampling.
5. Pixel-Lit Character shading using the shared dungeon lighting core.
6. Quantized/stylized dungeon lighting.
7. Optional project-wide low-resolution rendering/upscaling may be evaluated separately.

The shader is not responsible for converting conventional high-resolution artwork into pixel art.

## Requirements
- Add a dedicated `PixelLitCharacter` shader consuming `DungeonPixelLitCore.hlsl`.
- Support ordinary UV0 sampling suitable for Blender-authored skinned characters.
- Support point-filtered base-color textures and base tint.
- Work correctly with `SkinnedMeshRenderer` deformation and animated normals.
- Preserve the RENDER-00 material-mask contract:
  - R = emission
  - G = roughness
  - B = metallic
  - A = reserved
- Support a separate character customization mask. Do not overload the material mask with appearance-selection data.
- Establish four initial customization regions:
  - R = skin
  - G = hair
  - B = primary clothing
  - A = secondary clothing
- Expose runtime colors for those four regions.
- Preserve authored base color on pixels not selected by the customization mask.
- Define a predictable authoring method for customizable pixels so their value/detail can be retained while hue is supplied at runtime.
- Permit multiple character renderers to share a base material while receiving different appearance values through renderer-level property overrides where practical.
- Do not require a separate material asset for every generated character appearance.
- Preserve the established emission, roughness, metallic, specular, diffuse, ambient, overbright, hot-wash, and vertex-AO behavior where applicable.
- Keep clean extension points for RENDER-05 runtime effects without implementing those effects here.
- Keep terrain atlas addressing, procedural character generation, animation state logic, and equipment gameplay semantics out of the shader.

## Character / Prop Boundary
Use the Character shader for deformable and/or appearance-customizable character surfaces such as body, hair, clothing, and skinned armor.

Rigid equipped objects such as swords, pickaxes, shields, torches, backpacks, and rigid helmets should normally use `PixelLitProp` from RENDER-02 and attach to character equipment sockets. A rigid item should not require the Character shader solely because an NPC equipped it.

## Acceptance Criteria
- A representative low-poly skinned humanoid renders correctly while idle and moving under dungeon lighting.
- Character lighting is visually coherent beside terrain and Pixel-Lit props.
- Low-resolution character textures remain crisp and stable during skeletal animation.
- At least three instances can share the same source material/texture set while displaying different skin, hair, primary-clothing, and secondary-clothing colors.
- Non-customizable pixels retain their authored colors across those instances.
- Appearance variation does not require persistent duplicated material assets per NPC.
- Material-mask emission, roughness, and metallic response remain coherent with terrain and props.
- A rigid equipped validation item can use `PixelLitProp` while following the animated character through an attachment socket.
- Terrain and prop shaders remain unchanged except for narrowly required compatibility fixes.

## Validation Asset
Use or create a deliberately small representative humanoid rather than a production character. It should include:
- low-poly skinned body
- low-resolution BaseMap
- customization mask
- optional material mask test regions
- interchangeable or separable hair if practical
- minimal idle and walk animation
- one rigid equipped prop, such as a sword or pickaxe, using `PixelLitProp`

Validate at least three simultaneous appearance combinations using the same shared material where practical.

## Out of Scope
- Procedural character generation rules
- Player character-creator UI
- Final humanoid skeleton standard
- Full shared animation library
- Equipment inventory/gameplay
- Equipment attachment system beyond a minimal validation socket
- Hit flash, poison/frozen/status effects, or selection highlighting (RENDER-05)
- Dissolve/death effects
- Generated sprites or sprite-sheet baking
- Billboard characters
- Final texture-resolution/texel-density standardization
- Runtime renderer/mesh combining optimization
- Full-screen pixelation or render-resolution policy

## Manual Validation
1. Place the validation humanoid beside representative terrain and a Pixel-Lit prop.
2. Run idle and walk animation and inspect UV stability, skinning, normals, and lighting from several viewing angles.
3. Move the same dungeon light sources across terrain, props, and the character and compare quantized diffuse, minimum light, color propagation, overbright/hot-wash, emission, and specular response.
4. Spawn/configure at least three character instances sharing the same source material and assign visibly different skin, hair, primary-clothing, and secondary-clothing colors.
5. Confirm unmasked eyes, belts, boots, buckles, or other authored regions remain unchanged.
6. Equip a rigid Pixel-Lit Prop item on a validation socket and confirm it follows animation while retaining prop rendering behavior.
7. Confirm no per-instance persistent material assets were required for appearance variation.

## Post-Implementation Report
Record:
- shader/material/assets added or changed
- final customization-mask contract
- customization blend/recolor method
- renderer-level property mechanism used for per-character appearance
- material-mask compatibility
- skinned-renderer validation results
- validation character and animations used
- rigid equipment validation method
- visual comparison notes against terrain and props
- performance/material-instance implications discovered
- requirements deferred to RENDER-05 and the Character Architecture tickets

## Git
Suggested implementation branch: `render/render04-pixel-lit-character`

Proceed according to `docs/AGENTS.md`.
