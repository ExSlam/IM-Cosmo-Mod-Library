using System;
using System.Globalization;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A22: makes Relationships.InitialCreationForOldSave() deterministic without
    /// changing ordinary relationship creation. Vanilla's legacy bootstrap calls
    /// _relationship.Initialize(), whose non-date-compatible Dynamic choice uses
    /// Unity RNG. A scoped replacement preserves all Initialize semantics while
    /// deriving that one missing legacy value from stable unordered idol-pair identity.
    /// </summary>
    internal static class LegacyRelationshipBootstrapMigration
    {
        private const string FamilyCode = "a22-relationship-bootstrap-v1";

        [ThreadStatic]
        private static BootstrapScope activeScope;

        private sealed class BootstrapScope
        {
            internal SaveManager.SavedData Target;
            internal int InitializedCount;
            internal int PositiveCount;
            internal int DeterministicDomainCount;
            internal bool PhysicalPathUnavailable;
        }

        private static readonly object Sync = new object();
        private static long observedBootstrapCount;
        private static long deterministicallyInitializedCount;
        private static long positiveCompatibilityCount;
        private static long deterministicDomainCount;
        private static long physicalPathUnavailableCount;
        private static long invalidPairCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented { get { return LegacyRelationshipBootstrapMigrationPatchHealth.IsHealthy; } }
        internal static long ObservedBootstrapCount { get { return Interlocked.Read(ref observedBootstrapCount); } }
        internal static long DeterministicallyInitializedCount { get { return Interlocked.Read(ref deterministicallyInitializedCount); } }
        internal static long PositiveCompatibilityCount { get { return Interlocked.Read(ref positiveCompatibilityCount); } }
        internal static long DeterministicDomainCount { get { return Interlocked.Read(ref deterministicDomainCount); } }
        internal static long PhysicalPathUnavailableCount { get { return Interlocked.Read(ref physicalPathUnavailableCount); } }
        internal static long InvalidPairCount { get { return Interlocked.Read(ref invalidPairCount); } }
        internal static string LastDiagnostic { get { lock (Sync) { return lastDiagnostic; } } }

        internal static void EnterLegacyBootstrap()
        {
            // InitialCreationForOldSave is non-recursive in the supplied source. Refuse
            // accidental nesting rather than allowing one mod-driven invocation to
            // overwrite another scope on the same thread.
            if (activeScope != null)
            {
                Interlocked.Increment(ref invalidPairCount);
                SetDiagnostic("A22 observed a nested InitialCreationForOldSave scope and left the existing scope authoritative.");
                return;
            }

            SaveManager.SavedData target = GetTargetSavedData();
            activeScope = new BootstrapScope { Target = target };
            Interlocked.Increment(ref observedBootstrapCount);
        }

        internal static void ExitLegacyBootstrap(Exception exception)
        {
            BootstrapScope scope = activeScope;
            activeScope = null;
            if (scope == null)
            {
                return;
            }

            if (scope.PhysicalPathUnavailable)
            {
                Interlocked.Increment(ref physicalPathUnavailableCount);
            }

            Interlocked.Add(ref deterministicallyInitializedCount, scope.InitializedCount);
            Interlocked.Add(ref positiveCompatibilityCount, scope.PositiveCount);
            Interlocked.Add(ref deterministicDomainCount, scope.DeterministicDomainCount);

            if (exception != null)
            {
                SetDiagnostic("A22 legacy relationship bootstrap exited with vanilla exception after deterministic initialization: " + exception.Message);
                return;
            }

            SetDiagnostic(
                "A22 deterministically initialized " + scope.InitializedCount.ToString(CultureInfo.InvariantCulture) +
                " legacy relationship row(s): " + scope.PositiveCount.ToString(CultureInfo.InvariantCulture) +
                " date-compatible positive and " + scope.DeterministicDomainCount.ToString(CultureInfo.InvariantCulture) +
                " pair-keyed vanilla-domain Dynamic choice(s)." +
                (scope.PhysicalPathUnavailable
                    ? " Physical path association was unavailable, so stable target-save metadata was used as the save identity fallback."
                    : string.Empty));
        }

        /// <summary>
        /// Returns true when vanilla Initialize() should run. During the audited legacy
        /// scope this method performs the complete source-equivalent initialization and
        /// returns false so UnityEngine.Random.Range(1,4) is never consumed.
        /// </summary>
        internal static bool ShouldRunVanillaInitialize(Relationships._relationship relationship)
        {
            BootstrapScope scope = activeScope;
            if (scope == null)
            {
                return true;
            }

            if (relationship == null || relationship.Girls == null || relationship.Girls.Count != 2 ||
                relationship.Girls[0] == null || relationship.Girls[1] == null)
            {
                Interlocked.Increment(ref invalidPairCount);
                SetDiagnostic("A22 found a malformed legacy relationship pair; it was initialized to the deterministic neutral fallback without consuming Unity RNG.");
                InitializeMalformedFallback(relationship);
                return false;
            }

            int id0 = relationship.Girls[0].id;
            int id1 = relationship.Girls[1].id;
            if (id0 == id1)
            {
                Interlocked.Increment(ref invalidPairCount);
                SetDiagnostic("A22 found duplicate stable idol IDs in one legacy relationship pair; deterministic neutral fallback was used.");
                InitializeMalformedFallback(relationship);
                return false;
            }

            relationship.Vals.Add(1);
            relationship.Vals.Add(-1);

            if (relationship.CanDate())
            {
                relationship.Dynamic = Relationships._relationship._dynamic.positive;
                scope.PositiveCount++;
            }
            else
            {
                int low = Math.Min(id0, id1);
                int high = Math.Max(id0, id1);
                bool usedPhysicalPath;
                LegacyMigrationDeterminism.Stream stream = LegacyMigrationDeterminism.CreateStream(
                    scope.Target,
                    FamilyCode,
                    "pair:" + low.ToString(CultureInfo.InvariantCulture) + ":" + high.ToString(CultureInfo.InvariantCulture),
                    "dynamic",
                    out usedPhysicalPath);
                scope.PhysicalPathUnavailable |= !usedPhysicalPath;

                // Mirrors vanilla _relationship.Initialize(): Random.Range(1, 4),
                // corresponding exactly to positive / neutral / negative.
                relationship.Dynamic = (Relationships._relationship._dynamic)stream.NextInt(1, 4);
                scope.DeterministicDomainCount++;
            }

            relationship.Recalc(false);
            scope.InitializedCount++;
            return false;
        }

        private static void InitializeMalformedFallback(Relationships._relationship relationship)
        {
            if (relationship == null)
            {
                return;
            }

            relationship.Vals.Add(1);
            relationship.Vals.Add(-1);
            relationship.Dynamic = Relationships._relationship._dynamic.neutral;
            relationship.Recalc(false);
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
                SetDiagnostic("A22 could not resolve target SavedData; deterministic pair identity will use the utility's metadata-unavailable fallback: " + exception.Message);
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

    internal static class LegacyRelationshipBootstrapMigrationPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 2;

        private static int resolvedTargetMethodCount;
        private static int failureCount;
        private static string lastDiagnostic = string.Empty;

        internal static int ResolvedTargetMethodCount { get { return Volatile.Read(ref resolvedTargetMethodCount); } }
        internal static int FailureCount { get { return Volatile.Read(ref failureCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }
        internal static bool IsHealthy
        {
            get { return ResolvedTargetMethodCount == ExpectedTargetMethodCount && FailureCount == 0; }
        }

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
