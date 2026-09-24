using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Optional, version-gated wide-numeric compatibility for Fans Watch Shows 1.0.0
    /// by TrueBlueSwablu. SNLF deliberately has no compile-time reference to that mod.
    ///
    /// The compatibility profile reproduces the 1.0.0 audience formula and settings,
    /// but keeps scalable fan/audience operands in Int64-safe arithmetic rather than
    /// narrowing them through Single/Mathf.RoundToInt. Future FWS versions are never
    /// assumed compatible until their implementation is audited explicitly.
    /// </summary>
    internal static class FansWatchShowsWideNumericInterop
    {
        internal const string HarmonyOwner = "com.tbs.fanswatch";
        internal const string ExpectedAssemblyName = "com.tbs.fanswatch";
        internal const string PatchTypeName = "FansWatchShows.GetAudiencePatch";
        internal const string SupportedAssemblyVersion = "1.0.0.0";
        internal const string SupportedInformationalVersion = "1.0.0";

        private static readonly object Sync = new object();
        private static readonly Version SupportedVersion = new Version(1, 0, 0, 0);

        private static PropertyInfo casualBaseProperty;
        private static PropertyInfo hardBaseProperty;
        private static PropertyInfo staticMultiplierProperty;
        private static bool initialized;
        private static bool profileActive;
        private static string status = "Fans Watch Shows not detected.";
        private static long appliedCount;

        internal static bool ProfileActive
        {
            get { lock (Sync) return profileActive; }
        }

        internal static string Status
        {
            get { lock (Sync) return status; }
        }

        internal static long AppliedCount
        {
            get { return Interlocked.Read(ref appliedCount); }
        }

        internal static void EnsureInitialized()
        {
            bool shouldScan = false;
            lock (Sync)
            {
                if (!initialized)
                {
                    initialized = true;
                    AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
                    shouldScan = true;
                }
            }

            // Patch processing may run before or after FWS depending on the enabled-mod
            // order. Scan once for already-loaded copies; AssemblyLoad covers later ones.
            if (shouldScan)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    TryInstallProfile(assembly);
            }
            else
            {
                // Harmony Integration can unpatch/reapply SNLF without unloading this
                // assembly. Re-check the known FWS assembly so our same-owner dynamic
                // prefix is restored after an SNLF toggle.
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    AssemblyName identity = assembly.GetName();
                    if (identity != null && string.Equals(identity.Name,
                        ExpectedAssemblyName, StringComparison.Ordinal))
                    {
                        TryInstallProfile(assembly);
                        break;
                    }
                }
            }
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args == null || args.LoadedAssembly == null) return;
            TryInstallProfile(args.LoadedAssembly);
        }

        private static void TryInstallProfile(Assembly assembly)
        {
            if (assembly == null) return;
            AssemblyName identity = assembly.GetName();
            if (identity == null || !string.Equals(identity.Name,
                ExpectedAssemblyName, StringComparison.Ordinal))
                return;

            Version installedVersion = identity.Version;
            string informationalVersion = GetInformationalVersion(assembly);
            if (installedVersion == null || !installedVersion.Equals(SupportedVersion) ||
                !string.Equals(informationalVersion, SupportedInformationalVersion,
                    StringComparison.Ordinal))
            {
                SetStatus(false,
                    "Fans Watch Shows detected, but version " +
                    (installedVersion == null ? "<unknown>" : installedVersion.ToString()) +
                    " / " + (string.IsNullOrEmpty(informationalVersion)
                        ? "<no informational version>"
                        : informationalVersion) +
                    " is not the audited 1.0.0 profile. No FWS compatibility override was applied.");
                return;
            }

            Type patchType = assembly.GetType(PatchTypeName, false);
            MethodInfo postfix = patchType == null ? null : AccessTools.Method(
                patchType,
                "Postfix",
                new Type[]
                {
                    typeof(Shows._show),
                    typeof(int?),
                    typeof(long).MakeByRefType()
                });
            MethodInfo getBonusFans = patchType == null ? null : AccessTools.Method(
                patchType,
                "GetBonusFans",
                new Type[] { typeof(Shows._show) });
            PropertyInfo casual = patchType == null ? null : AccessTools.Property(
                patchType, "CASUAL_BASE");
            PropertyInfo hard = patchType == null ? null : AccessTools.Property(
                patchType, "HARD_BASE");
            PropertyInfo multiplier = patchType == null ? null : AccessTools.Property(
                patchType, "STATIC_MULTIPLIER");

            if (!IsExpectedPostfix(postfix) ||
                getBonusFans == null || getBonusFans.ReturnType != typeof(long) ||
                !IsExpectedFloatProperty(casual) ||
                !IsExpectedFloatProperty(hard) ||
                !IsExpectedFloatProperty(multiplier))
            {
                SetStatus(false,
                    "Fans Watch Shows 1.0.0 was detected, but its GetAudiencePatch shape no " +
                    "longer matches the audited profile. No compatibility override was applied.");
                return;
            }

            lock (Sync)
            {
                casualBaseProperty = casual;
                hardBaseProperty = hard;
                staticMultiplierProperty = multiplier;
            }

            MethodInfo compatibilityPrefix = AccessTools.Method(
                typeof(FansWatchShowsWideNumericInterop),
                nameof(FansWatchShowsPostfixPrefix),
                new Type[]
                {
                    typeof(Shows._show),
                    typeof(int?),
                    typeof(long).MakeByRefType()
                });
            if (compatibilityPrefix == null)
            {
                SetStatus(false,
                    "Fans Watch Shows 1.0.0 was detected, but the SNLF compatibility prefix " +
                    "could not be resolved.");
                return;
            }

            try
            {
                Patches existing = Harmony.GetPatchInfo(postfix);
                bool alreadyInstalled = existing != null && existing.Owners != null &&
                    existing.Owners.Contains(SaveNLoadFixesConstants.HarmonyId);
                if (!alreadyInstalled)
                {
                    Harmony harmony = new Harmony(SaveNLoadFixesConstants.HarmonyId);
                    harmony.Patch(postfix, prefix: new HarmonyMethod(compatibilityPrefix));
                }

                Patches verified = Harmony.GetPatchInfo(postfix);
                bool installed = verified != null && verified.Owners != null &&
                    verified.Owners.Contains(SaveNLoadFixesConstants.HarmonyId);
                if (!installed)
                {
                    SetStatus(false,
                        "Fans Watch Shows 1.0.0 was detected, but its audited postfix could " +
                        "not be wrapped by SNLF.");
                    return;
                }

                SetStatus(true,
                    "Fans Watch Shows 1.0.0 wide-number compatibility profile active.");
            }
            catch (Exception exception)
            {
                SetStatus(false,
                    "Fans Watch Shows 1.0.0 compatibility could not be installed: " +
                    exception.GetType().Name + ": " + exception.Message);
            }
        }

        // This patches the FWS Harmony postfix itself. __0/__1/__2 deliberately bind
        // the original static method's arguments; __instance would mean the patch
        // method instance to Harmony and is therefore not used here.
        private static bool FansWatchShowsPostfixPrefix(
            Shows._show __0,
            int? __1,
            ref long __2)
        {
            ApplySupportedPostfix(__0, __1, ref __2);
            return false;
        }

        private static bool IsExpectedPostfix(MethodInfo method)
        {
            if (method == null || !method.IsStatic || method.ReturnType != typeof(void))
                return false;
            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 3 &&
                parameters[0].ParameterType == typeof(Shows._show) &&
                parameters[1].ParameterType == typeof(int?) &&
                parameters[2].ParameterType == typeof(long).MakeByRefType();
        }

        private static bool IsExpectedFloatProperty(PropertyInfo property)
        {
            MethodInfo getter = property == null ? null : property.GetGetMethod();
            return property != null && property.PropertyType == typeof(float) &&
                getter != null && getter.IsStatic;
        }

        private static string GetInformationalVersion(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(
                typeof(AssemblyInformationalVersionAttribute), false);
            if (attributes == null || attributes.Length != 1) return string.Empty;
            AssemblyInformationalVersionAttribute attribute =
                attributes[0] as AssemblyInformationalVersionAttribute;
            return attribute == null ? string.Empty : attribute.InformationalVersion;
        }

        private static void SetStatus(bool active, string message)
        {
            lock (Sync)
            {
                profileActive = active;
                status = message ?? string.Empty;
                if (!active)
                {
                    casualBaseProperty = null;
                    hardBaseProperty = null;
                    staticMultiplierProperty = null;
                }
            }
        }

        internal static void ApplySupportedPostfix(
            Shows._show show,
            int? episodeNumber,
            ref long audience)
        {
            // FWS 1.0.0 accepts episodeNumber but does not use it. Keep the same surface.
            if (!ProfileActive)
                throw new InvalidOperationException(
                    "Fans Watch Shows compatibility ran without an active audited profile.");
            if (show == null)
                throw new ArgumentNullException(nameof(show));

            float casualBase = ReadProfileFloat(casualBaseProperty, "CASUAL_BASE");
            float hardBase = ReadProfileFloat(hardBaseProperty, "HARD_BASE");
            float staticMultiplier = ReadProfileFloat(
                staticMultiplierProperty, "STATIC_MULTIPLIER");

            long bonusFans = CalculateBonusFans(show, casualBase, hardBase);
            Debug.Log(string.Format(
                "[BalancePatcher] Adding bonus viewers to {0} {1}",
                show.title,
                bonusFans));

            long combinedAudience = WideNumericRepair.Add(
                audience,
                bonusFans,
                "Fans Watch Shows 1.0.0 bonus audience");
            long finalAudience = TruncateFwsProductWideWhenNeeded(
                combinedAudience,
                "Fans Watch Shows 1.0.0 FWS_STATIC audience multiplier",
                staticMultiplier);

            // Commit only after every checked calculation succeeds.
            audience = finalAudience;
            Interlocked.Increment(ref appliedCount);
        }

        private static float ReadProfileFloat(PropertyInfo property, string propertyName)
        {
            if (property == null)
                throw new MissingMemberException(PatchTypeName, propertyName);
            return Convert.ToSingle(property.GetValue(null, null));
        }

        private static long CalculateBonusFans(
            Shows._show show,
            float casualBase,
            float hardBase)
        {
            long total = 0L;
            if (show.castType == Shows._show._castType.entireGroup)
            {
                long fans = resources.GetFansTotal(null);
                total = RoundFwsProductWideWhenNeeded(
                    fans,
                    "Fans Watch Shows 1.0.0 entire-group fan audience",
                    0.1f);
            }
            else
            {
                var cast = show.GetCast();
                foreach (data_girls.girls girl in cast)
                {
                    float casual = casualBase;
                    float hardcore = hardBase;

                    if (cast.Count == 1 && girl.trait == (traits._trait._type)21)
                    {
                        casual += 0.3f;
                        hardcore += 0.3f;
                    }

                    bool tvTrait = false;
                    if (girl.trait == (traits._trait._type)32)
                    {
                        Shows._param._media_type? mediaType = show.medium.media_type;
                        Shows._param._media_type tv = 0;
                        tvTrait = mediaType.GetValueOrDefault() == tv && mediaType != null;
                    }
                    if (tvTrait)
                    {
                        casual += 0.2f;
                        hardcore += 0.2f;
                    }

                    if (girl.trait == (traits._trait._type)31 &&
                        show.medium.media_type.GetValueOrDefault() ==
                            (Shows._param._media_type)2)
                        casual += 0.3f;

                    if (show.medium.media_type.GetValueOrDefault() ==
                        (Shows._param._media_type)2)
                        casual += 0.2f;

                    if (show.medium.media_type.GetValueOrDefault() ==
                        (Shows._param._media_type)1)
                        hardcore += 0.2f;

                    long casualAudience = RoundFwsProductWideWhenNeeded(
                        girl.GetFan_Count(resources.fanType.casual),
                        "Fans Watch Shows 1.0.0 casual fan audience",
                        casual);
                    long hardcoreAudience = RoundFwsProductWideWhenNeeded(
                        girl.GetFan_Count(resources.fanType.hardcore),
                        "Fans Watch Shows 1.0.0 hardcore fan audience",
                        hardcore);
                    long girlAudience = WideNumericRepair.Add(
                        casualAudience,
                        hardcoreAudience,
                        "Fans Watch Shows 1.0.0 per-idol fan audience");
                    total = WideNumericRepair.Add(
                        total,
                        girlAudience,
                        "Fans Watch Shows 1.0.0 accumulated fan audience");
                }
            }

            float fatigueMultiplier = 1f - show.GetFatigue(null) / 100f;
            return RoundFwsProductWideWhenNeeded(
                total,
                "Fans Watch Shows 1.0.0 fatigue-adjusted fan audience",
                fatigueMultiplier);
        }

        private const long MaxExactlyRepresentableSingleInteger = 16777216L;

        /// <summary>
        /// Preserve the exact FWS 1.0.0 Single + Mathf.RoundToInt behavior while the
        /// Int64 operand is exactly representable as Single and the rounded result is
        /// safely inside Int32. Switch to SNLF's exact-Single wide implementation only
        /// when FWS's original narrowing would lose integer information or range.
        /// </summary>
        private static long RoundFwsProductWideWhenNeeded(
            long value,
            string context,
            float coefficient)
        {
            if (value >= -MaxExactlyRepresentableSingleInteger &&
                value <= MaxExactlyRepresentableSingleInteger)
            {
                float scaled = (float)value * coefficient;
                double scaledWide = scaled;
                if (!float.IsNaN(scaled) && !float.IsInfinity(scaled) &&
                    scaledWide >= int.MinValue && scaledWide <= int.MaxValue)
                    return Mathf.RoundToInt(scaled);
            }

            return WideNumericRepair.RoundSingleProductToEven(
                value,
                context,
                coefficient);
        }

        /// <summary>
        /// Preserve FWS 1.0.0's Single multiply + truncating Int64 cast while the
        /// audience is exactly representable as Single. At wider values, avoid the
        /// lossy Int64 -> Single conversion but keep the same truncation direction.
        /// </summary>
        private static long TruncateFwsProductWideWhenNeeded(
            long value,
            string context,
            float coefficient)
        {
            if (value >= -MaxExactlyRepresentableSingleInteger &&
                value <= MaxExactlyRepresentableSingleInteger)
            {
                float scaled = (float)value * coefficient;
                double scaledWide = scaled;
                if (!float.IsNaN(scaled) && !float.IsInfinity(scaled) &&
                    scaledWide >= -9223372036854775808d &&
                    scaledWide < 9223372036854775808d)
                    return (long)scaled;
            }

            return WideNumericRepair.TruncateSingleProduct(
                value,
                context,
                coefficient);
        }
    }
}
