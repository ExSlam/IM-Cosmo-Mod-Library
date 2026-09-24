using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A33.1 checked-wide arithmetic and failure ownership. Authoritative gameplay
    /// values never saturate: a signed Int64 overflow is reported, latched, and thrown.
    /// Int32 clamping is reserved for explicitly named compatibility mirrors.
    /// </summary>
    internal static class WideNumericRepair
    {
        private static readonly object Sync = new object();

        private static long checkedOperationCount;
        private static long overflowFailureCount;
        private static long invariantFailureCount;
        private static long businessHistoryCorrectionCount;
        private static int failureLatched;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get
            {
                return WideNumericPatchHealth.IsHealthy &&
                    WideNumericContinuationPatchHealth.IsHealthy;
            }
        }

        internal static bool IsHealthy
        {
            get { return IsImplemented && !HasLatchedFailure; }
        }

        internal static bool IsCheckpointSafe
        {
            get { return IsHealthy; }
        }

        internal static bool HasLatchedFailure
        {
            get { return Volatile.Read(ref failureLatched) != 0; }
        }

        internal static long CheckedOperationCount
        {
            get { return Interlocked.Read(ref checkedOperationCount); }
        }

        internal static long OverflowFailureCount
        {
            get { return Interlocked.Read(ref overflowFailureCount); }
        }

        internal static long InvariantFailureCount
        {
            get { return Interlocked.Read(ref invariantFailureCount); }
        }

        internal static long BusinessHistoryCorrectionCount
        {
            get { return Interlocked.Read(ref businessHistoryCorrectionCount); }
        }

        internal static string LastDiagnostic
        {
            get
            {
                lock (Sync)
                {
                    return lastDiagnostic;
                }
            }
        }

        internal static string CheckpointUnsafeReason
        {
            get
            {
                if (!WideNumericPatchHealth.IsHealthy)
                {
                    string failure = WideNumericPatchHealth.Failure;
                    if (!string.IsNullOrEmpty(failure))
                    {
                        return "A33.1 patch health failed: " + failure;
                    }

                    return string.Concat(
                        "A33.1 patch manifest is incomplete (",
                        WideNumericPatchHealth.ResolvedTargetMethodCount.ToString(
                            CultureInfo.InvariantCulture),
                        "/",
                        WideNumericPatchHealth.ExpectedTargetMethodCount.ToString(
                            CultureInfo.InvariantCulture),
                        ")");
                }

                if (!WideNumericContinuationPatchHealth.IsHealthy)
                {
                    string failure = WideNumericContinuationPatchHealth.Failure;
                    if (!string.IsNullOrEmpty(failure))
                    {
                        return "A33.2-A33.6 patch health failed: " + failure;
                    }
                    return string.Concat(
                        "A33.2-A33.6 patch manifest is incomplete (",
                        WideNumericContinuationPatchHealth.ResolvedTargetMethodCount.ToString(
                            CultureInfo.InvariantCulture),
                        "/",
                        WideNumericContinuationPatchHealth.ExpectedTargetMethodCount.ToString(
                            CultureInfo.InvariantCulture),
                        ")");
                }

                if (HasLatchedFailure)
                {
                    return "A33 numeric failure is latched: " + LastDiagnostic;
                }

                return string.Empty;
            }
        }

        internal static long Add(long left, long right, string context)
        {
            try
            {
                long result = WideNumericMath.Add(left, right);
                Interlocked.Increment(ref checkedOperationCount);
                return result;
            }
            catch (OverflowException exception)
            {
                throw CreateOverflowException(context, "add", left, right, exception);
            }
        }

        internal static long Subtract(long left, long right, string context)
        {
            try
            {
                long result = WideNumericMath.Subtract(left, right);
                Interlocked.Increment(ref checkedOperationCount);
                return result;
            }
            catch (OverflowException exception)
            {
                throw CreateOverflowException(context, "subtract", left, right, exception);
            }
        }

        internal static long Multiply(long left, long right, string context)
        {
            try
            {
                long result = WideNumericMath.Multiply(left, right);
                Interlocked.Increment(ref checkedOperationCount);
                return result;
            }
            catch (OverflowException exception)
            {
                throw CreateOverflowException(context, "multiply", left, right, exception);
            }
        }

        internal static long RoundRatioToEven(
            long value,
            int numerator,
            int denominator,
            string context)
        {
            try
            {
                long result = WideNumericMath.RoundRatioToEven(
                    value,
                    numerator,
                    denominator);
                Interlocked.Increment(ref checkedOperationCount);
                return result;
            }
            catch (OverflowException exception)
            {
                throw CreateOverflowException(
                    context,
                    "round-ratio " + numerator.ToString(CultureInfo.InvariantCulture) +
                        "/" + denominator.ToString(CultureInfo.InvariantCulture),
                    value,
                    0L,
                    exception);
            }
        }

        internal static long RoundDecimalProductToEven(
            long value,
            decimal coefficient,
            string context)
        {
            try
            {
                long result = WideNumericMath.RoundDecimalProductToEven(value, coefficient);
                Interlocked.Increment(ref checkedOperationCount);
                return result;
            }
            catch (OverflowException exception)
            {
                throw CreateOverflowException(
                    context,
                    "round-decimal " + coefficient.ToString(CultureInfo.InvariantCulture),
                    value,
                    0L,
                    exception);
            }
        }

        internal static long RoundSingleProductToEven(
            long value,
            string context,
            params float[] coefficients)
        {
            try
            {
                long result = WideNumericMath.RoundSingleProductToEven(value, coefficients);
                Interlocked.Increment(ref checkedOperationCount);
                return result;
            }
            catch (OverflowException exception)
            {
                throw CreateOverflowException(
                    context,
                    "exact Single-coefficient product",
                    value,
                    coefficients == null ? 0L : coefficients.Length,
                    exception);
            }
        }

        internal static long TruncateSingleProduct(
            long value,
            string context,
            params float[] coefficients)
        {
            try
            {
                long result = WideNumericMath.TruncateSingleProduct(value, coefficients);
                Interlocked.Increment(ref checkedOperationCount);
                return result;
            }
            catch (OverflowException exception)
            {
                throw CreateOverflowException(
                    context,
                    "exact Single-coefficient truncation",
                    value,
                    coefficients == null ? 0L : coefficients.Length,
                    exception);
            }
        }

        internal static long FloorSingleProduct(
            long value,
            string context,
            params float[] coefficients)
        {
            try
            {
                long result = WideNumericMath.FloorSingleProduct(value, coefficients);
                Interlocked.Increment(ref checkedOperationCount);
                return result;
            }
            catch (OverflowException exception)
            {
                throw CreateOverflowException(
                    context,
                    "exact Single-coefficient floor",
                    value,
                    coefficients == null ? 0L : coefficients.Length,
                    exception);
            }
        }

        internal static long CeilingSingleProduct(
            long value,
            string context,
            params float[] coefficients)
        {
            try
            {
                long result = WideNumericMath.CeilingSingleProduct(value, coefficients);
                Interlocked.Increment(ref checkedOperationCount);
                return result;
            }
            catch (OverflowException exception)
            {
                throw CreateOverflowException(
                    context,
                    "exact Single-coefficient ceiling",
                    value,
                    coefficients == null ? 0L : coefficients.Length,
                    exception);
            }
        }

        internal static long DivideRoundToEven(
            long numerator,
            long denominator,
            string context)
        {
            try
            {
                long result = WideNumericMath.DivideRoundToEven(numerator, denominator);
                Interlocked.Increment(ref checkedOperationCount);
                return result;
            }
            catch (OverflowException exception)
            {
                throw CreateOverflowException(
                    context,
                    "divide-round-to-even",
                    numerator,
                    denominator,
                    exception);
            }
        }

        internal static long RoundRatioWithSingleProductsToEven(
            long value,
            long denominator,
            string context,
            params float[] coefficients)
        {
            try
            {
                long result = WideNumericMath.RoundRatioWithSingleProductsToEven(
                    value, denominator, coefficients);
                Interlocked.Increment(ref checkedOperationCount);
                return result;
            }
            catch (OverflowException exception)
            {
                throw CreateOverflowException(context, "exact rational/Single product",
                    value, denominator, exception);
            }
        }

        internal static long RoundProductRatioWithSingleProductsToEven(
            long left,
            long right,
            long denominator,
            string context,
            params float[] coefficients)
        {
            try
            {
                long result = WideNumericMath.RoundProductRatioWithSingleProductsToEven(
                    left, right, denominator, coefficients);
                Interlocked.Increment(ref checkedOperationCount);
                return result;
            }
            catch (OverflowException exception)
            {
                throw CreateOverflowException(context,
                    "exact product-ratio/Single product", left, right, exception);
            }
        }

        internal static long RoundLinearCombinationWithSingleProductsToEven(
            long first,
            long firstFactor,
            long second,
            long secondFactor,
            long denominator,
            string context,
            params float[] coefficients)
        {
            try
            {
                long result = WideNumericMath.RoundLinearCombinationWithSingleProductsToEven(
                    first, firstFactor, second, secondFactor, denominator, coefficients);
                Interlocked.Increment(ref checkedOperationCount);
                return result;
            }
            catch (OverflowException exception)
            {
                throw CreateOverflowException(context,
                    "exact linear-combination/Single product", first, second, exception);
            }
        }

        internal static int ToInt32Exact(long value, string context)
        {
            try
            {
                int result = WideNumericMath.ToInt32Exact(value);
                Interlocked.Increment(ref checkedOperationCount);
                return result;
            }
            catch (OverflowException exception)
            {
                throw CreateOverflowException(context, "Int64-to-Int32", value, 0L, exception);
            }
        }

        internal static void ValidateResourceAdd(resources.type type, long delta)
        {
            if (delta == 0L)
            {
                return;
            }

            Add(
                resources.Get(type, false),
                delta,
                "resources._Add(" + type.ToString() + ")");
        }

        internal static void ValidateFanPeopleAdd(resources._fan fan, long delta)
        {
            if (delta == 0L)
            {
                return;
            }

            Add(fan.people, delta, "resources._fan.AddPeople");
        }

        internal static long CalculateTicketSales(Theaters._theater theater)
        {
            int visitors = theater.GetNumberOfVisitors();
            return Multiply(
                (long)visitors,
                (long)theater.Ticket_Price,
                "Theaters._theater.GetTicketSales");
        }

        internal static long SumShowLongValues(List<long> values)
        {
            long total = 0L;
            for (int index = 0; index < values.Count; index++)
            {
                total = Add(
                    total,
                    values[index],
                    "Shows._show.GetTotalParam(List<Int64>)");
            }

            return total;
        }

        internal static long CalculateShowTotalProfit(Shows._show show)
        {
            long revenue = show.GetTotalParam(show.revenue);
            long cost = Multiply(
                (long)show.GetBudget(),
                (long)show.revenue.Count,
                "Shows._show.GetTotalProfit episode cost");
            return Subtract(revenue, cost, "Shows._show.GetTotalProfit");
        }

        internal static long CalculateTotalAvailableLoanAmount(loans._loan._type type)
        {
            int fameLevel = resources.GetFameLevel();
            long amount;
            if (fameLevel == 0)
            {
                amount = 500000L;
            }
            else
            {
                amount = Multiply(1000000L, (long)fameLevel,
                    "loans.GetTotalAvailableAmount fame scale");
                amount = Multiply(amount, (long)fameLevel,
                    "loans.GetTotalAvailableAmount fame square");
            }

            if (type == loans._loan._type.fujimoto)
            {
                amount = Multiply(amount, 2L,
                    "loans.GetTotalAvailableAmount Fujimoto multiplier");
            }

            if (type == loans._loan._type.bank)
            {
                amount = Multiply(amount, 10L,
                    "loans.GetTotalAvailableAmount bank multiplier");
            }

            return TbsBalancePatchWideNumericInterop.ApplyLoanAvailabilityMultiplier(amount);
        }

        internal static long CalculateAmountAvailableForLoan(loans._loan._type type)
        {
            long committed = 0L;
            foreach (loans._loan loan in loans.Loans)
            {
                if (loan.Active && loan.Type == type)
                {
                    committed = Add(
                        committed,
                        loan.Amount,
                        "loans.GetAmountAvailableForLoan committed amount");
                }
            }

            return Subtract(
                CalculateTotalAvailableLoanAmount(type),
                committed,
                "loans.GetAmountAvailableForLoan");
        }

        internal static long CalculateTotalDebt()
        {
            long total = 0L;
            foreach (loans._loan loan in loans.Loans)
            {
                total = Add(total, loan.GetDebt(), "loans.GetTotalDebt");
            }

            return total;
        }

        internal static long CalculateBusinessMoneyEarned(business.active_proposal proposal)
        {
            long completedWeeks = Subtract(
                12L,
                (long)proposal.GetWeeksLeft(),
                "business.active_proposal.GetMoneyEarned elapsed weeks");
            return Multiply(
                completedWeeks,
                WideNumericState.GetBusinessContractPayment(proposal),
                "business.active_proposal.GetMoneyEarned");
        }

        internal static long CalculateBusinessLiability(business._proposal proposal)
        {
            business._data data = business.GetData(proposal.type);
            if (!data.liability)
            {
                return 0L;
            }

            long payment = WideNumericContinuation.GetBusinessProposalPayment(proposal);
            long liability;
            if (proposal.duration == 0)
            {
                liability = Multiply(payment, 2L,
                    "business._proposal.GetLiability immediate payment");
            }
            else
            {
                liability = Multiply(payment, 2L,
                    "business._proposal.GetLiability doubled payment");
                liability = Multiply(liability, (long)proposal.duration,
                    "business._proposal.GetLiability duration");
                liability = Multiply(liability, 4L,
                    "business._proposal.GetLiability weeks per month");
            }

            if (proposal.girl != null && proposal.girl.GetScandalPoints() > 4)
            {
                liability = Multiply(liability, 2L,
                    "business._proposal.GetLiability high-scandal multiplier");
            }
            else if (proposal.girl != null && proposal.girl.GetScandalPoints() > 2)
            {
                liability = RoundDecimalProductToEven(
                    liability,
                    1.5m,
                    "business._proposal.GetLiability scandal multiplier");
            }

            decimal coefficient = ScandalPoints.GetLiabilityCoeff(-1);
            if (coefficient > 1m)
            {
                liability = RoundDecimalProductToEven(
                    liability,
                    coefficient,
                    "business._proposal.GetLiability global multiplier");
            }

            return liability;
        }

        internal static WideBusinessAcceptState PrepareBusinessAccept(business owner)
        {
            if (owner == null || owner.ActiveProposal == null)
            {
                return null;
            }

            business._proposal proposal = owner.ActiveProposal;
            WideNumericContinuation.PreflightBusinessAcceptCounters(proposal);
            long exactPayment = WideNumericContinuation.GetBusinessProposalPayment(proposal);
            int observedPayment = WideNumericMath.ClampToInt32(exactPayment);

            // Accept applies one immediate payment to both agency money and the idol's
            // career earnings even when the proposal also becomes a weekly contract.
            // Preflight the authoritative exact mutation before vanilla commits the
            // Int32 compatibility image.
            long agencyBefore = resources.Get(resources.type.money, false);
            Add(agencyBefore, exactPayment, "business.Accept agency payment preflight");
            if (proposal.girl != null)
                Add(proposal.girl.Earnings_CurrentMonth, exactPayment,
                    "business.Accept idol earnings preflight");

            long exactMoney = exactPayment;
            long expectedVanillaMoney = observedPayment;
            if (proposal.duration > 0)
            {
                exactMoney = Multiply(exactMoney, (long)proposal.duration,
                    "business.Accept history duration");
                exactMoney = Multiply(exactMoney, 4L,
                    "business.Accept history weeks per month");
                expectedVanillaMoney = (long)unchecked(observedPayment * proposal.duration * 4);
            }

            return new WideBusinessAcceptState
            {
                Proposal = proposal,
                HistoryCount = business.History.Count,
                ExactPayment = exactPayment,
                CompatibilityPayment = observedPayment,
                ExactHistoryMoney = exactMoney,
                ExpectedVanillaHistoryMoney = expectedVanillaMoney
            };
        }

        internal static void CompleteBusinessAccept(WideBusinessAcceptState state)
        {
            if (state == null)
            {
                return;
            }

            long paymentCorrection = BuffMeWideNumericInterop.CalculateExactResourceCorrection(
                resources.type.money, state.ExactPayment, state.CompatibilityPayment,
                "business.Accept BuffMe-aware exact payment correction");
            long idolEarningsCorrection = Subtract(
                state.ExactPayment, state.CompatibilityPayment,
                "business.Accept exact idol-earnings correction");
            if (paymentCorrection != 0L)
            {
                BuffMeWideNumericInterop.BeginResourceMultiplierSuppression();
                try
                {
                    resources.Add(resources.type.money, paymentCorrection);
                }
                finally
                {
                    BuffMeWideNumericInterop.EndResourceMultiplierSuppression();
                }
            }
            if (idolEarningsCorrection != 0L && state.Proposal != null &&
                state.Proposal.girl != null)
                state.Proposal.girl.Earn(idolEarningsCorrection);

            if (business.History == null ||
                business.History.Count != state.HistoryCount + 1)
            {
                LatchInvariantFailure(
                    "business.Accept did not append exactly one history row; the checked " +
                    "history value could not be committed authoritatively.");
                return;
            }

            business._history history = business.History[business.History.Count - 1];
            if (history == null ||
                history.Girl != state.Proposal.girl ||
                history.Type != state.Proposal.type)
            {
                LatchInvariantFailure(
                    "business.Accept appended a history row whose proposal witness did " +
                    "not match the preflighted proposal.");
                return;
            }

            if (history.Money != state.ExpectedVanillaHistoryMoney &&
                history.Money != state.ExactHistoryMoney)
            {
                LatchInvariantFailure(
                    "business.Accept history money matched neither the preflighted " +
                    "vanilla Int32 image nor the exact checked Int64 value. Another " +
                    "runtime mutation changed the audited arithmetic inputs.");
                return;
            }

            if (history.Money != state.ExactHistoryMoney)
            {
                Interlocked.Increment(ref businessHistoryCorrectionCount);
            }

            history.Money = state.ExactHistoryMoney;
            Interlocked.Increment(ref checkedOperationCount);
        }

        internal static void AddGroupBuzz()
        {
            int multiplier = 0;
            policies._value value = policies.GetSelectedPolicyValue(
                policies._type.social_media).Value;
            if (value == policies._value.social_media_premoderated)
            {
                multiplier = 1;
            }
            else if (value == policies._value.social_media_no_restrictions)
            {
                multiplier = 2;
            }

            if (multiplier == 0)
            {
                return;
            }

            long delta = Multiply(
                (long)data_girls.GetActiveGirls(null).Count,
                (long)multiplier,
                "data_girls.AddBuzz");
            resources.Add(resources.type.buzz, delta);
        }

        internal static long CalculateConcertProductionCost(
            SEvent_Concerts._concert._projectedValues projected)
        {
            long songCost = Multiply(
                (long)projected.Parent.GetListOfSongs().Count,
                (long)SEvent_Concerts.GetVenueSongCost(projected.Parent.Venue),
                "SEvent_Concerts._concert._projectedValues.GetProductionCost song cost");
            long rawCost = Add(
                (long)SEvent_Concerts.GetVenueBaseCost(projected.Parent.Venue),
                songCost,
                "SEvent_Concerts._concert._projectedValues.GetProductionCost base plus songs");

            int numerator = 1;
            int denominator = 1;
            if (Awards.HasAward(Awards._type.most_prolific_group))
            {
                numerator = 1;
                denominator = 2;
            }
            else if (Awards.HasNomination(Awards._type.most_prolific_group))
            {
                numerator = 3;
                denominator = 4;
            }

            if (variables.Get("CHEAPER_EVENTS") == "true" &&
                staticVars.PlayerData.Chapter < tasks._chapter.chapter_5)
            {
                numerator = checked(numerator * 3);
                denominator = checked(denominator * 4);
            }

            return RoundRatioToEven(
                rawCost,
                numerator,
                denominator,
                "SEvent_Concerts._concert._projectedValues.GetProductionCost coefficient");
        }

        internal static long CalculateStaffSeverance(staff._staff person)
        {
            if (person == null) return 0L;
            if ((staticVars.dateTime - person.HireDate).Days < 30) return 0L;
            return Multiply(person.GetSalary(), 48L,
                "staff._staff.Severance salary times 48 weeks");
        }

        internal static long CalculateStaffFireSeverance()
        {
            if (Staff_Fire.Staff == null) return 0L;

            long severance = CalculateStaffSeverance(Staff_Fire.Staff);
            long availableMoney = resources.Money();
            if (severance <= 0L || availableMoney <= 0L) return 0L;

            long doubled = Multiply(
                severance,
                2L,
                "Staff_Fire.GetSeverance doubled severance");
            if (doubled < availableMoney / 2L) return doubled;
            if (severance > availableMoney) return availableMoney;
            return severance;
        }

        internal static int CalculateSeveranceCompatibility()
        {
            // The vanilla ABI is Int32. Keep it as a deterministic compatibility
            // mirror only; exact SNLF consumers use CalculateStaffFireSeverance().
            return WideNumericMath.ClampToInt32(CalculateStaffFireSeverance());
        }

        internal static long CalculateFansByType(
            resources.fanType? gender,
            resources.fanType? hardcoreness,
            resources.fanType? age)
        {
            long total = 0L;
            foreach (resources._fan fan in resources.Fans)
            {
                if (gender != null && fan.gender != gender.Value)
                {
                    continue;
                }

                if (hardcoreness != null && fan.hardcoreness != hardcoreness.Value)
                {
                    continue;
                }

                if (age != null && fan.age != age.Value)
                {
                    continue;
                }

                total = Add(total, fan.people, "resources.FansByType");
            }

            return total;
        }

        internal static long CalculateLegacyFanTotal()
        {
            long total = 0L;
            foreach (resources._fan fan in resources.Fans_Legacy)
            {
                total = Add(total, fan.people, "resources.GetFansTotal_Legacy");
            }

            return total;
        }

        internal static long CalculateActiveIdolFanTotal(resources.fanType? fanType)
        {
            long total = 0L;
            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (girl.status != data_girls._status.graduated)
                {
                    total = Add(
                        total,
                        girl.GetFans_Total(fanType),
                        "resources.GetFansTotal");
                }
            }

            if (total >= 1000000L)
            {
                Achievements.Unlock(Achievements.ID.ACH_MILLION_FANS);
            }

            return total;
        }

        internal static long CalculateGroupFansByType(
            Groups._group group,
            resources.fanType gender,
            resources.fanType hardcoreness,
            resources.fanType age)
        {
            if (group == null)
            {
                LatchInvariantFailure("Groups._group.GetFansOfType received a null group.");
                throw new ArgumentNullException(nameof(group));
            }

            long total = 0L;
            foreach (data_girls.girls girl in group.GetGirls(true, false, null))
            {
                total = Add(
                    total,
                    girl.GetFan_Count(gender, hardcoreness, age),
                    "Groups._group.GetFansOfType(demographic)");
            }
            return total;
        }

        internal static long CalculateGroupFansByType(
            Groups._group group,
            resources.fanType? type)
        {
            if (group == null)
            {
                LatchInvariantFailure("Groups._group.GetFansOfType received a null group.");
                throw new ArgumentNullException(nameof(group));
            }

            long total = 0L;
            foreach (data_girls.girls girl in group.GetGirls(true, false, null))
            {
                total = Add(total, girl.GetFans_Total(type),
                    "Groups._group.GetFansOfType(Nullable)");
            }
            return total;
        }

        private static OverflowException CreateOverflowException(
            string context,
            string operation,
            long left,
            long right,
            OverflowException inner)
        {
            Interlocked.Increment(ref overflowFailureCount);
            string diagnostic = string.Concat(
                "A33.1 refused an overflowing ",
                operation,
                " in ",
                context ?? "<unknown>",
                ": left=",
                left.ToString(CultureInfo.InvariantCulture),
                ", right=",
                right.ToString(CultureInfo.InvariantCulture),
                ". The patched arithmetic result was refused before it could be committed.");

            LatchFailure(diagnostic);
            return new OverflowException(
                SaveNLoadFixesConstants.LogPrefix + diagnostic,
                inner);
        }

        internal static void LatchInvariantFailure(string diagnostic)
        {
            Interlocked.Increment(ref invariantFailureCount);
            LatchFailure("A33 invariant failure: " + diagnostic);
        }

        internal static void RefuseIdentityExhaustion(string context)
        {
            string diagnostic = "A33.6 refused exhausted persistent Int32 identity at " +
                (context ?? "<unknown>") + ". No wrapped or reused ID was allocated.";
            LatchInvariantFailure(diagnostic);
            throw new OverflowException(SaveNLoadFixesConstants.LogPrefix + diagnostic);
        }

        internal static void RefuseCounterExhaustion(
            string context, int current, int delta)
        {
            string diagnostic = string.Concat(
                "A33.6 refused an overflowing persistent Int32 lifetime counter at ",
                context ?? "<unknown>", ": current=",
                current.ToString(CultureInfo.InvariantCulture), ", delta=",
                delta.ToString(CultureInfo.InvariantCulture),
                ". The owning gameplay action was refused before mutation.");
            LatchInvariantFailure(diagnostic);
            throw new OverflowException(SaveNLoadFixesConstants.LogPrefix + diagnostic);
        }

        private static void LatchFailure(string diagnostic)
        {
            Interlocked.Exchange(ref failureLatched, 1);
            lock (Sync)
            {
                lastDiagnostic = diagnostic ?? "unknown A33.1 numeric failure";
            }

            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + LastDiagnostic);
        }
    }

    internal sealed class WideBusinessAcceptState
    {
        internal business._proposal Proposal;
        internal int HistoryCount;
        internal long ExactPayment;
        internal int CompatibilityPayment;
        internal long ExactHistoryMoney;
        internal long ExpectedVanillaHistoryMoney;
    }

    internal static class WideNumericPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 17;

        private static readonly object Sync = new object();
        private static readonly HashSet<string> ExpectedTargetIds =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "resources._Add(resources.type,Int64)",
                "resources._fan.AddPeople(Int64)",
                "resources.FansByType(Nullable,Nullable,Nullable)",
                "resources.GetFansTotal_Legacy()",
                "resources.GetFansTotal(Nullable)",
                "Theaters._theater.GetTicketSales()",
                "Shows._show.GetTotalParam(List<Int64>)",
                "Shows._show.GetTotalProfit()",
                "loans.GetTotalAvailableAmount(_type)",
                "loans.GetAmountAvailableForLoan(_type)",
                "loans.GetTotalDebt()",
                "business.active_proposal.GetMoneyEarned()",
                "business._proposal.GetLiability()",
                "business.Accept()",
                "data_girls.AddBuzz()",
                "SEvent_Concerts._concert._projectedValues.GetProductionCost()",
                "Staff_Fire.GetSeverance()"
            };
        private static readonly HashSet<string> ResolvedTargetIds =
            new HashSet<string>(StringComparer.Ordinal);

        private static string failure = string.Empty;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return ResolvedTargetIds.Count == ExpectedTargetMethodCount &&
                        string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static int ResolvedTargetMethodCount
        {
            get
            {
                lock (Sync)
                {
                    return ResolvedTargetIds.Count;
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

        internal static string LastDiagnostic
        {
            get
            {
                lock (Sync)
                {
                    return lastDiagnostic;
                }
            }
        }

        internal static MethodBase ResolveTarget(
            string targetId,
            Type declaringType,
            string methodName,
            Type[] parameterTypes,
            Type returnType,
            bool isStatic)
        {
            MethodInfo method = AccessTools.Method(declaringType, methodName, parameterTypes);
            if (method == null ||
                method.DeclaringType != declaringType ||
                method.ReturnType != returnType ||
                method.IsStatic != isStatic)
            {
                string diagnostic = string.Concat(
                    "A33.1 could not resolve the frozen target ",
                    targetId,
                    " with its audited declaring type, parameters, return type, and " +
                    "static/instance shape.");
                ReportFailure(diagnostic);
                throw new MissingMethodException(declaringType.FullName, methodName);
            }

            ReportTargetResolved(targetId);
            return method;
        }

        internal static void ReportTargetResolved(string targetId)
        {
            lock (Sync)
            {
                if (!ExpectedTargetIds.Contains(targetId))
                {
                    failure = "A33.1 resolved an unrecognized target identity: " + targetId;
                    lastDiagnostic = failure;
                    return;
                }

                // HarmonyX may rediscover an already-installed target during
                // recomposition. Identity-set insertion makes that observation neutral.
                ResolvedTargetIds.Add(targetId);
                lastDiagnostic = string.Concat(
                    "A33.1 resolved ",
                    ResolvedTargetIds.Count.ToString(CultureInfo.InvariantCulture),
                    "/",
                    ExpectedTargetMethodCount.ToString(CultureInfo.InvariantCulture),
                    " frozen numeric targets.");
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                if (string.IsNullOrEmpty(failure))
                {
                    failure = diagnostic ?? "unknown A33.1 patch failure";
                }

                lastDiagnostic = failure;
            }
        }
    }
}
