using System;
using System.Reflection;

namespace ApproximatelyUpMod
{
    internal static class BurstCompatibility
    {
        public static void TryDisableBurstForHarmony()
        {
            try
            {
                Type burstCompilerType = Type.GetType("Unity.Burst.BurstCompiler, Unity.Burst");
                if (burstCompilerType == null)
                {
                    ModLog.Warn("[BuildingGridMod] BurstCompiler type not found. Burst may remain active.");
                    return;
                }

                PropertyInfo optionsProperty = burstCompilerType.GetProperty("Options", BindingFlags.Public | BindingFlags.Static);
                object options = optionsProperty?.GetValue(null, null);
                if (options == null)
                {
                    ModLog.Warn("[BuildingGridMod] BurstCompiler.Options not found.");
                    return;
                }

                PropertyInfo enableCompilationProperty = options.GetType().GetProperty("EnableBurstCompilation", BindingFlags.Public | BindingFlags.Instance);
                PropertyInfo enableSafetyProperty = options.GetType().GetProperty("EnableBurstSafetyChecks", BindingFlags.Public | BindingFlags.Instance);

                if (enableCompilationProperty != null)
                {
                    enableCompilationProperty.SetValue(options, false, null);
                }

                if (enableSafetyProperty != null)
                {
                    enableSafetyProperty.SetValue(options, true, null);
                }

                ModLog.Warn("[BuildingGridMod] Burst compilation disabled to ensure Harmony patches affect DOTS building systems.");
            }
            catch (Exception ex)
            {
                ModLog.Warn("[BuildingGridMod] Failed to configure Burst compatibility: " + ex.Message);
            }
        }
    }
}
