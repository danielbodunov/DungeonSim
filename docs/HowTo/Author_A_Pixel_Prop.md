# Author a Pixel-Lit Prop

## Purpose

Use this workflow for Blender-authored socket props, traps, decorations, and
interactables rendered with `DungeonSim/Pixel Lit Prop`. It supports reusable
material patches and unique prop artwork without using the terrain tile grid.

## Production assets and naming

The shared production set is:

- `Assets/Textures/Props/PropAtlas.png` — 512x512 sRGB base color;
- `Assets/Textures/Props/PropAtlas_Mask.png` — matching 512x512 linear mask;
- `Assets/Materials/PixelLitProp.mat` — standard shared material;
- `Assets/Materials/Props/PLP_<AssetName>.mat` — only when a distinct material
  instance is justified.

Name editable source art `PropAtlas_Source.<ext>`. Exported filenames and
dimensions are the contract. Use `PLP_` for Pixel Lit Prop materials,
`PropAtlas_` for atlas textures, and `<AssetName>_Base` / `<AssetName>_Mask` for
approved standalone pairs. Name the FBX material slot `PixelLitProp` and remap
it to the Unity material instead of keeping an automatically imported material.

## Atlas layout

Start the production atlas at 512x512. Keep it power-of-two and do not resize or
repack existing regions to fill space. When committed art no longer fits, grow
both textures together to 1024x1024 while preserving every existing pixel
coordinate. Going beyond 1024 or creating another shared atlas requires an
explicit art/rendering review.

Coordinates below are inclusive and use Unity/Blender's lower-left origin.
Image editors with a top-left origin must convert Y.

| Zone | Pixel range | Intended content |
| --- | --- | --- |
| Shared | `x 0-255, y 0-511` | Reusable wood, metal, stone, bone, cloth, and trim patches |
| Unique | `x 256-511, y 0-447` | Named prop silhouettes, labels, faces, damage, and asset-specific art |
| Validation/reserve | `x 256-511, y 448-511` | Swatches, pipeline validation, and future allocation |

These are allocation zones, not UV tiles. Regions may be any integer-size
rectangle. Typical patches are 8-64 pixels per side and typical unique islands
are 8-128 pixels per side; larger rectangles are allowed when world-space
coverage warrants them. Record the owner and outer allocation rectangle in the
atlas source before using free space.

### Padding and gutters

Every independent region owns a two-pixel gutter on all four sides. Extrude the
nearest edge base color and corresponding mask values through both pixels. For
alpha-clipped art, extrude RGB under transparent gutter pixels but keep alpha
zero outside the silhouette. UVs use only the inner artwork and sample edge
texels at their centers, never the boundary between texels. Do not allocate the
gutter to another region.

Intentional UV overlap does not need a gutter per island: every reused island
points to the same already-padded inner rectangle.

## Texel density

The baseline is **96 texels per Unity world unit**. Terrain uses 32 texels per
logical surface cell and three logical cells per one-unit dungeon tile, giving
props the same apparent pixel size.

After applying Blender object scale, size visible UV surfaces so one
meter/Unity unit spans 96 atlas pixels. Check them beside a floor and wall. A
72-120 texel/unit range is acceptable for small bevels, hidden faces, or
silhouette-critical detail, but do not change density merely to fill a region.
Hero close-ups or deliberately chunkier art require an art-direction decision.

Overlap/reuse is encouraged for plain wood, metal, stone, bone, cloth, unseen
backs, repeated trap teeth, and mirrored pieces without directional marks.
Separate UVs for text, directional wear, asymmetry, emission, or mask values
that must differ. Shared base texels necessarily share mask texels.

## Blender UV workflow

1. Model at project scale, apply object scale, keep correct normals, and
   triangulate predictably when export triangulation could alter UV interpolation.
2. Use UV0 for both base and mask. Do not use terrain UV2 semantic or
   world-projection conventions.
3. Set the UV editor to the production atlas dimensions and lay out islands at
   96 texels per unit against integer pixel coordinates.
4. Snap axis-aligned bounds and deliberate seams to pixel boundaries. Sample
   texel `(x, y)` in a `W x H` atlas at `((x + 0.5) / W, (y + 0.5) / H)`.
5. Keep islands inside the inner artwork rectangle. Diagonals need not follow
   the grid, but important horizontal/vertical color breaks should align to it.
6. Overlap UVs deliberately for shared patches and name/group those islands so
   reuse cannot be mistaken for an accident.
7. Paint structural/contact AO in vertex-color red (`1` clear, `0` occluded).
   Leave other channels neutral unless another documented workflow owns them.
8. Export FBX with UV0, vertex colors, applied transforms, and no embedded or
   automatically copied textures.

## Base and material-mask artwork

Base and mask must have identical dimensions, allocations, and UV0. The mask
uses the RENDER-00 contract:

- R: emission (`0` none, `1` full);
- G: roughness (`0` smooth, `1` rough);
- B: metallic (`0` dielectric, `1` metal);
- A: reserved; author `1` and assign no new meaning.

Use `(0, 1, 0, 1)` for ordinary non-emissive, rough, nonmetallic pixels. If two
surfaces need different mask values, they cannot overlap even if base colors
match.

## Unity import and material setup

For `PropAtlas.png` use Texture Type Default, sRGB enabled, Point filtering,
mipmaps disabled, Clamp, Compression None, no platform downscaling, Non-Power
of 2 None, and disabled Read/Write and Streaming Mipmaps. Set Max Size at least
to the source dimension. Use Input Texture Alpha and Alpha Is Transparency when
alpha clipping is present.

Use the same settings for `PropAtlas_Mask.png`, except disable sRGB and Alpha Is
Transparency. Never compress or resize one member of the pair independently.

Use `Assets/Materials/PixelLitProp.mat` when the renderer uses the production
pair and standard controls. Multiple props should share it, including props
overlapping atlas texels. Create `Assets/Materials/Props/PLP_<AssetName>.mat`
only for a standalone texture pair, different alpha cutoff/culling, emission
color/intensity, or materially different lighting/specular tuning. A new mesh
or unique atlas rectangle alone does not justify a new material.

Enable Material Mask only when the assigned mask is meaningful. Enable Alpha
Clipping only for binary cutouts; this shader does not blend transparency.

Use standalone `<AssetName>_Base` / `<AssetName>_Mask` textures for animated or
frequently replaced content, unreasonable atlas consumption, incompatible
import/wrap needs, or independent ownership/release. Preserve the 96-texel/unit
baseline and import rules. Do not create them merely to avoid atlas coordination.

## Validation checklist

Validate one shared-patch prop and one unique-art prop using only this page:

1. Map a simple prop to a shared patch, deliberately overlap repeated faces,
   and import it with the shared material.
2. Record a padded Unique-zone rectangle, author matching base/mask pixels, and
   import a second prop.
3. Place both beside representative floor and wall terrain. Confirm pixel size
   matches and no neighboring art bleeds at oblique angles.
4. Confirm Point filtering is crisp, repeated UVs show identical pixels, and
   alpha edges (if used) are clean.
5. Inspect emission, roughness, and metal pixels under the same dungeon light
   as terrain. Confirm registration and neutral-mask behavior.
6. Reimport FBX and textures and confirm remapping and UVs stay stable.

Record prop names, atlas rectangles, screenshots, Unity version, target
platform, and pass/fail results in the ticket before marking validation complete.

## Related documentation

- [Prefab and Asset Conventions](../Reference/Prefab_Conventions.md#pixel-lit-prop-materials)
- [Add a Floor Prop](Add_A_Floor_Prop.md)
- [Add a Trap](Add_A_Trap.md)
- [RENDER-03 ticket](../TODO/RENDER-03-Prop-Material-Atlas-Pipeline.md)
