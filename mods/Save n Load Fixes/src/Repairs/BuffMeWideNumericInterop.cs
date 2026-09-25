using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Optional, version-gated compatibility for Vanilas' BuffMe 1.0.0.
    /// The audited mod multiplies positive money and fans through Double and applies
    /// its fan multiplier twice when a resource-level fan award is subsequently
    /// distributed through girls.AddFans(Int64,Nullable). This profile neutralizes
    /// those two numeric prefixes, preserves the configured percentages, widens the
    /// arithmetic, and applies the fan multiplier exactly once per semantic award.
    /// BuffMe's stamina patch remains native and untouched.
    /// </summary>
    internal static class BuffMeWideNumericInterop
    {
        internal const string HarmonyOwner = "com.vanilas.buffme";
        internal const string ExpectedAssemblyName = "com.vanilas.buffme";
        internal const string SupportedAssemblyVersion = "1.0.0.0";
        internal const string SupportedInformationalVersion =
            "1.0.0+843cfec06585addcfdd195ba57f360c66a7c0316";

        private const string MoneyVariable = "Money_Multiplier";
        private const string FanVariable = "Fan_Multiplier";
        private const int DefaultMoneyPercent = 200;
        private const int DefaultFanPercent = 200;
        private const long ExactDoubleIntegerBoundary = 9007199254740992L;

        private static readonly object Sync = new object();
        private static readonly Version SupportedVersion = new Version(1, 0, 0, 0);
        private static bool initialized;
        private static bool profileActive;
        private static string status = "BuffMe not detected.";
        private static long appliedCount;
        private static long duplicateFanCorrectionCount;

        [ThreadStatic]
        private static int resourceMultiplierSuppressionDepth;
        [ThreadStatic]
        private static int resourceFanDistributionDepth;

        internal static bool ProfileActive { get { lock (Sync) return profileActive; } }
        internal static string Status { get { lock (Sync) return status; } }
        internal static long AppliedCount { get { return Interlocked.Read(ref appliedCount); } }
        internal static long DuplicateFanCorrectionCount
        { get { return Interlocked.Read(ref duplicateFanCorrectionCount); } }

        internal static bool IsResourceFanDistribution
        { get { return resourceFanDistributionDepth > 0; } }

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
                    TryInstallProfileSafe(assembly);
            }
            else
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    AssemblyName identity = assembly.GetName();
                    if (identity != null && string.Equals(identity.Name,
                        ExpectedAssemblyName, StringComparison.Ordinal))
                    {
                        TryInstallProfileSafe(assembly);
                        break;
                    }
                }
            }
        }

        internal static void SafeEnsureInitialized()
        {
            try
            {
                EnsureInitialized();
            }
            catch (Exception ex)
            {
                SetStatus(false,
                    "BuffMe optional compatibility bootstrap failed safely: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void TryInstallProfileSafe(Assembly assembly)
        {
            try
            {
                TryInstallProfile(assembly);
            }
            catch (Exception ex)
            {
                SetStatus(false,
                    "BuffMe optional compatibility probe failed safely: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args != null && args.LoadedAssembly != null)
                TryInstallProfileSafe(args.LoadedAssembly);
        }

        private static void TryInstallProfile(Assembly assembly)
        {
            if (assembly == null) return;
            AssemblyName identity = assembly.GetName();
            if (identity == null || !string.Equals(identity.Name,
                ExpectedAssemblyName, StringComparison.Ordinal)) return;

            string informational = GetInformationalVersion(assembly);
            if (identity.Version == null || !identity.Version.Equals(SupportedVersion) ||
                !string.Equals(informational, SupportedInformationalVersion,
                    StringComparison.Ordinal))
            {
                SetStatus(false,
                    "BuffMe detected, but version " +
                    (identity.Version == null ? "<unknown>" : identity.Version.ToString()) +
                    " / " + (string.IsNullOrEmpty(informational)
                        ? "<no informational version>" : informational) +
                    " is not the audited 1.0.0 profile. No BuffMe compatibility override was applied.");
                return;
            }

            Type resourcesPatchType = assembly.GetType("BuffMe.Resources_Add", false);
            Type girlPatchType = assembly.GetType("BuffMe.DataGirls_Girls_AddFans", false);
            Type staminaPatchType = assembly.GetType("BuffMe.DataGirls_Girls_addParam", false);
            Type constantsType = assembly.GetType("BuffMe.BuffMe", false);
            if (resourcesPatchType == null || girlPatchType == null ||
                staminaPatchType == null || constantsType == null)
            {
                SetStatus(false,
                    "BuffMe 1.0.0 was detected, but one or more audited types are missing. " +
                    "No compatibility override was applied.");
                return;
            }

            MethodInfo resourcesPrefix = AccessTools.Method(resourcesPatchType, "Prefix",
                new Type[]
                {
                    typeof(resources.type).MakeByRefType(),
                    typeof(long).MakeByRefType()
                });
            MethodInfo girlPrefix = AccessTools.Method(girlPatchType, "Prefix",
                new Type[]
                {
                    typeof(long).MakeByRefType(),
                    typeof(resources.fanType?).MakeByRefType()
                });
            MethodInfo staminaPrefix = AccessTools.Method(staminaPatchType, "Prefix",
                new Type[]
                {
                    typeof(data_girls._paramType).MakeByRefType(),
                    typeof(float).MakeByRefType(),
                    typeof(bool).MakeByRefType()
                });

            if (!IsStaticBool(resourcesPrefix) || !IsStaticBool(girlPrefix) ||
                !IsStaticBool(staminaPrefix) ||
                !HasExpectedConstants(constantsType))
            {
                SetStatus(false,
                    "BuffMe 1.0.0 was detected, but its audited patch/constant shape no longer " +
                    "matches. No compatibility override was applied.");
                return;
            }

            try
            {
                Harmony harmony = new Harmony(SaveNLoadFixesConstants.HarmonyId);
                PatchBoolPrefixIfNeeded(harmony, resourcesPrefix);
                PatchBoolPrefixIfNeeded(harmony, girlPrefix);
                SetStatus(true,
                    "BuffMe 1.0.0 compatibility profile active: positive money/fans use " +
                    "wide arithmetic and fan rewards are multiplied exactly once; stamina " +
                    "remains under BuffMe's native patch.");
            }
            catch (Exception ex)
            {
                SetStatus(false,
                    "BuffMe 1.0.0 compatibility could not be installed: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static bool HasExpectedConstants(Type constantsType)
        {
            return HasConstString(constantsType, "MONEY_VARID", MoneyVariable) &&
                HasConstString(constantsType, "MONEY_DEFAULT_VAR", "200") &&
                HasConstString(constantsType, "FAN_VARID", FanVariable) &&
                HasConstString(constantsType, "FAN_DEFAULT_VAR", "200") &&
                HasConstString(constantsType, "STAMINA_USAGE_VARID", "Stamina_Usage_Multiplier") &&
                HasConstString(constantsType, "STAMINA_USAGE_DEFAULT_VAR", "50");
        }

        private static bool HasConstString(Type type, string name, string expected)
        {
            FieldInfo field = AccessTools.Field(type, name);
            return field != null && field.IsStatic && field.IsLiteral &&
                field.FieldType == typeof(string) &&
                string.Equals(field.GetRawConstantValue() as string, expected,
                    StringComparison.Ordinal);
        }

        private static void PatchBoolPrefixIfNeeded(Harmony harmony, MethodInfo target)
        {
            Patches existing = Harmony.GetPatchInfo(target);
            bool installed = existing != null && existing.Owners != null &&
                existing.Owners.Contains(SaveNLoadFixesConstants.HarmonyId);
            if (!installed)
            {
                MethodInfo neutralizer = AccessTools.Method(
                    typeof(BuffMeWideNumericInterop), nameof(NeutralizeBuffMeBoolPrefix),
                    new Type[] { typeof(bool).MakeByRefType() });
                if (neutralizer == null)
                    throw new MissingMethodException(typeof(BuffMeWideNumericInterop).FullName,
                        nameof(NeutralizeBuffMeBoolPrefix));
                harmony.Patch(target, prefix: new HarmonyMethod(neutralizer));
            }

            Patches verified = Harmony.GetPatchInfo(target);
            if (verified == null || verified.Owners == null ||
                !verified.Owners.Contains(SaveNLoadFixesConstants.HarmonyId))
                throw new InvalidOperationException(
                    "SNLF could not wrap an audited BuffMe 1.0.0 prefix.");
        }

        // BuffMe's numeric patches are themselves Harmony prefixes returning true.
        // SNLF patches those patch methods, skips their narrow implementation, and
        // returns true on their behalf so the surrounding vanilla Harmony chain remains neutral.
        private static bool NeutralizeBuffMeBoolPrefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        internal static long PreviewResourceDelta(resources.type type, long delta)
        {
            if (!ProfileActive || delta <= 0L || resourceMultiplierSuppressionDepth > 0)
                return delta;
            if (type == resources.type.money)
                return ApplyPercentCompatible(delta, ReadPercent(MoneyVariable,
                    DefaultMoneyPercent), "BuffMe 1.0.0 money multiplier");
            if (type == resources.type.fans)
                return ApplyPercentCompatible(delta, ReadPercent(FanVariable,
                    DefaultFanPercent), "BuffMe 1.0.0 resource fan multiplier");
            return delta;
        }

        internal static long ApplyResourceDelta(resources.type type, long delta)
        {
            long result = PreviewResourceDelta(type, delta);
            if (result != delta && ProfileActive)
                Interlocked.Increment(ref appliedCount);
            return result;
        }

        internal static long PreviewDirectGirlFanDelta(long delta)
        {
            if (!ProfileActive || delta <= 0L || resourceFanDistributionDepth > 0)
                return delta;
            return ApplyPercentCompatible(delta, ReadPercent(FanVariable,
                DefaultFanPercent), "BuffMe 1.0.0 direct idol fan multiplier");
        }

        internal static long ApplyDirectGirlFanDelta(long delta)
        {
            long result = PreviewDirectGirlFanDelta(delta);
            if (result != delta && ProfileActive)
                Interlocked.Increment(ref appliedCount);
            return result;
        }

        internal static void BeginResourceFanDistribution()
        {
            resourceFanDistributionDepth++;
            if (ProfileActive) Interlocked.Increment(ref duplicateFanCorrectionCount);
        }

        internal static void EndResourceFanDistribution()
        {
            if (resourceFanDistributionDepth <= 0)
                throw new InvalidOperationException(
                    "BuffMe resource-fan distribution suppression underflow.");
            resourceFanDistributionDepth--;
        }

        internal static void BeginResourceMultiplierSuppression()
        {
            resourceMultiplierSuppressionDepth++;
        }

        internal static void EndResourceMultiplierSuppression()
        {
            if (resourceMultiplierSuppressionDepth <= 0)
                throw new InvalidOperationException(
                    "BuffMe resource multiplier suppression underflow.");
            resourceMultiplierSuppressionDepth--;
        }

        internal static long CalculateExactResourceCorrection(
            resources.type type,
            long exactSemanticDelta,
            long compatibilitySemanticDelta,
            string context)
        {
            long exactEffective = PreviewResourceDelta(type, exactSemanticDelta);
            long compatibilityEffective = PreviewResourceDelta(type, compatibilitySemanticDelta);
            return WideNumericRepair.Subtract(exactEffective, compatibilityEffective, context);
        }

        private static long ApplyPercentCompatible(long value, int percent, string context)
        {
            if (percent < 0)
                throw new InvalidOperationException("BuffMe multiplier percent was negative.");

            double multiplier = (double)percent / 100.0;
            double product = (double)value * multiplier;
            if (value >= -ExactDoubleIntegerBoundary && value <= ExactDoubleIntegerBoundary &&
                !double.IsNaN(product) && !double.IsInfinity(product) &&
                product >= long.MinValue && product <= long.MaxValue)
                return (long)product;

            return WideNumericMath.TruncateRatio(value, percent, 100L);
        }

        private static int ReadPercent(string variableId, int defaultValue)
        {
            string raw = variables.Get(variableId) ?? defaultValue.ToString(CultureInfo.InvariantCulture);
            int value;
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) &&
                !int.TryParse(raw, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
                throw new FormatException("BuffMe variable '" + variableId +
                    "' was not an integer percentage: " + raw);
            return value;
        }

        private static bool IsStaticBool(MethodInfo method)
        {
            return method != null && method.IsStatic && method.ReturnType == typeof(bool);
        }

        private static string GetInformationalVersion(Assembly assembly)
        {
            object[] attrs = assembly.GetCustomAttributes(
                typeof(AssemblyInformationalVersionAttribute), false);
            if (attrs == null || attrs.Length != 1) return string.Empty;
            AssemblyInformationalVersionAttribute attr =
                attrs[0] as AssemblyInformationalVersionAttribute;
            return attr == null ? string.Empty : attr.InformationalVersion ?? string.Empty;
        }

        private static void SetStatus(bool active, string value)
        {
            lock (Sync)
            {
                profileActive = active;
                status = value ?? string.Empty;
            }
            if (active) Debug.Log(SaveNLoadFixesConstants.LogPrefix + status);
            else Debug.LogWarning(SaveNLoadFixesConstants.LogPrefix + status);
        }
    }
}
