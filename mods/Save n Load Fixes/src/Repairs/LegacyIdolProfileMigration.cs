using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A20: makes the two legacy idol-profile fallback fields deterministic without
    /// claiming to recover values the old schema never stored. The in-memory target
    /// DTO is populated before vanilla reconstruction so the next ordinary save commits
    /// the migrated values through vanilla GirlData normally.
    /// </summary>
    internal static class LegacyIdolProfileMigration
    {
        private const string FamilyCode = "a20-idol-profile-v1";

        private static readonly object Sync = new object();
        private static long migratedLoadCount;
        private static long migratedBirthdayCount;
        private static long migratedPeakAgeCount;
        private static long physicalPathUnavailableCount;
        private static long skippedInvalidTargetCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented { get { return LegacyIdolProfileMigrationPatchHealth.IsHealthy; } }
        internal static long MigratedLoadCount { get { return Interlocked.Read(ref migratedLoadCount); } }
        internal static long MigratedBirthdayCount { get { return Interlocked.Read(ref migratedBirthdayCount); } }
        internal static long MigratedPeakAgeCount { get { return Interlocked.Read(ref migratedPeakAgeCount); } }
        internal static long PhysicalPathUnavailableCount { get { return Interlocked.Read(ref physicalPathUnavailableCount); } }
        internal static long SkippedInvalidTargetCount { get { return Interlocked.Read(ref skippedInvalidTargetCount); } }
        internal static string LastDiagnostic { get { lock (Sync) { return lastDiagnostic; } } }

        internal static void PrepareLegacyFieldsBeforeGirlLoad()
        {
            try
            {
                SaveManager.SavedData target = GetTargetSavedData();
                if (target == null || target.data_girls__Girls == null)
                {
                    Interlocked.Increment(ref skippedInvalidTargetCount);
                    SetDiagnostic("A20 skipped because the target SavedData/idol list is unavailable.");
                    return;
                }

                DateTime targetGameDate;
                if (!TryGetTargetGameDate(target, out targetGameDate))
                {
                    Interlocked.Increment(ref skippedInvalidTargetCount);
                    SetDiagnostic("A20 skipped because target staticVars__dateTime is missing or invalid.");
                    return;
                }

                int birthdayCount = 0;
                int peakAgeCount = 0;
                bool useTweaksPeakAgeRange = TweaksNQoLCompatibility.UseTweaksPeakAgeRange;
                bool physicalPathUnavailableObserved = false;
                List<data_girls.GirlData> girls = target.data_girls__Girls;

                for (int index = 0; index < girls.Count; index++)
                {
                    data_girls.GirlData row = girls[index];
                    if (row == null)
                    {
                        continue;
                    }

                    string entityCode =
                        "girl:" + row.id.ToString(CultureInfo.InvariantCulture) +
                        ":ordinal:" + index.ToString(CultureInfo.InvariantCulture);

                    if (row.birthday == null)
                    {
                        bool usedPhysicalPath;
                        LegacyMigrationDeterminism.Stream stream =
                            LegacyMigrationDeterminism.CreateStream(
                                target,
                                FamilyCode,
                                entityCode,
                                "birthday",
                                out usedPhysicalPath);
                        physicalPathUnavailableObserved |= !usedPhysicalPath;

                        // Mirrors vanilla GenerateBirthday(): target date - 24 years,
                        // then independent Range(0,12), Range(0,12), Range(0,31).
                        DateTime birthday = targetGameDate
                            .AddYears(-24)
                            .AddYears(stream.NextInt(0, 12))
                            .AddMonths(stream.NextInt(0, 12))
                            .AddDays(stream.NextInt(0, 31));

                        row.birthday = ExtensionMethods.ToDataString(birthday);
                        birthdayCount++;
                    }

                    if (row.peakAge <= 1)
                    {
                        bool usedPhysicalPath;
                        LegacyMigrationDeterminism.Stream stream =
                            LegacyMigrationDeterminism.CreateStream(
                                target,
                                FamilyCode,
                                entityCode,
                                "peak-age",
                                out usedPhysicalPath);
                        physicalPathUnavailableObserved |= !usedPhysicalPath;

                        if (useTweaksPeakAgeRange)
                        {
                            // Shelon TweaksNQoL 1.0.0 replaces GeneratePeakAge with Random.Range(23,45).
                            row.peakAge = stream.NextInt(23, 45);
                            TweaksNQoLCompatibility.RecordPeakAgeMigration();
                        }
                        else
                        {
                            // Mirrors vanilla GeneratePeakAge(): Random.Range(16, 25).
                            row.peakAge = stream.NextInt(16, 25);
                        }
                        peakAgeCount++;
                    }
                }

                if (physicalPathUnavailableObserved)
                {
                    Interlocked.Increment(ref physicalPathUnavailableCount);
                }

                if (birthdayCount == 0 && peakAgeCount == 0)
                {
                    SetDiagnostic("A20 observed no legacy missing birthday/peak-age fields; target save is unchanged.");
                    return;
                }

                Interlocked.Increment(ref migratedLoadCount);
                Interlocked.Add(ref migratedBirthdayCount, birthdayCount);
                Interlocked.Add(ref migratedPeakAgeCount, peakAgeCount);
                SetDiagnostic(
                    "A20 deterministically migrated " + birthdayCount +
                    " birthday field(s) and " + peakAgeCount +
                    " peak-age field(s) before vanilla idol reconstruction." +
                    (physicalPathUnavailableObserved
                        ? " Physical path association was unavailable, so stable target-save metadata was used as the save identity fallback."
                        : string.Empty));
            }
            catch (Exception exception)
            {
                Interlocked.Increment(ref skippedInvalidTargetCount);
                SetDiagnostic("A20 failed safely before idol reconstruction: " + exception.Message);
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + LastDiagnostic);
            }
        }

        private static bool TryGetTargetGameDate(
            SaveManager.SavedData target,
            out DateTime gameDate)
        {
            gameDate = default(DateTime);
            if (target == null || string.IsNullOrEmpty(target.staticVars__dateTime))
            {
                return false;
            }

            try
            {
                gameDate = ExtensionMethods.ToDateTime(target.staticVars__dateTime);
                return gameDate != default(DateTime);
            }
            catch
            {
                return false;
            }
        }

        private static SaveManager.SavedData GetTargetSavedData()
        {
            try
            {
                mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
                return main == null ? null : main.GetSavedData();
            }
            catch (Exception exception)
            {
                SetDiagnostic("A20 could not resolve target SavedData: " + exception.Message);
                return null;
            }
        }

        private static void SetDiagnostic(string value)
        {
            lock (Sync)
            {
                lastDiagnostic = value ?? string.Empty;
            }
        }
    }

    internal static class LegacyIdolProfileMigrationPatchHealth
    {
        private static int resolvedTargetMethodCount;
        private static int failureCount;
        private static string lastDiagnostic = string.Empty;

        internal static int ResolvedTargetMethodCount { get { return Volatile.Read(ref resolvedTargetMethodCount); } }
        internal static int FailureCount { get { return Volatile.Read(ref failureCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }
        internal static bool IsHealthy { get { return ResolvedTargetMethodCount == 1 && FailureCount == 0; } }

        internal static void ReportTargetResolved()
        {
            Interlocked.Increment(ref resolvedTargetMethodCount);
        }

        internal static void ReportFailure(string diagnostic)
        {
            Interlocked.Increment(ref failureCount);
            lastDiagnostic = diagnostic ?? string.Empty;
        }
    }
}
