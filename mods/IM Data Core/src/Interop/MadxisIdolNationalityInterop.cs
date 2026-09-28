using System;
using System.Reflection;
using HarmonyLib;

namespace IMDataCore
{
    /// <summary>
    /// Optional, reflection-only bridge for Madxis' current Idol Nationality + Name Editor.
    /// IM Data Core deliberately has no compile-time dependency on that mod.  The exact
    /// head-version mutation methods are patched only when its assembly is present.
    /// </summary>
    internal static class MadxisIdolNationalityInterop
    {
        private const string TargetAssemblyName = "com.madxis.idolnationality";
        private const string ControllerTypeName = "MadxisIdolNationality.NationalityProfileUIController";
        private const string RuntimeTypeName = "MadxisIdolNationality.NationalityRuntime";
        private const string CatalogTypeName = "MadxisIdolNationality.NationalityCatalog";
        private const string SaveEditedNameMethodName = "SaveEditedName";
        private const string SelectNationalityMethodName = "SelectNationality";
        private const string PopupFieldName = "popup";
        private const string GetNationalityCodeMethodName = "GetNationalityCode";
        private const string FormatFullNameMethodName = "FormatFullName";
        private const string GetDisplayNameMethodName = "GetDisplayName";
        private const string HarmonyId = "com.cosmo.imdatacore.interop.madxis.idolnationality";

        private static readonly object Sync = new object();
        private static Harmony harmony;
        private static bool assemblyLoadSubscribed;
        private static bool namePatchInstalled;
        private static bool nationalityPatchInstalled;
        private static FieldInfo popupField;
        private static MethodInfo getNationalityCodeMethod;
        private static MethodInfo formatFullNameMethod;
        private static MethodInfo getDisplayNameMethod;

        internal static void EnsureInstalled()
        {
            lock (Sync)
            {
                if (!assemblyLoadSubscribed)
                {
                    AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
                    assemblyLoadSubscribed = true;
                }

                TryInstallLocked();
            }
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            Assembly loaded = args != null ? args.LoadedAssembly : null;
            if (loaded == null ||
                !string.Equals(loaded.GetName().Name, TargetAssemblyName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                EnsureInstalled();
            }
            catch (Exception exception)
            {
                CoreLog.Warn("Madxis identity-history bridge could not install after assembly load: " + exception.Message);
            }
        }

        private static void TryInstallLocked()
        {
            if (namePatchInstalled && nationalityPatchInstalled)
            {
                return;
            }

            Assembly targetAssembly = FindTargetAssembly();
            if (targetAssembly == null)
            {
                return;
            }

            Type controllerType = targetAssembly.GetType(ControllerTypeName, false);
            Type runtimeType = targetAssembly.GetType(RuntimeTypeName, false);
            Type catalogType = targetAssembly.GetType(CatalogTypeName, false);
            if (controllerType == null || runtimeType == null || catalogType == null)
            {
                CoreLog.Warn("Madxis identity-history bridge found the mod assembly but not its current editor/runtime types.");
                return;
            }

            popupField = controllerType.GetField(PopupFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            getNationalityCodeMethod = runtimeType.GetMethod(
                GetNationalityCodeMethodName,
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(int) },
                null);
            formatFullNameMethod = runtimeType.GetMethod(
                FormatFullNameMethodName,
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(string), typeof(string), typeof(string) },
                null);
            getDisplayNameMethod = catalogType.GetMethod(
                GetDisplayNameMethodName,
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(string) },
                null);

            if (popupField == null || getNationalityCodeMethod == null ||
                formatFullNameMethod == null || getDisplayNameMethod == null)
            {
                CoreLog.Warn("Madxis identity-history bridge found the mod but its current reflection contract is incomplete.");
                return;
            }

            if (harmony == null)
            {
                harmony = new Harmony(HarmonyId);
            }

            if (!namePatchInstalled)
            {
                MethodInfo target = controllerType.GetMethod(
                    SaveEditedNameMethodName,
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null);
                if (target != null)
                {
                    harmony.Patch(
                        target,
                        new HarmonyMethod(typeof(MadxisIdolNationalityInterop), nameof(SaveEditedNamePrefix)),
                        new HarmonyMethod(typeof(MadxisIdolNationalityInterop), nameof(SaveEditedNamePostfix)));
                    namePatchInstalled = true;
                    CoreLog.Info("Madxis Idol Nationality + Name Editor rename history capture enabled.");
                }
                else
                {
                    CoreLog.Warn("Madxis rename history capture skipped: current SaveEditedName method was not found.");
                }
            }

            if (!nationalityPatchInstalled)
            {
                MethodInfo target = controllerType.GetMethod(
                    SelectNationalityMethodName,
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(int), typeof(string), typeof(string) },
                    null);
                if (target != null)
                {
                    harmony.Patch(
                        target,
                        new HarmonyMethod(typeof(MadxisIdolNationalityInterop), nameof(SelectNationalityPrefix)),
                        new HarmonyMethod(typeof(MadxisIdolNationalityInterop), nameof(SelectNationalityPostfix)));
                    nationalityPatchInstalled = true;
                    CoreLog.Info("Madxis Idol Nationality + Name Editor nationality history capture enabled.");
                }
                else
                {
                    CoreLog.Warn("Madxis nationality history capture skipped: current SelectNationality method was not found.");
                }
            }
        }

        private static Assembly FindTargetAssembly()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                Assembly assembly = assemblies[index];
                if (assembly != null &&
                    string.Equals(assembly.GetName().Name, TargetAssemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    return assembly;
                }
            }
            return null;
        }

        private static void SaveEditedNamePrefix(object __instance, ref NameEditState __state)
        {
            __state = CaptureNameState(__instance);
        }

        private static void SaveEditedNamePostfix(object __instance, NameEditState __state)
        {
            try
            {
                if (__state == null || __state.IdolId < CoreConstants.MinimumValidIdolIdentifier)
                {
                    return;
                }

                data_girls.girls idol = ResolveGirl(__instance, __state.IdolId);
                if (idol == null)
                {
                    return;
                }

                string newFirstName = idol.firstName ?? string.Empty;
                string newLastName = idol.lastName ?? string.Empty;
                if (string.Equals(__state.FirstName, newFirstName, StringComparison.Ordinal) &&
                    string.Equals(__state.LastName, newLastName, StringComparison.Ordinal))
                {
                    return;
                }

                string nationalityCode = GetNationalityCode(__state.IdolId);
                IMDataCoreController.Instance.CaptureMadxisIdolNameChanged(
                    __state.IdolId,
                    __state.FirstName,
                    __state.LastName,
                    newFirstName,
                    newLastName,
                    __state.DisplayName,
                    FormatFullName(newFirstName, newLastName, nationalityCode),
                    nationalityCode,
                    GetNationalityDisplayName(nationalityCode));
            }
            catch (Exception exception)
            {
                CoreLog.Warn("Madxis rename history capture failed without blocking the editor: " + exception.Message);
            }
        }

        private static void SelectNationalityPrefix(object __instance, object[] __args, ref NationalityEditState __state)
        {
            __state = null;
            try
            {
                if (__args == null || __args.Length < 1 || !(__args[0] is int))
                {
                    return;
                }

                int idolId = (int)__args[0];
                data_girls.girls idol = ResolveGirl(__instance, idolId);
                if (idol == null)
                {
                    return;
                }

                string oldCode = GetNationalityCode(idolId);
                __state = new NationalityEditState
                {
                    IdolId = idolId,
                    FirstName = idol.firstName ?? string.Empty,
                    LastName = idol.lastName ?? string.Empty,
                    NationalityCode = oldCode,
                    NationalityName = GetNationalityDisplayName(oldCode),
                    DisplayName = FormatFullName(idol.firstName, idol.lastName, oldCode)
                };
            }
            catch (Exception exception)
            {
                CoreLog.Warn("Madxis nationality pre-change snapshot failed without blocking the editor: " + exception.Message);
            }
        }

        private static void SelectNationalityPostfix(object __instance, object[] __args, NationalityEditState __state)
        {
            try
            {
                if (__state == null || __state.IdolId < CoreConstants.MinimumValidIdolIdentifier)
                {
                    return;
                }

                string newCode = GetNationalityCode(__state.IdolId);
                if (string.Equals(
                    NormalizeNationalityCode(__state.NationalityCode),
                    NormalizeNationalityCode(newCode),
                    StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                data_girls.girls idol = ResolveGirl(__instance, __state.IdolId);
                string firstName = idol != null ? idol.firstName ?? string.Empty : __state.FirstName;
                string lastName = idol != null ? idol.lastName ?? string.Empty : __state.LastName;
                IMDataCoreController.Instance.CaptureMadxisIdolNationalityChanged(
                    __state.IdolId,
                    firstName,
                    lastName,
                    __state.NationalityCode,
                    newCode,
                    __state.NationalityName,
                    GetNationalityDisplayName(newCode),
                    __state.DisplayName,
                    FormatFullName(firstName, lastName, newCode));
            }
            catch (Exception exception)
            {
                CoreLog.Warn("Madxis nationality history capture failed without blocking the editor: " + exception.Message);
            }
        }

        private static NameEditState CaptureNameState(object instance)
        {
            try
            {
                data_girls.girls idol = ResolveGirl(instance, CoreConstants.InvalidIdValue);
                if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
                {
                    return null;
                }

                string nationalityCode = GetNationalityCode(idol.id);
                return new NameEditState
                {
                    IdolId = idol.id,
                    FirstName = idol.firstName ?? string.Empty,
                    LastName = idol.lastName ?? string.Empty,
                    DisplayName = FormatFullName(idol.firstName, idol.lastName, nationalityCode)
                };
            }
            catch (Exception exception)
            {
                CoreLog.Warn("Madxis rename pre-change snapshot failed without blocking the editor: " + exception.Message);
                return null;
            }
        }

        private static data_girls.girls ResolveGirl(object instance, int preferredId)
        {
            if (preferredId >= CoreConstants.MinimumValidIdolIdentifier)
            {
                data_girls.girls canonical = data_girls.GetGirlByID(preferredId);
                if (canonical != null)
                {
                    return canonical;
                }
            }

            if (instance == null || popupField == null)
            {
                return null;
            }

            Profile_Popup popup = popupField.GetValue(instance) as Profile_Popup;
            return popup != null ? popup.Girl : null;
        }

        private static string GetNationalityCode(int idolId)
        {
            object value = getNationalityCodeMethod.Invoke(null, new object[] { idolId });
            return value as string ?? string.Empty;
        }

        private static string GetNationalityDisplayName(string code)
        {
            object value = getDisplayNameMethod.Invoke(null, new object[] { code ?? string.Empty });
            return value as string ?? (code ?? string.Empty);
        }

        private static string FormatFullName(string firstName, string lastName, string code)
        {
            object value = formatFullNameMethod.Invoke(
                null,
                new object[] { firstName ?? string.Empty, lastName ?? string.Empty, code ?? string.Empty });
            string formatted = value as string;
            if (!string.IsNullOrWhiteSpace(formatted))
            {
                return formatted;
            }

            return ((firstName ?? string.Empty) + " " + (lastName ?? string.Empty)).Trim();
        }

        private static string NormalizeNationalityCode(string code)
        {
            return string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim();
        }

        private sealed class NameEditState
        {
            internal int IdolId = CoreConstants.InvalidIdValue;
            internal string FirstName = string.Empty;
            internal string LastName = string.Empty;
            internal string DisplayName = string.Empty;
        }

        private sealed class NationalityEditState
        {
            internal int IdolId = CoreConstants.InvalidIdValue;
            internal string FirstName = string.Empty;
            internal string LastName = string.Empty;
            internal string NationalityCode = string.Empty;
            internal string NationalityName = string.Empty;
            internal string DisplayName = string.Empty;
        }
    }
}
