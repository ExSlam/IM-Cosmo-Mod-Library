using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A06: loans.LoadFunction() restores the serialized bankruptcy deadline
    /// but Reset() clears BankruptcyDanger. The next negative-money daily check would
    /// otherwise start a fresh one-month countdown.
    ///
    /// The danger flag is exactly reconstructible from the target save's serialized
    /// money row. Read that DTO directly so load-subscriber ordering cannot leak stale
    /// process RAM into the result, and mutate only the boolean flag.
    /// </summary>
    internal static class BankruptcyDangerRepair
    {
        private static long reconstructedCount;
        private static long negativeMoneyDangerCount;
        private static long nonNegativeMoneyClearCount;
        private static long missingMoneyRowCount;
        private static long duplicateMoneyRowCount;
        private static long failureCount;
        private static long lastSerializedMoney;
        private static long lastPreservedDeadlineTicks;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return BankruptcyDangerPatchHealth.IsHealthy; }
        }

        internal static long ReconstructedCount
        {
            get { return Interlocked.Read(ref reconstructedCount); }
        }

        internal static long NegativeMoneyDangerCount
        {
            get { return Interlocked.Read(ref negativeMoneyDangerCount); }
        }

        internal static long NonNegativeMoneyClearCount
        {
            get { return Interlocked.Read(ref nonNegativeMoneyClearCount); }
        }

        internal static long MissingMoneyRowCount
        {
            get { return Interlocked.Read(ref missingMoneyRowCount); }
        }

        internal static long DuplicateMoneyRowCount
        {
            get { return Interlocked.Read(ref duplicateMoneyRowCount); }
        }

        internal static long FailureCount
        {
            get { return Interlocked.Read(ref failureCount); }
        }

        internal static long LastSerializedMoney
        {
            get { return Interlocked.Read(ref lastSerializedMoney); }
        }

        internal static long LastPreservedDeadlineTicks
        {
            get { return Interlocked.Read(ref lastPreservedDeadlineTicks); }
        }

        internal static string LastDiagnostic
        {
            get { return lastDiagnostic; }
        }

        internal static void ReconstructFromSerializedMoney()
        {
            try
            {
                SaveManager.SavedData data = GetTargetSavedData();
                if (data == null)
                {
                    RecordFailure("target SavedData is unavailable at loans.LoadFunction completion");
                    return;
                }

                List<resources.ResourceData> rows = data.resources__Resources;
                if (rows == null)
                {
                    RecordMissingMoney("target SavedData has no resources__Resources collection");
                    return;
                }

                bool foundMoney = false;
                long serializedMoney = 0L;
                int moneyRowCount = 0;

                for (int index = 0; index < rows.Count; index++)
                {
                    resources.ResourceData row = rows[index];
                    if (row == null || row.Type != resources.type.money)
                    {
                        continue;
                    }

                    foundMoney = true;
                    serializedMoney = row.Val;
                    moneyRowCount++;
                }

                if (!foundMoney)
                {
                    RecordMissingMoney("target SavedData contains no serialized money ResourceData row");
                    return;
                }

                // If malformed input contains duplicate money rows, vanilla resources.LoadFunction()
                // applies them sequentially, so the last serialized row is the eventual target value.
                if (moneyRowCount > 1)
                {
                    Interlocked.Add(ref duplicateMoneyRowCount, moneyRowCount - 1L);
                }

                DateTime preservedDeadline = loans.BankruptcyDate;
                bool expectedDanger = serializedMoney < 0L;

                loans.BankruptcyDanger = expectedDanger;

                Interlocked.Exchange(ref lastSerializedMoney, serializedMoney);
                Interlocked.Exchange(ref lastPreservedDeadlineTicks, preservedDeadline.Ticks);
                Interlocked.Increment(ref reconstructedCount);

                if (expectedDanger)
                {
                    Interlocked.Increment(ref negativeMoneyDangerCount);
                }
                else
                {
                    Interlocked.Increment(ref nonNegativeMoneyClearCount);
                }

                lastDiagnostic =
                    "Reconstructed loans.BankruptcyDanger=" + expectedDanger +
                    " from serialized money=" + serializedMoney +
                    " while preserving BankruptcyDate ticks=" + preservedDeadline.Ticks +
                    (moneyRowCount > 1
                        ? "; duplicate money rows=" + moneyRowCount + " (last row used to match vanilla load order)."
                        : ".");
            }
            catch (Exception exception)
            {
                RecordFailure(
                    "Could not reconstruct loans.BankruptcyDanger: " +
                    exception.GetType().Name + ": " + exception.Message);
            }
        }

        private static SaveManager.SavedData GetTargetSavedData()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return null;
            }

            mainScript main = camera.GetComponent<mainScript>();
            return main == null ? null : main.GetSavedData();
        }

        private static void RecordMissingMoney(string diagnostic)
        {
            Interlocked.Increment(ref missingMoneyRowCount);
            lastDiagnostic = diagnostic ?? "serialized money row is unavailable";
        }

        private static void RecordFailure(string diagnostic)
        {
            Interlocked.Increment(ref failureCount);
            lastDiagnostic = diagnostic ?? "unknown BankruptcyDanger reconstruction failure";
        }
    }

    internal static class BankruptcyDangerPatchHealth
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
                failure = message ?? "unknown BankruptcyDanger patch failure";
            }
        }
    }
}
