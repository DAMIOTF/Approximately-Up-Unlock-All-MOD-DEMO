using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ApproximatelyUpMod
{
    internal static class WirelessTransmitterSystem
    {
        internal const int DefaultMaxChannels = 99;
        internal const int MinChannels = 1;
        internal const int MaxChannelsLimit = 9999;

        private static int _desiredMaxChannels = DefaultMaxChannels;
        private static double _nextRescanAt;
        private const double RescanInterval = 0.4;

        internal static int DesiredMaxChannels => _desiredMaxChannels;

        internal static void SetMaxChannels(int value)
        {
            _desiredMaxChannels = Math.Max(MinChannels, Math.Min(MaxChannelsLimit, value));
            _nextRescanAt = 0;
        }

        internal static void Reset()
        {
            SetMaxChannels(DefaultMaxChannels);
        }

        internal static void Tick()
        {
            if (Time.realtimeSinceStartupAsDouble < _nextRescanAt)
            {
                return;
            }

            _nextRescanAt = Time.realtimeSinceStartupAsDouble + RescanInterval;

            if (World.DefaultGameObjectInjectionWorld == null)
            {
                return;
            }

            Apply(World.DefaultGameObjectInjectionWorld.EntityManager);
        }

        private static void Apply(EntityManager em)
        {
            int desired = _desiredMaxChannels;

            EntityQuery query = em.CreateEntityQuery(typeof(SCTypeWirelessTransmitter));
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            query.Dispose();

            // Width of two monospace digits at scale 1 (2 × 0.375 + 1 gap × 0.085).
            // Used to activate the engine's built-in auto-shrink for 3- and 4-digit channels.
            const float TwoDigitMonospaceWidth = 0.835f;

            int changed = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity))
                {
                    continue;
                }

                SCTypeWirelessTransmitter tx = em.GetComponentData<SCTypeWirelessTransmitter>(entity);
                Entity lever = tx._actionableLeverEntity;
                if (lever == Entity.Null || !em.Exists(lever))
                {
                    continue;
                }

                // ── channel count override ───────────────────────────────────
                ActionableData ad = em.GetComponentData<ActionableData>(lever);
                if (ad._roundingIntervals != desired)
                {
                    int currentChannel = ad.GetValueInterval().x;
                    ad._roundingIntervals = desired;
                    int clampedChannel = Math.Max(0, Math.Min(desired - 1, currentChannel));
                    ad._value.x = (clampedChannel + 0.5f) / desired;
                    em.SetComponentData(lever, ad);
                    changed++;
                }

                // ── auto-shrink text for 3- and 4-digit channel numbers ──────
                Entity textEntity = tx._text3DRendererEntity;
                if (textEntity != Entity.Null && em.Exists(textEntity) && em.HasComponent<CRPText3D>(textEntity))
                {
                    CRPText3D t3d = em.GetComponentData<CRPText3D>(textEntity);
                    // Only activate when the prefab left _maxWidth at 0 (no existing limit).
                    if (t3d._maxWidth < 0.01f)
                    {
                        t3d._maxWidth = TwoDigitMonospaceWidth * t3d._baseScale;
                        em.SetComponentData(textEntity, t3d);
                    }
                }
            }

            entities.Dispose();

            if (changed > 0)
            {
                ModLog.Info($"Wireless transmitter max channels -> {desired} ({changed} units updated).");
            }
        }
    }
}
