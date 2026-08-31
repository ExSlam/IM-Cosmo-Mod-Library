using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A25 fixes the six audited immutable DateTime no-ops without reproducing any
    /// gameplay formula. For each Graduation_Date.AddMonths/AddDays statement, the
    /// transpiler duplicates the owning idol reference, loads the current field value,
    /// executes the original delta through a static SNLF helper, and stores the returned
    /// DateTime back into the same Graduation_Date field.
    /// </summary>
    [HarmonyPatch]
    internal static class GraduationDate_SaveNLoadFixes_Patch
    {
        private sealed class AdjustmentMatch
        {
            internal int FieldIndex;
            internal int CallIndex;
            internal int PopIndex;
            internal bool IsMonths;
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo injured = ResolveTarget(
                typeof(data_girls.girls),
                nameof(data_girls.girls.Set_Injured),
                Type.EmptyTypes,
                typeof(void),
                "data_girls.girls.Set_Injured()");
            MethodInfo depressed = ResolveTarget(
                typeof(data_girls.girls),
                nameof(data_girls.girls.Set_Depressed),
                Type.EmptyTypes,
                typeof(void),
                "data_girls.girls.Set_Depressed()");
            MethodInfo graduate = ResolveTarget(
                typeof(data_girls.girls),
                nameof(data_girls.girls.Graduate),
                new[] { typeof(bool), typeof(string) },
                typeof(void),
                "data_girls.girls.Graduate(bool,string)");
            MethodInfo update = ResolveTarget(
                typeof(data_girls.girls),
                nameof(data_girls.girls.Graduation_Date_Update),
                Type.EmptyTypes,
                typeof(bool),
                "data_girls.girls.Graduation_Date_Update()");
            MethodInfo accept = ResolveTarget(
                typeof(business),
                nameof(business.Accept),
                Type.EmptyTypes,
                typeof(void),
                "business.Accept()");

            yield return injured;
            yield return depressed;
            yield return graduate;
            yield return update;
            yield return accept;
        }

        private static MethodInfo ResolveTarget(
            Type declaringType,
            string methodName,
            Type[] arguments,
            Type returnType,
            string methodId)
        {
            MethodInfo method = AccessTools.Method(declaringType, methodName, arguments);
            if (method == null || method.ReturnType != returnType)
            {
                GraduationDatePatchHealth.ReportFailure(
                    methodId + " could not be resolved with the audited signature");
                throw new MissingMethodException(declaringType.FullName, methodName);
            }

            GraduationDatePatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase original)
        {
            List<CodeInstruction> result = new List<CodeInstruction>(instructions);

            FieldInfo graduationDateField = AccessTools.Field(
                typeof(data_girls.girls),
                nameof(data_girls.girls.Graduation_Date));
            MethodInfo addMonths = AccessTools.Method(
                typeof(DateTime),
                nameof(DateTime.AddMonths),
                new[] { typeof(int) });
            MethodInfo addDays = AccessTools.Method(
                typeof(DateTime),
                nameof(DateTime.AddDays),
                new[] { typeof(double) });
            MethodInfo applyMonths = AccessTools.Method(
                typeof(GraduationDateRepair),
                nameof(GraduationDateRepair.ApplyMonths),
                new[] { typeof(DateTime), typeof(int) });
            MethodInfo applyDays = AccessTools.Method(
                typeof(GraduationDateRepair),
                nameof(GraduationDateRepair.ApplyDays),
                new[] { typeof(DateTime), typeof(double) });

            if (graduationDateField == null || addMonths == null || addDays == null ||
                applyMonths == null || applyDays == null)
            {
                GraduationDatePatchHealth.ReportFailure(
                    "A25 could not resolve Graduation_Date, DateTime adjustment methods, or SNLF helpers");
                return result;
            }

            string methodId;
            int expected;
            if (!TryGetExpectedSiteCount(original, out methodId, out expected))
            {
                GraduationDatePatchHealth.ReportFailure(
                    "A25 transpiler was invoked for an unknown target: " +
                    (original == null ? "<null>" : original.DeclaringType.FullName + "." + original.Name));
                return result;
            }

            List<AdjustmentMatch> matches = FindMatches(
                result,
                graduationDateField,
                addMonths,
                addDays);
            GraduationDatePatchHealth.ReportSites(methodId, matches.Count, expected);
            if (matches.Count != expected)
            {
                // Fail closed: do not partially rewrite a method whose compiler/source
                // shape no longer matches the frozen audit manifest.
                return result;
            }

            for (int matchIndex = matches.Count - 1; matchIndex >= 0; matchIndex--)
            {
                AdjustmentMatch match = matches[matchIndex];
                CodeInstruction fieldInstruction = result[match.FieldIndex];

                // At the Graduation_Date field access the stack already contains the
                // owning data_girls.girls reference. Duplicate it so one copy survives
                // the field load and can be consumed by stfld after the pure helper
                // returns the adjusted DateTime.
                CodeInstruction duplicateOwner = new CodeInstruction(OpCodes.Dup);
                duplicateOwner.labels.AddRange(fieldInstruction.labels);
                fieldInstruction.labels.Clear();
                duplicateOwner.blocks.AddRange(fieldInstruction.blocks);
                fieldInstruction.blocks.Clear();

                result.Insert(match.FieldIndex, duplicateOwner);

                // All original indices at/after FieldIndex moved by one.
                fieldInstruction = result[match.FieldIndex + 1];
                fieldInstruction.opcode = OpCodes.Ldfld;
                fieldInstruction.operand = graduationDateField;

                CodeInstruction callInstruction = result[match.CallIndex + 1];
                callInstruction.opcode = OpCodes.Call;
                callInstruction.operand = match.IsMonths ? (object)applyMonths : applyDays;

                CodeInstruction popInstruction = result[match.PopIndex + 1];
                popInstruction.opcode = OpCodes.Stfld;
                popInstruction.operand = graduationDateField;
            }

            return result;
        }

        private static List<AdjustmentMatch> FindMatches(
            List<CodeInstruction> instructions,
            FieldInfo graduationDateField,
            MethodInfo addMonths,
            MethodInfo addDays)
        {
            List<AdjustmentMatch> matches = new List<AdjustmentMatch>();
            for (int index = 0; index < instructions.Count; index++)
            {
                CodeInstruction fieldInstruction = instructions[index];
                if ((fieldInstruction.opcode != OpCodes.Ldfld &&
                     fieldInstruction.opcode != OpCodes.Ldflda) ||
                    !Equals(fieldInstruction.operand, graduationDateField))
                {
                    continue;
                }

                int searchLimit = Math.Min(instructions.Count, index + 12);
                for (int callIndex = index + 1; callIndex < searchLimit; callIndex++)
                {
                    bool isMonths = instructions[callIndex].Calls(addMonths);
                    bool isDays = instructions[callIndex].Calls(addDays);
                    if (!isMonths && !isDays)
                    {
                        continue;
                    }

                    int popIndex = callIndex + 1;
                    if (popIndex >= instructions.Count ||
                        instructions[popIndex].opcode != OpCodes.Pop)
                    {
                        break;
                    }

                    matches.Add(new AdjustmentMatch
                    {
                        FieldIndex = index,
                        CallIndex = callIndex,
                        PopIndex = popIndex,
                        IsMonths = isMonths
                    });
                    break;
                }
            }

            return matches;
        }

        private static bool TryGetExpectedSiteCount(
            MethodBase original,
            out string methodId,
            out int expected)
        {
            methodId = string.Empty;
            expected = 0;
            if (original == null)
            {
                return false;
            }

            if (original.DeclaringType == typeof(data_girls.girls))
            {
                if (original.Name == nameof(data_girls.girls.Set_Injured))
                {
                    methodId = "data_girls.girls.Set_Injured()";
                    expected = 1;
                    return true;
                }
                if (original.Name == nameof(data_girls.girls.Set_Depressed))
                {
                    methodId = "data_girls.girls.Set_Depressed()";
                    expected = 1;
                    return true;
                }
                if (original.Name == nameof(data_girls.girls.Graduate))
                {
                    methodId = "data_girls.girls.Graduate(bool,string)";
                    expected = 2;
                    return true;
                }
                if (original.Name == nameof(data_girls.girls.Graduation_Date_Update))
                {
                    methodId = "data_girls.girls.Graduation_Date_Update()";
                    expected = 1;
                    return true;
                }
            }

            if (original.DeclaringType == typeof(business) &&
                original.Name == nameof(business.Accept))
            {
                methodId = "business.Accept()";
                expected = 1;
                return true;
            }

            return false;
        }
    }
}
