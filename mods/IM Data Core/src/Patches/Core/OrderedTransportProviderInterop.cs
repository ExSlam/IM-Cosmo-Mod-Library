using System;
using System.Reflection;

namespace IMDataCore
{
    /// <summary>
    /// Reflection-only bridge to the effective ordered vanilla save transport.
    /// Save n Load Fixes is preferred when it is authoritative and healthy;
    /// otherwise a healthy Save Write Ordering Fix provider is used. IMDataCore
    /// keeps no compile-time dependency on either optional companion mod.
    /// </summary>
    internal static class OrderedTransportProviderInterop
    {
        private const string SnlAssemblyName = "com.cosmo.savenloadfixes";
        private const string SnlApiTypeName = "SaveNLoadFixes.SaveTransportApi";
        private const string SwofAssemblyName = "com.cosmo.savewriteorderingfix";
        private const string SwofApiTypeName = "SaveWriteOrderingFix.SaveWriteOrderingApi";

        private const string VersionPropertyName = "Version";
        private const string IsAuthoritativePropertyName = "IsAuthoritativeTransport";
        private const string TransportOwnerPropertyName = "TransportOwner";
        private const string SavedDataHealthPropertyName = "SavedDataInterceptionHealthy";
        private const string EffectiveHealthPropertyName = "EffectiveTransportHealthy";
        private const string AcquireDirectoryMethodName = "TryAcquireExclusiveDirectoryAccess";
        private const int AcquireTimeoutMilliseconds = 30000;

        private static readonly object LookupLock = new object();
        private static ProviderBinding snlBinding;
        private static ProviderBinding swofBinding;

        internal static bool IsSavedDataTransportHealthy()
        {
            ProviderBinding provider;
            return TryResolvePreferredProvider(out provider);
        }

        internal static bool TryAcquireDirectoryLease(
            string absoluteDirectoryPath,
            out IDisposable lease,
            out string errorMessage)
        {
            lease = null;
            errorMessage = string.Empty;

            ProviderBinding provider;
            if (!TryResolvePreferredProvider(out provider))
            {
                // No companion installed means standalone IMDC remains valid. If a
                // known transport assembly is loaded but cannot prove a healthy
                // effective owner, deletion must fail closed: a partially installed
                // provider may still own queued writes for some call sites.
                if (FindLoadedAssembly(SnlAssemblyName) != null ||
                    FindLoadedAssembly(SwofAssemblyName) != null)
                {
                    errorMessage =
                        "A recognized ordered save transport is loaded but no healthy " +
                        "effective provider is available. Save deletion was blocked " +
                        "to avoid racing a partially owned write queue.";
                    return false;
                }

                return true;
            }

            MethodInfo method = provider.AcquireDirectoryMethod;
            if (method == null)
            {
                errorMessage =
                    provider.DisplayName +
                    " is the effective ordered transport but does not expose the required " +
                    "exclusive-directory lease API.";
                return false;
            }

            try
            {
                object[] arguments = new object[]
                {
                    absoluteDirectoryPath,
                    AcquireTimeoutMilliseconds,
                    null,
                    string.Empty
                };
                object result = method.Invoke(null, arguments);
                bool acquired = result is bool && (bool)result;
                lease = arguments[2] as IDisposable;
                errorMessage = arguments[3] as string ?? string.Empty;

                if (!acquired || lease == null)
                {
                    if (string.IsNullOrEmpty(errorMessage))
                    {
                        errorMessage =
                            provider.DisplayName +
                            " did not grant an exclusive directory lease.";
                    }
                    if (lease != null)
                    {
                        lease.Dispose();
                        lease = null;
                    }
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage =
                    provider.DisplayName +
                    " directory coordination failed: " +
                    exception.Message;
                if (lease != null)
                {
                    lease.Dispose();
                    lease = null;
                }
                return false;
            }
        }

        private static bool TryResolvePreferredProvider(out ProviderBinding provider)
        {
            provider = null;

            ProviderBinding candidate = GetSnlBinding();
            bool propertyValue;
            int apiVersion;
            string owner;
            if (candidate != null &&
                TryReadInt(candidate.VersionProperty, out apiVersion) &&
                apiVersion >= 1 &&
                TryReadBoolean(candidate.IsAuthoritativeProperty, out propertyValue) &&
                propertyValue &&
                TryReadString(candidate.TransportOwnerProperty, out owner) &&
                string.Equals(owner, SnlAssemblyName, StringComparison.Ordinal) &&
                TryReadBoolean(candidate.SavedDataHealthProperty, out propertyValue) &&
                propertyValue)
            {
                provider = candidate;
                return true;
            }

            candidate = GetSwofBinding();
            if (candidate == null)
            {
                return false;
            }

            // Current SWOF exposes EffectiveTransportHealthy and TransportOwner.
            // It may legitimately report SNLF as owner because its public API forwards
            // coordination calls to authoritative SNLF. Preserve compatibility with
            // the older SavedData-only cooperation surface when EffectiveTransportHealthy
            // is absent, but require a known owner whenever the current surface is used.
            PropertyInfo healthProperty = candidate.EffectiveHealthProperty ??
                candidate.SavedDataHealthProperty;
            if (!TryReadBoolean(healthProperty, out propertyValue) || !propertyValue)
            {
                return false;
            }

            if (candidate.EffectiveHealthProperty != null)
            {
                if (!TryReadString(candidate.TransportOwnerProperty, out owner) ||
                    (!string.Equals(owner, SwofAssemblyName, StringComparison.Ordinal) &&
                     !string.Equals(owner, SnlAssemblyName, StringComparison.Ordinal)))
                {
                    return false;
                }
            }

            provider = candidate;
            return true;
        }

        private static ProviderBinding GetSnlBinding()
        {
            ProviderBinding binding = snlBinding;
            if (binding != null)
            {
                return binding;
            }

            lock (LookupLock)
            {
                if (snlBinding == null)
                {
                    snlBinding = TryCreateBinding(
                        SnlAssemblyName,
                        SnlApiTypeName,
                        "Save n Load Fixes",
                        true);
                }
                return snlBinding;
            }
        }

        private static ProviderBinding GetSwofBinding()
        {
            ProviderBinding binding = swofBinding;
            if (binding != null)
            {
                return binding;
            }

            lock (LookupLock)
            {
                if (swofBinding == null)
                {
                    swofBinding = TryCreateBinding(
                        SwofAssemblyName,
                        SwofApiTypeName,
                        "Save Write Ordering Fix",
                        false);
                }
                return swofBinding;
            }
        }

        private static ProviderBinding TryCreateBinding(
            string assemblyName,
            string apiTypeName,
            string displayName,
            bool requireAuthorityProperty)
        {
            Assembly assembly = FindLoadedAssembly(assemblyName);
            if (assembly == null)
            {
                return null;
            }

            Type apiType = assembly.GetType(apiTypeName, false);
            if (apiType == null)
            {
                return null;
            }

            PropertyInfo versionProperty = apiType.GetProperty(
                VersionPropertyName,
                BindingFlags.Public | BindingFlags.Static);
            PropertyInfo authorityProperty = GetBooleanProperty(
                apiType,
                IsAuthoritativePropertyName);
            PropertyInfo transportOwnerProperty = apiType.GetProperty(
                TransportOwnerPropertyName,
                BindingFlags.Public | BindingFlags.Static);
            PropertyInfo savedDataHealthProperty = GetBooleanProperty(
                apiType,
                SavedDataHealthPropertyName);
            if ((requireAuthorityProperty &&
                 (versionProperty == null ||
                  versionProperty.PropertyType != typeof(int) ||
                  versionProperty.GetIndexParameters().Length != 0 ||
                  authorityProperty == null ||
                  transportOwnerProperty == null ||
                  transportOwnerProperty.PropertyType != typeof(string) ||
                  transportOwnerProperty.GetIndexParameters().Length != 0)) ||
                savedDataHealthProperty == null)
            {
                return null;
            }

            MethodInfo acquireDirectoryMethod = apiType.GetMethod(
                AcquireDirectoryMethodName,
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[]
                {
                    typeof(string),
                    typeof(int),
                    typeof(IDisposable).MakeByRefType(),
                    typeof(string).MakeByRefType()
                },
                null);

            return new ProviderBinding
            {
                DisplayName = displayName,
                VersionProperty = versionProperty,
                IsAuthoritativeProperty = authorityProperty,
                TransportOwnerProperty = transportOwnerProperty,
                SavedDataHealthProperty = savedDataHealthProperty,
                EffectiveHealthProperty = GetBooleanProperty(
                    apiType,
                    EffectiveHealthPropertyName),
                AcquireDirectoryMethod = acquireDirectoryMethod
            };
        }

        private static Assembly FindLoadedAssembly(string expectedName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                Assembly assembly = assemblies[index];
                AssemblyName name = assembly != null ? assembly.GetName() : null;
                if (name != null && string.Equals(
                        name.Name,
                        expectedName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return assembly;
                }
            }
            return null;
        }

        private static PropertyInfo GetBooleanProperty(Type apiType, string propertyName)
        {
            PropertyInfo property = apiType.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Static);
            return property != null &&
                property.PropertyType == typeof(bool) &&
                property.GetIndexParameters().Length == 0
                ? property
                : null;
        }

        private static bool TryReadInt(
            PropertyInfo property,
            out int value)
        {
            value = 0;
            if (property == null || property.PropertyType != typeof(int))
            {
                return false;
            }

            try
            {
                value = (int)property.GetValue(null, null);
                return true;
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core could not query ordered transport provider API version; " +
                    "using its standalone fallback. " +
                    exception.Message);
                return false;
            }
        }

        private static bool TryReadString(
            PropertyInfo property,
            out string value)
        {
            value = string.Empty;
            if (property == null || property.PropertyType != typeof(string))
            {
                return false;
            }

            try
            {
                value = property.GetValue(null, null) as string ?? string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core could not query ordered transport provider owner; " +
                    "using its standalone fallback. " +
                    exception.Message);
                return false;
            }
        }

        private static bool TryReadBoolean(
            PropertyInfo property,
            out bool value)
        {
            value = false;
            if (property == null)
            {
                return false;
            }

            try
            {
                value = (bool)property.GetValue(null, null);
                return true;
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core could not query ordered transport provider health; " +
                    "using its standalone fallback. " +
                    exception.Message);
                return false;
            }
        }

        private sealed class ProviderBinding
        {
            internal string DisplayName;
            internal PropertyInfo VersionProperty;
            internal PropertyInfo IsAuthoritativeProperty;
            internal PropertyInfo TransportOwnerProperty;
            internal PropertyInfo SavedDataHealthProperty;
            internal PropertyInfo EffectiveHealthProperty;
            internal MethodInfo AcquireDirectoryMethod;
        }
    }
}
