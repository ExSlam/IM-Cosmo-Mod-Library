using System;
using System.Reflection;
using System.Threading;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Optional exact-version compatibility for Shelon's Tweaks & QoL Improvements 1.0.0.
    /// That mod replaces vanilla GeneratePeakAge() output with Random.Range(23,45).
    /// A20 normally repairs legacy missing peakAge deterministically before vanilla load,
    /// so without this profile A20 would bypass Shelon's postfix and synthesize 16-24.
    /// </summary>
    internal static class TweaksNQoLCompatibility
    {
        internal const string ExpectedHarmonyOwner = "im.mod.shelon.tweaksnqol";
        internal const string ExpectedAssemblyName = "im.mod.shelon.tweaksnqol";
        internal const string SupportedAssemblyVersion = "1.0.0.0";
        internal const string SupportedInformationalVersion = "1.0.0";

        private static readonly object Sync = new object();
        private static readonly Version SupportedVersion = new Version(1, 0, 0, 0);
        private static bool initialized;
        private static bool profileActive;
        private static string status = "Shelon's Tweaks & QoL Improvements not detected.";
        private static long appliedPeakAgeMigrationCount;

        internal static bool ProfileActive
        {
            get
            {
                EnsureInitialized();
                lock (Sync) return profileActive;
            }
        }

        internal static string Status
        {
            get
            {
                EnsureInitialized();
                lock (Sync) return status;
            }
        }

        internal static long AppliedPeakAgeMigrationCount
        {
            get { return Interlocked.Read(ref appliedPeakAgeMigrationCount); }
        }

        internal static bool UseTweaksPeakAgeRange
        {
            get { return ProfileActive; }
        }

        internal static void RecordPeakAgeMigration()
        {
            Interlocked.Increment(ref appliedPeakAgeMigrationCount);
        }

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
                    TryActivateProfile(assembly);
            }
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args != null && args.LoadedAssembly != null)
                TryActivateProfile(args.LoadedAssembly);
        }

        private static void TryActivateProfile(Assembly assembly)
        {
            if (assembly == null) return;

            AssemblyName identity = assembly.GetName();
            if (identity == null || !string.Equals(identity.Name,
                ExpectedAssemblyName, StringComparison.Ordinal)) return;

            lock (Sync)
            {
                if (profileActive) return;
            }

            string informational = GetInformationalVersion(assembly);
            if (identity.Version == null || !identity.Version.Equals(SupportedVersion) ||
                !string.Equals(informational, SupportedInformationalVersion,
                    StringComparison.Ordinal))
            {
                SetStatus(false,
                    "Shelon's Tweaks & QoL Improvements detected, but version " +
                    (identity.Version == null ? "<unknown>" : identity.Version.ToString()) +
                    " / " + (string.IsNullOrEmpty(informational)
                        ? "<no informational version>" : informational) +
                    " is not the audited 1.0.0 profile. A20 keeps vanilla peak-age migration behavior.");
                return;
            }

            Type peakType = assembly.GetType("tweaksnqol.Girls_generatePeakAge", false);
            Type tooltipType = assembly.GetType("tweaksnqol.Param", false);
            if (peakType == null || tooltipType == null)
            {
                SetStatus(false,
                    "Shelon's Tweaks & QoL Improvements 1.0.0 was detected, but its audited types are missing. A20 keeps vanilla peak-age migration behavior.");
                return;
            }

            MethodInfo peakPostfix = peakType.GetMethod("Postfix",
                BindingFlags.NonPublic | BindingFlags.Static,
                null, new Type[] { typeof(data_girls.girls) }, null);
            MethodInfo tooltipPostfix = tooltipType.GetMethod("Postfix",
                BindingFlags.Public | BindingFlags.Static,
                null, new Type[]
                {
                    typeof(Shows._param).MakeByRefType(),
                    typeof(string).MakeByRefType()
                }, null);

            if (!IsStaticVoid(peakPostfix) || !IsStaticVoid(tooltipPostfix))
            {
                SetStatus(false,
                    "Shelon's Tweaks & QoL Improvements 1.0.0 was detected, but its audited method shape changed. A20 keeps vanilla peak-age migration behavior.");
                return;
            }

            SetStatus(true,
                "Shelon's Tweaks & QoL Improvements 1.0.0 compatibility profile active: A20 legacy peak-age migration uses the mod's 23-44 range; tooltip behavior remains native and untouched.");
        }

        private static bool IsStaticVoid(MethodInfo method)
        {
            return method != null && method.IsStatic && method.ReturnType == typeof(void);
        }

        private static string GetInformationalVersion(Assembly assembly)
        {
            object[] attrs = assembly.GetCustomAttributes(
                typeof(AssemblyInformationalVersionAttribute), false);
            if (attrs == null || attrs.Length == 0) return string.Empty;
            AssemblyInformationalVersionAttribute attr =
                attrs[0] as AssemblyInformationalVersionAttribute;
            return attr == null ? string.Empty : attr.InformationalVersion;
        }

        private static void SetStatus(bool active, string value)
        {
            lock (Sync)
            {
                profileActive = active;
                status = value ?? string.Empty;
            }
        }
    }
}
