# Character Visuals Architecture

## Purpose
Dungeon Sim characters use modular rigged 3D assets presented with a pixel-art visual language. The system favors runtime configurability, shared animation, visible equipment, and manageable authoring over hand-drawn or generated sprite-sheet permutations.

This document defines the intended visual architecture. Gameplay identity, jobs, AI, inventory, and procedural-generation rules remain separate systems.

## Direction
Characters remain 3D at authoring and runtime.

The target look comes from:
- simple/stylized low-poly geometry
- low-resolution authored textures
- point-filtered texture sampling
- shared Pixel-Lit dungeon lighting
- deliberately quantized/stylized light response
- restrained material detail
- optional project-wide low-resolution rendering evaluated separately

Generated sprite sheets and billboard characters are not the baseline character pipeline.

## Core Principles

### Appearance is data
Skin color, hair color, clothing colors, selected hair mesh, and similar traits should be character appearance data rather than separate material assets or duplicated character prefabs.

### Animation belongs to skeleton families
Characters with compatible anatomy should share a skeleton contract and animation library. A new hair color, shirt, weapon, or generated NPC must not require animation duplication.

### Modular visuals do not require one mesh
Body, hair, clothing, and armor may be separate renderers when that improves authoring and configurability. Renderer-count optimization should follow profiling rather than forcing all character combinations into monolithic meshes prematurely.

### Rigid equipment remains prop content
Rigid swords, tools, shields, torches, backpacks, and similar items should normally use the Pixel-Lit Prop shader and attach to equipment sockets. Deformable/skinned armor may use the Character shader.

### Rendering and gameplay stay separate
The renderer consumes resolved appearance/equipment data. It should not own procedural-generation probabilities, inventory rules, jobs, combat state, or save-game authority.

## Intended Runtime Shape

```text
Character
├── Identity / gameplay data
├── Appearance data
│   ├── body/species selection
│   ├── skin color
│   ├── hair selection
│   ├── hair color
│   ├── primary clothing color
│   └── secondary clothing color
├── Equipment data
├── Skeleton / Animator
└── Visual root
    ├── Body (skinned, PixelLitCharacter)
    ├── Hair (skinned or rigid, normally PixelLitCharacter)
    ├── Clothing / skinned armor (PixelLitCharacter)
    └── Equipment sockets
        ├── MainHand -> PixelLitProp item
        ├── OffHand -> PixelLitProp item
        ├── Head -> rigid or skinned item
        └── Back -> PixelLitProp item
```

The exact component/class names are intentionally deferred until the implementation tickets establish the minimum useful contracts.

## Texture Contracts

### BaseMap
Low-resolution authored color/value/detail. Non-customizable regions retain their authored colors.

### Customization Mask
Dedicated appearance-selection texture:
- R = skin
- G = hair
- B = primary clothing
- A = secondary clothing

Customizable artwork should be authored so value/detail can remain readable when runtime hue is applied. The exact recolor math is established by RENDER-04 and documented after validation.

### Material Mask
Separate from appearance customization and shared with the Pixel-Lit material model:
- R = emission
- G = roughness
- B = metallic
- A = reserved

Do not repurpose material-mask channels for skin/hair/clothing selection.

## Material Strategy
Character instances should share authored materials wherever possible. Per-character appearance values should use renderer-level overrides such as `MaterialPropertyBlock` or an equivalent validated mechanism rather than persistent material duplication.

A generated NPC should not require `NPC_1234.mat` merely because its hair or shirt color differs.

## Skeleton Families
Long term, animation reuse should be organized around compatible skeleton families rather than individual characters. Candidate families include humanoid, small humanoid, quadruped, and other creature-specific rigs as needed.

A skeleton-family standard should define bone names/hierarchy, attachment sockets, scale/orientation conventions, and animation compatibility. That work is intentionally separate from RENDER-04.

## Equipment Boundary
Rigid equipment is authored as reusable prop content and follows the character through named attachment sockets. This supports visible tools and equipment without baking them into character textures or animation frames.

Skinned clothing/armor can bind to the same skeleton family when deformation is required. Whether individual armor pieces are rigid or skinned is an asset-authoring choice, not an inventory-system distinction.

## Animation Boundary
Animation clips target skeleton families. Appearance modules and equipment should follow the rig rather than own duplicate copies of animation clips.

Initial shared categories may eventually include locomotion, work/tool use, combat, interaction, carrying, hit reactions, and death. The actual library and animator architecture are separate implementation work.

## Procedural and Player Customization
NPC generation and player customization should resolve into the same appearance data contract. A random generator may choose values automatically while a character creator lets the player choose them explicitly; the renderer should not care which system supplied the values.

This prevents separate player and NPC visual pipelines.

## Performance Position
Prefer a clear modular pipeline first. A modest number of extra vertices or renderers is acceptable while the character system is being established. Optimize renderer count, material batching, mesh combining, LODs, or other costs only after representative populations can be profiled.

Shared shaders/materials and avoiding unnecessary material instances are baseline requirements because they preserve later optimization options.

## Related Work
- RENDER-01: shared Pixel-Lit lighting core
- RENDER-02: Pixel-Lit Prop shader for rigid equipment
- RENDER-04: Pixel-Lit modular character shader and appearance-mask foundation
- RENDER-05: runtime character material effects
- CHAR-01: skeleton-family and animation compatibility contract
- CHAR-02: modular character appearance assembly
- CHAR-03: equipment attachment sockets and visible equipment
- CHAR-04: shared animation-library foundation
- CHAR-05: procedural/player appearance data contract
