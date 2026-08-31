using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A23: stabilizes only the old-save Rivals.Generate() compatibility bootstrap.
    /// Vanilla enters that branch from Rivals.LoadFunction() when no serialized rival
    /// groups reconstruct. This replacement mirrors the audited Generate() semantics
    /// while sourcing every bootstrap random choice from repair-owned deterministic
    /// entropy keyed to the physical save/migration identity.
    /// </summary>
    internal static class LegacyRivalBootstrapMigration
    {
        private const string FamilyCode = "a23-rival-bootstrap-v1";

        [ThreadStatic]
        private static LoadScope activeScope;

        private sealed class LoadScope
        {
            internal SaveManager.SavedData Target;
            internal bool EligibleLegacyTarget;
            internal bool GenerationIntercepted;
            internal bool PhysicalPathUnavailable;
            internal int GeneratedTrendCount;
            internal int GeneratedGroupCount;
        }

        private static readonly object Sync = new object();
        private static readonly object NamePoolSync = new object();
        private static List<string> canonicalGroupNames;
        private static List<string> canonicalGroupNameSource;

        private static readonly FieldInfo FuncGenreField = AccessTools.Field(typeof(Rivals), "FuncGenre");
        private static readonly FieldInfo FuncLyricsField = AccessTools.Field(typeof(Rivals), "FuncLyrics");
        private static readonly FieldInfo FuncChoreoField = AccessTools.Field(typeof(Rivals), "FuncChoreo");

        private static long observedLoadScopeCount;
        private static long eligibleLegacyLoadCount;
        private static long deterministicallyGeneratedLoadCount;
        private static long generatedTrendCount;
        private static long generatedGroupCount;
        private static long physicalPathUnavailableCount;
        private static long invalidTargetCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented { get { return LegacyRivalBootstrapMigrationPatchHealth.IsHealthy; } }
        internal static long ObservedLoadScopeCount { get { return Interlocked.Read(ref observedLoadScopeCount); } }
        internal static long EligibleLegacyLoadCount { get { return Interlocked.Read(ref eligibleLegacyLoadCount); } }
        internal static long DeterministicallyGeneratedLoadCount { get { return Interlocked.Read(ref deterministicallyGeneratedLoadCount); } }
        internal static long GeneratedTrendCount { get { return Interlocked.Read(ref generatedTrendCount); } }
        internal static long GeneratedGroupCount { get { return Interlocked.Read(ref generatedGroupCount); } }
        internal static long PhysicalPathUnavailableCount { get { return Interlocked.Read(ref physicalPathUnavailableCount); } }
        internal static long InvalidTargetCount { get { return Interlocked.Read(ref invalidTargetCount); } }
        internal static string LastDiagnostic { get { lock (Sync) { return lastDiagnostic; } } }

        internal static void EnterRivalLoadScope()
        {
            if (activeScope != null)
            {
                Interlocked.Increment(ref invalidTargetCount);
                SetDiagnostic("A23 observed a nested Rivals.LoadFunction scope and left the existing scope authoritative.");
                return;
            }

            SaveManager.SavedData target = GetTargetSavedData();
            bool eligible = target != null && target.Rivals__Groups != null && target.Rivals__Groups.Count == 0;
            activeScope = new LoadScope
            {
                Target = target,
                EligibleLegacyTarget = eligible
            };

            Interlocked.Increment(ref observedLoadScopeCount);
            if (eligible)
            {
                Interlocked.Increment(ref eligibleLegacyLoadCount);
            }
        }

        internal static void ExitRivalLoadScope(Exception exception)
        {
            LoadScope scope = activeScope;
            activeScope = null;
            if (scope == null)
            {
                return;
            }

            if (scope.PhysicalPathUnavailable)
            {
                Interlocked.Increment(ref physicalPathUnavailableCount);
            }

            if (scope.GenerationIntercepted)
            {
                Interlocked.Increment(ref deterministicallyGeneratedLoadCount);
                Interlocked.Add(ref generatedTrendCount, scope.GeneratedTrendCount);
                Interlocked.Add(ref generatedGroupCount, scope.GeneratedGroupCount);
            }

            if (exception != null)
            {
                SetDiagnostic("A23 rival load exited with an exception after compatibility handling: " + exception.Message);
                return;
            }

            if (!scope.EligibleLegacyTarget)
            {
                SetDiagnostic("A23 observed a current/nonlegacy rival load and left vanilla rival reconstruction unchanged.");
                return;
            }

            if (!scope.GenerationIntercepted)
            {
                SetDiagnostic("A23 observed an eligible zero-group target, but the audited Rivals.Generate() compatibility call was not reached.");
                return;
            }

            SetDiagnostic(
                "A23 deterministically generated " + scope.GeneratedTrendCount.ToString(CultureInfo.InvariantCulture) +
                " legacy rival trend row(s) and " + scope.GeneratedGroupCount.ToString(CultureInfo.InvariantCulture) +
                " rival group(s) without consuming Unity RNG." +
                (scope.PhysicalPathUnavailable
                    ? " Physical path association was unavailable, so stable target-save metadata was used as the save identity fallback."
                    : string.Empty));
        }

        /// <summary>
        /// Returns true when vanilla Rivals.Generate() should run. Only the exact
        /// zero-group compatibility call nested under Rivals.LoadFunction() is replaced.
        /// New-career Start() and later normal rival generation remain vanilla-owned.
        /// </summary>
        internal static bool ShouldRunVanillaGenerate(Rivals instance)
        {
            // Generate() also runs for a new career. Observe the rival-name pool before
            // that vanilla call can consume/remove names so a later old-save load can
            // restore the canonical pool rather than inherit discarded-timeline removals.
            EnsureCanonicalGroupNamePoolSnapshot();

            LoadScope scope = activeScope;
            if (scope == null || !scope.EligibleLegacyTarget)
            {
                return true;
            }

            if (scope.GenerationIntercepted)
            {
                Interlocked.Increment(ref invalidTargetCount);
                SetDiagnostic("A23 suppressed an unexpected duplicate Rivals.Generate() call inside one legacy load so migrated bootstrap state cannot be duplicated or rerandomized.");
                return false;
            }

            if (instance == null || Rivals.Groups == null || Rivals.Groups.Count != 0)
            {
                Interlocked.Increment(ref invalidTargetCount);
                SetDiagnostic("A23 refused deterministic rival bootstrap because the audited zero-group precondition was not satisfied.");
                return true;
            }

            try
            {
                GenerateDeterministically(instance, scope);
                scope.GenerationIntercepted = true;
                return false;
            }
            catch (Exception exception)
            {
                Interlocked.Increment(ref invalidTargetCount);
                SetDiagnostic("A23 deterministic rival bootstrap failed before commit: " + exception.Message);
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + LastDiagnostic);
                throw;
            }
        }

        private static void GenerateDeterministically(Rivals instance, LoadScope scope)
        {
            bool usedPhysicalPath;
            LegacyMigrationDeterminism.Stream stream = LegacyMigrationDeterminism.CreateStream(
                scope.Target,
                FamilyCode,
                "career",
                "bootstrap",
                out usedPhysicalPath);
            scope.PhysicalPathUnavailable |= !usedPhysicalPath;

            // Mirrors Rivals.Generate() trend construction exactly, replacing only
            // mainScript.chance(50) / Random.Range(0,500) with deterministic entropy.
            for (int index = 0; index < singles.Genres.Count; index++)
            {
                singles._param param = singles.Genres[index];
                Rivals._trend trend = new Rivals._trend();
                trend.Param = param;
                trend.IsRising = Chance(stream, 50);
                if (trend.Points == 0)
                {
                    trend.Points = stream.NextInt(0, 500);
                }
                Rivals.Genres.Add(trend);
                scope.GeneratedTrendCount++;
            }

            for (int index = 0; index < singles.Choreography.Count; index++)
            {
                singles._param param = singles.Choreography[index];
                Rivals._trend trend = new Rivals._trend();
                trend.Param = param;
                trend.IsRising = Chance(stream, 50);
                if (trend.Points == 0)
                {
                    trend.Points = stream.NextInt(0, 500);
                }
                Rivals.Choreography.Add(trend);
                scope.GeneratedTrendCount++;
            }

            for (int index = 0; index < singles.Lyrics.Count; index++)
            {
                singles._param param = singles.Lyrics[index];
                Rivals._trend trend = new Rivals._trend();
                trend.Param = param;
                trend.IsRising = Chance(stream, 50);
                if (trend.Points == 0)
                {
                    trend.Points = stream.NextInt(0, 500);
                }
                Rivals.Lyrics.Add(trend);
                scope.GeneratedTrendCount++;
            }

            SortTrendsLikeVanilla();

            List<string> groupNames = ResetGroupNamePoolToCanonical();
            for (int index = 0; index < 50; index++)
            {
                GenerateGroupDeterministically(groupNames, stream);
                scope.GeneratedGroupCount++;
            }

            // Mirrors the exact post-generation fixed top-three overrides.
            Rivals.Groups[0].Fans = 1000000L;
            Rivals.Groups[0].Genre = singles.Genres[0];
            Rivals.Groups[0].IsGenreFixed = true;
            Rivals.Groups[1].Fans = 800000L;
            Rivals.Groups[1].Genre = singles.Genres[1];
            Rivals.Groups[1].IsGenreFixed = true;
            Rivals.Groups[2].Fans = 500000L;
            Rivals.Groups[2].Genre = singles.Genres[3];
            Rivals.Groups[2].IsGenreFixed = true;

            if (staticVars.IsStoryMode())
            {
                Rivals.Groups[Rivals.Groups.Count - 1].Fans = 0L;
                Rivals.Groups[Rivals.Groups.Count - 1].IsRival = true;
                Rivals.Groups[Rivals.Groups.Count - 2].Fans = 3000000L;
                Rivals.Groups[Rivals.Groups.Count - 2].IsPhantasm = true;
            }

            SortGroupsLikeVanilla();
            InitializeFunctionsLikeVanilla(instance);
        }

        private static void EnsureCanonicalGroupNamePoolSnapshot()
        {
            List<string> current = nameGenerator.GetVariables("rival_group");
            if (current == null)
            {
                return;
            }

            lock (NamePoolSync)
            {
                // The name-generator list is mutable and vanilla GenerateGroup removes
                // names from it. Keep the first/fullest snapshot for one list instance;
                // recapture if content reload replaces the backing list.
                if (canonicalGroupNames == null ||
                    !object.ReferenceEquals(canonicalGroupNameSource, current) ||
                    current.Count > canonicalGroupNames.Count)
                {
                    canonicalGroupNameSource = current;
                    canonicalGroupNames = new List<string>(current);
                }
            }
        }

        private static List<string> ResetGroupNamePoolToCanonical()
        {
            EnsureCanonicalGroupNamePoolSnapshot();
            lock (NamePoolSync)
            {
                List<string> current = nameGenerator.GetVariables("rival_group");
                if (current == null)
                {
                    return null;
                }

                if (canonicalGroupNames != null)
                {
                    current.Clear();
                    for (int index = 0; index < canonicalGroupNames.Count; index++)
                    {
                        current.Add(canonicalGroupNames[index]);
                    }
                    canonicalGroupNameSource = current;
                }

                return current;
            }
        }

        private static void GenerateGroupDeterministically(
            List<string> groupNames,
            LegacyMigrationDeterminism.Stream stream)
        {
            Rivals._group group = new Rivals._group();
            group.ID = Rivals.Groups.Count;

            List<string> effectiveNames = groupNames;
            if (effectiveNames == null || effectiveNames.Count == 0)
            {
                effectiveNames = nameGenerator.GetVariables("rival_group");
            }

            group.GroupName = GetRandomElement(effectiveNames, stream);
            if (effectiveNames != null)
            {
                effectiveNames.Remove(group.GroupName);
            }

            int fanBand = stream.NextInt(0, 100);
            if (fanBand == 1)
            {
                group.Fans = (long)stream.NextInt(500000, 1000000);
            }
            else if (fanBand <= 11)
            {
                group.Fans = (long)stream.NextInt(50000, 500000);
            }
            else if (fanBand <= 30)
            {
                group.Fans = (long)stream.NextInt(10000, 100000);
            }
            else if (fanBand <= 60)
            {
                group.Fans = (long)stream.NextInt(1000, 50000);
            }
            else
            {
                group.Fans = (long)stream.NextInt(0, 10000);
            }

            if (Chance(stream, 20))
            {
                group.IsRising = false;
            }

            group.Genre = GetRandomElement(singles.Genres, stream);
            Rivals.Groups.Add(group);
        }

        private static T GetRandomElement<T>(IList<T> items, LegacyMigrationDeterminism.Stream stream)
        {
            if (items == null || items.Count == 0)
            {
                return default(T);
            }
            return items[stream.NextInt(0, items.Count)];
        }

        private static bool Chance(LegacyMigrationDeterminism.Stream stream, int percent)
        {
            return percent >= 100 || (percent > 0 && stream.NextInt(0, 100) < percent);
        }

        private static void SortTrendsLikeVanilla()
        {
            Rivals.Genres = Rivals.Genres.OrderByDescending(item => item.Points).ToList();
            Rivals.Lyrics = Rivals.Lyrics.OrderByDescending(item => item.Points).ToList();
            Rivals.Choreography = Rivals.Choreography.OrderByDescending(item => item.Points).ToList();
        }

        private static void SortGroupsLikeVanilla()
        {
            Rivals.Groups = Rivals.Groups.OrderByDescending(item => item.Fans).ToList();
        }

        private static void InitializeFunctionsLikeVanilla(Rivals instance)
        {
            if (FuncGenreField == null || FuncLyricsField == null || FuncChoreoField == null)
            {
                throw new MissingFieldException(typeof(Rivals).FullName, "FuncGenre/FuncLyrics/FuncChoreo");
            }

            LinearFunction._function genre = FuncGenreField.GetValue(instance) as LinearFunction._function;
            LinearFunction._function lyrics = FuncLyricsField.GetValue(instance) as LinearFunction._function;
            LinearFunction._function choreo = FuncChoreoField.GetValue(instance) as LinearFunction._function;
            if (genre == null || lyrics == null || choreo == null)
            {
                throw new InvalidOperationException("Rivals linear-function instances are unavailable during legacy bootstrap.");
            }

            genre.Init(0f, 0f, (float)(Rivals.Groups.Count - 1), (float)(Rivals.Genres.Count - 1));
            lyrics.Init(0f, 0f, (float)(Rivals.Groups.Count - 1), (float)(Rivals.Lyrics.Count - 1));
            choreo.Init(0f, 0f, (float)(Rivals.Groups.Count - 1), (float)(Rivals.Choreography.Count - 1));
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
                SetDiagnostic("A23 could not resolve target SavedData: " + exception.Message);
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

    internal static class LegacyRivalBootstrapMigrationPatchHealth
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
