using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace IMDataCore
{
    /// <summary>
    /// Optional, reflection-only bridge to Save n Load Fixes' authoritative wide-numeric
    /// calculations. IM Data Core must not re-run an Int32 compatibility getter when SNLF
    /// owns an exact Int64 value behind that getter. The bridge deliberately keeps IMDC's
    /// binary dependency surface unchanged: when SNLF is absent, vanilla semantics remain
    /// authoritative and the original ABI is used.
    /// </summary>
    internal static class SaveNLoadFixesWideNumericInterop
    {
        internal const string AssemblyName = "com.cosmo.savenloadfixes";
        internal const string ContinuationTypeName = "SaveNLoadFixes.Repairs.WideNumericContinuation";
        internal const string StateTypeName = "SaveNLoadFixes.Repairs.WideNumericState";
        internal const long ExactSingleIntegerBoundary = 16777216L;
        internal const long DaysPerWeek = 7L;

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, MethodInfo> MethodCache =
            new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
        private static readonly HashSet<string> WarnedFailures =
            new HashSet<string>(StringComparer.Ordinal);

        private static Assembly saveNLoadFixesAssembly;
        private static int lastAssemblyCount = -1;

        internal static bool IsAvailable
        {
            get { return ResolveAssembly() != null; }
        }

        internal static long GetTotalRent(resources resourceManager, bool addFloat)
        {
            long exact;
            if (TryInvokeLong(
                ContinuationTypeName,
                "GetTotalRent",
                new Type[] { typeof(bool) },
                new object[] { addFloat },
                out exact))
            {
                return exact;
            }

            return resourceManager != null
                ? resourceManager.Money_Rent(addFloat)
                : 0L;
        }

        internal static long GetTotalLoanPayment()
        {
            long exact;
            if (TryInvokeLong(
                ContinuationTypeName,
                "GetTotalLoanPayment",
                Type.EmptyTypes,
                null,
                out exact))
            {
                return exact;
            }

            return loans.GetTotalPaymentPerWeek();
        }

        internal static long GetStoryCh4FansNeeded()
        {
            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetStoryCh4",
                Type.EmptyTypes,
                null,
                out exact))
            {
                return exact;
            }

            return tasks.Story_Data != null
                ? tasks.Story_Data.ch4_fans_needed
                : -1L;
        }

        internal static long GetStoryCh4ScandalPoints()
        {
            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetStoryCh4Scandal",
                Type.EmptyTypes,
                null,
                out exact))
            {
                return exact;
            }

            return tasks.Story_Data != null
                ? tasks.Story_Data.ch4_scandal_points
                : -1L;
        }

        internal static long GetLoanPayment(loans._loan loan)
        {
            if (loan == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetLoanPayment",
                new Type[] { typeof(loans._loan) },
                new object[] { loan },
                out exact))
            {
                return exact;
            }

            return loan.PaymentPerWeek;
        }

        internal static long GetStaffSeverance(staff._staff staffer)
        {
            if (staffer == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                ContinuationTypeName,
                "GetStaffSeverance",
                new Type[] { typeof(staff._staff) },
                new object[] { staffer },
                out exact))
            {
                return exact;
            }

            return staffer.Severance();
        }

        internal static long GetDailyBusinessProfit(resources resourceManager)
        {
            if (resourceManager == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                ContinuationTypeName,
                "GetDailyBusinessProfit",
                new Type[] { typeof(resources) },
                new object[] { resourceManager },
                out exact))
            {
                return exact;
            }

            return resourceManager.Money_DailyProfit();
        }

        internal static long GetDailyBusinessBuzz(resources resourceManager)
        {
            business businessSystem = ResolveBusinessSystem();
            long exact;
            if (businessSystem != null && TryInvokeLong(
                ContinuationTypeName,
                "GetBusinessDailyBuzz",
                new Type[] { typeof(business) },
                new object[] { businessSystem },
                out exact))
            {
                return exact;
            }

            return resourceManager != null
                ? resourceManager.Buzz_Daily()
                : 0L;
        }

        internal static long GetDailyBusinessFame(resources resourceManager)
        {
            business businessSystem = ResolveBusinessSystem();
            long exact;
            if (businessSystem != null && TryInvokeLong(
                ContinuationTypeName,
                "GetBusinessDailyFame",
                new Type[] { typeof(business) },
                new object[] { businessSystem },
                out exact))
            {
                return exact;
            }

            return resourceManager != null
                ? resourceManager.Fame_Daily()
                : 0L;
        }

        internal static long GetCafeMoneyToAdd(Cafes._cafe cafe)
        {
            if (cafe == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                ContinuationTypeName,
                "GetCafeMoneyToAdd",
                new Type[] { typeof(Cafes._cafe) },
                new object[] { cafe },
                out exact))
            {
                return exact;
            }

            return cafe.GetMoneyToAdd();
        }

        internal static long GetCafeFansToAdd(Cafes._cafe cafe)
        {
            if (cafe == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                ContinuationTypeName,
                "GetCafeFansToAdd",
                new Type[] { typeof(Cafes._cafe) },
                new object[] { cafe },
                out exact))
            {
                return exact;
            }

            return cafe.GetFansToAdd();
        }

        internal static long GetCafeStatProfit(
            Cafes._cafe cafe,
            int ordinal,
            Cafes._cafe._stat stat)
        {
            if (cafe == null || stat == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetCafeProfit",
                new Type[] { typeof(Cafes._cafe), typeof(int), typeof(Cafes._cafe._stat) },
                new object[] { cafe, ordinal, stat },
                out exact))
            {
                return exact;
            }

            return stat.Profit;
        }

        internal static long GetCafeStatNewFans(
            Cafes._cafe cafe,
            int ordinal,
            Cafes._cafe._stat stat)
        {
            if (cafe == null || stat == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetCafeNewFans",
                new Type[] { typeof(Cafes._cafe), typeof(int), typeof(Cafes._cafe._stat) },
                new object[] { cafe, ordinal, stat },
                out exact))
            {
                return exact;
            }

            return stat.New_Fans;
        }

        internal static void GetSingleReleaseFans(
            singles._single single,
            out long ordinary,
            out long hardcore,
            out long casual)
        {
            ordinary = 0L;
            hardcore = 0L;
            casual = 0L;
            if (single == null)
            {
                return;
            }

            object runtime;
            if (TryInvokeObject(
                StateTypeName,
                "GetSingleReleaseFans",
                new Type[] { typeof(singles._single) },
                new object[] { single },
                out runtime) && runtime != null)
            {
                Type runtimeType = runtime.GetType();
                FieldInfo ordinaryField = runtimeType.GetField(
                    "Ordinary", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo hardcoreField = runtimeType.GetField(
                    "Hardcore", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo casualField = runtimeType.GetField(
                    "Casual", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (ordinaryField != null && hardcoreField != null && casualField != null)
                {
                    object ordinaryValue = ordinaryField.GetValue(runtime);
                    object hardcoreValue = hardcoreField.GetValue(runtime);
                    object casualValue = casualField.GetValue(runtime);
                    if (ordinaryValue is long && hardcoreValue is long && casualValue is long)
                    {
                        ordinary = (long)ordinaryValue;
                        hardcore = (long)hardcoreValue;
                        casual = (long)casualValue;
                        return;
                    }
                }

                WarnOnce(
                    StateTypeName + "::GetSingleReleaseFans::shape",
                    "SNLF single-release fan state has an unexpected shape. Falling back to the vanilla compatibility values.");
            }

            if (single.ReleaseData != null)
            {
                ordinary = single.ReleaseData.NewFans;
                hardcore = single.ReleaseData.NewHardcoreFans;
                casual = single.ReleaseData.NewCasualFans;
            }
        }

        internal static List<long> GetShowFans(Shows._show show)
        {
            List<long> fallback = new List<long>();
            if (show == null)
            {
                return fallback;
            }

            object exactObject;
            if (TryInvokeObject(
                StateTypeName,
                "GetShowFans",
                new Type[] { typeof(Shows._show) },
                new object[] { show },
                out exactObject))
            {
                List<long> exact = exactObject as List<long>;
                if (exact != null)
                {
                    return exact;
                }

                WarnOnce(
                    StateTypeName + "::GetShowFans::shape",
                    "SNLF show-fan state has an unexpected shape. Falling back to the vanilla compatibility values.");
            }

            if (show.fans != null)
            {
                for (int index = 0; index < show.fans.Count; index++)
                {
                    fallback.Add(show.fans[index]);
                }
            }
            return fallback;
        }

        internal static long GetTheaterAverageRevenue(Theaters._theater theater)
        {
            if (theater == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                ContinuationTypeName,
                "GetTheaterAverageRevenue",
                new Type[] { typeof(Theaters._theater) },
                new object[] { theater },
                out exact))
            {
                return exact;
            }

            return theater.GetAvgRevenue();
        }

        internal static long GetTheaterStatSubscribers(
            Theaters._theater theater,
            int ordinal,
            Theaters._theater._stat stat)
        {
            if (theater == null || stat == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetTheaterStatSubscribers",
                new Type[] { typeof(Theaters._theater), typeof(int), typeof(Theaters._theater._stat) },
                new object[] { theater, ordinal, stat },
                out exact))
            {
                return exact;
            }

            return stat.Subscribers;
        }

        /// <summary>
        /// Mirrors SNLF's exact per-idol split used by the wide performance path.
        /// The vanilla-compatible branch intentionally keeps Mathf's float rounding
        /// while the total reward still fits Int32; once the reward itself is wider,
        /// SNLF switches to exact midpoint-to-even integer division.
        /// </summary>
        internal static long GetActivityPerformanceIdolShare(long reward, int activeIdolCount)
        {
            if (activeIdolCount <= 0)
            {
                return 0L;
            }

            if (!IsAvailable || (reward >= int.MinValue && reward <= int.MaxValue))
            {
                return Mathf.RoundToInt((float)reward / activeIdolCount);
            }

            return DivideRoundToEven(reward, activeIdolCount);
        }

        internal static long GetTourProductionCost(SEvent_Tour.tour tour)
        {
            if (tour == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetTourProductionCost",
                new Type[] { typeof(SEvent_Tour.tour) },
                new object[] { tour },
                out exact))
            {
                return exact;
            }

            return tour.ProductionCost;
        }

        internal static long GetTourExpectedRevenue(SEvent_Tour.tour tour)
        {
            if (tour == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetTourExpectedRevenue",
                new Type[] { typeof(SEvent_Tour.tour) },
                new object[] { tour },
                out exact))
            {
                return exact;
            }

            return tour.ExpectedRevenue;
        }

        internal static long GetTourSaving(SEvent_Tour.tour tour)
        {
            if (tour == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetTourSaving",
                new Type[] { typeof(SEvent_Tour.tour) },
                new object[] { tour },
                out exact))
            {
                return exact;
            }

            return tour.Saving;
        }

        internal static long GetTourProfit(SEvent_Tour.tour tour)
        {
            if (tour == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetTourProfit",
                new Type[] { typeof(SEvent_Tour.tour) },
                new object[] { tour },
                out exact))
            {
                return exact;
            }

            return tour.GetProfit();
        }

        internal static long GetTourRevenue(SEvent_Tour.tour tour)
        {
            if (tour == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetTourRevenue",
                new Type[] { typeof(SEvent_Tour.tour) },
                new object[] { tour },
                out exact))
            {
                return exact;
            }

            long total = 0L;
            if (tour.SelectedCountries != null)
            {
                for (int index = 0; index < tour.SelectedCountries.Count; index++)
                {
                    SEvent_Tour.tour.selectedCountry country = tour.SelectedCountries[index];
                    if (country != null)
                    {
                        total = checked(total + (long)country.Revenue);
                    }
                }
            }
            return total;
        }

        internal static long GetTourTotalAudience(SEvent_Tour.tour tour)
        {
            if (tour == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetTourTotalAudience",
                new Type[] { typeof(SEvent_Tour.tour) },
                new object[] { tour },
                out exact))
            {
                return exact;
            }

            return tour.GetTotalAudience();
        }

        internal static long GetTourTotalCountryFans(SEvent_Tour.tour tour)
        {
            if (tour == null)
            {
                return 0L;
            }

            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetTourTotalCountryFans",
                new Type[] { typeof(SEvent_Tour.tour) },
                new object[] { tour },
                out exact))
            {
                return exact;
            }

            return tour.GetNewFans();
        }

        internal static long GetTourCountryAudience(
            SEvent_Tour.tour tour,
            int ordinal,
            SEvent_Tour.tour.selectedCountry selectedCountry)
        {
            if (tour == null || selectedCountry == null)
            {
                return 0L;
            }

            if (ordinal < 0)
            {
                return selectedCountry.Audience;
            }

            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetTourCountryAudience",
                new Type[] { typeof(SEvent_Tour.tour), typeof(int) },
                new object[] { tour, ordinal },
                out exact))
            {
                return exact;
            }

            return selectedCountry.Audience;
        }

        internal static long GetTourCountryFans(
            SEvent_Tour.tour tour,
            int ordinal,
            SEvent_Tour.tour.selectedCountry selectedCountry)
        {
            if (tour == null || selectedCountry == null)
            {
                return 0L;
            }

            if (ordinal < 0)
            {
                return selectedCountry.NewFans;
            }

            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetTourCountryFans",
                new Type[] { typeof(SEvent_Tour.tour), typeof(int) },
                new object[] { tour, ordinal },
                out exact))
            {
                return exact;
            }

            return selectedCountry.NewFans;
        }

        internal static long GetTourCountryRevenue(
            SEvent_Tour.tour tour,
            int ordinal,
            SEvent_Tour.tour.selectedCountry selectedCountry)
        {
            if (tour == null || selectedCountry == null)
            {
                return 0L;
            }

            if (ordinal < 0)
            {
                return selectedCountry.Revenue;
            }

            long exact;
            if (TryInvokeLong(
                StateTypeName,
                "GetTourCountryRevenue",
                new Type[] { typeof(SEvent_Tour.tour), typeof(int) },
                new object[] { tour, ordinal },
                out exact))
            {
                return exact;
            }

            return selectedCountry.Revenue;
        }

        /// <summary>
        /// Matches SNLF's compatibility contract for a weekly Int32 business value:
        /// preserve Idol Manager's float/Mathf rounding while the integer is exactly
        /// representable by Single, and use exact round-to-even division once it is not.
        /// </summary>
        internal static long GetDailyContractAllocation(int paymentPerWeek)
        {
            if (!IsAvailable)
            {
                return Mathf.RoundToInt((float)paymentPerWeek / (float)DaysPerWeek);
            }

            return DivideWeeklyAmountCompatible(paymentPerWeek);
        }

        private static long DivideWeeklyAmountCompatible(long weeklyAmount)
        {
            if (weeklyAmount >= -ExactSingleIntegerBoundary &&
                weeklyAmount <= ExactSingleIntegerBoundary)
            {
                return Mathf.RoundToInt((float)weeklyAmount / (float)DaysPerWeek);
            }

            return DivideRoundToEven(weeklyAmount, DaysPerWeek);
        }

        private static long DivideRoundToEven(long numerator, long denominator)
        {
            if (denominator <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(denominator));
            }

            long quotient = numerator / denominator;
            long remainder = numerator % denominator;
            if (remainder == 0L)
            {
                return quotient;
            }

            long absoluteRemainder = remainder < 0L ? -remainder : remainder;
            long doubledRemainder = absoluteRemainder * 2L;
            if (doubledRemainder < denominator)
            {
                return quotient;
            }

            long direction = numerator < 0L ? -1L : 1L;
            if (doubledRemainder > denominator)
            {
                return quotient + direction;
            }

            return (quotient & 1L) == 0L
                ? quotient
                : quotient + direction;
        }

        private static bool TryInvokeObject(
            string typeName,
            string methodName,
            Type[] parameterTypes,
            object[] arguments,
            out object value)
        {
            value = null;
            Assembly assembly = ResolveAssembly();
            if (assembly == null)
            {
                return false;
            }

            string cacheKey = typeName + "::" + methodName + "::" + parameterTypes.Length.ToString();
            MethodInfo method;
            lock (Sync)
            {
                if (!MethodCache.TryGetValue(cacheKey, out method))
                {
                    Type owner = assembly.GetType(typeName, false);
                    method = owner != null
                        ? owner.GetMethod(
                            methodName,
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                            null,
                            parameterTypes,
                            null)
                        : null;
                    MethodCache[cacheKey] = method;
                }
            }

            if (method == null)
            {
                WarnOnce(cacheKey, "SNLF wide-numeric method is unavailable: " + typeName + "." + methodName + ". Falling back to the vanilla compatibility value.");
                return false;
            }

            try
            {
                value = method.Invoke(null, arguments);
                return true;
            }
            catch (Exception exception)
            {
                Exception diagnostic = exception is TargetInvocationException && exception.InnerException != null
                    ? exception.InnerException
                    : exception;
                WarnOnce(cacheKey + "::invoke", "SNLF wide-numeric interop failed for " + typeName + "." + methodName + ": " + diagnostic.Message + ". Falling back to the vanilla compatibility value.");
                value = null;
                return false;
            }
        }

        private static bool TryInvokeLong(
            string typeName,
            string methodName,
            Type[] parameterTypes,
            object[] arguments,
            out long value)
        {
            value = 0L;
            Assembly assembly = ResolveAssembly();
            if (assembly == null)
            {
                return false;
            }

            string cacheKey = typeName + "::" + methodName + "::" + parameterTypes.Length.ToString();
            MethodInfo method;
            lock (Sync)
            {
                if (!MethodCache.TryGetValue(cacheKey, out method))
                {
                    Type owner = assembly.GetType(typeName, false);
                    method = owner != null
                        ? owner.GetMethod(
                            methodName,
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                            null,
                            parameterTypes,
                            null)
                        : null;
                    MethodCache[cacheKey] = method;
                }
            }

            if (method == null)
            {
                WarnOnce(cacheKey, "SNLF wide-numeric method is unavailable: " + typeName + "." + methodName + ". Falling back to the vanilla compatibility value.");
                return false;
            }

            try
            {
                object result = method.Invoke(null, arguments);
                if (result is long)
                {
                    value = (long)result;
                    return true;
                }

                WarnOnce(cacheKey + "::return", "SNLF wide-numeric method returned an unexpected type: " + typeName + "." + methodName + ". Falling back to the vanilla compatibility value.");
                return false;
            }
            catch (Exception exception)
            {
                Exception diagnostic = exception is TargetInvocationException && exception.InnerException != null
                    ? exception.InnerException
                    : exception;
                WarnOnce(cacheKey + "::invoke", "SNLF wide-numeric interop failed for " + typeName + "." + methodName + ": " + diagnostic.Message + ". Falling back to the vanilla compatibility value.");
                return false;
            }
        }

        private static business ResolveBusinessSystem()
        {
            try
            {
                if (Camera.main == null)
                {
                    return null;
                }

                mainScript main = Camera.main.GetComponent<mainScript>();
                if (main == null || main.Data == null)
                {
                    return null;
                }

                return main.Data.GetComponent<business>();
            }
            catch
            {
                return null;
            }
        }

        private static Assembly ResolveAssembly()
        {
            lock (Sync)
            {
                if (saveNLoadFixesAssembly != null)
                {
                    return saveNLoadFixesAssembly;
                }

                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                if (assemblies.Length == lastAssemblyCount)
                {
                    return null;
                }

                lastAssemblyCount = assemblies.Length;
                for (int index = 0; index < assemblies.Length; index++)
                {
                    Assembly assembly = assemblies[index];
                    if (assembly != null &&
                        string.Equals(assembly.GetName().Name, AssemblyName, StringComparison.Ordinal))
                    {
                        saveNLoadFixesAssembly = assembly;
                        MethodCache.Clear();
                        return assembly;
                    }
                }

                return null;
            }
        }

        private static void WarnOnce(string key, string message)
        {
            lock (Sync)
            {
                if (!WarnedFailures.Add(key))
                {
                    return;
                }
            }

            CoreLog.Warn(message);
        }
    }
}
