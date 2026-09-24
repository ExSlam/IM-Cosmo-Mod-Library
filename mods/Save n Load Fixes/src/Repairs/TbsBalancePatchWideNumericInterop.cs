using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Optional, version-gated compatibility for TrueBlueSwablu's Balance Patch 1.0.0.
    /// SNLF preserves the audited mod's gameplay formulas/settings while keeping SNLF-owned
    /// wide values out of Single/Int32 narrowing paths. The main-group fan postfix contains
    /// an audited 1.0.0 implementation mistake (it computes fame-scaled mult but applies the
    /// raw MAIN_GROUP_MULT); this profile intentionally applies the computed mult instead.
    /// </summary>
    internal static class TbsBalancePatchWideNumericInterop
    {
        internal const string HarmonyOwner = "com.tbs.balancepatch";
        internal const string ExpectedAssemblyName = "com.tbs.balancepatch";
        internal const string SupportedAssemblyVersion = "1.0.0.0";
        internal const string SupportedInformationalVersion = "1.0.0";

        private static readonly object Sync = new object();
        private static readonly Version SupportedVersion = new Version(1, 0, 0, 0);
        private static bool initialized;
        private static bool profileActive;
        private static string status = "TBS Balance Patch not detected.";
        private static long appliedCount;

        private static MethodInfo getMultMethod;
        private static PropertyInfo proposalScaleProperty;
        private static PropertyInfo proposalMultProperty;
        private static FieldInfo proposalBaseField;
        private static FieldInfo proposalClampField;
        private static PropertyInfo tourScaleProperty;
        private static PropertyInfo tourMultProperty;
        private static FieldInfo tourBaseField;
        private static FieldInfo tourClampField;
        private static PropertyInfo fanScaleProperty;
        private static PropertyInfo mainMultProperty;
        private static FieldInfo mainBaseField;
        private static PropertyInfo sisterMultProperty;
        private static FieldInfo sisterBaseField;
        private static PropertyInfo salaryCoeffProperty;
        private static PropertyInfo salaryStaticProperty;
        private static PropertyInfo fujimotoLoanProperty;

        internal static bool ProfileActive { get { lock (Sync) return profileActive; } }
        internal static string Status { get { lock (Sync) return status; } }
        internal static long AppliedCount { get { return Interlocked.Read(ref appliedCount); } }

        internal static void EnsureInitialized()
        {
            bool scan = false;
            lock (Sync)
            {
                if (!initialized)
                {
                    initialized = true;
                    AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
                    scan = true;
                }
            }
            if (scan)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    TryInstallProfile(assembly);
            }
            else
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    AssemblyName name = assembly.GetName();
                    if (name != null && string.Equals(name.Name, ExpectedAssemblyName, StringComparison.Ordinal))
                    {
                        TryInstallProfile(assembly);
                        break;
                    }
                }
            }
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args != null && args.LoadedAssembly != null) TryInstallProfile(args.LoadedAssembly);
        }

        private static void TryInstallProfile(Assembly assembly)
        {
            if (assembly == null) return;
            AssemblyName name = assembly.GetName();
            if (name == null || !string.Equals(name.Name, ExpectedAssemblyName, StringComparison.Ordinal)) return;
            string informational = GetInformationalVersion(assembly);
            if (name.Version == null || !name.Version.Equals(SupportedVersion) ||
                !string.Equals(informational, SupportedInformationalVersion, StringComparison.Ordinal))
            {
                SetStatus(false, "TBS Balance Patch detected, but version " +
                    (name.Version == null ? "<unknown>" : name.Version.ToString()) + " / " +
                    (string.IsNullOrEmpty(informational) ? "<no informational version>" : informational) +
                    " is not the audited 1.0.0 profile. No compatibility override was applied.");
                return;
            }

            Type generateType = assembly.GetType("BalancePatch.GenerateProposalPatch", false);
            Type mainFansType = assembly.GetType("BalancePatch.Patch_GetFameNewFansBaseCoeff", false);
            Type sisterFansType = assembly.GetType("BalancePatch.Patch_Groups_GetNewFansPerSingle", false);
            Type tourType = assembly.GetType("BalancePatch.AddRevenuePrefixPatch", false);
            Type moneyType = assembly.GetType("BalancePatch.GetMoneyPatch", false);
            Type salaryType = assembly.GetType("BalancePatch.data_girls_girls_GetExpectedSalary_Total", false);
            Type loanType = assembly.GetType("BalancePatch.Loans_GetTotalAvailableAmount_Patch", false);
            Type settingsType = assembly.GetType("BalancePatch.BalancePatch", false);

            if (generateType == null || mainFansType == null || sisterFansType == null ||
                tourType == null || moneyType == null || salaryType == null ||
                loanType == null || settingsType == null)
            {
                SetStatus(false, "TBS Balance Patch 1.0.0 was detected, but one or more audited types are missing. No compatibility override was applied.");
                return;
            }

            MethodInfo generate = AccessTools.Method(generateType, "Postfix", new Type[]
                { typeof(business), typeof(business._data), typeof(staff._staff), typeof(int) });
            MethodInfo mainFans = AccessTools.Method(mainFansType, "Postfix", new Type[]
                { typeof(singles), typeof(float).MakeByRefType() });
            MethodInfo sisterFans = AccessTools.Method(sisterFansType, "Postfix", new Type[]
                { typeof(Groups._group), typeof(resources.fanType), typeof(resources.fanType),
                  typeof(resources.fanType), typeof(int).MakeByRefType() });
            MethodInfo tour = AccessTools.Method(tourType, "Prefix", new Type[]
                { typeof(SEvent_Tour.tour), typeof(int).MakeByRefType() });
            MethodInfo softcap = AccessTools.Method(moneyType, "geometricSoftcap", new Type[]
                { typeof(long), typeof(long), typeof(long) });
            MethodInfo salary = AccessTools.Method(salaryType, "Postfix", new Type[]
                { typeof(long).MakeByRefType(), typeof(data_girls.girls) });
            MethodInfo loan = loanType == null ? null : loanType.GetMethod("Postfix",
                BindingFlags.Static | BindingFlags.NonPublic, null,
                new Type[] { typeof(long).MakeByRefType(), typeof(object[]) }, null);
            MethodInfo getMult = AccessTools.Method(settingsType, "GetMult", new Type[]
                { typeof(bool), typeof(float), typeof(float), typeof(float) });

            if (!IsStaticVoid(generate) || !IsStaticVoid(mainFans) || !IsStaticVoid(sisterFans) ||
                tour == null || !tour.IsStatic || tour.ReturnType != typeof(bool) ||
                softcap == null || !softcap.IsStatic || softcap.ReturnType != typeof(long) ||
                !IsStaticVoid(salary) || !IsStaticVoid(loan) ||
                getMult == null || !getMult.IsStatic || getMult.ReturnType != typeof(float) ||
                !ResolveSettings(settingsType))
            {
                SetStatus(false, "TBS Balance Patch 1.0.0 was detected, but its audited method/settings shape no longer matches. No compatibility override was applied.");
                return;
            }

            getMultMethod = getMult;
            try
            {
                Harmony harmony = new Harmony(SaveNLoadFixesConstants.HarmonyId);
                PatchIfNeeded(harmony, generate, nameof(GenerateProposalPostfixPrefix));
                PatchIfNeeded(harmony, mainFans, nameof(MainGroupPostfixPrefix));
                PatchIfNeeded(harmony, sisterFans, nameof(SisterGroupPostfixPrefix));
                PatchIfNeeded(harmony, tour, nameof(TourPrefixNeutralizer));
                PatchIfNeeded(harmony, softcap, nameof(SoftcapPrefix));
                PatchIfNeeded(harmony, salary, nameof(SalaryPostfixPrefix));
                PatchIfNeeded(harmony, loan, nameof(LoanPostfixNeutralizer));
                SetStatus(true, "TBS Balance Patch 1.0.0 wide-number compatibility profile active.");
            }
            catch (Exception ex)
            {
                SetStatus(false, "TBS Balance Patch 1.0.0 compatibility could not be installed: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static bool ResolveSettings(Type type)
        {
            if (type == null) return false;
            proposalScaleProperty = AccessTools.Property(type, "PROPOSAL_SCALE");
            proposalMultProperty = AccessTools.Property(type, "PROPOSAL_MULT_REV");
            proposalBaseField = AccessTools.Field(type, "PROPOSAL_BASE");
            proposalClampField = AccessTools.Field(type, "PROPOSAL_CLAMP");
            tourScaleProperty = AccessTools.Property(type, "TOUR_SCALE");
            tourMultProperty = AccessTools.Property(type, "TOUR_MULT_REV");
            tourBaseField = AccessTools.Field(type, "TOUR_BASE");
            tourClampField = AccessTools.Field(type, "TOUR_CLAMP");
            fanScaleProperty = AccessTools.Property(type, "FAN_BONUS_SCALE");
            mainMultProperty = AccessTools.Property(type, "MAIN_GROUP_MULT");
            mainBaseField = AccessTools.Field(type, "MAIN_BASE");
            sisterMultProperty = AccessTools.Property(type, "SIST_GROU_MULT");
            sisterBaseField = AccessTools.Field(type, "SIST_BASE");
            salaryCoeffProperty = AccessTools.Property(type, "SALARY_EARLY_COEFF");
            salaryStaticProperty = AccessTools.Property(type, "BP_IDOL_SALARY_STATIC");
            fujimotoLoanProperty = AccessTools.Property(type, "FUJIMOTO_LOAN_DECREASE");
            return IsBoolProperty(proposalScaleProperty) && IsFloatProperty(proposalMultProperty) &&
                IsFloatField(proposalBaseField) && IsFloatField(proposalClampField) &&
                IsBoolProperty(tourScaleProperty) && IsFloatProperty(tourMultProperty) &&
                IsFloatField(tourBaseField) && IsFloatField(tourClampField) &&
                IsBoolProperty(fanScaleProperty) && IsFloatProperty(mainMultProperty) &&
                IsFloatField(mainBaseField) && IsFloatProperty(sisterMultProperty) &&
                IsFloatField(sisterBaseField) && IsFloatProperty(salaryCoeffProperty) &&
                IsFloatProperty(salaryStaticProperty) && IsBoolProperty(fujimotoLoanProperty);
        }

        private static void PatchIfNeeded(Harmony harmony, MethodInfo target, string prefixName)
        {
            Patches patches = Harmony.GetPatchInfo(target);
            if (patches != null && patches.Owners != null &&
                patches.Owners.Contains(SaveNLoadFixesConstants.HarmonyId)) return;
            MethodInfo prefix = AccessTools.Method(typeof(TbsBalancePatchWideNumericInterop), prefixName);
            if (prefix == null) throw new MissingMethodException(typeof(TbsBalancePatchWideNumericInterop).FullName, prefixName);
            harmony.Patch(target, prefix: new HarmonyMethod(prefix));
        }

        private static bool GenerateProposalPostfixPrefix(business __0, business._data __1, staff._staff __2, int __3)
        {
            ApplyProposalMultiplier(__0);
            return false;
        }

        private static bool MainGroupPostfixPrefix(singles __0, ref float __1)
        {
            // Intentional correction of Balance Patch 1.0.0's implementation mix-up:
            // the original computes `mult` but multiplies by raw MAIN_GROUP_MULT.
            __1 *= GetMainGroupMultiplier();
            Interlocked.Increment(ref appliedCount);
            return false;
        }

        private static bool SisterGroupPostfixPrefix(Groups._group __0, resources.fanType __1,
            resources.fanType __2, resources.fanType __3, ref int __4)
        {
            long exact = ApplySisterGroupMultiplier(__4);
            __4 = WideNumericMath.ClampToInt32(exact);
            return false;
        }

        private static bool TourPrefixNeutralizer(SEvent_Tour.tour __0, ref int __1, ref bool __result)
        {
            // SNLF's authoritative tour paths apply the same audited multiplier in Int64.
            __result = true;
            return false;
        }

        private static bool SoftcapPrefix(long __0, long __1, long __2, ref long __result)
        {
            if (__2 <= 0L ||
                (IsSafeSingleInteger(__0) && IsSafeSingleInteger(__1) && IsSafeSingleInteger(__2)))
                return true;
            __result = GeometricSoftcapWide(__0, __1, __2);
            Interlocked.Increment(ref appliedCount);
            return false;
        }

        private static bool SalaryPostfixPrefix(ref long __0, data_girls.girls __1)
        {
            if (__1 == null) return false;
            if (CanUseOriginalSalaryPostfix(__1, __0))
                return true;

            int fame = __1.GetFameLevel();
            long candidate = GetWideSalaryCandidate(__1, fame);
            long expectedSalary = WideNumericContinuation.GetExpectedSalary(__1);
            if (fame > 0 && candidate > expectedSalary && candidate > __0)
                __0 = candidate;
            float staticMultiplier = GetFloat(salaryStaticProperty);
            if (staticMultiplier != 0f)
                __0 = TruncateSingleCompatible(__0, staticMultiplier,
                    "TBS Balance Patch 1.0.0 static idol salary multiplier");
            Interlocked.Increment(ref appliedCount);
            return false;
        }

        private static bool CanUseOriginalSalaryPostfix(data_girls.girls girl, long current)
        {
            if (!IsSafeSingleInteger(current) || girl == null ||
                !IsSafeSingleInteger(girl.Earnings_CurrentMonth))
                return false;
            long aggregate = girl.Earnings_CurrentMonth;
            try
            {
                foreach (long value in girl.Earnings_History)
                {
                    if (!IsSafeSingleInteger(value)) return false;
                    aggregate = checked(aggregate + value);
                }
            }
            catch (OverflowException)
            {
                return false;
            }
            return IsSafeSingleInteger(aggregate) &&
                IsSafeSingleInteger(girl.GetExpectedSalary());
        }

        private static long GetWideSalaryCandidate(data_girls.girls girl, int fame)
        {
            float coefficient = GetFloat(salaryCoeffProperty);
            if (girl.Earnings_History.Count >= 3)
            {
                int count = girl.Earnings_History.Count;
                long total = 0L;
                for (int offset = 1; offset <= 3; offset++)
                    total = WideNumericRepair.Add(total, girl.Earnings_History[count - offset],
                        "TBS Balance Patch 1.0.0 salary average");
                return WideNumericRepair.RoundRatioWithSingleProductsToEven(
                    total, 12L, "TBS Balance Patch 1.0.0 idol salary",
                    coefficient, (float)fame);
            }

            long aggregate = girl.Earnings_CurrentMonth;
            foreach (long value in girl.Earnings_History)
                aggregate = WideNumericRepair.Add(aggregate, value,
                    "TBS Balance Patch 1.0.0 salary average");
            long denominator = WideNumericRepair.Multiply(
                girl.Earnings_History.Count + 1L, 4L,
                "TBS Balance Patch 1.0.0 salary average denominator");
            long roundedAverage = WideNumericRepair.DivideRoundToEven(
                aggregate, denominator,
                "TBS Balance Patch 1.0.0 salary average");
            return WideNumericContinuation.RoundSingleProductCompatible(
                roundedAverage, "TBS Balance Patch 1.0.0 idol salary",
                coefficient, (float)fame);
        }

        private static bool LoanPostfixNeutralizer(ref long __0, object[] __1)
        {
            // CalculateTotalAvailableLoanAmount applies the audited modifier exactly once.
            return false;
        }

        internal static void ApplyProposalMultiplier(business owner)
        {
            if (!ProfileActive || owner == null || owner.ActiveProposal == null) return;
            business._proposal proposal = owner.ActiveProposal;
            long currentPayment = WideNumericContinuation.GetBusinessProposalPayment(proposal);
            float multiplier = GetProposalMultiplier();
            long adjustedBase = WideNumericContinuation.RoundSingleCompatible(
                currentPayment, multiplier, "TBS Balance Patch 1.0.0 business proposal payment");
            WideNumericState.SetBusinessProposalBasePayment(proposal, adjustedBase);
            Interlocked.Increment(ref appliedCount);
        }

        internal static long ApplyTourRevenueMultiplier(long revenue)
        {
            if (!ProfileActive) return revenue;
            Interlocked.Increment(ref appliedCount);
            return WideNumericContinuation.RoundSingleCompatible(
                revenue, GetTourMultiplier(), "TBS Balance Patch 1.0.0 tour revenue");
        }

        internal static long ApplySisterGroupMultiplier(long fans)
        {
            if (!ProfileActive) return fans;
            Interlocked.Increment(ref appliedCount);
            return WideNumericContinuation.RoundSingleCompatible(
                fans, GetSisterGroupMultiplier(), "TBS Balance Patch 1.0.0 sister-group fans");
        }

        internal static long ApplyLoanAvailabilityMultiplier(long amount)
        {
            if (!ProfileActive || !staticVars.IsHard() || !GetBool(fujimotoLoanProperty)) return amount;
            long fame = Math.Max(1, resources.GetFameLevel());
            long divided = amount / fame;
            Interlocked.Increment(ref appliedCount);
            return WideNumericRepair.Multiply(divided, 2L,
                "TBS Balance Patch 1.0.0 hard-mode loan availability");
        }

        private static long GeometricSoftcapWide(long rawValue, long productionCost, long realCap)
        {
            long thresholdBase = WideNumericRepair.Add(realCap, productionCost,
                "TBS Balance Patch 1.0.0 CD softcap threshold");
            if (WideNumericRepair.Subtract(rawValue, productionCost,
                    "TBS Balance Patch 1.0.0 CD softcap comparison") <= realCap)
                return rawValue;

            long result = thresholdBase;
            long remaining = WideNumericRepair.Subtract(rawValue, thresholdBase,
                "TBS Balance Patch 1.0.0 CD softcap remainder");
            long tierBase = realCap;
            long divisor = 2L;
            while (remaining > 0L)
            {
                long width = WideNumericRepair.Multiply(tierBase, divisor,
                    "TBS Balance Patch 1.0.0 CD softcap tier width");
                if (remaining <= width)
                {
                    result = WideNumericRepair.Add(result, remaining / divisor,
                        "TBS Balance Patch 1.0.0 CD softcap partial tier");
                    break;
                }
                result = WideNumericRepair.Add(result, tierBase,
                    "TBS Balance Patch 1.0.0 CD softcap full tier");
                remaining = WideNumericRepair.Subtract(remaining, width,
                    "TBS Balance Patch 1.0.0 CD softcap consume tier");
                tierBase = WideNumericRepair.Multiply(tierBase, 2L,
                    "TBS Balance Patch 1.0.0 CD softcap next tier");
                divisor = WideNumericRepair.Multiply(divisor, 2L,
                    "TBS Balance Patch 1.0.0 CD softcap divisor");
            }
            return result;
        }

        private static long TruncateSingleCompatible(long value, float coefficient, string context)
        {
            float product = (float)value * coefficient;
            if (IsSafeSingleInteger(value) && !float.IsNaN(product) && !float.IsInfinity(product) &&
                product >= -16777216f && product <= 16777216f)
                return (long)product;
            return WideNumericRepair.TruncateSingleProduct(value, context, coefficient);
        }

        private static float GetProposalMultiplier()
        {
            return InvokeGetMult(GetBool(proposalScaleProperty), GetFloat(proposalMultProperty),
                GetFloat(proposalBaseField), GetFloat(proposalClampField));
        }
        private static float GetTourMultiplier()
        {
            return InvokeGetMult(GetBool(tourScaleProperty), GetFloat(tourMultProperty),
                GetFloat(tourBaseField), GetFloat(tourClampField));
        }
        private static float GetMainGroupMultiplier()
        {
            return InvokeGetMult(GetBool(fanScaleProperty), GetFloat(mainMultProperty),
                GetFloat(mainBaseField), 1f);
        }
        private static float GetSisterGroupMultiplier()
        {
            return InvokeGetMult(GetBool(fanScaleProperty), GetFloat(sisterMultProperty),
                GetFloat(sisterBaseField), -1f);
        }
        private static float InvokeGetMult(bool scale, float mult, float initial, float clamp)
        {
            object value = getMultMethod.Invoke(null, new object[] { scale, mult, initial, clamp });
            return (float)value;
        }

        private static bool IsSafeSingleInteger(long value)
        { return value >= -16777216L && value <= 16777216L; }
        private static bool IsStaticVoid(MethodInfo method)
        { return method != null && method.IsStatic && method.ReturnType == typeof(void); }
        private static bool IsFloatProperty(PropertyInfo property)
        { return property != null && property.PropertyType == typeof(float) && property.GetGetMethod() != null; }
        private static bool IsBoolProperty(PropertyInfo property)
        { return property != null && property.PropertyType == typeof(bool) && property.GetGetMethod() != null; }
        private static bool IsFloatField(FieldInfo field)
        { return field != null && field.FieldType == typeof(float) && field.IsStatic; }
        private static float GetFloat(PropertyInfo property) { return (float)property.GetValue(null, null); }
        private static bool GetBool(PropertyInfo property) { return (bool)property.GetValue(null, null); }
        private static float GetFloat(FieldInfo field) { return (float)field.GetValue(null); }

        private static string GetInformationalVersion(Assembly assembly)
        {
            object[] attrs = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
            if (attrs == null || attrs.Length != 1) return string.Empty;
            return ((AssemblyInformationalVersionAttribute)attrs[0]).InformationalVersion ?? string.Empty;
        }
        private static void SetStatus(bool active, string value)
        {
            lock (Sync) { profileActive = active; status = value ?? string.Empty; }
            if (active) Debug.Log(SaveNLoadFixesConstants.LogPrefix + status);
            else Debug.LogWarning(SaveNLoadFixesConstants.LogPrefix + status);
        }
    }
}
