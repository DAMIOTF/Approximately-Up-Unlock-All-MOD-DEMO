# Building Grid Mod Fix Report

## What Was Broken

The original implementation was not taking effect in gameplay even though code compiled.

Primary failure modes identified and addressed:

- DOTS building logic (`GarageGrabber`, preview job, placement managed path) is Burst-heavy, so Harmony patches can be effectively bypassed at runtime.
- Grid override depended on a brittle transpiler-only hook (`_snapping` field reads in `GarageGrabber.OnUpdate`) without a direct post-math enforcement point.
- Collision disabling depended on a partial bypass strategy (tick list + `CheckBox`) without a deterministic override in `CanMountComponentToGaragePos`.
- There was no runtime verification that patches were applied and actually executing.

## What Was Rewritten

## 1) Runtime diagnostics and IL inspection

Added:

- `Mods/BuildingGridMod/PatchDiagnostics.cs`

This now:

- logs target method resolution for:
  - `GarageGrabber.OnUpdate`
  - `GarageGrabber.CanMountComponentToGaragePos(...)`
  - `CollisionWorld.CheckBox(float3, quaternion, float3, CollisionFilter, QueryInteraction)`
- logs attached prefixes/postfixes/transpilers
- dumps IL for the same methods (opcode-level dump)

## 2) Burst/DOTS compatibility strategy

Added:

- `Mods/BuildingGridMod/BurstCompatibility.cs`

And invoked during mod init:

- `ModEntry.OnInitializeMelon()` now calls `BurstCompatibility.TryDisableBurstForHarmony()` before `PatchAll()`.

This forces managed execution paths so Harmony interception is reliable for relevant building systems.

## 3) Grid snapping override

Updated:

- `Mods/BuildingGridMod/GridSnappingPatch.cs`

Kept and instrumented:

- transpiler on `GarageGrabber.OnUpdate` replacing `_snapping` reads with `BuildingModConfig.GetEffectiveSnapping(...)`

Added reliable direct override:

- new patch on `GarageGrabber.SetGarageTransform(...)` (`GridSnapMathPatch`)
- re-applies snap math directly to final transform:

`snapped = round((pos - offset) / step) * step + offset`

Where:

- `step` uses current `BuildingModConfig.GridSize` (converted to rotated `float3` step)
- `offset` is based on component bounds and rotation (same structural logic as game snap expression)

This directly targets the snapping math stage instead of only patching field reads.

## 4) Collision disabling logic

Updated:

- `Mods/BuildingGridMod/PlacementCollisionPatch.cs`

### `GarageGrabber.CanMountComponentToGaragePos(...)`

Rewritten prefix behavior:

- if `DisablePlacementCollisions == false`: run original
- if `true`: skip original and return custom result that keeps garage bounds validation only

Bounds-only validation computes min/max using component bounds and garage transform, then checks against `garageGrabber._boundsSize`.

### `CollisionWorld.CheckBox(...)`

Retained and hardened:

- verifies target overload resolution with fallback scan
- bypasses only building channel checks (`BelongsTo=64 && CollidesWith=64`) when collision toggle is enabled

This supports preview/placement code paths that rely on `CheckBox`.

## Methods Now Correctly Intercepted

- `GarageGrabber.OnUpdate(ref SystemState)`
- `GarageGrabber.SetGarageTransform(EntityManager, GarageTransform, Entity, GarageGrabberSingleton, GarageTransform)`
- `GarageGrabber.CanMountComponentToGaragePos(EntityManager, ref GarageGrabberSingleton, NativeArray<GarageTickPlacedComponent>, GarageTransform, SCPrefab)`
- `CollisionWorld.CheckBox(float3, quaternion, float3, CollisionFilter, QueryInteraction)`

## Patch Execution Logging Added

Each major patch now logs:

- when the patch target is applied
- first observed runtime execution

Examples in log output use `[BuildingGridMod]` tags.

## Burst / DOTS Resolution Summary

Because Burst can bypass managed Harmony patch paths, the fix uses two layers:

- disables Burst compilation via runtime reflection (`BurstCompatibility`)
- patches deterministic managed call points (`SetGarageTransform`, `CanMountComponentToGaragePos`, `CheckBox`)

This provides stable behavior across preview and final placement without modifying decompiled game source.

## Build Status

Project compiles successfully in Release:

- output: `bin/Release/ApproximatelyUpMOD.dll`
- remaining warning: non-blocking `System.Net.Http` assembly version conflict (`MSB3277`)
