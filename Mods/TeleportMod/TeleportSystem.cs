using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Entities;

namespace ApproximatelyUpMod
{
    public partial class ItemListController
    {
        private static readonly UniverseLocationID[] _teleportStationIds =
        {
            UniverseLocationID.BlackHole,
            UniverseLocationID.PlanetStation_Earth_Headquarters,
            UniverseLocationID.PlanetStation_Earth_Tutorial,
            UniverseLocationID.PlanetStation_Moon_A,
            UniverseLocationID.PlanetStation_Baobara_A,
            UniverseLocationID.PlanetStation_Helirion_A,
            UniverseLocationID.PlanetStation_Aundara_UndaPrime,
            UniverseLocationID.PlanetStation_Aundara_TealHorizon,
            UniverseLocationID.PlanetStation_Greensphere_A,
            UniverseLocationID.PlanetStation_Titan_A
        };

        private static readonly UniverseLocationID[] _teleportPlanetIds =
        {
            UniverseLocationID.Star_Sun,
            UniverseLocationID.Planet_Earth,
            UniverseLocationID.Planet_Moon,
            UniverseLocationID.Planet_Baobara,
            UniverseLocationID.Planet_Helirion,
            UniverseLocationID.Planet_Aundara,
            UniverseLocationID.Planet_Greensphere,
            UniverseLocationID.Planet_Titan,
            UniverseLocationID.Star_RedDwarf,
            UniverseLocationID.Planet_Rimshell,
            UniverseLocationID.Planet_Outcast,
            UniverseLocationID.Planet_Kovo
        };

        private static readonly Dictionary<UniverseLocationID, UniverseLocationID> _planetToLaunchStation =
            new Dictionary<UniverseLocationID, UniverseLocationID>
            {
                { UniverseLocationID.Planet_Earth, UniverseLocationID.PlanetStation_Earth_Headquarters },
                { UniverseLocationID.Planet_Moon, UniverseLocationID.PlanetStation_Moon_A },
                { UniverseLocationID.Planet_Baobara, UniverseLocationID.PlanetStation_Baobara_A },
                { UniverseLocationID.Planet_Helirion, UniverseLocationID.PlanetStation_Helirion_A },
                { UniverseLocationID.Planet_Aundara, UniverseLocationID.PlanetStation_Aundara_UndaPrime },
                { UniverseLocationID.Planet_Greensphere, UniverseLocationID.PlanetStation_Greensphere_A },
                { UniverseLocationID.Planet_Titan, UniverseLocationID.PlanetStation_Titan_A }
            };

        private static readonly HashSet<UniverseLocationID> _demoUnavailablePlanetTargets =
            new HashSet<UniverseLocationID>
            {
                UniverseLocationID.Planet_Baobara,
                UniverseLocationID.Planet_Helirion,
                UniverseLocationID.Planet_Aundara,
                UniverseLocationID.Planet_Greensphere,
                UniverseLocationID.Planet_Titan,
                UniverseLocationID.Star_RedDwarf,
                UniverseLocationID.Planet_Rimshell,
                UniverseLocationID.Planet_Outcast,
                UniverseLocationID.Planet_Kovo
            };

        private static readonly HashSet<UniverseLocationID> _demoUnavailableStationTargets =
            new HashSet<UniverseLocationID>
            {
                UniverseLocationID.PlanetStation_Baobara_A,
                UniverseLocationID.PlanetStation_Helirion_A,
                UniverseLocationID.PlanetStation_Aundara_UndaPrime,
                UniverseLocationID.PlanetStation_Aundara_TealHorizon,
                UniverseLocationID.PlanetStation_Greensphere_A,
                UniverseLocationID.PlanetStation_Titan_A
            };

        private void TeleportToStation(UniverseLocationID stationId)
        {
            TryTeleport(stationId, preferDirectStation: true);
        }

        private void TeleportToPlanet(UniverseLocationID planetId)
        {
            TryTeleport(planetId, preferDirectStation: false);
        }

        private void TryTeleport(UniverseLocationID targetId, bool preferDirectStation)
        {
            try
            {
                ModLog.Info("Teleport request -> " + targetId);

                if (World.DefaultGameObjectInjectionWorld == null)
                {
                    ModLog.Error("Teleport failed: world is not initialized.");
                    return;
                }

                EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                Netcore.Singleton netcore;
                if (!Utility.TryGetSingleton(entityManager, out netcore))
                {
                    ModLog.Error("Teleport failed: Netcore singleton is unavailable.");
                    return;
                }

                if (!netcore._isServer)
                {
                    ModLog.Warn("Teleport blocked: only host/server can trigger station launch.");
                    return;
                }

                Core core = Core.Get();
                if (core == null)
                {
                    ModLog.Error("Teleport failed: Core is unavailable.");
                    return;
                }

                Entity stationEntity;
                UniverseLocationID launchId;
                string failureReason;
                if (!TryResolveLaunchStation(entityManager, targetId, preferDirectStation, out stationEntity, out launchId, out failureReason))
                {
                    ModLog.Error($"Teleport failed for {targetId}: {failureReason}");
                    ModLog.Warn(BuildTeleportDiagnosticReport(entityManager, targetId));
                    return;
                }

                int requiredWorld = entityManager.HasComponent<WormholeWorldIndex>(stationEntity)
                    ? entityManager.GetComponentData<WormholeWorldIndex>(stationEntity)._world
                    : int.MinValue;

                if (requiredWorld != int.MinValue)
                {
                    ModLog.Info("Teleport resolved world: WormholeWorld=" + requiredWorld);
                }

                ModLog.Info("Launch station resolved: " + launchId);
                core.ServerLaunchAtPlanetStation(stationEntity, launchId);
                ModLog.Info("Teleport successful.");
            }
            catch (Exception ex)
            {
                ModLog.Error($"Teleport failed for {targetId}: {ex}");
            }
        }

        private static string BuildTeleportDiagnosticReport(EntityManager entityManager, UniverseLocationID targetId)
        {
            try
            {
                var sb = new StringBuilder(2048);
                sb.Append("[ApproximatelyUpMod] Teleport diagnostics -> target=").Append(targetId);

                Entity targetEntity = Entity.Null;
                int targetWorld = int.MinValue;
                bool targetHasWorld = false;

                EntityQuery locationQuery = entityManager.CreateEntityQuery(typeof(UniverseLocationData));
                NativeArray<Entity> locationEntities = locationQuery.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < locationEntities.Length; i++)
                {
                    Entity e = locationEntities[i];
                    if (entityManager.GetComponentData<UniverseLocationData>(e)._id == targetId)
                    {
                        targetEntity = e;
                        break;
                    }
                }

                if (targetEntity != Entity.Null)
                {
                    sb.Append(" | targetEntity=YES");
                    if (entityManager.HasComponent<WormholeWorldIndex>(targetEntity))
                    {
                        targetWorld = entityManager.GetComponentData<WormholeWorldIndex>(targetEntity)._world;
                        targetHasWorld = true;
                        sb.Append(" | targetWorld=").Append(targetWorld);
                    }
                    else
                    {
                        sb.Append(" | targetWorld=N/A");
                    }
                }
                else
                {
                    sb.Append(" | targetEntity=NO");
                }

                locationEntities.Dispose();
                locationQuery.Dispose();

                EntityQuery stationQuery = entityManager.CreateEntityQuery(typeof(PlanetStationData), typeof(UniverseLocationData));
                NativeArray<Entity> stationEntities = stationQuery.ToEntityArray(Allocator.Temp);
                sb.Append(" | stationCount=").Append(stationEntities.Length);

                int sameWorldCount = 0;
                if (targetHasWorld)
                {
                    for (int i = 0; i < stationEntities.Length; i++)
                    {
                        Entity s = stationEntities[i];
                        if (entityManager.HasComponent<WormholeWorldIndex>(s) && entityManager.GetComponentData<WormholeWorldIndex>(s)._world == targetWorld)
                        {
                            sameWorldCount++;
                        }
                    }
                    sb.Append(" | stationsInTargetWorld=").Append(sameWorldCount);
                }

                sb.Append(" | stations=[");
                int maxDump = Math.Min(20, stationEntities.Length);
                for (int i = 0; i < maxDump; i++)
                {
                    Entity s = stationEntities[i];
                    UniverseLocationID sid = entityManager.GetComponentData<UniverseLocationData>(s)._id;
                    sb.Append(sid);
                    if (entityManager.HasComponent<WormholeWorldIndex>(s))
                    {
                        sb.Append("@W").Append(entityManager.GetComponentData<WormholeWorldIndex>(s)._world);
                    }

                    if (i < maxDump - 1)
                    {
                        sb.Append(", ");
                    }
                }

                if (stationEntities.Length > maxDump)
                {
                    sb.Append(", ...");
                }

                sb.Append("]");
                stationEntities.Dispose();
                stationQuery.Dispose();

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "[ApproximatelyUpMod] Teleport diagnostics failed: " + ex.Message;
            }
        }

        private static bool TryResolveLaunchStation(EntityManager entityManager, UniverseLocationID targetId, bool preferDirectStation, out Entity stationEntity, out UniverseLocationID launchId, out string failureReason)
        {
            stationEntity = Entity.Null;
            launchId = targetId;
            failureReason = string.Empty;

            UniverseLocationID requestedStationId = targetId;
            if (!preferDirectStation)
            {
                if (!_planetToLaunchStation.TryGetValue(targetId, out requestedStationId))
                {
                    failureReason = $"Planet/star '{targetId}' has no direct station launch mapping in this game build.";
                    return false;
                }
            }

            EntityQuery stationQuery = entityManager.CreateEntityQuery(typeof(PlanetStationData), typeof(UniverseLocationData));
            NativeArray<Entity> stationEntities = stationQuery.ToEntityArray(Allocator.Temp);
            if (stationEntities.Length == 0)
            {
                stationEntities.Dispose();
                stationQuery.Dispose();
                failureReason = "No PlanetStation entities exist in current world state.";
                return false;
            }

            for (int i = 0; i < stationEntities.Length; i++)
            {
                Entity candidate = stationEntities[i];
                UniverseLocationID candidateId = entityManager.GetComponentData<UniverseLocationData>(candidate)._id;
                if (candidateId == requestedStationId)
                {
                    stationEntity = candidate;
                    launchId = candidateId;
                    stationEntities.Dispose();
                    stationQuery.Dispose();
                    return true;
                }
            }

            stationEntities.Dispose();
            stationQuery.Dispose();

            failureReason = preferDirectStation
                ? $"Requested station '{targetId}' is not loaded in current runtime world."
                : $"Target '{targetId}' maps to station '{requestedStationId}', but that station is not loaded in current runtime world.";

            return false;
        }
    }
}
