using System;
using Unity.Entities;

namespace ApproximatelyUpMod
{
    public partial class ItemListController
    {
        public static bool DisableShipTearingBySpeed;
        public static bool EnablePlayerGodmode;

        private bool _lastAppliedDestructibleJoints = true;
        private bool _hasAppliedDestructibleJoints;
        private bool _lastAppliedGodmodePlayers;

        internal void SyncShipTearingOverride()
        {
            try
            {
                if (World.DefaultGameObjectInjectionWorld == null)
                {
                    return;
                }

                EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
                EntityQuery query = em.CreateEntityQuery(typeof(GameOverrides));
                if (query.IsEmptyIgnoreFilter)
                {
                    query.Dispose();
                    return;
                }

                GameOverrides gameOverrides = query.GetSingleton<GameOverrides>();
                query.Dispose();

                bool desiredDestructibleJoints = !DisableShipTearingBySpeed;
                bool desiredGodmodePlayers = EnablePlayerGodmode;
                bool shouldWrite = !_hasAppliedDestructibleJoints
                    || gameOverrides._destructibleJoints != desiredDestructibleJoints
                    || gameOverrides._godmodePlayers != desiredGodmodePlayers;
                if (!shouldWrite)
                {
                    return;
                }

                gameOverrides._destructibleJoints = desiredDestructibleJoints;
                gameOverrides._godmodePlayers = desiredGodmodePlayers;

                EntityQuery writeQuery = em.CreateEntityQuery(typeof(GameOverrides));
                writeQuery.SetSingleton(gameOverrides);
                writeQuery.Dispose();

                _hasAppliedDestructibleJoints = true;
                if (_lastAppliedDestructibleJoints != desiredDestructibleJoints)
                {
                    _lastAppliedDestructibleJoints = desiredDestructibleJoints;
                    ModLog.Info("Ship speed tearing: " + (DisableShipTearingBySpeed ? "DISABLED" : "ENABLED"));
                }

                if (_lastAppliedGodmodePlayers != desiredGodmodePlayers)
                {
                    _lastAppliedGodmodePlayers = desiredGodmodePlayers;
                    ModLog.Info("Player godmode: " + (EnablePlayerGodmode ? "ENABLED" : "DISABLED"));
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn("Sync ship tearing override failed: " + ex.Message);
            }
        }
    }
}
