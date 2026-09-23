using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    internal static class RivalsRebornWideNumericTranspiler
    {
        private static readonly MethodInfo TruncateProduct = AccessTools.Method(
            typeof(RivalsRebornWideNumericInterop),
            nameof(RivalsRebornWideNumericInterop.TruncateProduct),
            new Type[] { typeof(long), typeof(float) });

        private static readonly MethodInfo TruncateProductAtLeast = AccessTools.Method(
            typeof(RivalsRebornWideNumericInterop),
            nameof(RivalsRebornWideNumericInterop.TruncateProductAtLeast),
            new Type[] { typeof(float), typeof(long), typeof(float) });

        private static readonly MethodInfo GetExactFanTotal = AccessTools.Method(
            typeof(RivalsRebornWideNumericInterop),
            nameof(RivalsRebornWideNumericInterop.GetExactFanTotal),
            new Type[] { typeof(data_girls.girls) });

        private static readonly MethodInfo CheckedMultiply = AccessTools.Method(
            typeof(RivalsRebornWideNumericInterop),
            nameof(RivalsRebornWideNumericInterop.CheckedMultiply),
            new Type[] { typeof(long), typeof(long) });

        private static readonly MethodInfo CheckedAdd = AccessTools.Method(
            typeof(RivalsRebornWideNumericInterop),
            nameof(RivalsRebornWideNumericInterop.CheckedAdd),
            new Type[] { typeof(long), typeof(long) });

        private static readonly MethodInfo Log10FansForAwardScore = AccessTools.Method(
            typeof(RivalsRebornWideNumericInterop),
            nameof(RivalsRebornWideNumericInterop.Log10FansForAwardScore),
            new Type[] { typeof(long) });

        private static readonly MethodInfo MathMaxInt64 = AccessTools.Method(
            typeof(Math),
            nameof(Math.Max),
            new Type[] { typeof(long), typeof(long) });

        private static readonly FieldInfo GirlFans = AccessTools.Field(
            typeof(data_girls.girls),
            "fans");

        private static readonly FieldInfo RivalGroupFans = AccessTools.Field(
            typeof(Rivals._group),
            "Fans");

        internal static IEnumerable<CodeInstruction> RewriteDampFanGrowth(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            List<Tuple<int, int, int>> sites = FindSimpleTruncatingProducts(code);
            if (sites.Count != 2)
            {
                RivalsRebornWideNumericInterop.WarnShape("RosterSim.DampFanGrowth", 2, sites.Count);
                return code;
            }

            for (int index = 0; index < sites.Count; index++)
            {
                RewriteSimpleProduct(code, sites[index]);
            }
            return code;
        }

        internal static IEnumerable<CodeInstruction> RewriteDoAccusation(
            IEnumerable<CodeInstruction> instructions)
        {
            return RewriteExpectedSimpleProducts(
                instructions,
                "SpecialLabels.DoAccusation",
                1);
        }

        internal static IEnumerable<CodeInstruction> RewriteComputeIdolFans(
            IEnumerable<CodeInstruction> instructions)
        {
            return RewriteExpectedSimpleProducts(
                instructions,
                "Portraits.ComputeIdolFans",
                1);
        }

        internal static IEnumerable<CodeInstruction> RewriteRefreshFans(
            IEnumerable<CodeInstruction> instructions)
        {
            return RewriteExpectedSimpleProducts(
                instructions,
                "Portraits.RefreshFans",
                1);
        }

        internal static IEnumerable<CodeInstruction> RewriteFoundFromRetiree(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            List<int> fanLoads = FindGirlFanLoads(code);
            List<Tuple<int, int, int>> sites = FindSimpleTruncatingProducts(code);
            if (fanLoads.Count != 1 || sites.Count != 1)
            {
                RivalsRebornWideNumericInterop.WarnShape(
                    "SpecialLabels.FoundFromRetiree",
                    2,
                    fanLoads.Count + sites.Count);
                return code;
            }

            ReplaceGirlFanLoad(code, fanLoads[0], true);
            RewriteSimpleProduct(code, sites[0]);
            return code;
        }

        internal static IEnumerable<CodeInstruction> RewritePickTarget(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            List<int> fanLoads = FindGirlFanLoads(code);
            if (fanLoads.Count != 2)
            {
                RivalsRebornWideNumericInterop.WarnShape(
                    "PoachSim.PickTarget",
                    2,
                    fanLoads.Count);
                return code;
            }

            int thresholdConstant = NextNonNop(code, fanLoads[0] + 1);
            int logConversion = NextNonNop(code, fanLoads[1] + 1);
            if (thresholdConstant < 0 ||
                !IsLdcI4Value(code[thresholdConstant], 2000) ||
                logConversion < 0 ||
                code[logConversion].opcode != OpCodes.Conv_R8)
            {
                RivalsRebornWideNumericInterop.WarnDetail(
                    "PoachSim.PickTarget:detail",
                    "Rivals Reborn wide-numeric target PoachSim.PickTarget no longer " +
                    "matches the expected HEAD threshold/log fan-consumer shape; the " +
                    "method was left unchanged.");
                return code;
            }

            ReplaceGirlFanLoad(code, fanLoads[0], false);
            code[thresholdConstant].opcode = OpCodes.Ldc_I8;
            code[thresholdConstant].operand = 2000L;
            ReplaceGirlFanLoad(code, fanLoads[1], false);
            return code;
        }

        internal static IEnumerable<CodeInstruction> RewriteBuyoutPrice(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            List<int> fanLoads = FindGirlFanLoads(code);
            if (fanLoads.Count != 1)
            {
                RivalsRebornWideNumericInterop.WarnShape(
                    "PoachSim.BuyoutPrice",
                    1,
                    fanLoads.Count);
                return code;
            }

            int fanLoad = fanLoads[0];
            int baseConvR4 = FindNextOpcode(
                code,
                fanLoad + 1,
                code.Count,
                OpCodes.Conv_R4);
            if (baseConvR4 < 0)
            {
                RivalsRebornWideNumericInterop.WarnDetail(
                    "PoachSim.BuyoutPrice:base",
                    "Rivals Reborn wide-numeric target PoachSim.BuyoutPrice no longer " +
                    "contains the expected wide-base-to-Single boundary; the method was " +
                    "left unchanged.");
                return code;
            }

            List<int> checkedMultiplications = new List<int>();
            List<int> checkedAdditions = new List<int>();
            for (int index = fanLoad + 1; index < baseConvR4; index++)
            {
                if (code[index].opcode == OpCodes.Mul)
                {
                    checkedMultiplications.Add(index);
                }
                else if (code[index].opcode == OpCodes.Add)
                {
                    checkedAdditions.Add(index);
                }
            }

            int finalMul = FindNextOpcode(
                code,
                baseConvR4 + 1,
                Math.Min(code.Count, baseConvR4 + 18),
                OpCodes.Mul);
            int finalConvI8 = finalMul < 0 ? -1 : NextNonNop(code, finalMul + 1);

            if (checkedMultiplications.Count != 2 ||
                checkedAdditions.Count != 2 ||
                finalMul < 0 ||
                finalConvI8 < 0 ||
                code[finalConvI8].opcode != OpCodes.Conv_I8)
            {
                RivalsRebornWideNumericInterop.WarnDetail(
                    "PoachSim.BuyoutPrice:shape",
                    "Rivals Reborn wide-numeric target PoachSim.BuyoutPrice no longer " +
                    "matches the expected HEAD checked-base/product shape; the method was " +
                    "left unchanged.");
                return code;
            }

            ReplaceGirlFanLoad(code, fanLoad, true);
            for (int index = 0; index < checkedMultiplications.Count; index++)
            {
                code[checkedMultiplications[index]].opcode = OpCodes.Call;
                code[checkedMultiplications[index]].operand = CheckedMultiply;
            }
            for (int index = 0; index < checkedAdditions.Count; index++)
            {
                code[checkedAdditions[index]].opcode = OpCodes.Call;
                code[checkedAdditions[index]].operand = CheckedAdd;
            }

            MakeNop(code[baseConvR4]);
            code[finalMul].opcode = OpCodes.Call;
            code[finalMul].operand = TruncateProduct;
            MakeNop(code[finalConvI8]);
            return code;
        }

        internal static IEnumerable<CodeInstruction> RewriteRivalAwardsPickLabel(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            List<int> fanLoads = new List<int>();
            for (int index = 0; index < code.Count; index++)
            {
                if (code[index].opcode == OpCodes.Ldfld &&
                    Equals(code[index].operand, RivalGroupFans))
                {
                    fanLoads.Add(index);
                }
            }

            if (fanLoads.Count != 1)
            {
                RivalsRebornWideNumericInterop.WarnShape(
                    "RivalAwards.PickLabel", 1, fanLoads.Count);
                return code;
            }

            int fanLoad = fanLoads[0];
            int convR4 = NextNonNop(code, fanLoad + 1);
            int maxCall = convR4 >= 0 ? NextNonNop(code, convR4 + 1) : -1;
            int logCall = maxCall >= 0 ? NextNonNop(code, maxCall + 1) : -1;
            int minimum = FindPreviousLdcR4(code, fanLoad, 10f, 12);

            if (minimum < 0 || convR4 < 0 || code[convR4].opcode != OpCodes.Conv_R4 ||
                maxCall < 0 || !IsMathfMaxCall(code[maxCall]) ||
                logCall < 0 || !IsMathfLog10Call(code[logCall]))
            {
                RivalsRebornWideNumericInterop.WarnDetail(
                    "RivalAwards.PickLabel:detail",
                    "Rivals Reborn wide-numeric target RivalAwards.PickLabel no longer " +
                    "matches the expected HEAD max/log fan-score shape; the method was " +
                    "left unchanged.");
                return code;
            }

            // Remove Mathf.Max(10f, (float)Fans) + Mathf.Log10(...) as one unit.
            // The ldfld still leaves the Int64 fan count on the stack for our helper.
            MakeNop(code[minimum]);
            code[convR4].opcode = OpCodes.Call;
            code[convR4].operand = Log10FansForAwardScore;
            MakeNop(code[maxCall]);
            MakeNop(code[logCall]);
            return code;
        }

        internal static IEnumerable<CodeInstruction> RewriteQueueFounderRoll(
            IEnumerable<CodeInstruction> instructions)
        {
            return RewriteExactFanThreshold(
                instructions,
                "SpecialLabels.QueueFounderRoll",
                100000);
        }

        internal static IEnumerable<CodeInstruction> RewriteFromPlayerGirl(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            List<int> fanLoads = FindGirlFanLoads(code);
            if (fanLoads.Count != 1)
            {
                RivalsRebornWideNumericInterop.WarnShape(
                    "RosterSim.FromPlayerGirl",
                    1,
                    fanLoads.Count);
                return code;
            }

            int fanLoad = fanLoads[0];
            int minimum = FindPreviousLdcI4(code, fanLoad, 10, 8);
            int maxCall = FindNextSystemMathMaxInt32(code, fanLoad + 1, Math.Min(code.Count, fanLoad + 8));
            int convR8 = maxCall < 0 ? -1 : NextNonNop(code, maxCall + 1);
            if (minimum < 0 || maxCall < 0 || convR8 < 0 || code[convR8].opcode != OpCodes.Conv_R8)
            {
                RivalsRebornWideNumericInterop.WarnDetail(
                    "RosterSim.FromPlayerGirl:fan-fame",
                    "Rivals Reborn wide-numeric target RosterSim.FromPlayerGirl no longer " +
                    "matches the expected HEAD Math.Max(10, girl.fans) logarithm shape; " +
                    "the method was left unchanged.");
                return code;
            }

            code[minimum].opcode = OpCodes.Ldc_I8;
            code[minimum].operand = 10L;
            ReplaceGirlFanLoad(code, fanLoad, false);
            code[maxCall].operand = MathMaxInt64;
            return code;
        }

        internal static IEnumerable<CodeInstruction> RewriteResolveDatingChoice(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            List<int> fanLoads = FindGirlFanLoads(code);
            if (fanLoads.Count != 1)
            {
                RivalsRebornWideNumericInterop.WarnShape(
                    "XRelSim.ResolveDatingChoice",
                    1,
                    fanLoads.Count);
                return code;
            }

            int next = NextNonNop(code, fanLoads[0] + 1);
            if (next < 0 || code[next].opcode != OpCodes.Conv_I8)
            {
                RivalsRebornWideNumericInterop.WarnDetail(
                    "XRelSim.ResolveDatingChoice:coverup",
                    "Rivals Reborn wide-numeric target XRelSim.ResolveDatingChoice no longer " +
                    "matches the expected HEAD (long)activeGirl.fans cover-up-cost shape; " +
                    "the method was left unchanged.");
                return code;
            }

            ReplaceGirlFanLoad(code, fanLoads[0], true);
            return code;
        }

        private static IEnumerable<CodeInstruction> RewriteExactFanThreshold(
            IEnumerable<CodeInstruction> instructions,
            string methodName,
            int threshold)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            List<int> fanLoads = FindGirlFanLoads(code);
            if (fanLoads.Count != 1)
            {
                RivalsRebornWideNumericInterop.WarnShape(methodName, 1, fanLoads.Count);
                return code;
            }

            int constant = NextNonNop(code, fanLoads[0] + 1);
            if (constant < 0 || !IsLdcI4Value(code[constant], threshold))
            {
                RivalsRebornWideNumericInterop.WarnDetail(
                    methodName + ":threshold",
                    "Rivals Reborn wide-numeric target " + methodName +
                    " no longer matches the expected HEAD exact-fan threshold shape; " +
                    "the method was left unchanged.");
                return code;
            }

            ReplaceGirlFanLoad(code, fanLoads[0], false);
            code[constant].opcode = OpCodes.Ldc_I8;
            code[constant].operand = (long)threshold;
            return code;
        }

        internal static IEnumerable<CodeInstruction> RewriteResolveTempt(
            IEnumerable<CodeInstruction> instructions)
        {
            return RewriteExpectedSimpleProducts(
                instructions,
                "PoachSim.ResolveTempt",
                2);
        }

        private static IEnumerable<CodeInstruction> RewriteExpectedSimpleProducts(
            IEnumerable<CodeInstruction> instructions,
            string methodName,
            int expected)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            List<Tuple<int, int, int>> sites = FindSimpleTruncatingProducts(code);
            if (sites.Count != expected)
            {
                RivalsRebornWideNumericInterop.WarnShape(methodName, expected, sites.Count);
                return code;
            }

            for (int index = 0; index < sites.Count; index++)
            {
                RewriteSimpleProduct(code, sites[index]);
            }
            return code;
        }

        internal static IEnumerable<CodeInstruction> RewriteAdjustSales(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            List<Tuple<int, int, int>> simpleSites = FindSimpleTruncatingProducts(code);
            List<Tuple<int, int, int, int>> minimumSites = FindMinimumTruncatingProducts(code);

            // AdjustSales HEAD contains the initial fan-to-sales product and the >350k
            // damping product as simple sites. The momentum path is Mathf.Max(100f, ...).
            if (simpleSites.Count != 2 || minimumSites.Count != 1)
            {
                RivalsRebornWideNumericInterop.WarnShape(
                    "RosterSim.AdjustSales", 3, simpleSites.Count + minimumSites.Count);
                return code;
            }

            for (int index = 0; index < simpleSites.Count; index++)
            {
                RewriteSimpleProduct(code, simpleSites[index]);
            }
            RewriteMinimumProduct(code, minimumSites[0]);
            return code;
        }

        /// <summary>
        /// Finds HEAD-style (long -> conv.r4 -> coefficient -> mul -> conv.i8) sites.
        /// Sites whose multiplication feeds Mathf.Max are deliberately excluded and
        /// handled by FindMinimumTruncatingProducts.
        /// </summary>
        private static List<Tuple<int, int, int>> FindSimpleTruncatingProducts(
            List<CodeInstruction> code)
        {
            List<Tuple<int, int, int>> result = new List<Tuple<int, int, int>>();
            for (int conv = 0; conv < code.Count; conv++)
            {
                if (code[conv].opcode != OpCodes.Conv_R4) continue;

                int mul = FindOpcode(code, conv + 1, Math.Min(code.Count, conv + 12), OpCodes.Mul);
                if (mul < 0) continue;

                int next = NextNonNop(code, mul + 1);
                if (next < 0) continue;
                if (IsMathfMaxCall(code[next])) continue;
                if (code[next].opcode != OpCodes.Conv_I8) continue;

                result.Add(Tuple.Create(conv, mul, next));
            }
            return result;
        }

        /// <summary>
        /// Finds Mathf.Max(minimum, (float)longValue * coefficient) -> long.
        /// Leaving the minimum float on the evaluation stack lets the replacement helper
        /// consume (float minimum, long value, float coefficient) without reordering IL.
        /// </summary>
        private static List<Tuple<int, int, int, int>> FindMinimumTruncatingProducts(
            List<CodeInstruction> code)
        {
            List<Tuple<int, int, int, int>> result = new List<Tuple<int, int, int, int>>();
            for (int conv = 0; conv < code.Count; conv++)
            {
                if (code[conv].opcode != OpCodes.Conv_R4) continue;

                int mul = FindOpcode(code, conv + 1, Math.Min(code.Count, conv + 8), OpCodes.Mul);
                if (mul < 0) continue;
                int maxCall = NextNonNop(code, mul + 1);
                if (maxCall < 0 || !IsMathfMaxCall(code[maxCall])) continue;
                int convI8 = NextNonNop(code, maxCall + 1);
                if (convI8 < 0 || code[convI8].opcode != OpCodes.Conv_I8) continue;

                result.Add(Tuple.Create(conv, mul, maxCall, convI8));
            }
            return result;
        }

        private static List<int> FindGirlFanLoads(List<CodeInstruction> code)
        {
            List<int> result = new List<int>();
            for (int index = 0; index < code.Count; index++)
            {
                if (code[index].opcode == OpCodes.Ldfld &&
                    Equals(code[index].operand, GirlFans))
                {
                    result.Add(index);
                }
            }
            return result;
        }

        private static void ReplaceGirlFanLoad(
            List<CodeInstruction> code,
            int fanLoad,
            bool removeFollowingInt64Conversion)
        {
            code[fanLoad].opcode = OpCodes.Call;
            code[fanLoad].operand = GetExactFanTotal;
            if (!removeFollowingInt64Conversion)
            {
                return;
            }

            int next = NextNonNop(code, fanLoad + 1);
            if (next >= 0 && code[next].opcode == OpCodes.Conv_I8)
            {
                MakeNop(code[next]);
            }
        }

        private static bool IsLdcI4Value(CodeInstruction instruction, int value)
        {
            if (instruction.opcode == OpCodes.Ldc_I4)
            {
                return instruction.operand is int && (int)instruction.operand == value;
            }
            if (instruction.opcode == OpCodes.Ldc_I4_S)
            {
                if (instruction.operand is sbyte) return (sbyte)instruction.operand == value;
                if (instruction.operand is byte) return (byte)instruction.operand == value;
                return false;
            }
            if (value == -1 && instruction.opcode == OpCodes.Ldc_I4_M1) return true;
            if (value == 0 && instruction.opcode == OpCodes.Ldc_I4_0) return true;
            if (value == 1 && instruction.opcode == OpCodes.Ldc_I4_1) return true;
            if (value == 2 && instruction.opcode == OpCodes.Ldc_I4_2) return true;
            if (value == 3 && instruction.opcode == OpCodes.Ldc_I4_3) return true;
            if (value == 4 && instruction.opcode == OpCodes.Ldc_I4_4) return true;
            if (value == 5 && instruction.opcode == OpCodes.Ldc_I4_5) return true;
            if (value == 6 && instruction.opcode == OpCodes.Ldc_I4_6) return true;
            if (value == 7 && instruction.opcode == OpCodes.Ldc_I4_7) return true;
            if (value == 8 && instruction.opcode == OpCodes.Ldc_I4_8) return true;
            return false;
        }

        private static void RewriteSimpleProduct(
            List<CodeInstruction> code,
            Tuple<int, int, int> site)
        {
            MakeNop(code[site.Item1]);
            code[site.Item2].opcode = OpCodes.Call;
            code[site.Item2].operand = TruncateProduct;
            MakeNop(code[site.Item3]);
        }

        private static void RewriteMinimumProduct(
            List<CodeInstruction> code,
            Tuple<int, int, int, int> site)
        {
            MakeNop(code[site.Item1]);
            MakeNop(code[site.Item2]);
            code[site.Item3].opcode = OpCodes.Call;
            code[site.Item3].operand = TruncateProductAtLeast;
            MakeNop(code[site.Item4]);
        }

        private static int FindOpcode(
            List<CodeInstruction> code,
            int start,
            int endExclusive,
            OpCode opcode)
        {
            for (int index = start; index < endExclusive; index++)
            {
                if (code[index].opcode == opcode) return index;
                if (code[index].opcode == OpCodes.Conv_R4 || code[index].opcode == OpCodes.Conv_I8)
                    break;
            }
            return -1;
        }

        private static int FindNextOpcode(
            List<CodeInstruction> code,
            int start,
            int endExclusive,
            OpCode opcode)
        {
            for (int index = start; index < endExclusive; index++)
            {
                if (code[index].opcode == opcode)
                {
                    return index;
                }
            }
            return -1;
        }

        private static int NextNonNop(List<CodeInstruction> code, int start)
        {
            for (int index = start; index < code.Count; index++)
            {
                if (code[index].opcode != OpCodes.Nop) return index;
            }
            return -1;
        }

        private static int FindPreviousLdcI4(
            List<CodeInstruction> code,
            int startExclusive,
            int value,
            int maxDistance)
        {
            int lower = Math.Max(0, startExclusive - maxDistance);
            for (int index = startExclusive - 1; index >= lower; index--)
            {
                if (IsLdcI4Value(code[index], value))
                {
                    return index;
                }
            }
            return -1;
        }

        private static int FindNextSystemMathMaxInt32(
            List<CodeInstruction> code,
            int start,
            int endExclusive)
        {
            for (int index = start; index < endExclusive; index++)
            {
                if (code[index].opcode != OpCodes.Call && code[index].opcode != OpCodes.Callvirt)
                {
                    continue;
                }

                MethodInfo method = code[index].operand as MethodInfo;
                if (method == null || method.Name != nameof(Math.Max) ||
                    method.DeclaringType != typeof(Math) || method.ReturnType != typeof(int))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 2 &&
                    parameters[0].ParameterType == typeof(int) &&
                    parameters[1].ParameterType == typeof(int))
                {
                    return index;
                }
            }
            return -1;
        }

        private static int FindPreviousLdcR4(
            List<CodeInstruction> code,
            int startExclusive,
            float value,
            int maxDistance)
        {
            int lower = Math.Max(0, startExclusive - maxDistance);
            for (int index = startExclusive - 1; index >= lower; index--)
            {
                if (code[index].opcode == OpCodes.Ldc_R4 &&
                    code[index].operand is float &&
                    (float)code[index].operand == value)
                {
                    return index;
                }
            }
            return -1;
        }

        private static bool IsMathfLog10Call(CodeInstruction instruction)
        {
            if (instruction.opcode != OpCodes.Call && instruction.opcode != OpCodes.Callvirt)
                return false;
            MethodInfo method = instruction.operand as MethodInfo;
            return method != null && method.Name == "Log10" &&
                method.DeclaringType != null &&
                method.DeclaringType.FullName == "UnityEngine.Mathf" &&
                method.ReturnType == typeof(float);
        }

        private static bool IsMathfMaxCall(CodeInstruction instruction)
        {
            if (instruction.opcode != OpCodes.Call && instruction.opcode != OpCodes.Callvirt)
                return false;
            MethodInfo method = instruction.operand as MethodInfo;
            if (method == null || method.Name != "Max") return false;
            return method.DeclaringType != null &&
                method.DeclaringType.FullName == "UnityEngine.Mathf" &&
                method.ReturnType == typeof(float);
        }

        private static void MakeNop(CodeInstruction instruction)
        {
            instruction.opcode = OpCodes.Nop;
            instruction.operand = null;
        }
    }

    [HarmonyPatch]
    internal static class RivalsReborn_DampFanGrowth_WideNumeric_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return RivalsRebornWideNumericInterop.ResolveRosterMethod(
                "DampFanGrowth",
                null);
        }

        [HarmonyTranspiler]
        [HarmonyAfter(RivalsRebornWideNumericInterop.Owner)]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return RivalsRebornWideNumericTranspiler.RewriteDampFanGrowth(instructions);
        }
    }

    [HarmonyPatch]
    internal static class RivalsReborn_AdjustSales_WideNumeric_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return RivalsRebornWideNumericInterop.ResolveRosterMethod(
                "AdjustSales",
                null);
        }

        [HarmonyTranspiler]
        [HarmonyAfter(RivalsRebornWideNumericInterop.Owner)]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return RivalsRebornWideNumericTranspiler.RewriteAdjustSales(instructions);
        }
    }

    [HarmonyPatch]
    internal static class RivalsReborn_DoAccusation_WideNumeric_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return RivalsRebornWideNumericInterop.ResolveSpecialLabelsMethod(
                "DoAccusation",
                null);
        }

        [HarmonyTranspiler]
        [HarmonyAfter(RivalsRebornWideNumericInterop.Owner)]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return RivalsRebornWideNumericTranspiler.RewriteDoAccusation(instructions);
        }
    }

    [HarmonyPatch]
    internal static class RivalsReborn_ComputeIdolFans_WideNumeric_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return RivalsRebornWideNumericInterop.ResolvePortraitsMethod(
                "ComputeIdolFans",
                null);
        }

        [HarmonyTranspiler]
        [HarmonyAfter(RivalsRebornWideNumericInterop.Owner)]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return RivalsRebornWideNumericTranspiler.RewriteComputeIdolFans(instructions);
        }
    }

    [HarmonyPatch]
    internal static class RivalsReborn_RefreshFans_WideNumeric_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return RivalsRebornWideNumericInterop.ResolvePortraitsMethod(
                "RefreshFans",
                null);
        }

        [HarmonyTranspiler]
        [HarmonyAfter(RivalsRebornWideNumericInterop.Owner)]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return RivalsRebornWideNumericTranspiler.RewriteRefreshFans(instructions);
        }
    }

    [HarmonyPatch]
    internal static class RivalsReborn_FoundFromRetiree_WideNumeric_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return RivalsRebornWideNumericInterop.ResolveSpecialLabelsMethod(
                "FoundFromRetiree",
                new Type[] { typeof(data_girls.girls) });
        }

        [HarmonyTranspiler]
        [HarmonyAfter(RivalsRebornWideNumericInterop.Owner)]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return RivalsRebornWideNumericTranspiler.RewriteFoundFromRetiree(instructions);
        }
    }

    [HarmonyPatch]
    internal static class RivalsReborn_PickTarget_WideNumeric_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return RivalsRebornWideNumericInterop.ResolvePoachMethod(
                "PickTarget",
                Type.EmptyTypes);
        }

        [HarmonyTranspiler]
        [HarmonyAfter(RivalsRebornWideNumericInterop.Owner)]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return RivalsRebornWideNumericTranspiler.RewritePickTarget(instructions);
        }
    }

    [HarmonyPatch]
    internal static class RivalsReborn_BuyoutPrice_WideNumeric_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type labelType = AccessTools.TypeByName("RivalsReborn.RLabel");
            if (labelType == null)
            {
                yield break;
            }

            foreach (MethodBase method in RivalsRebornWideNumericInterop.ResolvePoachMethod(
                "BuyoutPrice",
                new Type[] { labelType, typeof(data_girls.girls) }))
            {
                yield return method;
            }
        }

        [HarmonyTranspiler]
        [HarmonyAfter(RivalsRebornWideNumericInterop.Owner)]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return RivalsRebornWideNumericTranspiler.RewriteBuyoutPrice(instructions);
        }
    }

    [HarmonyPatch]
    internal static class RivalsReborn_ResolveTempt_WideNumeric_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return RivalsRebornWideNumericInterop.ResolvePoachMethod(
                "ResolveTempt",
                new Type[] { typeof(string) });
        }

        [HarmonyTranspiler]
        [HarmonyAfter(RivalsRebornWideNumericInterop.Owner)]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return RivalsRebornWideNumericTranspiler.RewriteResolveTempt(instructions);
        }
    }

    [HarmonyPatch]
    internal static class RivalsReborn_QueueFounderRoll_ExactFans_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return RivalsRebornWideNumericInterop.ResolveSpecialLabelsMethod(
                "QueueFounderRoll",
                new Type[] { typeof(data_girls.girls) });
        }

        [HarmonyTranspiler]
        [HarmonyAfter(RivalsRebornWideNumericInterop.Owner)]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return RivalsRebornWideNumericTranspiler.RewriteQueueFounderRoll(instructions);
        }
    }

    [HarmonyPatch]
    internal static class RivalsReborn_FromPlayerGirl_ExactFans_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return RivalsRebornWideNumericInterop.ResolveRosterMethod(
                "FromPlayerGirl",
                new Type[] { typeof(data_girls.girls) });
        }

        [HarmonyTranspiler]
        [HarmonyAfter(RivalsRebornWideNumericInterop.Owner)]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return RivalsRebornWideNumericTranspiler.RewriteFromPlayerGirl(instructions);
        }
    }

    [HarmonyPatch]
    internal static class RivalsReborn_ResolveDatingChoice_ExactFans_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return RivalsRebornWideNumericInterop.ResolveXRelMethod(
                "ResolveDatingChoice",
                new Type[] { typeof(string) });
        }

        [HarmonyTranspiler]
        [HarmonyAfter(RivalsRebornWideNumericInterop.Owner)]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return RivalsRebornWideNumericTranspiler.RewriteResolveDatingChoice(instructions);
        }
    }

    [HarmonyPatch]
    internal static class RivalsReborn_RivalAwardsPickLabel_WideNumeric_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return RivalsRebornWideNumericInterop.ResolveRivalAwardsMethod(
                "PickLabel",
                null);
        }

        [HarmonyTranspiler]
        [HarmonyAfter(RivalsRebornWideNumericInterop.Owner)]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return RivalsRebornWideNumericTranspiler.RewriteRivalAwardsPickLabel(instructions);
        }
    }

    [HarmonyPatch]
    internal static class RivalsReborn_FormatFans_WideNumeric_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return RivalsRebornWideNumericInterop.ResolveRivalsUiMethod(
                "FormatFans",
                new Type[] { typeof(long) });
        }

        [HarmonyPrefix]
        [HarmonyAfter(RivalsRebornWideNumericInterop.Owner)]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(long fans, ref string __result)
        {
            __result = RivalsRebornWideNumericInterop.FormatFansWide(fans);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class RivalsReborn_YearlyShakeout_WideNumeric_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return RivalsRebornWideNumericInterop.ResolveRosterMethod(
                "YearlyShakeout",
                new Type[] { typeof(DateTime) });
        }

        [HarmonyPrefix]
        [HarmonyAfter(RivalsRebornWideNumericInterop.Owner)]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(DateTime now)
        {
            return !RivalsRebornWideNumericInterop.TryRunYearlyShakeout(now);
        }
    }
}
