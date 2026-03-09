using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Unity.Mathematics;
using Unity.Physics;

namespace ApproximatelyUpMod
{
    internal static class PatchDiagnostics
    {
        private static readonly Dictionary<ushort, OpCode> OpCodeMap = BuildOpcodeMap();

        public static void LogPatchTargetsAndIl(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodBase onUpdate = AccessTools.Method(typeof(GarageGrabber), nameof(GarageGrabber.OnUpdate), new[] { typeof(Unity.Entities.SystemState).MakeByRefType() });
                MethodBase canMount = AccessTools.Method(
                    typeof(GarageGrabber),
                    nameof(GarageGrabber.CanMountComponentToGaragePos),
                    new[]
                    {
                        typeof(Unity.Entities.EntityManager),
                        typeof(GarageGrabberSingleton).MakeByRefType(),
                        typeof(Unity.Collections.NativeArray<GarageGrabber.GarageTickPlacedComponent>),
                        typeof(GarageTransform),
                        typeof(SCPrefab)
                    });

                MethodBase checkBox = AccessTools.Method(
                    typeof(CollisionWorld),
                    nameof(CollisionWorld.CheckBox),
                    new[] { typeof(float3), typeof(quaternion), typeof(float3), typeof(CollisionFilter), typeof(QueryInteraction) });

                LogPatchInfo(harmony, onUpdate, "GarageGrabber.OnUpdate");
                LogPatchInfo(harmony, canMount, "GarageGrabber.CanMountComponentToGaragePos");
                LogPatchInfo(harmony, checkBox, "CollisionWorld.CheckBox(float3, quaternion, float3, CollisionFilter, QueryInteraction)");

                DumpMethodIl("GarageGrabber.OnUpdate", onUpdate, 180);
                DumpMethodIl("GarageGrabber.CanMountComponentToGaragePos", canMount, 220);
                DumpMethodIl("CollisionWorld.CheckBox", checkBox, 120);
            }
            catch (Exception ex)
            {
                ModLog.Warn("[BuildingGridMod] Patch diagnostics failed: " + ex);
            }
        }

        private static void LogPatchInfo(HarmonyLib.Harmony harmony, MethodBase method, string label)
        {
            if (method == null)
            {
                ModLog.Warn("[BuildingGridMod] Target not found: " + label);
                return;
            }

            Patches patches = HarmonyLib.Harmony.GetPatchInfo(method);
            int prefixes = patches?.Prefixes?.Count ?? 0;
            int postfixes = patches?.Postfixes?.Count ?? 0;
            int transpilers = patches?.Transpilers?.Count ?? 0;

            ModLog.Info("[BuildingGridMod] Target resolved: " + label + " -> " + method);
            ModLog.Info("[BuildingGridMod] Patches on target: prefixes=" + prefixes + ", postfixes=" + postfixes + ", transpilers=" + transpilers);

            if (patches != null)
            {
                foreach (Patch patch in patches.Prefixes.Concat(patches.Postfixes).Concat(patches.Transpilers))
                {
                    ModLog.Info("[BuildingGridMod]  patch owner=" + patch.owner + " method=" + patch.PatchMethod);
                }
            }

            if (harmony != null)
            {
                MethodBase[] patched = harmony.GetPatchedMethods().ToArray();
                ModLog.Info("[BuildingGridMod] Harmony instance patched methods count=" + patched.Length);
            }
        }

        private static void DumpMethodIl(string label, MethodBase method, int maxInstructions)
        {
            if (method == null)
            {
                return;
            }

            MethodBody body = (method as MethodInfo)?.GetMethodBody();
            if (body == null)
            {
                ModLog.Warn("[BuildingGridMod] No IL body for: " + label);
                return;
            }

            byte[] il = body.GetILAsByteArray();
            if (il == null || il.Length == 0)
            {
                ModLog.Warn("[BuildingGridMod] Empty IL for: " + label);
                return;
            }

            ModLog.Info("[BuildingGridMod] IL dump start: " + label + " (" + il.Length + " bytes)");
            int index = 0;
            int printed = 0;
            while (index < il.Length && printed < maxInstructions)
            {
                int offset = index;
                ushort opcodeValue = il[index++];
                if (opcodeValue == 0xFE && index < il.Length)
                {
                    opcodeValue = (ushort)(0xFE00 | il[index++]);
                }

                OpCode opcode;
                if (!OpCodeMap.TryGetValue(opcodeValue, out opcode))
                {
                    ModLog.Warn("[BuildingGridMod] IL " + label + " 0x" + offset.ToString("X4") + " : <unknown opcode 0x" + opcodeValue.ToString("X") + ">");
                    break;
                }

                string operandText;
                index += GetOperandSizeAndText(il, index, opcode, out operandText);
                ModLog.Info("[BuildingGridMod] IL " + label + " 0x" + offset.ToString("X4") + " : " + opcode.Name + operandText);
                printed++;
            }
            ModLog.Info("[BuildingGridMod] IL dump end: " + label);
        }

        private static int GetOperandSizeAndText(byte[] il, int index, OpCode opcode, out string text)
        {
            text = string.Empty;
            switch (opcode.OperandType)
            {
                case OperandType.InlineNone:
                    return 0;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    if (index < il.Length)
                    {
                        text = " " + il[index];
                    }
                    return 1;
                case OperandType.InlineVar:
                    if (index + 1 < il.Length)
                    {
                        text = " " + BitConverter.ToUInt16(il, index);
                    }
                    return 2;
                case OperandType.InlineI:
                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                    if (index + 3 < il.Length)
                    {
                        text = " 0x" + BitConverter.ToInt32(il, index).ToString("X8");
                    }
                    return 4;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    if (index + 7 < il.Length)
                    {
                        text = " 0x" + BitConverter.ToInt64(il, index).ToString("X16");
                    }
                    return 8;
                case OperandType.ShortInlineR:
                    if (index + 3 < il.Length)
                    {
                        text = " " + BitConverter.ToSingle(il, index).ToString("0.###");
                    }
                    return 4;
                case OperandType.InlineSwitch:
                    if (index + 3 >= il.Length)
                    {
                        return 0;
                    }

                    int count = BitConverter.ToInt32(il, index);
                    text = " (switch " + count + ")";
                    return 4 + count * 4;
                default:
                    return 0;
            }
        }

        private static Dictionary<ushort, OpCode> BuildOpcodeMap()
        {
            Dictionary<ushort, OpCode> map = new Dictionary<ushort, OpCode>();
            FieldInfo[] fields = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType != typeof(OpCode))
                {
                    continue;
                }

                OpCode opcode = (OpCode)field.GetValue(null);
                map[(ushort)opcode.Value] = opcode;
            }
            return map;
        }
    }
}
