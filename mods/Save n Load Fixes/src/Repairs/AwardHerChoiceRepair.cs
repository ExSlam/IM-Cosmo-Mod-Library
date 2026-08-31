using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A24: Awards._speech.GetThanks() is non-pure for her_choice and vanilla
    /// asks it repeatedly while delivering one solo/group award speech. The first
    /// game-requested resolution is authoritative for that live speech occurrence;
    /// subsequent calls on the same _speech object reuse it.
    ///
    /// The cache is process-local and weakly keyed to the live speech object. It does
    /// not persist a resolved category, does not roll independently, and does not own
    /// completed award history. The existing checkpoint/dialogue safety layer keeps
    /// this transient occurrence out of legal save boundaries.
    /// </summary>
    internal static class AwardHerChoiceRepair
    {
        private sealed class ResolvedThanks
        {
            internal ResolvedThanks(Date_GroupTalk._message._category value)
            {
                Value = value;
            }

            internal readonly Date_GroupTalk._message._category Value;
        }

        private static readonly object CacheSync = new object();
        private static readonly ConditionalWeakTable<Awards._speech, ResolvedThanks> Cache =
            new ConditionalWeakTable<Awards._speech, ResolvedThanks>();

        private static long firstResolutionCount;
        private static long cachedReuseCount;
        private static long playerResolutionCount;
        private static long familyResolutionCount;
        private static long unexpectedResolutionCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return AwardHerChoicePatchHealth.IsHealthy; }
        }

        internal static long FirstResolutionCount
        {
            get { return Interlocked.Read(ref firstResolutionCount); }
        }

        internal static long CachedReuseCount
        {
            get { return Interlocked.Read(ref cachedReuseCount); }
        }

        internal static long PlayerResolutionCount
        {
            get { return Interlocked.Read(ref playerResolutionCount); }
        }

        internal static long FamilyResolutionCount
        {
            get { return Interlocked.Read(ref familyResolutionCount); }
        }

        internal static long UnexpectedResolutionCount
        {
            get { return Interlocked.Read(ref unexpectedResolutionCount); }
        }

        internal static string LastDiagnostic
        {
            get { return lastDiagnostic; }
        }

        internal static bool TryUseCachedResolution(
            Awards._speech speech,
            out Date_GroupTalk._message._category result)
        {
            result = default(Date_GroupTalk._message._category);

            if (speech == null || speech.Thanks != Awards._speech._thanks.her_choice)
            {
                return false;
            }

            ResolvedThanks cached;
            lock (CacheSync)
            {
                if (!Cache.TryGetValue(speech, out cached))
                {
                    return false;
                }
            }

            result = cached.Value;
            Interlocked.Increment(ref cachedReuseCount);
            lastDiagnostic =
                "Reused the first resolved her_choice category for the same live award-speech occurrence: " +
                result + ".";
            return true;
        }

        internal static void ObserveOriginalResolution(
            Awards._speech speech,
            Date_GroupTalk._message._category result)
        {
            if (speech == null || speech.Thanks != Awards._speech._thanks.her_choice)
            {
                return;
            }

            bool added = false;
            lock (CacheSync)
            {
                ResolvedThanks existing;
                if (!Cache.TryGetValue(speech, out existing))
                {
                    Cache.Add(speech, new ResolvedThanks(result));
                    added = true;
                }
            }

            // Harmony Postfix still runs when our Prefix skips the original. Only the
            // first original result becomes authoritative; cached calls are counted in
            // TryUseCachedResolution and never overwrite that first value.
            if (!added)
            {
                return;
            }

            Interlocked.Increment(ref firstResolutionCount);
            if (result == Date_GroupTalk._message._category.thanks_player)
            {
                Interlocked.Increment(ref playerResolutionCount);
            }
            else if (result == Date_GroupTalk._message._category.thanks_family)
            {
                Interlocked.Increment(ref familyResolutionCount);
            }
            else
            {
                Interlocked.Increment(ref unexpectedResolutionCount);
            }

            lastDiagnostic =
                "Captured first authoritative her_choice resolution for one live award-speech occurrence: " +
                result + ".";
        }
    }

    internal static class AwardHerChoicePatchHealth
    {
        private static readonly object Sync = new object();
        private static bool targetResolved;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return targetResolved && string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static string Failure
        {
            get
            {
                lock (Sync)
                {
                    return failure;
                }
            }
        }

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                targetResolved = true;
            }
        }

        internal static void ReportFailure(string message)
        {
            lock (Sync)
            {
                failure = message ?? "unknown award her_choice patch failure";
            }
        }
    }
}
