using System;
using System.Collections.Generic;
using System.Threading;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A18: girls_trivia.LoadFunction() and Graduation_Trivia.LoadFunction()
    /// skip serialized rows whose Counter is zero. On a fresh process that happens to
    /// look harmless because authored counters start at zero, but same-process F9 can
    /// retain a positive counter from the discarded timeline.
    ///
    /// Reset the current authored counter tables to their neutral zero baseline before
    /// vanilla applies the target save. Vanilla then restores every nonzero row exactly
    /// as before, while explicit serialized zero rows remain authoritative zero. No
    /// supplemental persistence or loader replay is required.
    /// </summary>
    internal static class TriviaCounterRepair
    {
        private static long girlsLoadPrefixCount;
        private static long graduationLoadPrefixCount;
        private static long rowsVisitedCount;
        private static long nonzeroResetCount;
        private static long nullRowCount;
        private static long resetFailureCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return TriviaCounterPatchHealth.IsHealthy; }
        }

        internal static long GirlsLoadPrefixCount
        {
            get { return Interlocked.Read(ref girlsLoadPrefixCount); }
        }

        internal static long GraduationLoadPrefixCount
        {
            get { return Interlocked.Read(ref graduationLoadPrefixCount); }
        }

        internal static long RowsVisitedCount
        {
            get { return Interlocked.Read(ref rowsVisitedCount); }
        }

        internal static long NonzeroResetCount
        {
            get { return Interlocked.Read(ref nonzeroResetCount); }
        }

        internal static long NullRowCount
        {
            get { return Interlocked.Read(ref nullRowCount); }
        }

        internal static long ResetFailureCount
        {
            get { return Interlocked.Read(ref resetFailureCount); }
        }

        internal static string LastDiagnostic
        {
            get { return lastDiagnostic; }
        }

        internal static void ResetGirlsTriviaCounters()
        {
            Interlocked.Increment(ref girlsLoadPrefixCount);

            try
            {
                List<girls_trivia._data> rows = girls_trivia.Data;
                if (rows == null)
                {
                    RecordFailure("girls_trivia.Data is null at LoadFunction entry");
                    return;
                }

                int visited = 0;
                int reset = 0;
                int nullRows = 0;

                for (int index = 0; index < rows.Count; index++)
                {
                    girls_trivia._data row = rows[index];
                    if (row == null)
                    {
                        nullRows++;
                        continue;
                    }

                    visited++;
                    if (row.Counter != 0)
                    {
                        row.Counter = 0;
                        reset++;
                    }
                }

                Interlocked.Add(ref rowsVisitedCount, visited);
                Interlocked.Add(ref nonzeroResetCount, reset);
                Interlocked.Add(ref nullRowCount, nullRows);
                lastDiagnostic =
                    "Reset girls_trivia authored counters before target load: visited=" + visited +
                    ", nonzero_reset=" + reset + ", null_rows=" + nullRows + ".";
            }
            catch (Exception exception)
            {
                RecordFailure(
                    "Could not reset girls_trivia counters: " +
                    exception.GetType().Name + ": " + exception.Message);
            }
        }

        internal static void ResetGraduationTriviaCounters()
        {
            Interlocked.Increment(ref graduationLoadPrefixCount);

            try
            {
                List<Graduation_Trivia._trivia> rows = Graduation_Trivia.Trivia;
                if (rows == null)
                {
                    RecordFailure("Graduation_Trivia.Trivia is null at LoadFunction entry");
                    return;
                }

                int visited = 0;
                int reset = 0;
                int nullRows = 0;

                for (int index = 0; index < rows.Count; index++)
                {
                    Graduation_Trivia._trivia row = rows[index];
                    if (row == null)
                    {
                        nullRows++;
                        continue;
                    }

                    visited++;
                    if (row.Counter != 0)
                    {
                        row.Counter = 0;
                        reset++;
                    }
                }

                Interlocked.Add(ref rowsVisitedCount, visited);
                Interlocked.Add(ref nonzeroResetCount, reset);
                Interlocked.Add(ref nullRowCount, nullRows);
                lastDiagnostic =
                    "Reset Graduation_Trivia authored counters before target load: visited=" + visited +
                    ", nonzero_reset=" + reset + ", null_rows=" + nullRows + ".";
            }
            catch (Exception exception)
            {
                RecordFailure(
                    "Could not reset Graduation_Trivia counters: " +
                    exception.GetType().Name + ": " + exception.Message);
            }
        }

        private static void RecordFailure(string diagnostic)
        {
            Interlocked.Increment(ref resetFailureCount);
            lastDiagnostic = diagnostic ?? "unknown trivia-counter reset failure";
        }
    }

    internal static class TriviaCounterPatchHealth
    {
        private static readonly object Sync = new object();
        private static bool girlsTargetResolved;
        private static bool graduationTargetResolved;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return girlsTargetResolved &&
                           graduationTargetResolved &&
                           string.IsNullOrEmpty(failure);
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

        internal static void ReportGirlsTargetResolved()
        {
            lock (Sync)
            {
                girlsTargetResolved = true;
            }
        }

        internal static void ReportGraduationTargetResolved()
        {
            lock (Sync)
            {
                graduationTargetResolved = true;
            }
        }

        internal static void ReportFailure(string message)
        {
            lock (Sync)
            {
                failure = message ?? "unknown trivia-counter patch failure";
            }
        }
    }
}
