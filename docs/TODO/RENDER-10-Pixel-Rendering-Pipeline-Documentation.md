# RENDER-10 — Pixel Rendering Pipeline Documentation

## Tracking
- **ID:** RENDER-10
- **Status:** Planned
- **Milestone:** Pixel Rendering / Documentation
- **Depends on:** RENDER-03; RENDER-07

## Goal
Document the finalized pixel-art rendering pipeline so future terrain, prop, trap, character, and FX assets can be authored consistently, and so a contributor can choose the correct rendering path without inspecting shader implementation code.

This ticket is a consolidation/onboarding pass. It must document behavior that has already been implemented and validated rather than establish new rendering standards.

## Primary Deliverable
Create or update the canonical rendering reference at:

`docs/Reference/Pixel_Rendering_Pipeline.md`

The reference should sit above task-specific How-To guides. It should explain which rendering path applies and why, then link to detailed authoring workflows rather than duplicating them wholesale.

## Requirements

### Shader / Asset Selection
- Provide an asset-type/shader-selection matrix covering at minimum:
  - terrain / dungeon tiles;
  - props, socket decorations, traps, and interactables;
  - characters;
  - FX.
- For each category, identify:
  - shader family / material path;
  - texture-addressing model;
  - normal texture organization;
  - relevant masks;
  - important category-specific constraints.
- A contributor must be able to determine the correct shader/material/texture organization without inspecting shader source.

### Shader-Family Responsibilities and Boundaries
- Document the shared Pixel-Lit lighting core and what it owns.
- Document terrain-only responsibilities such as rotation-safe atlas selection, surface-family lookup, ground-depth/height behavior, and terrain-specific UV/addressing logic.
- Document ordinary UV0 responsibilities for props and characters.
- Document why FX may intentionally diverge from opaque terrain/prop lighting behavior where established.
- Make clear that sharing the Pixel-Lit lighting model does not imply sharing the same atlas or UV-addressing system.

### Texture Organization
- Document the terrain atlas versus prop atlas versus character-texture versus FX-texture workflows where implemented.
- Explain why each asset family uses its particular texture organization rather than only listing filenames.
- Document cases where systems intentionally do not share an atlas.
- For props/traps, reference the established RENDER-03 production atlas/standalone-texture rules rather than duplicating the full How-To guide.

### Material-Mask Contract
- Document the shared material-mask channel contract:
  - R = emission;
  - G = roughness;
  - B = metallic;
  - A = reserved.
- Document neutral/default mask values where established.
- Make the distinction between shared channel meanings and shader-family-specific addressing explicit:
  - terrain base/mask use the same final rotation-safe resolved UV;
  - ordinary UV0 assets use matching UV0 for base/mask;
  - character customization masks, where implemented, are separate semantic data and must not be confused with the material mask.

### Rotation-Safe Terrain Constraints
- Document the authoritative rotation-safe terrain addressing path.
- State that base color and all supported material-mask channels must remain registered through every supported tile rotation.
- Document terrain authoring constraints that preserve rotation safety.
- Explicitly list unsupported/deferred rotation-sensitive features rather than implying support, including rotation-safe tangent-space normal maps unless that support is implemented before this ticket executes.

### Blender UV Authoring
- Document Blender UV expectations for ordinary UV0-based props and characters where finalized.
- Reference the detailed prop workflow in `docs/HowTo/Author_A_Pixel_Prop.md` rather than reproducing every authoring step.
- Document meaningful differences between terrain authoring and ordinary Blender-authored mesh UVs.

### Texel Density
- Document the established project visual baseline for texel density.
- Preserve the RENDER-03 prop standard of 96 texels per Unity world unit and its documented 72-120 exception range where applicable.
- Clearly distinguish:
  - project-wide visual baseline;
  - asset-family-specific production standard;
  - intentional exceptions validated for characters or FX.
- Do not invent character/FX exceptions that have not been established by their implementation/validation tickets.

### Unity Texture Import Rules
- Document import requirements by texture role rather than pretending all textures share identical settings.
- Include at minimum where implemented:
  - base-color textures;
  - material masks;
  - character customization masks;
  - FX textures.
- Record Point filtering, mipmap, compression, wrap, color-space, alpha, and platform-downscaling requirements where established.
- Make paired texture registration requirements explicit where base/mask dimensions and coordinates must match.

### Material Naming and Location
- Document established material naming/location conventions for terrain, props, characters, and FX where implemented.
- Reference the RENDER-03 prop conventions, including shared versus justified distinct prop materials.
- Do not create naming conventions for shader families that have not yet established one.

### Material Authoring Guidance
- Document both technical encoding and practical authoring guidance for:
  - emission;
  - roughness;
  - metallic;
  - stylized specular.
- Explain any validated project-specific constraints that affect author expectations, including the current art-directed specular direction if the propagated dungeon-light field still lacks per-source direction at the time this ticket executes.
- Keep gameplay light-generation semantics separate from shader emission; emissive pixels do not automatically create Unity/dungeon light sources unless separately implemented.

### Known Unsupported / Deferred Features
- Include a dedicated section for explicitly unsupported or deferred rendering features that contributors might otherwise assume exist.
- Examples may include, when still accurate:
  - rotation-safe tangent-space normal maps;
  - parallax/height mapping;
  - runtime-generated lights from emissive pixels;
  - unsupported transparent modes or sorting guarantees;
  - other limitations recorded by RENDER-00 through RENDER-07.
- Only document limitations that are supported by implemented tickets/code or confirmed validation results.

### Validation and Cross-Links
- Link the relevant implementation tickets from RENDER-00 through RENDER-07 as appropriate.
- Link the RENDER-07 validation scene and document how it should be used for visual regression/reference checks.
- Link existing task-specific How-To and Reference pages rather than duplicating them.
- Validate all added/updated relative links.
- Repair stale links encountered directly within the rendering documentation touched by this ticket, including links to completed rendering tickets that have moved under `docs/TODO/Complete/`.

## Documentation Structure Guidance
The canonical reference should be organized approximately around:

1. Which shader/rendering path should I use?
2. Shared Pixel-Lit concepts
3. Terrain
4. Props / traps
5. Characters
6. FX
7. Material masks
8. Texel density
9. Texture import rules
10. Material naming/location
11. Material authoring guidance
12. Known unsupported/deferred features
13. Validation scene / regression checks
14. Links to detailed How-To guides and implementation references

This is structural guidance, not a requirement to preserve these exact headings if a clearer organization emerges from the finalized implementation.

## Acceptance Criteria
- `docs/Reference/Pixel_Rendering_Pipeline.md` exists and represents the implemented production pipeline.
- A contributor can determine which shader/material/texture organization to use for a new terrain tile, socket prop/trap, character, or FX asset without inspecting shader code.
- The shader/asset selection matrix clearly distinguishes terrain, prop/trap, character, and FX rendering paths.
- Shader-family responsibilities and boundaries are unambiguous.
- Terrain atlas, prop atlas, character-texture, and FX-texture responsibilities are clearly separated.
- The material-mask channel contract and addressing rules are unambiguous.
- Rotation-safety rules and currently unsupported rotation-sensitive features are explicit.
- Texel-density guidance distinguishes the common visual baseline from validated category-specific exceptions.
- Unity import requirements are organized by texture role and do not imply incorrect sRGB/alpha/wrap behavior across different texture types.
- The documentation distinguishes the material mask from character customization data where applicable.
- Known implementation limitations are explicitly documented rather than silently treated as supported features.
- The documentation reflects implemented/validated behavior rather than speculative features.
- Relevant How-To, Reference, implementation-ticket, completed-ticket, and validation-scene links resolve correctly.

## Execution Gate
Do not complete this ticket while core character, FX, or validation-scene behavior that the reference is expected to describe remains speculative.

At minimum, RENDER-07 must be complete. Because RENDER-07 depends on the representative character and FX rendering paths, those paths should be implemented and validated before this documentation is finalized.

RENDER-05 character runtime material effects do not need to block this ticket unless runtime-effect API documentation is intentionally included in the production rendering reference.

If a required category is not yet implemented, leave this ticket Planned/Blocked rather than documenting the intended design as if it were production behavior.

## Out of Scope
- New shader implementation
- New Blender tooling
- Rendering redesign
- Inventing unresolved character/FX authoring conventions
- Changing shader responsibilities to make the documentation simpler
- Broad documentation reorganization unrelated to the rendering pipeline

## Manual Validation
Follow the documentation as if onboarding a new contributor and verify no undocumented project-specific decisions are required for the normal workflow.

At minimum, walk through the documented decision path for:
1. a new dungeon terrain tile;
2. a shared-atlas prop or trap;
3. a character asset;
4. an FX asset.

For each case, verify that the contributor can identify:
- shader/material path;
- texture organization;
- required masks;
- UV/addressing model;
- texel-density expectation;
- Unity import settings;
- relevant limitations;
- detailed How-To or validation reference.

Also verify every rendering-related relative link added or modified by this ticket resolves to the intended file/section.

## Post-Implementation Report
Record:
- documentation files created/updated;
- shader/asset selection matrix included;
- implementation and How-To cross-links added or repaired;
- stale links corrected;
- finalized shader-family boundaries documented;
- texel-density/import/material conventions captured;
- unsupported/deferred features recorded;
- unresolved decisions intentionally left undocumented;
- onboarding/manual-validation results;
- future tooling opportunities discovered without expanding this ticket's implementation scope.

## Git
Suggested implementation branch: `docs/render10-pixel-rendering-pipeline`

Proceed according to `docs/AGENTS.md`.
