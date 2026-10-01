using System;
using System.Collections.Generic;

namespace IMDataCore
{
    public static partial class IMDataCoreApi
    {
        /// <summary>
        /// Sums actual, reconciled idol salary payments on the selected save branch.
        /// A zero without complete coverage or recorded payments is unknown.
        /// Does not estimate past pay from salary rates or change vanilla earnings.
        /// </summary>
        public static bool TryGetIdolCareerPay(int idolId, out long totalPaid,
            out bool isComplete, out bool hasRecordedPayments, out string errorMessage)
        {
            return IMDataCoreController.Instance.TryGetIdolCareerPay(idolId,
                out totalPaid, out isComplete, out hasRecordedPayments, out errorMessage);
        }
    }

    public static partial class IMDataCoreInteropApi
    {
        // Primitive-only signature supports optional consumers without a hard dependency.
        public static bool TryGetIdolCareerPay(int idolId, out long totalPaid,
            out bool isComplete, out bool hasRecordedPayments, out string errorMessage)
        {
            return IMDataCoreApi.TryGetIdolCareerPay(idolId,
                out totalPaid, out isComplete, out hasRecordedPayments, out errorMessage);
        }
    }

    public static partial class IMDataCoreAPI
    {
        public static bool TryGetIdolCareerPay(int idolId, out long totalPaid,
            out bool isComplete, out bool hasRecordedPayments, out string errorMessage)
        {
            return IMDataCoreApi.TryGetIdolCareerPay(idolId,
                out totalPaid, out isComplete, out hasRecordedPayments, out errorMessage);
        }
    }

    internal sealed partial class IMDataCoreController
    {
        private const string InvalidCareerPayIdMessage = "Career pay requires a non-negative idol identifier.";

        internal bool TryGetIdolCareerPay(int idolId, out long totalPaid,
            out bool isComplete, out bool hasRecordedPayments, out string errorMessage)
        {
            totalPaid = MoneyLedgerConstants.ZeroMoney;
            isComplete = false;
            hasRecordedPayments = false;
            errorMessage = string.Empty;
            if (idolId < 0)
            {
                errorMessage = InvalidCareerPayIdMessage;
                return false;
            }

            lock (runtimeLock)
            {
                if (!EnsureInitializedLocked(out errorMessage) || !FlushLocked(true, out errorMessage))
                    return false;
                long hireSequence;
                bool allocationsExact;
                if (!storageEngine.TryReadIdolCareerPay(idolId, out totalPaid,
                    out hasRecordedPayments, out hireSequence, out allocationsExact, out errorMessage))
                    return false;

                // Dates alone cannot prove that IMDC observed the hire or stayed active.
                // Assess durable coverage on the selected branch, including tracking gaps.
                if (hireSequence > 0L && allocationsExact && captureSequence < long.MaxValue)
                {
                    IMDataCoreCoverageAssessment assessment;
                    if (!TryAssessMoneyHistoryRange(
                        new IMDataCoreHistoryBoundary(hireSequence, DateTime.MinValue, string.Empty),
                        new IMDataCoreHistoryBoundary(captureSequence + 1L, staticVars.dateTime, string.Empty),
                        out assessment, out errorMessage))
                        return false;
                    isComplete = assessment != null &&
                        assessment.Knownness == IMDataCoreHistoryKnownness.Complete;
                }
                return true;
            }
        }
    }

    internal sealed partial class LightweightCoreStorageEngine
    {
        private const string CareerPayReadFailure = "Reading recorded idol salary payments failed: ";
        private const string InvalidCareerPayTransaction = "A money transaction could not be decoded.";

        internal bool TryReadIdolCareerPay(int idolId, out long totalPaid,
            out bool hasRecordedPayments, out long hireSequence,
            out bool allocationsExact, out string errorMessage)
        {
            totalPaid = MoneyLedgerConstants.ZeroMoney;
            hasRecordedPayments = false;
            hireSequence = 0L;
            allocationsExact = true;
            errorMessage = string.Empty;
            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();
                    List<LightweightEventRecord> timeline;
                    if (timelineEventsByIdolId.TryGetValue(idolId, out timeline))
                    {
                        foreach (LightweightEventRecord record in timeline)
                        {
                            if (record.EventType == CoreConstants.EventTypeIdolHired &&
                                (hireSequence == 0L || record.Sequence < hireSequence))
                                hireSequence = record.Sequence;
                        }
                    }

                    var payments = new Dictionary<string, IMDataCoreMoneyTransaction>(StringComparer.Ordinal);
                    var ambiguousGroups = new HashSet<string>(StringComparer.Ordinal);
                    // resources._Add already records each idol's allocation at payment time.
                    // This aggregate has no UI paging cap. Active indexes automatically
                    // rewind it when an earlier save checkpoint is selected.
                    foreach (List<LightweightEventRecord> rows in moneyTransactionsByDateKey.Values)
                    {
                        foreach (LightweightEventRecord record in rows)
                        {
                            IMDataCoreEvent moneyEvent = ToPublicEvent(record);
                            moneyEvent.PayloadJson = CorePayloadCompaction.ExpandMoneyTransactionPayloadForPublic(record);
                            IMDataCoreMoneyTransaction transaction = MoneyLedgerPayloadUtility.ToPublicModel(moneyEvent);
                            if (transaction == null)
                                throw new InvalidOperationException(InvalidCareerPayTransaction);
                            string group = transaction.TransactionGroup ?? string.Empty;
                            if (transaction.DetailCode == MoneyLedgerConstants.DetailWeeklyRemainder &&
                                transaction.Amount != MoneyLedgerConstants.ZeroMoney)
                            {
                                // A skipped, clamped, or altered debit cannot prove individual
                                // payments. Never split an unexplained remainder among idols.
                                ambiguousGroups.Add(group);
                                if (hireSequence == 0L || record.Sequence >= hireSequence)
                                    allocationsExact = false;
                            }
                            if (transaction.CategoryCode != MoneyLedgerConstants.CategoryIdolSalaries)
                                continue;
                            IMDataCoreMoneyTransactionDetail detail = transaction.Details;
                            if (detail == null || detail.Kind != MoneyLedgerConstants.DetailKindIdolSalary ||
                                detail.IdolId < 0)
                            {
                                allocationsExact = false;
                                continue;
                            }
                            if (detail.IdolId != idolId)
                                continue;
                            bool valid = !string.IsNullOrEmpty(group) && detail.SalaryAmount >= 0L &&
                                transaction.Amount != long.MinValue && -transaction.Amount == detail.SalaryAmount &&
                                checked(transaction.BalanceAfter - transaction.BalanceBefore) == transaction.Amount;
                            if (!valid || payments.ContainsKey(group))
                            {
                                ambiguousGroups.Add(group);
                                allocationsExact = false;
                                continue;
                            }
                            payments.Add(group, transaction);
                            if (hireSequence > 0L && record.Sequence < hireSequence)
                                allocationsExact = false;
                        }
                    }
                    foreach (KeyValuePair<string, IMDataCoreMoneyTransaction> payment in payments)
                    {
                        if (ambiguousGroups.Contains(payment.Key))
                            continue;
                        totalPaid = checked(totalPaid - payment.Value.Amount);
                        hasRecordedPayments = true;
                    }
                    return true;
                }
                catch (Exception exception)
                {
                    totalPaid = MoneyLedgerConstants.ZeroMoney;
                    hasRecordedPayments = false;
                    allocationsExact = false;
                    errorMessage = CareerPayReadFailure + exception.Message;
                    return false;
                }
            }
        }
    }
}

