using System.Reflection;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace ApproximatelyUpMod
{
    [HarmonyPatch]
    internal static class PlacementCollisionEntityOverloadPatch
    {
        private static MethodBase TargetMethod()
        {
            MethodBase method = AccessTools.Method(
                typeof(GarageGrabber),
                nameof(GarageGrabber.CanMountComponentToGaragePos),
                new[]
                {
                    typeof(EntityManager),
                    typeof(GarageGrabberSingleton).MakeByRefType(),
                    typeof(NativeArray<GarageGrabber.GarageTickPlacedComponent>),
                    typeof(GarageTransform),
                    typeof(Entity)
                });

            ModLog.Info("[BuildingGridMod] Applying patch: PlacementCollisionEntityOverloadPatch -> " + (method == null ? "NOT FOUND" : method.ToString()));
            return method;
        }

        private static bool Prefix(ref bool __result)
        {
            if (!BuildingModConfig.DisablePlacementCollisions)
            {
                return true;
            }

            __result = true;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class PlacementCollisionPatch
    {
        private static bool _loggedExecution;

        private static MethodBase TargetMethod()
        {
            MethodBase method = AccessTools.Method(
                typeof(GarageGrabber),
                nameof(GarageGrabber.CanMountComponentToGaragePos),
                new[]
                {
                    typeof(EntityManager),
                    typeof(GarageGrabberSingleton).MakeByRefType(),
                    typeof(NativeArray<GarageGrabber.GarageTickPlacedComponent>),
                    typeof(GarageTransform),
                    typeof(SCPrefab)
                });

            ModLog.Info("[BuildingGridMod] Applying patch: PlacementCollisionPatch -> " + (method == null ? "NOT FOUND" : method.ToString()));
            return method;
        }

        // Primary bypass is now data-level (_bounds set tiny via BuildingRuntimeOverrides).
        // This Harmony prefix acts as a safety net for managed call paths (e.g. server auth).
        private static bool Prefix(ref bool __result)
        {
            if (!BuildingModConfig.DisablePlacementCollisions)
            {
                return true;
            }

            if (!_loggedExecution)
            {
                _loggedExecution = true;
                ModLog.Info("[BuildingGridMod] Patch executed: GarageGrabber.CanMountComponentToGaragePos (managed path)");
            }

            __result = true;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class BuildingCheckBoxBypassPatch
    {
        private static bool _loggedExecution;

        private static MethodBase TargetMethod()
        {
            MethodBase method = AccessTools.Method(
                typeof(CollisionWorld),
                nameof(CollisionWorld.CheckBox),
                new[]
                {
                    typeof(float3),
                    typeof(quaternion),
                    typeof(float3),
                    typeof(CollisionFilter),
                    typeof(QueryInteraction)
                });

            if (method == null)
            {
                foreach (MethodInfo candidate in AccessTools.GetDeclaredMethods(typeof(CollisionWorld)))
                {
                    if (candidate.Name != nameof(CollisionWorld.CheckBox))
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = candidate.GetParameters();
                    if (parameters.Length == 5
                        && parameters[0].ParameterType == typeof(float3)
                        && parameters[1].ParameterType == typeof(quaternion)
                        && parameters[2].ParameterType == typeof(float3)
                        && parameters[3].ParameterType == typeof(CollisionFilter)
                        && parameters[4].ParameterType == typeof(QueryInteraction))
                    {
                        method = candidate;
                        break;
                    }
                }
            }

            ModLog.Info("[BuildingGridMod] Applying patch: BuildingCheckBoxBypassPatch -> " + (method == null ? "NOT FOUND" : method.ToString()));
            return method;
        }

        // Building placement validation uses BelongsTo/CollidesWith = 64; bypass only this channel.
        private static bool Prefix(CollisionFilter filter, ref bool __result)
        {
            if (!BuildingModConfig.DisablePlacementCollisions)
            {
                return true;
            }

            if (filter.BelongsTo == 64U && filter.CollidesWith == 64U)
            {
                if (!_loggedExecution)
                {
                    _loggedExecution = true;
                    ModLog.Info("[BuildingGridMod] Patch executed: CollisionWorld.CheckBox building channel bypass");
                }

                __result = false;
                return false;
            }

            return true;
        }
    }
}
