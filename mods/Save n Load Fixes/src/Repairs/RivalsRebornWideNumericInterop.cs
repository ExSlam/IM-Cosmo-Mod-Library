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

        private const int ExpectedSurfaceCountValue = 15;

        private static readonly object Sync = new object();
        private static readonly HashSet<string> Warnings = new HashSet<string>();
        private static readonly HashSet<string> InstalledSurfaces = new HashSet<string>();
        private static readonly HashSet<string> FailedSurfaces = new HashSet<string>();
        private static bool initialized;
        private static bool installInProgress;
        private static bool profileDetected;
        private static bool profileActive;
        private static string status = "Rivals Reborn not detected.";
        private static string lastLoggedStatus = string.Empty;

        internal static bool ProfileDetected
        {
            get { lock (Sync) return profileDetected; }
        }

        internal static bool ProfileActive
        {
            get { lock (Sync) return profileActive; }
        }

        internal static string Status
        {
            get { lock (Sync) return status; }
        }

        internal static int ExpectedSurfaceCount
        {
            get { return ExpectedSurfaceCountValue; }
        }

        internal static int InstalledSurfaceCount
        {
            get { lock (Sync) return InstalledSurfaces.Count; }
        }

        internal static int FailedSurfaceCount
        {
            get { lock (Sync) return FailedSurfaces.Count; }
        }

        /// <summary>
        /// Initializes the optional RR bridge without making RR a PatchAll-time dependency.
        /// The scan handles assemblies that are already present and AssemblyLoad handles
        /// either load order. Absence is the normal, quiet state.
        /// </summary>
        internal static void EnsureInitialized()
        {
            bool scanAll = false;
            lock (Sync)
            {
                if (!initialized)
                {
                    initialized = true;
                    AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
                    scanAll = true;
                }
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                if (scanAll || IsRivalsAssembly(assemblies[index]))
                    TryInstallProfileSafe(assemblies[index]);
            }
        }

        /// <summary>
        /// Patch-discovery-safe wrapper. Optional integration initialization must never
        /// escape into SNLF's assembly-wide Harmony PatchAll operation.
        /// </summary>
        internal static void SafeEnsureInitialized()
        {
            try
            {
                EnsureInitialized();
            }
            catch (Exception ex)
            {
                RecordInstallerFailure(
                    "bootstrap",
                    "Rivals Reborn optional compatibility bootstrap failed safely: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args == null || args.LoadedAssembly == null) return;
            TryInstallProfileSafe(args.LoadedAssembly);
        }

        private static void TryInstallProfileSafe(Assembly assembly)
        {
            try
            {
                TryInstallProfile(assembly);
            }
            catch (Exception ex)
            {
                RecordInstallerFailure(
                    "assembly-load",
                    "Rivals Reborn optional compatibility probe failed safely: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static bool IsRivalsAssembly(Assembly assembly)
        {
            if (assembly == null) return false;
            try
            {
                return assembly.GetType(RosterSimTypeName, false) != null ||
                    assembly.GetType(SpecialLabelsTypeName, false) != null ||
                    assembly.GetType(PortraitsTypeName, false) != null ||
                    assembly.GetType(PoachSimTypeName, false) != null ||
                    assembly.GetType(RivalAwardsTypeName, false) != null ||
                    assembly.GetType(RivalsUiTypeName, false) != null ||
                    assembly.GetType(XRelSimTypeName, false) != null;
            }
            catch
            {
                return false;
            }
        }

        private static void TryInstallProfile(Assembly assembly)
        {
            if (!IsRivalsAssembly(assembly)) return;

            lock (Sync)
            {
                profileDetected = true;
                if (installInProgress) return;
                installInProgress = true;
            }

            try
            {
                Harmony harmony = new Harmony(SaveNLoadFixesConstants.HarmonyId);

                TryPatchTranspiler(harmony, "RosterSim.DampFanGrowth",
                    ResolveOptionalMethod(RosterSimTypeName, "DampFanGrowth", null),
                    nameof(RivalsRebornWideNumericTranspiler.RewriteDampFanGrowth));
                TryPatchTranspiler(harmony, "RosterSim.AdjustSales",
                    ResolveOptionalMethod(RosterSimTypeName, "AdjustSales", null),
                    nameof(RivalsRebornWideNumericTranspiler.RewriteAdjustSales));
                TryPatchTranspiler(harmony, "SpecialLabels.DoAccusation",
                    ResolveOptionalMethod(SpecialLabelsTypeName, "DoAccusation", null),
                    nameof(RivalsRebornWideNumericTranspiler.RewriteDoAccusation));
                TryPatchTranspiler(harmony, "Portraits.ComputeIdolFans",
                    ResolveOptionalMethod(PortraitsTypeName, "ComputeIdolFans", null),
                    nameof(RivalsRebornWideNumericTranspiler.RewriteComputeIdolFans));
                TryPatchTranspiler(harmony, "Portraits.RefreshFans",
                    ResolveOptionalMethod(PortraitsTypeName, "RefreshFans", null),
                    nameof(RivalsRebornWideNumericTranspiler.RewriteRefreshFans));
                TryPatchTranspiler(harmony, "SpecialLabels.FoundFromRetiree",
                    ResolveOptionalMethod(SpecialLabelsTypeName, "FoundFromRetiree",
                        new Type[] { typeof(data_girls.girls) }),
                    nameof(RivalsRebornWideNumericTranspiler.RewriteFoundFromRetiree));
                TryPatchTranspiler(harmony, "PoachSim.PickTarget",
                    ResolveOptionalMethod(PoachSimTypeName, "PickTarget", Type.EmptyTypes),
                    nameof(RivalsRebornWideNumericTranspiler.RewritePickTarget));

                Type labelType = AccessTools.TypeByName(RLabelTypeName);
                MethodInfo buyout = labelType == null ? null : ResolveOptionalMethod(
                    PoachSimTypeName,
                    "BuyoutPrice",
                    new Type[] { labelType, typeof(data_girls.girls) });
                TryPatchTranspiler(harmony, "PoachSim.BuyoutPrice", buyout,
                    nameof(RivalsRebornWideNumericTranspiler.RewriteBuyoutPrice));

                TryPatchTranspiler(harmony, "PoachSim.ResolveTempt",
                    ResolveOptionalMethod(PoachSimTypeName, "ResolveTempt",
                        new Type[] { typeof(string) }),
                    nameof(RivalsRebornWideNumericTranspiler.RewriteResolveTempt));
                TryPatchTranspiler(harmony, "SpecialLabels.QueueFounderRoll",
                    ResolveOptionalMethod(SpecialLabelsTypeName, "QueueFounderRoll",
                        new Type[] { typeof(data_girls.girls) }),
                    nameof(RivalsRebornWideNumericTranspiler.RewriteQueueFounderRoll));
                TryPatchTranspiler(harmony, "RosterSim.FromPlayerGirl",
                    ResolveOptionalMethod(RosterSimTypeName, "FromPlayerGirl",
                        new Type[] { typeof(data_girls.girls) }),
                    nameof(RivalsRebornWideNumericTranspiler.RewriteFromPlayerGirl));
                TryPatchTranspiler(harmony, "XRelSim.ResolveDatingChoice",
                    ResolveOptionalMethod(XRelSimTypeName, "ResolveDatingChoice",
                        new Type[] { typeof(string) }),
                    nameof(RivalsRebornWideNumericTranspiler.RewriteResolveDatingChoice));
                TryPatchTranspiler(harmony, "RivalAwards.PickLabel",
                    ResolveOptionalMethod(RivalAwardsTypeName, "PickLabel", null),
                    nameof(RivalsRebornWideNumericTranspiler.RewriteRivalAwardsPickLabel));
                TryPatchPrefix(harmony, "RivalsUI.FormatFans",
                    ResolveOptionalMethod(RivalsUiTypeName, "FormatFans",
                        new Type[] { typeof(long) }),
                    nameof(FormatFansPrefix));
                TryPatchPrefix(harmony, "RosterSim.YearlyShakeout",
                    ResolveOptionalMethod(RosterSimTypeName, "YearlyShakeout",
                        new Type[] { typeof(DateTime) }),
                    nameof(YearlyShakeoutPrefix));
            }
            finally
            {
                lock (Sync) installInProgress = false;
                RefreshStatus();
            }
        }

        private static MethodInfo ResolveOptionalMethod(
            string typeName,
            string methodName,
            Type[] parameters)
        {
            Type type = AccessTools.TypeByName(typeName);
            if (type == null) return null;
            return parameters == null
                ? AccessTools.Method(type, methodName)
                : AccessTools.Method(type, methodName, parameters);
        }

        private static void TryPatchTranspiler(
            Harmony harmony,
            string surface,
            MethodInfo target,
            string transpilerName)
        {
            MethodInfo patchMethod = AccessTools.Method(
                typeof(RivalsRebornWideNumericTranspiler), transpilerName);
            TryPatch(harmony, surface, target, patchMethod, false);
        }

        private static void TryPatchPrefix(
            Harmony harmony,
            string surface,
            MethodInfo target,
            string prefixName)
        {
            MethodInfo patchMethod = AccessTools.Method(
                typeof(RivalsRebornWideNumericInterop), prefixName);
            TryPatch(harmony, surface, target, patchMethod, true);
        }

        private static void TryPatch(
            Harmony harmony,
            string surface,
            MethodInfo target,
            MethodInfo patchMethod,
            bool prefix)
        {
            if (target == null)
            {
                MarkSurfaceFailure(surface,
                    "Rivals Reborn compatibility target " + surface +
                    " was not found; only this optional surface was skipped.");
                return;
            }
            if (patchMethod == null)
            {
                MarkSurfaceFailure(surface,
                    "SNLF compatibility method for " + surface +
                    " was not found; only this optional surface was skipped.");
                return;
            }

            if (HasSnlfPatch(target))
            {
                MarkSurfaceInstalled(surface);
                return;
            }

            try
            {
                HarmonyMethod method = new HarmonyMethod(patchMethod);
                method.priority = Priority.Last;
                method.after = new string[] { Owner };
                if (prefix)
                    harmony.Patch(target, prefix: method);
                else
                    harmony.Patch(target, transpiler: method);

                if (!HasSnlfPatch(target))
                    throw new InvalidOperationException(
                        "Harmony did not report the SNLF owner after patching.");
                MarkSurfaceInstalled(surface);
            }
            catch (Exception ex)
            {
                MarkSurfaceFailure(surface,
                    "Rivals Reborn optional compatibility surface " + surface +
                    " failed safely: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static bool HasSnlfPatch(MethodBase target)
        {
            Patches patches = Harmony.GetPatchInfo(target);
            if (patches == null || patches.Owners == null) return false;
            foreach (string owner in patches.Owners)
            {
                if (string.Equals(owner, SaveNLoadFixesConstants.HarmonyId,
                    StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static void MarkSurfaceInstalled(string surface)
        {
            lock (Sync)
            {
                InstalledSurfaces.Add(surface);
                FailedSurfaces.Remove(surface);
            }
        }

        private static void MarkSurfaceFailure(string surface, string message)
        {
            lock (Sync)
            {
                InstalledSurfaces.Remove(surface);
                FailedSurfaces.Add(surface);
            }
            WarnOnce("install:" + surface, message);
        }

        private static void RecordInstallerFailure(string key, string message)
        {
            lock (Sync)
            {
                profileActive = false;
                status = message;
            }
            WarnOnce("installer:" + key, message);
        }

        private static void RefreshStatus()
        {
            string next;
            bool active;
            bool detected;
            int installed;
            int failed;
            lock (Sync)
            {
                detected = profileDetected;
                installed = InstalledSurfaces.Count;
                failed = FailedSurfaces.Count;
                active = detected && installed == ExpectedSurfaceCountValue && failed == 0;
                profileActive = active;
                if (!detected)
                    next = "Rivals Reborn not detected.";
                else if (active)
                    next = "Rivals Reborn wide-number compatibility active: " +
                        installed + "/" + ExpectedSurfaceCountValue + " optional surfaces installed.";
                else
                    next = "Rivals Reborn detected; optional compatibility is degraded (" +
                        installed + "/" + ExpectedSurfaceCountValue + " surfaces installed, " +
                        failed + " unresolved). Core SNLF remains active.";
                status = next;
                if (string.Equals(lastLoggedStatus, next, StringComparison.Ordinal)) return;
                lastLoggedStatus = next;
            }

            if (detected)
            {
                if (active) Debug.Log(SaveNLoadFixesConstants.LogPrefix + next);
                else Debug.LogWarning(SaveNLoadFixesConstants.LogPrefix + next);
            }
        }

        private static bool FormatFansPrefix(long fans, ref string __result)
        {
            __result = FormatFansWide(fans);
            return false;
        }

        private static bool YearlyShakeoutPrefix(DateTime now)
        {
            return !TryRunYearlyShakeout(now);
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
