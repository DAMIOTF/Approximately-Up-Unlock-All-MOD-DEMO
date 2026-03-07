using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ApproximatelyUpMod
{
    internal static class ThrusterPowerSystem
    {
        internal struct UiEntry
        {
            internal ulong PrefabHash;
            internal string Name;
            internal float Multiplier;
        }

        private sealed class ThrusterTypeState
        {
            internal string Name;
            internal float Multiplier = 1f;
        }

        private const float MinMultiplier = 1f;
        private const float MaxMultiplier = 10f;
        private const double RescanIntervalSeconds = 0.4;

        private static readonly Dictionary<ulong, ThrusterTypeState> _types = new Dictionary<ulong, ThrusterTypeState>(64);
        private static readonly Dictionary<Entity, float3> _baseForceByEntity = new Dictionary<Entity, float3>(256);
        private static readonly Dictionary<Entity, ulong> _entityToType = new Dictionary<Entity, ulong>(256);
        private static readonly HashSet<Entity> _scanSeenEntities = new HashSet<Entity>();
        private static readonly List<Entity> _staleEntities = new List<Entity>(128);

        private static int _revision;
        private static double _nextRescanAt;

        internal static int Revision => _revision;

        internal static void Tick()
        {
            if (Time.realtimeSinceStartupAsDouble < _nextRescanAt)
            {
                return;
            }

            _nextRescanAt = Time.realtimeSinceStartupAsDouble + RescanIntervalSeconds;
            EnsureCatalogFromCore();
            RescanAndApply();
        }

        internal static List<UiEntry> GetUiEntries()
        {
            var list = new List<UiEntry>(_types.Count);
            foreach (var kv in _types)
            {
                list.Add(new UiEntry
                {
                    PrefabHash = kv.Key,
                    Name = kv.Value.Name,
                    Multiplier = kv.Value.Multiplier
                });
            }

            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return list;
        }

        internal static void SetMultiplier(ulong prefabHash, float multiplier)
        {
            ThrusterTypeState typeState;
            if (!_types.TryGetValue(prefabHash, out typeState))
            {
                typeState = new ThrusterTypeState
                {
                    Name = "Thruster " + prefabHash.ToString("X")
                };
                _types[prefabHash] = typeState;
                _revision++;
            }

            float clamped = Mathf.Clamp(multiplier, MinMultiplier, MaxMultiplier);
            if (Mathf.Approximately(typeState.Multiplier, clamped))
            {
                return;
            }

            typeState.Multiplier = clamped;
            ApplyKnownThrusters(prefabHash, clamped);
            ModLog.Info($"Thruster multiplier changed: {typeState.Name} -> x{clamped:0}");
        }

        private static void EnsureCatalogFromCore()
        {
            try
            {
                Core core = Core.Get();
                if (core == null || core._componentsMap == null || core._componentsMap.Count == 0)
                {
                    return;
                }

                bool discoveredNewType = false;
                foreach (var kv in core._componentsMap)
                {
                    EPC_SpaceshipComponent component = kv.Value;
                    if (!(component is EPC_SCThruster))
                    {
                        continue;
                    }

                    ulong prefabHash = kv.Key._prefab;
                    if (_types.ContainsKey(prefabHash))
                    {
                        continue;
                    }

                    _types[prefabHash] = new ThrusterTypeState
                    {
                        Name = ResolveThrusterName(component, prefabHash)
                    };

                    discoveredNewType = true;
                }

                if (discoveredNewType)
                {
                    _revision++;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn("Thruster catalog scan failed: " + ex.Message);
            }
        }

        private static void ApplyKnownThrusters(ulong prefabHash, float multiplier)
        {
            if (World.DefaultGameObjectInjectionWorld == null)
            {
                return;
            }

            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            foreach (var kv in _entityToType)
            {
                if (kv.Value != prefabHash)
                {
                    continue;
                }

                Entity entity = kv.Key;
                if (!em.Exists(entity) || !em.HasComponent<SCTypeThruster>(entity))
                {
                    continue;
                }

                float3 baseForce;
                if (!_baseForceByEntity.TryGetValue(entity, out baseForce))
                {
                    continue;
                }

                SCTypeThruster thruster = em.GetComponentData<SCTypeThruster>(entity);
                float3 targetForce = baseForce * multiplier;
                if (!ApproximatelyEqual(thruster._maxForce, targetForce))
                {
                    thruster._maxForce = targetForce;
                    em.SetComponentData(entity, thruster);
                }
            }
        }

        private static void RescanAndApply()
        {
            try
            {
                if (World.DefaultGameObjectInjectionWorld == null)
                {
                    return;
                }

                EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
                EntityQuery query = em.CreateEntityQuery(typeof(SCTypeThruster), typeof(SCPrefab), typeof(SCManagedPrefabReference));
                NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

                _scanSeenEntities.Clear();
                bool discoveredNewType = false;

                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    _scanSeenEntities.Add(entity);

                    SCPrefab scPrefab = em.GetComponentData<SCPrefab>(entity);
                    ulong prefabHash = scPrefab._prefab;

                    ThrusterTypeState typeState;
                    if (!_types.TryGetValue(prefabHash, out typeState))
                    {
                        typeState = new ThrusterTypeState
                        {
                            Name = ResolveThrusterName(em, entity, prefabHash)
                        };
                        _types[prefabHash] = typeState;
                        discoveredNewType = true;
                        ModLog.Info("Discovered thruster type: " + typeState.Name);
                    }

                    SCTypeThruster thruster = em.GetComponentData<SCTypeThruster>(entity);

                    float3 baseForce;
                    if (!_baseForceByEntity.TryGetValue(entity, out baseForce))
                    {
                        baseForce = thruster._maxForce;
                        _baseForceByEntity[entity] = baseForce;
                    }

                    _entityToType[entity] = prefabHash;

                    float3 targetForce = baseForce * typeState.Multiplier;
                    if (!ApproximatelyEqual(thruster._maxForce, targetForce))
                    {
                        thruster._maxForce = targetForce;
                        em.SetComponentData(entity, thruster);
                    }
                }

                entities.Dispose();
                query.Dispose();

                RemoveStaleEntities();

                if (discoveredNewType)
                {
                    _revision++;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn("Thruster power rescan failed: " + ex.Message);
            }
        }

        private static void RemoveStaleEntities()
        {
            _staleEntities.Clear();

            foreach (var kv in _entityToType)
            {
                if (!_scanSeenEntities.Contains(kv.Key))
                {
                    _staleEntities.Add(kv.Key);
                }
            }

            for (int i = 0; i < _staleEntities.Count; i++)
            {
                Entity entity = _staleEntities[i];
                _entityToType.Remove(entity);
                _baseForceByEntity.Remove(entity);
            }
        }

        private static string ResolveThrusterName(EntityManager em, Entity entity, ulong prefabHash)
        {
            if (!em.HasComponent<SCManagedPrefabReference>(entity))
            {
                return "Thruster " + prefabHash.ToString("X");
            }

            SCManagedPrefabReference managedPrefab = em.GetComponentData<SCManagedPrefabReference>(entity);
            EPC_SpaceshipComponent component = managedPrefab._prefab.Managed<EPC_SpaceshipComponent>();
            if (component == null)
            {
                return "Thruster " + prefabHash.ToString("X");
            }

            string name;
            try
            {
                name = component.GetName();
            }
            catch
            {
                name = component.name;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = component.name;
            }

            return string.IsNullOrWhiteSpace(name) ? ("Thruster " + prefabHash.ToString("X")) : name;
        }

        private static string ResolveThrusterName(EPC_SpaceshipComponent component, ulong prefabHash)
        {
            if (component == null)
            {
                return "Thruster " + prefabHash.ToString("X");
            }

            string name;
            try
            {
                name = component.GetName();
            }
            catch
            {
                name = component.name;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = component.name;
            }

            return string.IsNullOrWhiteSpace(name) ? ("Thruster " + prefabHash.ToString("X")) : name;
        }

        private static bool ApproximatelyEqual(float3 left, float3 right)
        {
            return Mathf.Abs(left.x - right.x) <= 0.01f
                && Mathf.Abs(left.y - right.y) <= 0.01f
                && Mathf.Abs(left.z - right.z) <= 0.01f;
        }
    }
}
