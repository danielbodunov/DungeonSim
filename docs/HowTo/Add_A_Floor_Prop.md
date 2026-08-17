# Add a Floor Prop

## Goal

Add ordinary gameplay content that occupies a built dungeon cell without introducing new topology semantics.

## Preferred path

1. Create/configure the prefab.
2. Add a `FloorProp` component, or subclass it if placement compatibility/state is specialized.
3. Add any child `DungeonPointOfInterest` components needed for NPC interaction.
4. Add the prefab to the build database as `ObjectPlacementType.FloorProp`.
5. Test placement, local tile re-resolution, NPC interaction, and save/load.

## Compatibility

The base `FloorProp` accepts any built cell. Override both compatibility forms when adding restrictions:

- live-grid compatibility;
- `TileGridGenerator.PlacementValidationContext` compatibility.

Keeping both implementations consistent allows previews/transactions to reject invalid placement before mutating the dungeon.

## POIs

`FloorProp.Initialize` binds child `DungeonPointOfInterest` components to the owning grid/cell. This is the preferred route for NPC-visible interaction points on a floor prop.

## Persistence

If the floor prop has a stable resolved state that must survive load, use the existing resolved-state hooks rather than making `GameSaveManager` understand the prop's private fields directly.

## When not to use FloorProp

Use another model when the object:

- changes passage topology;
- requires a stable authored wall/edge socket;
- connects two traversal locations;
- occupies multiple cells as one logical structure;
- is purely generated/decorative and should be reconstructed by `PropGenerator`.

## Verification

- valid/invalid placement;
- collision with other reserved content;
- NPC POI interaction;
- local tile replacement/re-resolution;
- save/load;
- removal and replacement.

## Read next

- [`../Architecture/Props_and_Traps.md`](../Architecture/Props_and_Traps.md)
- [`../Architecture/Save_System.md`](../Architecture/Save_System.md)
