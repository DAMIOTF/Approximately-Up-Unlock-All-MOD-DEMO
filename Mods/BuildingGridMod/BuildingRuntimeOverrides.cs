using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace ApproximatelyUpMod
{
    internal static class BuildingRuntimeOverrides
    {
        // Track last applied values so we only re-apply on change.
        private static float _lastGrid = -1f;
        private static bool _lastBypass;
        private static bool _initialized;
        private static World _lastWorld;

        /// <summary>Force re-application of all overrides on the next Tick (e.g. after a Reset).</summary>
        public static void ForceReapply() => _initialized = false;

        public static void Tick()
        {
            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null) return;

                // Detect scene reload / world recreation and force re-apply.
                if (!ReferenceEquals(world, _lastWorld))
                {
                    _lastWorld = world;
                    _initialized = false;
                }

                EntityManager em = world.EntityManager;
                float currentGrid = BuildingModConfig.GridSize;
                bool currentBypass = BuildingModConfig.DisablePlacementCollisions;

                // Apply whenever config changes, world changed, or on first run.
                if (!_initialized || _lastGrid != currentGrid || _lastBypass != currentBypass)
                {
                    ApplyPrefabDataOverrides(em, currentGrid, currentBypass);
                    _lastGrid = currentGrid;
                    _lastBypass = currentBypass;
                    _initialized = true;
                }

                // Force visual green for blueprint-placing ghosts (visual only, primary
                // bypass is data-driven via _bounds override above).
                if (currentBypass)
                {
                    ForceCanMountForBlueprintPlacing(em);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn("[BuildingGridMod] Runtime override tick failed: " + ex.Message);
            }
        }

        // Directly mutate _scPrefabDataMap in the Core.Singleton ECS component.
        // GarageGrabber.OnUpdate is [BurstCompile] so Harmony can't intercept it;
        // modifying the native map data is the only reliable way to affect snapping
        // and collision inside the Burst execution path.
        private static unsafe void ApplyPrefabDataOverrides(EntityManager em, float grid, bool bypass)
        {
            Core core = Core.Get();
            if (core == null || core._spaceshipComponents == null) return;

            Core.Singleton coreSingleton;
            if (!Utility.TryGetSingleton<Core.Singleton>(em, out coreSingleton)) return;

            UnsafeHashMap<SCPrefab, SpaceshipComponentPrefabData>* mapPtr = coreSingleton._scPrefabDataMap;
            if (mapPtr == null) return;

            float3 snapping = new float3(grid, grid, grid);
            // With _bounds = 0.02: CheckBox half-extents = 0.5*0.02-0.01 = 0 → no hit → placement allowed.
            float3 tinyBounds = new float3(0.02f, 0.02f, 0.02f);

            int applied = 0;
            for (int i = 0; i < core._spaceshipComponents.Length; i++)
            {
                EPC_SpaceshipComponent epc = core._spaceshipComponents[i];
                if (epc == null) continue;

                Entity prefabEntity = EntityPrefabComponent.Get(epc);
                if (!em.Exists(prefabEntity) || !em.HasComponent<SCPrefab>(prefabEntity)) continue;

                SCPrefab key = em.GetComponentData<SCPrefab>(prefabEntity);
                SpaceshipComponentPrefabData data;
                if (!mapPtr->TryGetValue(key, out data)) continue;

                data._snapping = snapping;
                data._bounds = bypass ? tinyBounds : epc._bounds;

                // UnsafeHashMap has no set-indexer in this Unity.Collections version;
                // remove then re-add is the safe overwrite pattern.
                mapPtr->Remove(key);
                mapPtr->Add(key, data);
                applied++;
            }

            ModLog.Info("[BuildingGridMod] PrefabData overrides: grid=" + grid.ToString("0.###")
                + " bypass=" + bypass + " entries=" + applied);
        }

        private static void ForceCanMountForBlueprintPlacing(EntityManager em)
        {
            EntityQuery q = em.CreateEntityQuery(typeof(GarageBlueprintPlacingEntity));
            if (q.IsEmptyIgnoreFilter)
            {
                q.Dispose();
                return;
            }

            NativeArray<Entity> entities = q.ToEntityArray(Allocator.Temp);
            q.Dispose();

            for (int i = 0; i < entities.Length; i++)
            {
                Entity e = entities[i];
                if (!em.Exists(e))
                {
                    continue;
                }

                GarageBlueprintPlacingEntity data = em.GetComponentData<GarageBlueprintPlacingEntity>(e);
                if (data._currentCanMount != GarageBlueprintPlacingEntity.CanMount.Yes)
                {
                    data._currentCanMount = GarageBlueprintPlacingEntity.CanMount.Yes;
                    em.SetComponentData(e, data);
                }

                if (em.HasComponent<SCGarageMountColor>(e))
                {
                    SCGarageMountColor color = em.GetComponentData<SCGarageMountColor>(e);
                    color.SetBlueprintOK();
                    em.SetComponentData(e, color);
                }
            }

            entities.Dispose();
        }
    }
}
