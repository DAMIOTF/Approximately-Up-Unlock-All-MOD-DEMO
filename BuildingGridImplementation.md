# Building Grid Mod Implementation

## Summary

Implemented runtime-modifiable building behavior in separate mod files under `Mods/BuildingGridMod/` using Harmony patches and existing UniverseLib panel UI.

Features added:

- Adjustable grid size: `1.0`, `0.5`, `0.25`, `0.1`
- Toggle: `Disable Placing Collisions`

## Added Files

- `Mods/BuildingGridMod/BuildingModConfig.cs`
- `Mods/BuildingGridMod/GridSnappingPatch.cs`
- `Mods/BuildingGridMod/PlacementCollisionPatch.cs`
- `Mods/BuildingGridMod/ModUI.cs`

## Patched Methods

## 1) Grid snapping patch

### Target

`GarageGrabber.OnUpdate(ref SystemState state)`

### File

`Mods/BuildingGridMod/GridSnappingPatch.cs`

### Behavior

A Harmony transpiler replaces reads of `SpaceshipComponentPrefabData._snapping` with:

`BuildingModConfig.GetEffectiveSnapping(original)`

So placement snapping uses the selected UI grid value by returning a uniform `float3(grid, grid, grid)`.

## 2) Primary placement validation patch

### Target

`GarageGrabber.CanMountComponentToGaragePos(EntityManager, ref GarageGrabberSingleton, NativeArray<GarageGrabber.GarageTickPlacedComponent>, GarageTransform, SCPrefab)`

### File

`Mods/BuildingGridMod/PlacementCollisionPatch.cs`

### Behavior

When `DisablePlacementCollisions` is enabled, the prefix clears `tickPlacedComponents` to bypass same-tick overlap rejection.

Garage bounds checks in original method are preserved.

## 3) Collision query bypass patch

### Target

`CollisionWorld.CheckBox(float3, quaternion, float3, CollisionFilter, QueryInteraction)`

### File

`Mods/BuildingGridMod/PlacementCollisionPatch.cs`

### Behavior

When `DisablePlacementCollisions` is enabled and filter is the building channel (`BelongsTo=64`, `CollidesWith=64`), the prefix returns `false` and skips original collision test.

This affects:

- `GarageGrabber.CanMountComponentToGaragePos(...)`
- `GarageGrabber.UpdateGarageBlueprintPlacingEntitiesJob.Execute(...)`
- `GarageGrabberManaged.Bursted.PlaceBlueprintPlacingEntities$BurstManaged(...)`

So preview and burst placement paths no longer fail for overlaps while bounds checks remain active.

## UI Integration

### Added controls in existing mod panel

Implemented in:

- `Mods/BuildingGridMod/ModUI.cs`
- with small wiring changes in `ItemListController.UI.cs`

Controls:

- `Grid Size` with `-` / `+` cycling through `1.0`, `0.5`, `0.25`, `0.1`
- `[ ] / [x] Disable Placing Collisions`

Both update `BuildingModConfig` at runtime.

## Config Source of Truth

`Mods/BuildingGridMod/BuildingModConfig.cs`

- `GridSize`
- `DisablePlacementCollisions`

## Notes

- No decompiled game source files were modified.
- All behavior changes are done via runtime patching.
- Harmony is initialized via existing `ModEntry` (`PatchAll`).
