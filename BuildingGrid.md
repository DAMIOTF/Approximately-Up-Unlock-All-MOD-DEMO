# Building System Analysis

Scope: static analysis of the decompiled Assembly-CSharp project and current mod workspace. No source code was modified.

## Grid Snapping

### Finding 1: Main component placement snapping (primary target)

### Class

`GarageGrabber`

### Method

Placement flow inside update logic (mounting state), calling `CanMountComponentToGaragePos(...)`

### File

`ZDEKOMPILOWANE Assembly-CSharp/GarageGrabber.cs:1660`

### Relevant Code

```csharp
rhs3 = math.abs(math.rotate(quaternion, spaceshipComponentPrefabData._snapping));
rhs4 = math.abs(math.rotate(quaternion, 0.5f * spaceshipComponentPrefabData._bounds));

// snap to grid
float17 = math.round((float17 - rhs4) / rhs3) * rhs3 + rhs4;

GarageTransform garageTransform4 = new GarageTransform(float17, quaternion);
if (GarageGrabber.CanMountComponentToGaragePos(state.EntityManager, ref valueRW, tickPlacedComponents, garageTransform4, this._handPrefab))
{
	this._isHandComponentReadyToMount = true;
}
```

### Description

This is the core position snap formula for placing spaceship building elements in garage/build mode.

- `rhs3` is the effective snap step per axis, derived from prefab `_snapping` and current rotation.
- `rhs4` is a pivot/offset term derived from half bounds.
- Position is snapped by a round-to-nearest operation in snapped local garage coordinates.

This is the strongest hook point for adjustable grid size.

### Potential Hook Point

- Replace/scale `spaceshipComponentPrefabData._snapping` before the `math.round` expression.
- Or replace `rhs3` with custom UI-driven value(s): `1.0`, `0.5`, `0.25`, `0.1`.
- Important: because `_snapping` is a `float3`, custom scalar value likely needs to be expanded to `float3(custom, custom, custom)` unless axis-specific behavior is desired.

---

### Finding 2: Default snap value source

### Class

`EPC_SpaceshipComponent`

### Method

Prefab data creation `GetPrefabData()`

### File

`ZDEKOMPILOWANE Assembly-CSharp/EPC_SpaceshipComponent.cs:31`
`ZDEKOMPILOWANE Assembly-CSharp/EPC_SpaceshipComponent.cs:358`

### Relevant Code

```csharp
public SpaceshipComponentPrefabData GetPrefabData()
{
	return new SpaceshipComponentPrefabData
	{
		_bounds = this._bounds,
		_snapping = this._snapping,
		...
	};
}

public float3 _snapping = 0.25f;
```

### Description

Each component prefab carries its own snap vector. Default serialized value is `0.25f`.

### Potential Hook Point

- Runtime override of `SpaceshipComponentPrefabData._snapping` or its consumption site in `GarageGrabber`.
- Changing this globally affects all components that rely on this prefab data.

---

### Finding 3: Prefab snap field definition

### Class

`SpaceshipComponentPrefabData`

### File

`ZDEKOMPILOWANE Assembly-CSharp/SpaceshipComponentPrefabData.cs:11`

### Relevant Code

```csharp
public float3 _snapping;
```

### Description

Confirms snap step is part of prefab runtime data, not a hardcoded local variable in placement logic.

### Potential Hook Point

- Data-level interception: mutate this field at load/init time.

---

### Finding 4: Rotation snapping (related but separate from positional grid)

### Class

`GarageTransform`

### Method

`GetSnapRotation(quaternion rot)`

### File

`ZDEKOMPILOWANE Assembly-CSharp/GarageTransform.cs:59`

### Relevant Code

```csharp
public static quaternion GetSnapRotation(quaternion rot)
{
	quaternion quaternion = GarageTransform.ROTATIONS[0];
	float num = math.abs(math.dot(rot, quaternion));
	for (int i = 1; i < 24; i++)
	{
		float num2 = math.abs(math.dot(rot, GarageTransform.ROTATIONS[i]));
		if (num2 > num)
		{
			quaternion = GarageTransform.ROTATIONS[i];
			num = num2;
		}
	}
	return quaternion;
}
```

### Description

Rotation is snapped to one of 24 predefined orientations. This is not position grid size, but it is part of overall placement snapping behavior.

### Potential Hook Point

- If future mod needs free/custom rotation granularity, this method is the main rotational snapping gate.

---

### Finding 5: Additional fixed quantization (`0.125f`) in selection/serialization

### Class

`GarageGrabber`

### File

`ZDEKOMPILOWANE Assembly-CSharp/GarageGrabber.cs:1751`

### Relevant Code

```csharp
float24 = math.floor(float24 / 0.125f) * 0.125f + 0.0625f;
```

### Description

Selection box points in garage selection flow are quantized to 1/8 grid increments (0.125).

### Potential Hook Point

- If custom grid size is expected to affect selection tools, this hardcoded constant must be included.

### Class

`GarageTransform`

### Method

`Encode(...)` / `Decode(...)`

### File

`ZDEKOMPILOWANE Assembly-CSharp/GarageTransform.cs:82`
`ZDEKOMPILOWANE Assembly-CSharp/GarageTransform.cs:100`

### Relevant Code

```csharp
uint3 u = (uint3)((this._transform.pos - rhs) * 8f + 0.5f);
...
float3 translation = new float3(data & 511U, data >> 9 & 511U, data >> 18 & 511U) * 0.125f + rhs;
```

### Description

Network/compact transform encoding also assumes 0.125 positional resolution.

### Potential Hook Point

- Any large grid-size redesign may need compatibility review with this encode/decode quantization.

---

## Placement Collision

### Finding 1: Primary placement validator

### Class

`GarageGrabber`

### Method

`CanMountComponentToGaragePos(...)`

### File

`ZDEKOMPILOWANE Assembly-CSharp/GarageGrabber.cs:595`

### Relevant Code

```csharp
float3 rhs = math.abs(math.rotate(garageTransform.Rotation(), 0.5f * spaceshipComponentPrefabData._bounds - 0.01f));
float3 min = garageTransform.Position() - rhs;
float3 max = garageTransform.Position() + rhs;

// bounds check against garage volume
if (min.x < 0f || min.y < 0f || min.z < 0f ||
	max.x > garageGrabber._boundsSize.x ||
	max.y > garageGrabber._boundsSize.y ||
	max.z > garageGrabber._boundsSize.z)
{
	return false;
}

CollisionFilter filter = new CollisionFilter { BelongsTo = 64U, CollidesWith = 64U };
if (collisionWorld.CheckBox(..., orientation, 0.5f * spaceshipComponentPrefabData._bounds - 0.01f, filter, QueryInteraction.Default))
{
	return false;
}

for (int i = 0; i < tickPlacedComponents.Length; i++)
{
	if (math.all(max >= tickPlacedComponents[i]._min) && math.all(tickPlacedComponents[i]._max >= min))
	{
		return false;
	}
}
return true;
```

### Description

This method enforces all major "can place" checks:

- garage bounds limits (`_boundsSize`)
- physics world overlap via `CollisionWorld.CheckBox`
- same-tick overlap against staged placements (`tickPlacedComponents` AABB intersection)

### Potential Hook Point

- "Disable Placing Collisions" can be implemented by bypassing only collision/overlap checks while optionally preserving bounds limits.
- Specific bypass points:
  - `collisionWorld.CheckBox(...)` gate
  - `tickPlacedComponents` overlap loop

---

### Finding 2: Blueprint placing preview collision status

### Class

`GarageGrabber.UpdateGarageBlueprintPlacingEntitiesJob`

### Method

`Execute(...)`

### File

`ZDEKOMPILOWANE Assembly-CSharp/GarageGrabber.cs:5158`

### Relevant Code

```csharp
flag = !this._collisionWorld.CheckBox(this._universeCore.UprToCw(universePosition), orientation,
	0.5f * spaceshipComponentPrefabData._bounds - 0.01f, filter, QueryInteraction.Default);

valueRW3._currentCanMount = (flag ? GarageBlueprintPlacingEntity.CanMount.Yes : GarageBlueprintPlacingEntity.CanMount.No);
```

### Description

This updates the visual/material validity state for blueprint placement entities (`CanMount.Yes/No`), driven by overlap test.

### Potential Hook Point

- For disable-collision mode, forcing `flag = true` here prevents preview from showing invalid due to overlap.

---

### Finding 3: Managed Burst path for actual blueprint placement

### Class

`GarageGrabberManaged.Bursted`

### Method

`PlaceBlueprintPlacingEntities$BurstManaged(...)`

### File

`ZDEKOMPILOWANE Assembly-CSharp/GarageGrabberManaged.cs:617`

### Relevant Code

```csharp
flag = !collisionWorldPtr->CheckBox(universeCorePtr->UprToCw(pos), quaternion,
	0.5f * spaceshipComponentPrefabData._bounds - 0.01f, filter, QueryInteraction.Default);

if (flag)
{
	// place entity
}
else
{
	emPtr->DestroyEntity(entity);
}
```

### Description

Even in managed/bursted placement path, overlap collision controls whether entity is placed or destroyed.

### Potential Hook Point

- Disable-collision option likely needs to affect this path too, otherwise some placement workflows may still reject colliding entities.

---

### Finding 4: Multiplayer/server authority also uses the same validator

### Class

`NetcoreDoIncomingEventsSystem`

### Method

Handling `NetcoreDynamicEventID.RequestGrgAwakeAuthorization`

### File

`ZDEKOMPILOWANE Assembly-CSharp/NetcoreDoIncomingEventsSystem.cs:1214`

### Relevant Code

```csharp
if (GarageGrabber.CanMountComponentToGaragePos(ptr.EntityManager, ref ptr5,
	tickPlacedComponents,
	netcoreDynamicEvent_RequestGrgAwakeAuthorization._garageTransform,
	netcoreDynamicEvent_RequestGrgAwakeAuthorization._scPrefab))
{
	// authorize and create
}
```

### Description

Server-side network authorization reuses `CanMountComponentToGaragePos`. This means collision bypass done only on client may still fail in multiplayer.

### Potential Hook Point

- Disable-collision option should be mirrored in the server-authoritative validation path to avoid desync/rejection.

---

## Practical Map for Future Mod Agent

### Most important grid-snap hook points

1. `ZDEKOMPILOWANE Assembly-CSharp/GarageGrabber.cs:1660` (`math.round((float17 - rhs4) / rhs3) * rhs3 + rhs4`)
2. `ZDEKOMPILOWANE Assembly-CSharp/EPC_SpaceshipComponent.cs:358` (`_snapping = 0.25f` default source)
3. `ZDEKOMPILOWANE Assembly-CSharp/GarageGrabber.cs:1751` (hardcoded `0.125f` selection quantization)
4. `ZDEKOMPILOWANE Assembly-CSharp/GarageTransform.cs:82` and `ZDEKOMPILOWANE Assembly-CSharp/GarageTransform.cs:100` (0.125 encode/decode quantization)

### Most important collision-bypass hook points

1. `ZDEKOMPILOWANE Assembly-CSharp/GarageGrabber.cs:595` (`CanMountComponentToGaragePos`)
2. `ZDEKOMPILOWANE Assembly-CSharp/GarageGrabber.cs:631` (`collisionWorld.CheckBox(...)` rejection)
3. `ZDEKOMPILOWANE Assembly-CSharp/GarageGrabber.cs:5158` (blueprint preview validity check)
4. `ZDEKOMPILOWANE Assembly-CSharp/GarageGrabberManaged.cs:648` (bursted managed placement `CheckBox`)
5. `ZDEKOMPILOWANE Assembly-CSharp/NetcoreDoIncomingEventsSystem.cs:1214` (multiplayer authorization validator)

## Notes

- The decompiled assembly shows building placement heavily centered around garage/ship construction systems (`GarageGrabber*`).
- The requested example behavior (occupied tile rejecting second frame piece) is consistent with `CheckBox` + AABB overlap gates found above.
- No implementation changes were made in this task.
