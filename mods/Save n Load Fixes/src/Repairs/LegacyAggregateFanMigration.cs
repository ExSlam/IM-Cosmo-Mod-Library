using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A21: replaces only vanilla's legacy aggregate-fan redistribution bootstrap.
    /// The old save contains one already-committed aggregate fan total but no per-idol
    /// fan vectors. Vanilla synthesizes those vectors through two shuffled orders, so
    /// repeated loads of one untouched save can produce different current gameplay
    /// state. A21 preserves the same fame/appeal weighting model while making rounding
    /// ownership deterministic and preserving the legacy aggregate total exactly.
    /// </summary>
    internal static class LegacyAggregateFanMigration
    {
        private const string FamilyCode = "a21-aggregate-fans-v1";

        private sealed class Candidate
        {
            internal data_girls.girls Girl;
            internal int OriginalOrdinal;
        }

        private static readonly object Sync = new object();
        private static long observedLegacyBranchCount;
        private static long migratedLoadCount;
        private static long migratedFanTotal;
        private static long alreadyModernSkipCount;
        private static long zeroAggregateSkipCount;
        private static long invalidTargetSkipCount;
        private static long physicalPathUnavailableCount;
        private static long exactTotalVerificationFailureCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented { get { return LegacyAggregateFanMigrationPatchHealth.IsHealthy; } }
        internal static long ObservedLegacyBranchCount { get { return Interlocked.Read(ref observedLegacyBranchCount); } }
        internal static long MigratedLoadCount { get { return Interlocked.Read(ref migratedLoadCount); } }
        internal static long MigratedFanTotal { get { return Interlocked.Read(ref migratedFanTotal); } }
        internal static long AlreadyModernSkipCount { get { return Interlocked.Read(ref alreadyModernSkipCount); } }
        internal static long ZeroAggregateSkipCount { get { return Interlocked.Read(ref zeroAggregateSkipCount); } }
        internal static long InvalidTargetSkipCount { get { return Interlocked.Read(ref invalidTargetSkipCount); } }
        internal static long PhysicalPathUnavailableCount { get { return Interlocked.Read(ref physicalPathUnavailableCount); } }
        internal static long ExactTotalVerificationFailureCount { get { return Interlocked.Read(ref exactTotalVerificationFailureCount); } }
        internal static string LastDiagnostic { get { lock (Sync) { return lastDiagnostic; } } }

        /// <summary>
        /// Harmony Prefix replacement for data_girls.RedistributeFansOnOldSaveLoad().
        /// The caller always suppresses vanilla after this returns. That is deliberate:
        /// if deterministic migration cannot establish a valid target, running vanilla's
        /// shuffled fallback would reintroduce the exact compatibility defect A21 owns.
        /// </summary>
        internal static void ReplaceVanillaLegacyRedistribution()
        {
            Interlocked.Increment(ref observedLegacyBranchCount);

            try
            {
                List<Candidate> active = CollectActiveGirls();
                if (HasAnyPerIdolFans(active))
                {
                    Interlocked.Increment(ref alreadyModernSkipCount);
                    SetDiagnostic("A21 observed existing active-idol fan state; legacy redistribution is not required.");
                    return;
                }

                long legacyTotal = resources.GetFansTotal_Legacy();
                if (legacyTotal == 0L)
                {
                    Interlocked.Increment(ref zeroAggregateSkipCount);
                    SetDiagnostic("A21 observed a zero legacy aggregate fan total; no migration is required.");
                    return;
                }

                // resources._fan.AddPeople() clamps at zero, and legacy fan rows are
                // populations. A negative aggregate is therefore malformed rather than
                // a meaningful migration request.
                if (legacyTotal < 0L || active.Count == 0)
                {
                    Interlocked.Increment(ref invalidTargetSkipCount);
                    SetDiagnostic(
                        "A21 failed closed because the legacy aggregate is invalid or no active idols exist " +
                        "(legacyTotal=" + legacyTotal.ToString(CultureInfo.InvariantCulture) +
                        ", active=" + active.Count.ToString(CultureInfo.InvariantCulture) + ").");
                    return;
                }

                if (!CanPrepareAllFanVectors(active))
                {
                    Interlocked.Increment(ref invalidTargetSkipCount);
                    SetDiagnostic("A21 failed closed because an active idol has a malformed non-empty fan vector.");
                    return;
                }

                SaveManager.SavedData target = GetTargetSavedData();
                if (target == null)
                {
                    Interlocked.Increment(ref invalidTargetSkipCount);
                    SetDiagnostic("A21 failed closed because the exact target SavedData is unavailable.");
                    return;
                }

                bool usedPhysicalPath;
                LegacyMigrationDeterminism.Stream rosterStream =
                    LegacyMigrationDeterminism.CreateStream(
                        target,
                        FamilyCode,
                        "active-roster",
                        "idol-order",
                        out usedPhysicalPath);

                if (!usedPhysicalPath)
                {
                    Interlocked.Increment(ref physicalPathUnavailableCount);
                }

                LegacyMigrationDeterminism.ShuffleInPlace(active, rosterStream);

                float totalFame = 0f;
                for (int index = 0; index < active.Count; index++)
                {
                    float fame = active[index].Girl.GetFamePoints();
                    if (!float.IsNaN(fame) && !float.IsInfinity(fame) && fame > 0f)
                    {
                        totalFame += fame;
                    }
                }

                long assigned = 0L;
                int nonZeroIdolAllocations = 0;
                for (int index = 0; index < active.Count; index++)
                {
                    Candidate candidate = active[index];
                    long remaining = legacyTotal - assigned;
                    if (remaining <= 0L)
                    {
                        break;
                    }

                    long idolShare;
                    if (index == active.Count - 1)
                    {
                        // Vanilla's rounded fame shares can end below the old aggregate.
                        // The audit contract requires the migration to preserve that
                        // already-saved aggregate exactly, so the final eligible idol owns
                        // any remaining rounding residue.
                        idolShare = remaining;
                    }
                    else if (totalFame < 1f)
                    {
                        // Mirrors AddFans_Equally(): Ceil(total / count), with the same
                        // shuffled-order ownership, while still capping at exact remainder.
                        long equalShare = (long)Mathf.Ceil((float)legacyTotal / (float)active.Count);
                        idolShare = equalShare > remaining ? remaining : equalShare;
                    }
                    else
                    {
                        float fame = candidate.Girl.GetFamePoints();
                        if (float.IsNaN(fame) || float.IsInfinity(fame) || fame < 0f)
                        {
                            fame = 0f;
                        }

                        long weightedShare = (long)Mathf.Round((float)legacyTotal * (fame / totalFame));
                        if (weightedShare < 0L)
                        {
                            weightedShare = 0L;
                        }
                        idolShare = weightedShare > remaining ? remaining : weightedShare;
                    }

                    if (idolShare <= 0L)
                    {
                        continue;
                    }

                    bool bucketUsedPhysicalPath;
                    LegacyMigrationDeterminism.Stream bucketStream =
                        LegacyMigrationDeterminism.CreateStream(
                            target,
                            FamilyCode,
                            "girl:" + candidate.Girl.id.ToString(CultureInfo.InvariantCulture) +
                                ":ordinal:" + candidate.OriginalOrdinal.ToString(CultureInfo.InvariantCulture),
                            "fan-bucket-order",
                            out bucketUsedPhysicalPath);
                    if (!bucketUsedPhysicalPath && usedPhysicalPath)
                    {
                        // This should not normally diverge for one target object, but keep
                        // diagnostics exact if the weak association changes unexpectedly.
                        Interlocked.Increment(ref physicalPathUnavailableCount);
                    }

                    if (!TryAssignIdolShare(candidate.Girl, idolShare, bucketStream))
                    {
                        Interlocked.Increment(ref invalidTargetSkipCount);
                        SetDiagnostic(
                            "A21 failed closed while distributing a deterministic idol share for girl " +
                            candidate.Girl.id.ToString(CultureInfo.InvariantCulture) + ".");
                        return;
                    }

                    assigned += idolShare;
                    nonZeroIdolAllocations++;
                }

                long reconstructedTotal = GetActiveFanTotal(active);
                if (assigned != legacyTotal || reconstructedTotal != legacyTotal)
                {
                    Interlocked.Increment(ref exactTotalVerificationFailureCount);
                    SetDiagnostic(
                        "A21 exact-total verification failed; migration remains fail-closed " +
                        "(legacy=" + legacyTotal.ToString(CultureInfo.InvariantCulture) +
                        ", assigned=" + assigned.ToString(CultureInfo.InvariantCulture) +
                        ", reconstructed=" + reconstructedTotal.ToString(CultureInfo.InvariantCulture) + ").");
                    Debug.LogError(SaveNLoadFixesConstants.LogPrefix + LastDiagnostic);
                    return;
                }

                // Vanilla girls.AddFans(...) raises one notification for each non-zero
                // idol allocation, then data_girls.UpdateFansDisplay() raises one final
                // notification. Defer all notifications until the deterministic state
                // has passed exact-total verification so observers never see a partial
                // migration if validation fails.
                for (int notification = 0; notification < nonZeroIdolAllocations; notification++)
                {
                    resources._OnFansChange();
                }
                resources._OnFansChange();

                Interlocked.Increment(ref migratedLoadCount);
                Interlocked.Add(ref migratedFanTotal, legacyTotal);
                SetDiagnostic(
                    "A21 deterministically redistributed the exact legacy aggregate of " +
                    legacyTotal.ToString(CultureInfo.InvariantCulture) + " fans across " +
                    active.Count.ToString(CultureInfo.InvariantCulture) + " active idol(s)." +
                    (!usedPhysicalPath
                        ? " Physical path association was unavailable, so stable target-save metadata was used as the save identity fallback."
                        : string.Empty));
            }
            catch (Exception exception)
            {
                Interlocked.Increment(ref invalidTargetSkipCount);
                SetDiagnostic("A21 failed closed during deterministic legacy fan redistribution: " + exception.Message);
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + LastDiagnostic);
            }
        }

        private static List<Candidate> CollectActiveGirls()
        {
            List<Candidate> active = new List<Candidate>();
            List<data_girls.girls> roster = data_girls.girl;
            if (roster == null)
            {
                return active;
            }

            for (int index = 0; index < roster.Count; index++)
            {
                data_girls.girls girl = roster[index];
                if (girl == null || girl.status == data_girls._status.graduated)
                {
                    continue;
                }

                active.Add(new Candidate
                {
                    Girl = girl,
                    OriginalOrdinal = index
                });
            }

            return active;
        }

        private static bool HasAnyPerIdolFans(List<Candidate> active)
        {
            if (active == null)
            {
                return false;
            }

            for (int index = 0; index < active.Count; index++)
            {
                if (GetGirlFanTotal(active[index].Girl) > 0L)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanPrepareAllFanVectors(List<Candidate> active)
        {
            if (active == null)
            {
                return false;
            }

            for (int candidateIndex = 0; candidateIndex < active.Count; candidateIndex++)
            {
                data_girls.girls girl = active[candidateIndex].Girl;
                if (girl == null || girl.Fans == null || girl.Fans.Count == 0)
                {
                    continue;
                }

                bool hasNonNullBucket = false;
                for (int fanIndex = 0; fanIndex < girl.Fans.Count; fanIndex++)
                {
                    if (girl.Fans[fanIndex] != null)
                    {
                        hasNonNullBucket = true;
                        break;
                    }
                }

                if (!hasNonNullBucket)
                {
                    return false;
                }
            }

            return true;
        }

        private static long GetActiveFanTotal(List<Candidate> active)
        {
            long total = 0L;
            if (active == null)
            {
                return total;
            }

            for (int index = 0; index < active.Count; index++)
            {
                total += GetGirlFanTotal(active[index].Girl);
            }

            return total;
        }

        private static long GetGirlFanTotal(data_girls.girls girl)
        {
            if (girl == null || girl.Fans == null)
            {
                return 0L;
            }

            long total = 0L;
            for (int index = 0; index < girl.Fans.Count; index++)
            {
                resources._fan fan = girl.Fans[index];
                if (fan != null && fan.people > 0L)
                {
                    total += fan.people;
                }
            }

            return total;
        }

        private static bool TryAssignIdolShare(
            data_girls.girls girl,
            long idolShare,
            LegacyMigrationDeterminism.Stream bucketStream)
        {
            if (girl == null || idolShare < 0L || bucketStream == null)
            {
                return false;
            }

            if (idolShare == 0L)
            {
                return true;
            }

            if (girl.Fans == null)
            {
                girl.Fans = new List<resources._fan>();
            }
            if (girl.Fans.Count == 0)
            {
                girl.CreateFans();
            }

            int nonNullBucketCount = 0;
            for (int index = 0; index < girl.Fans.Count; index++)
            {
                if (girl.Fans[index] != null)
                {
                    nonNullBucketCount++;
                }
            }
            if (nonNullBucketCount == 0)
            {
                // A missing legacy vector should have taken CreateFans() above. A
                // non-empty all-null vector is malformed and is not safe to reinterpret.
                return false;
            }

            girl.RecalcFanAppeal();
            float appealTotal = girl.GetAppeal_Total();
            long assigned = 0L;

            if (!float.IsNaN(appealTotal) && !float.IsInfinity(appealTotal) && appealTotal > 0f)
            {
                for (int index = 0; index < girl.Fans.Count; index++)
                {
                    resources._fan fan = girl.Fans[index];
                    if (fan == null)
                    {
                        continue;
                    }

                    float ratio = fan.GetTotalAppeal() / appealTotal;
                    long share = (long)Mathf.Floor((float)idolShare * ratio);
                    if (share < 0L)
                    {
                        share = 0L;
                    }

                    long remaining = idolShare - assigned;
                    if (share > remaining)
                    {
                        share = remaining;
                    }

                    if (share > 0L)
                    {
                        fan.AddPeople(share);
                        assigned += share;
                    }
                }
            }

            // Vanilla shuffles the actual Fans list before assigning integer remainder.
            // Preserve that list-order side effect with repair-owned deterministic
            // entropy. For a zero-appeal malformed-but-usable vector, the full idol
            // share becomes remainder and is spread evenly through this deterministic
            // order so the already-saved aggregate remains exact.
            LegacyMigrationDeterminism.ShuffleInPlace(girl.Fans, bucketStream);

            long residue = idolShare - assigned;
            if (residue <= 0L)
            {
                return true;
            }

            long each = residue / nonNullBucketCount;
            int remainder = (int)(residue % nonNullBucketCount);
            int seen = 0;
            for (int index = 0; index < girl.Fans.Count; index++)
            {
                resources._fan fan = girl.Fans[index];
                if (fan == null)
                {
                    continue;
                }

                long add = each + (seen < remainder ? 1L : 0L);
                if (add > 0L)
                {
                    fan.AddPeople(add);
                }
                seen++;
            }

            return seen == nonNullBucketCount;
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
                SetDiagnostic("A21 could not resolve target SavedData: " + exception.Message);
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

    internal static class LegacyAggregateFanMigrationPatchHealth
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
