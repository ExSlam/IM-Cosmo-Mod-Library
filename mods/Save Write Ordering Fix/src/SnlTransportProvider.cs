using System;
using System.Reflection;
using UnityEngine;

namespace SaveWriteOrderingFix
{
    /// <summary>
    /// Reflection-only adapter for Save n Load Fixes' embedded ordered transport.
    ///
    /// SWOF must remain independently shippable and therefore has no compile-time
    /// reference to the SNLF assembly. When SNLF reports itself authoritative, SWOF
    /// public coordination APIs forward to that one effective queue owner instead of
    /// consulting their local coordinator. Absence or non-authoritative SNLF leaves
    /// standalone SWOF behavior unchanged.
    /// </summary>
    internal static class SnlTransportProvider
    {
        internal const string ApiTypeName = "SaveNLoadFixes.SaveTransportApi";
        internal const string EmbeddedTransportTypeName =
            "SaveNLoadFixes.Transport.OrderedSaveTransport";

        private static readonly object SyncRoot = new object();

        private static Type apiType;
        private static PropertyInfo versionProperty;
        private static PropertyInfo authoritativeProperty;
        private static PropertyInfo ownerProperty;
        private static PropertyInfo savedDataHealthyProperty;
        private static PropertyInfo globalDataHealthyProperty;
        private static MethodInfo hasPendingWritesMethod;
        private static MethodInfo waitForPendingWritesMethod;
        private static MethodInfo exclusiveFileMethod;
        private static MethodInfo exclusiveDirectoryMethod;
        private static bool bindingFailed;
        private static bool bindingFailureLogged;

        internal static bool IsAuthoritative
        {
            get
            {
                if (!TryEnsureBound())
                {
                    return false;
                }

                int apiVersion;
                bool authoritative;
                string owner;
                if (!TryReadInt(versionProperty, out apiVersion) ||
                    apiVersion < 1 ||
                    !TryReadBool(authoritativeProperty, out authoritative) ||
                    !authoritative ||
                    !TryReadString(ownerProperty, out owner))
                {
                    return false;
                }

                return string.Equals(
                    owner,
                    SaveWriteOrderingConstants.SnlHarmonyId,
                    StringComparison.Ordinal);
            }
        }

        internal static bool TryGetSavedDataHealth(out bool healthy)
        {
            healthy = false;
            if (!IsAuthoritative)
            {
                return false;
            }

            return TryReadBool(savedDataHealthyProperty, out healthy);
        }

        internal static bool TryGetGlobalDataHealth(out bool healthy)
        {
            healthy = false;
            if (!IsAuthoritative)
            {
                return false;
            }

            return TryReadBool(globalDataHealthyProperty, out healthy);
        }

        internal static bool EffectiveTransportHealthy
        {
            get
            {
                bool savedDataHealthy;
                bool globalDataHealthy;
                return IsAuthoritative &&
                    TryGetSavedDataHealth(out savedDataHealthy) &&
                    savedDataHealthy &&
                    TryGetGlobalDataHealth(out globalDataHealthy) &&
                    globalDataHealthy;
            }
        }

        internal static string TransportOwner
        {
            get
            {
                if (!IsAuthoritative)
                {
                    return string.Empty;
                }

                string owner;
                return TryReadString(ownerProperty, out owner)
                    ? owner
                    : string.Empty;
            }
        }

        internal static bool HasPendingWrites(string absoluteSavePath)
        {
            object result;
            string error;
            if (!TryInvoke(
                    hasPendingWritesMethod,
                    new object[] { absoluteSavePath },
                    out result,
                    out error))
            {
                LogDelegationFailure("HasPendingWrites", error);
                return false;
            }

            return result is bool && (bool)result;
        }

        internal static bool TryWaitForPendingWrites(
            string absoluteSavePath,
            int timeoutMilliseconds,
            out string errorMessage)
        {
            object[] args =
            {
                absoluteSavePath,
                timeoutMilliseconds,
                string.Empty
            };

            object result;
            string invokeError;
            if (!TryInvoke(
                    waitForPendingWritesMethod,
                    args,
                    out result,
                    out invokeError))
            {
                errorMessage = BuildDelegationError(
                    "TryWaitForPendingWrites",
                    invokeError);
                return false;
            }

            errorMessage = args[2] as string ?? string.Empty;
            return result is bool && (bool)result;
        }

        internal static bool TryRunExclusiveFileAccess(
            string absoluteSavePath,
            Action fileAction,
            int timeoutMilliseconds,
            out string errorMessage)
        {
            object[] args =
            {
                absoluteSavePath,
                fileAction,
                timeoutMilliseconds,
                string.Empty
            };

            object result;
            string invokeError;
            if (!TryInvoke(
                    exclusiveFileMethod,
                    args,
                    out result,
                    out invokeError))
            {
                errorMessage = BuildDelegationError(
                    "TryRunExclusiveFileAccess",
                    invokeError);
                return false;
            }

            errorMessage = args[3] as string ?? string.Empty;
            return result is bool && (bool)result;
        }

        internal static bool TryAcquireExclusiveDirectoryAccess(
            string absoluteDirectoryPath,
            int timeoutMilliseconds,
            out IDisposable exclusiveAccess,
            out string errorMessage)
        {
            object[] args =
            {
                absoluteDirectoryPath,
                timeoutMilliseconds,
                null,
                string.Empty
            };

            object result;
            string invokeError;
            if (!TryInvoke(
                    exclusiveDirectoryMethod,
                    args,
                    out result,
                    out invokeError))
            {
                exclusiveAccess = null;
                errorMessage = BuildDelegationError(
                    "TryAcquireExclusiveDirectoryAccess",
                    invokeError);
                return false;
            }

            exclusiveAccess = args[2] as IDisposable;
            errorMessage = args[3] as string ?? string.Empty;
            return result is bool && (bool)result;
        }

        /// <summary>
        /// Patch-time recognition deliberately does not depend on runtime provider
        /// activation. Harmony may execute individual transpilers before SNLF has
        /// finished reporting all four health surfaces. The already-rewritten call
        /// shape itself is sufficient proof that this concrete call site is delegated.
        /// </summary>
        internal static bool IsEmbeddedTransportCall(
            MethodInfo calledMethod,
            string expectedMethodName,
            Type returnType,
            params Type[] parameterTypes)
        {
            if (calledMethod == null ||
                calledMethod.DeclaringType == null ||
                !string.Equals(
                    calledMethod.DeclaringType.FullName,
                    EmbeddedTransportTypeName,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    calledMethod.Name,
                    expectedMethodName,
                    StringComparison.Ordinal) ||
                calledMethod.ReturnType != returnType)
            {
                return false;
            }

            ParameterInfo[] parameters = calledMethod.GetParameters();
            if (parameters.Length != parameterTypes.Length)
            {
                return false;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType != parameterTypes[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryEnsureBound()
        {
            lock (SyncRoot)
            {
                if (apiType != null)
                {
                    return true;
                }
                if (bindingFailed)
                {
                    return false;
                }

                Type discovered = FindApiType();
                if (discovered == null)
                {
                    // Do not cache absence permanently. Mod assembly load order can
                    // differ, and a later call should be allowed to discover SNLF.
                    return false;
                }

                try
                {
                    PropertyInfo foundVersion = RequireStaticProperty(
                        discovered,
                        "Version",
                        typeof(int));
                    PropertyInfo foundAuthoritative = RequireStaticProperty(
                        discovered,
                        "IsAuthoritativeTransport",
                        typeof(bool));
                    PropertyInfo foundOwner = RequireStaticProperty(
                        discovered,
                        "TransportOwner",
                        typeof(string));
                    PropertyInfo foundSavedHealth = RequireStaticProperty(
                        discovered,
                        "SavedDataInterceptionHealthy",
                        typeof(bool));
                    PropertyInfo foundGlobalHealth = RequireStaticProperty(
                        discovered,
                        "GlobalDataInterceptionHealthy",
                        typeof(bool));

                    MethodInfo foundHasPending = RequireStaticMethod(
                        discovered,
                        "HasPendingWrites",
                        typeof(bool),
                        new[] { typeof(string) });
                    MethodInfo foundWait = RequireStaticMethod(
                        discovered,
                        "TryWaitForPendingWrites",
                        typeof(bool),
                        new[]
                        {
                            typeof(string),
                            typeof(int),
                            typeof(string).MakeByRefType()
                        });
                    MethodInfo foundFile = RequireStaticMethod(
                        discovered,
                        "TryRunExclusiveFileAccess",
                        typeof(bool),
                        new[]
                        {
                            typeof(string),
                            typeof(Action),
                            typeof(int),
                            typeof(string).MakeByRefType()
                        });
                    MethodInfo foundDirectory = RequireStaticMethod(
                        discovered,
                        "TryAcquireExclusiveDirectoryAccess",
                        typeof(bool),
                        new[]
                        {
                            typeof(string),
                            typeof(int),
                            typeof(IDisposable).MakeByRefType(),
                            typeof(string).MakeByRefType()
                        });

                    versionProperty = foundVersion;
                    authoritativeProperty = foundAuthoritative;
                    ownerProperty = foundOwner;
                    savedDataHealthyProperty = foundSavedHealth;
                    globalDataHealthyProperty = foundGlobalHealth;
                    hasPendingWritesMethod = foundHasPending;
                    waitForPendingWritesMethod = foundWait;
                    exclusiveFileMethod = foundFile;
                    exclusiveDirectoryMethod = foundDirectory;
                    apiType = discovered;
                    return true;
                }
                catch (Exception exception)
                {
                    bindingFailed = true;
                    LogBindingFailure(exception.Message);
                    return false;
                }
            }
        }

        private static Type FindApiType()
        {
            Assembly[] assemblies;
            try
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }
            catch (Exception)
            {
                return null;
            }

            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                if (assembly == null)
                {
                    continue;
                }

                Type type;
                try
                {
                    type = assembly.GetType(ApiTypeName, false);
                }
                catch (Exception)
                {
                    continue;
                }

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static PropertyInfo RequireStaticProperty(
            Type type,
            string propertyName,
            Type propertyType)
        {
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Static);
            if (property == null || property.PropertyType != propertyType ||
                property.GetGetMethod() == null || !property.GetGetMethod().IsStatic)
            {
                throw new MissingMemberException(
                    type.FullName,
                    propertyName);
            }
            return property;
        }

        private static MethodInfo RequireStaticMethod(
            Type type,
            string methodName,
            Type returnType,
            Type[] parameterTypes)
        {
            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static,
                null,
                parameterTypes,
                null);
            if (method == null || method.ReturnType != returnType)
            {
                throw new MissingMethodException(type.FullName, methodName);
            }
            return method;
        }

        private static bool TryReadInt(PropertyInfo property, out int value)
        {
            value = 0;
            try
            {
                object result = property.GetValue(null, null);
                if (!(result is int))
                {
                    return false;
                }
                value = (int)result;
                return true;
            }
            catch (Exception exception)
            {
                LogDelegationFailure(
                    property == null ? "<unknown property>" : property.Name,
                    exception.Message);
                return false;
            }
        }

        private static bool TryReadBool(PropertyInfo property, out bool value)
        {
            value = false;
            try
            {
                object result = property.GetValue(null, null);
                if (!(result is bool))
                {
                    return false;
                }
                value = (bool)result;
                return true;
            }
            catch (Exception exception)
            {
                LogDelegationFailure(
                    property == null ? "<unknown property>" : property.Name,
                    exception.Message);
                return false;
            }
        }

        private static bool TryReadString(PropertyInfo property, out string value)
        {
            value = string.Empty;
            try
            {
                value = property.GetValue(null, null) as string ?? string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                LogDelegationFailure(
                    property == null ? "<unknown property>" : property.Name,
                    exception.Message);
                return false;
            }
        }

        private static bool TryInvoke(
            MethodInfo method,
            object[] arguments,
            out object result,
            out string error)
        {
            result = null;
            error = string.Empty;
            if (method == null)
            {
                error = "SNLF transport method is unavailable.";
                return false;
            }

            try
            {
                result = method.Invoke(null, arguments);
                return true;
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                error = inner.Message;
                return false;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static string BuildDelegationError(string operation, string error)
        {
            return "SNLF transport delegation failed for " + operation + ": " +
                (error ?? string.Empty);
        }

        private static void LogBindingFailure(string error)
        {
            if (bindingFailureLogged)
            {
                return;
            }
            bindingFailureLogged = true;
            Debug.LogWarning(
                SaveWriteOrderingConstants.LogPrefix +
                "SNLF SaveTransportApi was discovered but did not match the required delegation contract: " +
                (error ?? string.Empty));
        }

        private static void LogDelegationFailure(string operation, string error)
        {
            Debug.LogWarning(
                SaveWriteOrderingConstants.LogPrefix +
                BuildDelegationError(operation, error));
        }
    }
}
