using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Optional Rivals Reborn bridge for A33 wide-numeric continuity. This type has no
    /// compile-time dependency on rivalsreborn.dll. Cumulative stages cover authoritative
    /// rival fan growth, rival single sales, accusation growth, rival-idol fan allocation,
    /// exact player-idol fan bridging for RR consumers, poach arithmetic, exact yearly
    /// shakeout ordering, wide award-score input, exact wide-fan UI formatting, and
    /// elimination of remaining stable gameplay reads of RR's capped player-idol fan mirror
    /// while preserving RR's own RNG/settings and gameplay decisions.
    /// </summary>
    internal static class RivalsRebornWideNumericInterop
    {
        internal const string Owner = "rivalsreborn";
        private const string RosterSimTypeName = "RivalsReborn.RosterSim";
        private const string SpecialLabelsTypeName = "RivalsReborn.SpecialLabels";
        private const string PortraitsTypeName = "RivalsReborn.Portraits";
        private const string PoachSimTypeName = "RivalsReborn.PoachSim";
        private const string RivalAwardsTypeName = "RivalsReborn.RivalAwards";
        private const string RivalsUiTypeName = "RivalsReborn.RivalsUI";
        private const string XRelSimTypeName = "RivalsReborn.XRelSim";
        private const string RrTypeName = "RivalsReborn.RR";
        private const string RrStateTypeName = "RivalsReborn.RRState";
        private const string RLabelTypeName = "RivalsReborn.RLabel";
        private const string NewsTypeName = "RivalsReborn.News";

        private static readonly object Sync = new object();
        private static readonly HashSet<string> Warnings = new HashSet<string>();

        internal static IEnumerable<MethodBase> ResolveRosterMethod(string name, Type[] parameters)
        {
            return ResolveMethod(RosterSimTypeName, "RosterSim", name, parameters);
        }

        internal static IEnumerable<MethodBase> ResolveSpecialLabelsMethod(string name, Type[] parameters)
        {
            return ResolveMethod(SpecialLabelsTypeName, "SpecialLabels", name, parameters);
        }

        internal static IEnumerable<MethodBase> ResolvePortraitsMethod(string name, Type[] parameters)
        {
            return ResolveMethod(PortraitsTypeName, "Portraits", name, parameters);
        }

        internal static IEnumerable<MethodBase> ResolvePoachMethod(string name, Type[] parameters)
        {
            return ResolveMethod(PoachSimTypeName, "PoachSim", name, parameters);
        }

        internal static IEnumerable<MethodBase> ResolveRivalAwardsMethod(string name, Type[] parameters)
        {
            return ResolveMethod(RivalAwardsTypeName, "RivalAwards", name, parameters);
        }

        internal static IEnumerable<MethodBase> ResolveRivalsUiMethod(string name, Type[] parameters)
        {
            return ResolveMethod(RivalsUiTypeName, "RivalsUI", name, parameters);
        }

        internal static IEnumerable<MethodBase> ResolveXRelMethod(string name, Type[] parameters)
        {
            return ResolveMethod(XRelSimTypeName, "XRelSim", name, parameters);
        }

        private static IEnumerable<MethodBase> ResolveMethod(
            string typeName,
            string displayType,
            string name,
            Type[] parameters)
        {
            Type type = AccessTools.TypeByName(typeName);
            if (type == null)
            {
                yield break;
            }

            MethodInfo method = parameters == null
                ? AccessTools.Method(type, name)
                : AccessTools.Method(type, name, parameters);
            if (method == null)
            {
                string qualified = displayType + "." + name;
                WarnOnce(qualified, "Rivals Reborn wide-numeric target " + qualified +
                    " was not found; this compatibility patch was skipped.");
                yield break;
            }

            yield return method;
        }

        internal static long GetExactFanTotal(data_girls.girls girl)
        {
            return girl == null ? 0L : girl.GetFans_Total(null);
        }

        internal static long TruncateProduct(long value, float coefficient)
        {
            return WideNumericRepair.TruncateSingleProduct(
                value,
                "Rivals Reborn wide numeric product",
                coefficient);
        }

        internal static long TruncateProductAtLeast(float minimum, long value, float coefficient)
        {
            long product = WideNumericRepair.TruncateSingleProduct(
                value,
                "Rivals Reborn wide numeric product with minimum",
                coefficient);
            long minimumLong = checked((long)minimum);
            return product < minimumLong ? minimumLong : product;
        }

        internal static long CheckedMultiply(long left, long right)
        {
            return WideNumericRepair.Multiply(
                left,
                right,
                "Rivals Reborn checked wide multiplication");
        }

        internal static long CheckedAdd(long left, long right)
        {
            return WideNumericRepair.Add(
                left,
                right,
                "Rivals Reborn checked wide addition");
        }


        /// <summary>
        /// Keeps RR's original Single-domain award score behavior while the fan count is
        /// exactly representable as Single. Once the fan total enters SNLF's widened domain,
        /// take the logarithm from the Int64 value instead of first discarding low fan bits.
        /// The returned score remains Single because RR's award system is intrinsically a
        /// float score (momentum + random tie noise), not authoritative fan state.
        /// </summary>
        internal static float Log10FansForAwardScore(long fans)
        {
            const long ExactSingleIntegerLimit = 16777216L;
            if (fans <= ExactSingleIntegerLimit)
            {
                return Mathf.Log(Mathf.Max(10f, (float)fans), 10f);
            }

            return (float)Math.Log10(Math.Max(10.0, (double)fans));
        }

        /// <summary>
        /// RR's UI originally converts Int64 fans to Single before formatting millions or
        /// thousands. Decimal keeps every Int64 fan count exact through the display division.
        /// </summary>
        internal static string FormatFansWide(long fans)
        {
            if (fans >= 1000000L)
            {
                return ((decimal)fans / 1000000m).ToString("0.0") + "M";
            }
            if (fans >= 1000L)
            {
                return ((decimal)fans / 1000m).ToString("0.#") + "K";
            }
            return fans.ToString();
        }

        internal static int CompareShakeoutScores(
            long leftFans,
            float leftMomentum,
            bool leftFormerIdol,
            long rightFans,
            float rightMomentum,
            bool rightFormerIdol)
        {
            return WideNumericMath.CompareSingleProducts(
                leftFans,
                leftMomentum,
                leftFormerIdol,
                rightFans,
                rightMomentum,
                rightFormerIdol);
        }

        /// <summary>
        /// Reimplements only RR HEAD's yearly "weakest label" selection with an exact
        /// comparison of Fans * Momentum * optional 2.5f. All actual disband/state/news
        /// mutations are delegated back to RR/game methods and fields.
        ///
        /// Returns true when SNLF handled the call and the original RR method should be
        /// skipped. Returns false when the RR reflection surface is unavailable, allowing
        /// Harmony to fall back to RR's own implementation rather than guessing.
        /// </summary>
        internal static bool TryRunYearlyShakeout(DateTime now)
        {
            if (now.Month != 1)
            {
                return true;
            }

            Type rrType = AccessTools.TypeByName(RrTypeName);
            Type stateType = AccessTools.TypeByName(RrStateTypeName);
            Type labelType = AccessTools.TypeByName(RLabelTypeName);
            Type rosterType = AccessTools.TypeByName(RosterSimTypeName);
            Type newsType = AccessTools.TypeByName(NewsTypeName);
            if (rrType == null || stateType == null || labelType == null || rosterType == null ||
                newsType == null)
            {
                WarnOnce(
                    "YearlyShakeout:types",
                    "Rivals Reborn yearly-shakeout compatibility surface is unavailable; " +
                    "RR's original comparison was left in place.");
                return false;
            }

            FieldInfo stateField = AccessTools.Field(rrType, "State");
            FieldInfo labelsField = AccessTools.Field(stateType, "Labels");
            FieldInfo groupIdField = AccessTools.Field(labelType, "GroupId");
            FieldInfo nameField = AccessTools.Field(labelType, "Name");
            FieldInfo momentumField = AccessTools.Field(labelType, "Momentum");
            FieldInfo disbandedField = AccessTools.Field(labelType, "Disbanded");
            FieldInfo formerField = AccessTools.Field(labelType, "IsFormerIdol");
            FieldInfo specialField = AccessTools.Field(labelType, "IsSpecial");
            FieldInfo foundedDayField = AccessTools.Field(labelType, "FoundedDay");
            MethodInfo getVanillaGroup = AccessTools.Method(
                rrType,
                "GetVanillaGroup",
                new Type[] { typeof(int) });
            MethodInfo queueNews = AccessTools.Method(
                rrType,
                "QueueNews",
                new Type[] { typeof(string) });
            MethodInfo disbandLabel = AccessTools.Method(
                rosterType,
                "DisbandLabel",
                new Type[] { labelType });
            MethodInfo postSns = AccessTools.Method(
                newsType,
                "PostSNS",
                new Type[] { typeof(string) });

            if (stateField == null || labelsField == null || groupIdField == null ||
                nameField == null || momentumField == null || disbandedField == null ||
                formerField == null || specialField == null || foundedDayField == null ||
                getVanillaGroup == null || queueNews == null || disbandLabel == null ||
                postSns == null)
            {
                WarnOnce(
                    "YearlyShakeout:members",
                    "Rivals Reborn yearly-shakeout compatibility members no longer match " +
                    "the expected HEAD surface; RR's original comparison was left in place.");
                return false;
            }

            bool mutationStarted = false;
            try
            {
                object state = stateField.GetValue(null);
                IList labels = state == null ? null : labelsField.GetValue(state) as IList;
                if (labels == null)
                {
                    return false;
                }
                if (labels.Count <= 10)
                {
                    return true;
                }

                int today = (int)(staticVars.dateTime - new DateTime(2000, 1, 1)).TotalDays;
                object selectedLabel = null;
                Rivals._group selectedGroup = null;
                long selectedFans = 0L;
                float selectedMomentum = 0f;
                bool selectedFormer = false;

                for (int index = 0; index < labels.Count; index++)
                {
                    object candidate = labels[index];
                    if (candidate == null)
                    {
                        continue;
                    }

                    int groupId = (int)groupIdField.GetValue(candidate);
                    Rivals._group group =
                        getVanillaGroup.Invoke(null, new object[] { groupId }) as Rivals._group;
                    bool disbanded = (bool)disbandedField.GetValue(candidate);
                    bool isSpecial = (bool)specialField.GetValue(candidate);
                    bool isFormer = (bool)formerField.GetValue(candidate);
                    int foundedDay = (int)foundedDayField.GetValue(candidate);

                    if (group == null || disbanded || isSpecial ||
                        (isFormer && today - foundedDay < 730))
                    {
                        continue;
                    }

                    float momentum = (float)momentumField.GetValue(candidate);
                    if (selectedLabel == null ||
                        CompareShakeoutScores(
                            group.Fans,
                            momentum,
                            isFormer,
                            selectedFans,
                            selectedMomentum,
                            selectedFormer) < 0)
                    {
                        selectedLabel = candidate;
                        selectedGroup = group;
                        selectedFans = group.Fans;
                        selectedMomentum = momentum;
                        selectedFormer = isFormer;
                    }
                }

                if (selectedLabel == null)
                {
                    return true;
                }

                string displayName = selectedGroup != null
                    ? selectedGroup.GetGroupName()
                    : (string)nameField.GetValue(selectedLabel);

                mutationStarted = true;
                disbandLabel.Invoke(null, new object[] { selectedLabel });
                if (selectedGroup != null)
                {
                    selectedGroup.IsDead = true;
                }
                labels.Remove(selectedLabel);
                queueNews.Invoke(
                    null,
                    new object[] { displayName + " has closed its doors after a difficult year." });
                postSns.Invoke(
                    null,
                    new object[] { displayName + " shutting down... end of an era honestly" });
                return true;
            }
            catch (Exception exception)
            {
                WarnOnce(
                    "YearlyShakeout:runtime",
                    "Rivals Reborn exact yearly-shakeout compatibility path failed: " +
                    exception.GetType().Name + ": " + exception.Message +
                    (mutationStarted
                        ? ". The original RR method was suppressed because mutation had started."
                        : ". Falling back to RR's original method."));
                return mutationStarted;
            }
        }

        internal static void WarnShape(string methodName, int expected, int actual)
        {
            WarnOnce(
                methodName + ":shape",
                "Rivals Reborn wide-numeric target " + methodName +
                " no longer matches the expected HEAD IL shape (expected " + expected +
                " rewrite sites, found " + actual + "); the method was left unchanged.");
        }

        internal static void WarnDetail(string key, string message)
        {
            WarnOnce(key, message);
        }

        private static void WarnOnce(string key, string message)
        {
            lock (Sync)
            {
                if (!Warnings.Add(key)) return;
            }
            Debug.LogWarning("[Save n Load Fixes] " + message);
        }
    }
}
