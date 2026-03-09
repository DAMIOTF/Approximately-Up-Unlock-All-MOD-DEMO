using HarmonyLib;
using MelonLoader;
using System;
using UnityEngine;

[assembly: MelonInfo(typeof(ApproximatelyUpMod.ModEntry), "ApproximatelyUpMod", "2.0.0", "discord: dmtftf")]
[assembly: MelonGame(null, null)]

namespace ApproximatelyUpMod
{
    internal static class ModLog
    {
        internal const string Prefix = "[ApproximatelyUpMod]";

        internal static void Info(string message)
        {
            MelonLogger.Msg($"{Prefix} {message}");
        }

        internal static void Warn(string message)
        {
            MelonLogger.Warning($"{Prefix} {message}");
        }

        internal static void Error(string message)
        {
            MelonLogger.Error($"{Prefix} {message}");
        }
    }

    public class ModEntry : MelonMod
    {
        private const string ControllerObjectName = "Mod_ItemList_GUI";

        public override void OnInitializeMelon()
        {
            try
            {
                BurstCompatibility.TryDisableBurstForHarmony();

                var harmony = new HarmonyLib.Harmony("com.ApproximatelyUp.Mod");
                harmony.PatchAll();
                ModLog.Info("Harmony patches initialized.");
                PatchDiagnostics.LogPatchTargetsAndIl(harmony);

                MelonEvents.OnSceneWasLoaded.Subscribe(OnSceneLoaded);
                ModLog.Info("Subscribed to scene load events.");
            }
            catch (Exception ex)
            {
                ModLog.Error("Critical initialization error: " + ex);
            }
        }

        private void OnSceneLoaded(int buildIndex, string sceneName)
        {
            try
            {
                ModLog.Info($"Scene loaded: {sceneName} (build {buildIndex}).");

                var existingController = GameObject.Find(ControllerObjectName);
                if (existingController != null)
                {
                    var controller = existingController.GetComponent<ItemListController>();
                    controller?.NotifySceneLoaded(sceneName);
                    return;
                }

                var go = new GameObject(ControllerObjectName);
                UnityEngine.Object.DontDestroyOnLoad(go);
                var injectedController = go.AddComponent<ItemListController>();
                injectedController.NotifySceneLoaded(sceneName);
                ModLog.Info("Controller injected and marked as DontDestroyOnLoad.");
            }
            catch (Exception ex)
            {
                ModLog.Error("OnSceneLoaded failed: " + ex);
            }
        }
    }
}