using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using SaveNLoadFixes.Persistence;
using SaveNLoadFixes.Safety;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Owns A33 values whose vanilla ABI/DTO is Int32. The registry is bound to the
    /// current LoadEpoch and is never used as cross-career history. Int32 members in
    /// Assembly-CSharp are compatibility mirrors only.
    /// </summary>
    internal static class WideNumericState
    {
        internal const int LegacySectionVersion = 1;
        internal const int PreviousSectionVersion = 2;
        internal const int SectionVersion = 3;

        private static readonly object Sync = new object();
        private static long epoch;
        // Runtime ownership follows the live object, not its numeric ID. A new-tour
        // popup carries the default ID until Initiate assigns the persistent ID, and
        // keying that draft by ID would both collide with tour 0 and lose its exact
        // values at the assignment boundary.
        private static ConditionalWeakTable<SEvent_Tour.tour, WideTourRuntime> Tours =
            new ConditionalWeakTable<SEvent_Tour.tour, WideTourRuntime>();
        private static readonly Dictionary<int, WideSingleRuntime> Singles =
            new Dictionary<int, WideSingleRuntime>();
        private static readonly Dictionary<int, List<long>> ShowFanSeries =
            new Dictionary<int, List<long>>();
        private static readonly Dictionary<string, long> TheaterSubscribers =
            new Dictionary<string, long>(StringComparer.Ordinal);
        private static readonly Dictionary<string, long> TheaterStats =
            new Dictionary<string, long>(StringComparer.Ordinal);
        private static readonly Dictionary<int, long> LoanPayments =
            new Dictionary<int, long>();
        private static readonly Dictionary<string, long> CafeProfits =
            new Dictionary<string, long>(StringComparer.Ordinal);
        private static readonly Dictionary<string, long> CafeNewFans =
            new Dictionary<string, long>(StringComparer.Ordinal);
        private static ConditionalWeakTable<business._proposal, WideBusinessProposalRuntime> BusinessProposals =
            new ConditionalWeakTable<business._proposal, WideBusinessProposalRuntime>();
        private static ConditionalWeakTable<business.active_proposal, WideBusinessContractRuntime> BusinessContracts =
            new ConditionalWeakTable<business.active_proposal, WideBusinessContractRuntime>();
        private static List<long> statsTotalFans = new List<long>();
        private static List<long> statsFanChanges = new List<long>();
        private static bool hasStoryCh3;
        private static long storyCh3;
        private static bool hasStoryCh4;
        private static long storyCh4;
        private static bool hasStoryCh4Scandal;
        private static long storyCh4Scandal;
        private static long restoredSectionCount;
        private static long legacySeedCount;
        private static string lastDiagnostic = string.Empty;

        internal static long RestoredSectionCount
        {
            get { return Interlocked.Read(ref restoredSectionCount); }
        }

        internal static long LegacySeedCount
        {
            get { return Interlocked.Read(ref legacySeedCount); }
        }

        internal static string LastDiagnostic
        {
            get { lock (Sync) { return lastDiagnostic; } }
        }

        internal static string Format(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        internal static bool TryParseCanonical(string value, out long parsed)
        {
            parsed = 0L;
            if (string.IsNullOrEmpty(value) ||
                (value.Length > 1 && value[0] == '0') ||
                (value.Length > 1 && value[0] == '-' && value[1] == '0') ||
                value[0] == '+')
            {
                return false;
            }

            int start = value[0] == '-' ? 1 : 0;
            if (start == value.Length)
            {
                return false;
            }
            for (int index = start; index < value.Length; index++)
            {
                if (value[index] < '0' || value[index] > '9')
                {
                    return false;
                }
            }

            return long.TryParse(
                    value,
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out parsed) &&
                string.Equals(Format(parsed), value, StringComparison.Ordinal);
        }

        internal static bool TryValidateEnvelopeRecord(
            RepairEnvelopeRecordsV1 records,
            out string error)
        {
            error = string.Empty;
            if (records == null)
            {
                error = "A33 records container is null.";
                return false;
            }
            if (records.wide_numeric_state_version == 0)
            {
                return true;
            }
            if ((records.wide_numeric_state_version != LegacySectionVersion &&
                    records.wide_numeric_state_version != PreviousSectionVersion &&
                    records.wide_numeric_state_version != SectionVersion) ||
                records.wide_numeric_state == null)
            {
                error = "A33 wide_numeric_state marker/object is unsupported or missing.";
                return false;
            }

            WideNumericStateRecordV1 state = records.wide_numeric_state;
            if (state.tours == null || state.single_releases == null ||
                state.show_fans == null || state.theater_subscribers == null ||
                state.theater_stats == null || state.stats_total_fans_per_week == null ||
                state.stats_fans_change_per_week == null || state.loan_payments == null ||
                state.cafe_profits == null || state.business_contract_payments == null)
            {
                error = "A33 wide_numeric_state contains a null typed collection.";
                return false;
            }

            HashSet<int> ids = new HashSet<int>();
            for (int index = 0; index < state.tours.Count; index++)
            {
                WideTourRecordV1 tour = state.tours[index];
                if (tour == null || tour.tour_id < 0 || !ids.Add(tour.tour_id) ||
                    tour.countries == null ||
                    !Canonical(tour.production_cost, tour.expected_revenue, tour.saving,
                        tour.revenue, tour.new_fans))
                {
                    error = "A33 tour records contain a null, duplicate ID, null country list, or noncanonical Int64.";
                    return false;
                }

                HashSet<string> countries = new HashSet<string>(StringComparer.Ordinal);
                for (int countryIndex = 0; countryIndex < tour.countries.Count; countryIndex++)
                {
                    WideTourCountryRecordV1 country = tour.countries[countryIndex];
                    string key = country == null ? string.Empty :
                        country.country.ToString(CultureInfo.InvariantCulture) + ":" +
                        country.ordinal.ToString(CultureInfo.InvariantCulture);
                    if (country == null || country.ordinal < 0 || !countries.Add(key) ||
                        !Canonical(country.audience, country.new_fans, country.revenue))
                    {
                        error = "A33 tour-country records contain a null, duplicate identity, or noncanonical Int64.";
                        return false;
                    }
                }
            }

            ids.Clear();
            for (int index = 0; index < state.single_releases.Count; index++)
            {
                WideSingleReleaseRecordV1 single = state.single_releases[index];
                if (single == null || single.single_id < 0 || !ids.Add(single.single_id) ||
                    !Canonical(single.new_fans, single.new_hardcore_fans,
                        single.new_casual_fans))
                {
                    error = "A33 single-release records contain a null, duplicate ID, or noncanonical Int64.";
                    return false;
                }
            }

            ids.Clear();
            for (int index = 0; index < state.show_fans.Count; index++)
            {
                WideShowFansRecordV1 show = state.show_fans[index];
                if (show == null || show.show_id < 0 || !ids.Add(show.show_id) ||
                    show.episode_fans == null || show.episode_count != show.episode_fans.Count ||
                    !Canonical(show.episode_fans))
                {
                    error = "A33 show-fan records contain a null, duplicate ID, count mismatch, or noncanonical Int64.";
                    return false;
                }
            }

            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < state.theater_subscribers.Count; index++)
            {
                WideTheaterSubscriberRecordV1 item = state.theater_subscribers[index];
                string key = item == null ? string.Empty : SubscriberKey(
                    item.theater_id, item.ordinal, item.gender, item.hardcoreness, item.age);
                if (item == null || item.theater_id < 0 || item.ordinal < 0 ||
                    !keys.Add(key) || !Canonical(item.people))
                {
                    error = "A33 theater subscriber records contain a null, duplicate identity, or noncanonical Int64.";
                    return false;
                }
            }

            keys.Clear();
            for (int index = 0; index < state.theater_stats.Count; index++)
            {
                WideTheaterStatRecordV1 item = state.theater_stats[index];
                string key = item == null ? string.Empty : StatKey(item.theater_id, item.ordinal);
                if (item == null || item.theater_id < 0 || item.ordinal < 0 ||
                    string.IsNullOrEmpty(item.date) || !keys.Add(key) ||
                    !Canonical(item.subscribers))
                {
                    error = "A33 theater stat records contain a null, duplicate identity, missing date witness, or noncanonical Int64.";
                    return false;
                }
            }

            if (state.stats_total_fans_per_week.Count !=
                    state.stats_fans_change_per_week.Count ||
                !Canonical(state.stats_total_fans_per_week) ||
                !Canonical(state.stats_fans_change_per_week) ||
                !Canonical(state.story_ch3_aya_fans) ||
                !Canonical(state.story_ch4_fans_needed) ||
                !Canonical(state.story_ch4_scandal_points) ||
                (records.wide_numeric_state_version == LegacySectionVersion &&
                    (state.has_story_ch4_scandal_points ||
                     !string.Equals(state.story_ch4_scandal_points, "0",
                         StringComparison.Ordinal))))
            {
                error = "A33 Stats/story records have unequal series lengths or a noncanonical Int64.";
                return false;
            }

            ids.Clear();
            for (int index = 0; index < state.loan_payments.Count; index++)
            {
                WideLoanPaymentRecordV1 loan = state.loan_payments[index];
                long payment;
                if (loan == null || loan.loan_id < 0 || !ids.Add(loan.loan_id) ||
                    !Canonical(loan.amount) || !TryParseCanonical(loan.payment_per_week, out payment) ||
                    (payment >= int.MinValue && payment <= int.MaxValue))
                {
                    error = "A33 loan records must be unique, canonical, and sparse to out-of-Int32 payments.";
                    return false;
                }
            }

            keys.Clear();
            for (int index = 0; index < state.cafe_profits.Count; index++)
            {
                WideCafeProfitRecordV1 cafe = state.cafe_profits[index];
                long profit;
                long newFans;
                string key = cafe == null ? string.Empty : CafeKey(cafe.cafe_id, cafe.stat_ordinal);
                if (cafe == null || cafe.cafe_id < 0 || cafe.stat_ordinal < 0 ||
                    !keys.Add(key) || !TryParseCanonical(cafe.profit, out profit) ||
                    !TryParseCanonical(cafe.new_fans, out newFans) ||
                    ((profit >= int.MinValue && profit <= int.MaxValue) &&
                     (newFans >= int.MinValue && newFans <= int.MaxValue)))
                {
                    error = "A33 café records must be unique, canonical, and sparse to an out-of-Int32 profit or fan result.";
                    return false;
                }
            }
            ids.Clear();
            for (int index = 0; index < state.business_contract_payments.Count; index++)
            {
                WideBusinessContractPaymentRecordV1 item = state.business_contract_payments[index];
                long payment;
                if (item == null || item.ordinal < 0 || !ids.Add(item.ordinal) ||
                    item.girl_id < -1 || string.IsNullOrEmpty(item.end_date) ||
                    !TryParseCanonical(item.payment_per_week, out payment) ||
                    (payment >= int.MinValue && payment <= int.MaxValue))
                {
                    error = "A33 business-contract payment records must be unique, witnessed, canonical, and sparse to out-of-Int32 values.";
                    return false;
                }
            }
            if (records.wide_numeric_state_version < SectionVersion &&
                state.business_contract_payments.Count != 0)
            {
                error = "A33 pre-v3 wide state unexpectedly contains business-contract payment records.";
                return false;
            }
            return true;
        }

        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData data,
            out WideNumericStateRecordV1 record,
            out string error)
        {
            record = null;
            error = string.Empty;
            EnsureEpoch();
            if (!WideNumericRepair.IsImplemented)
            {
                error = "A33 cannot capture while its frozen patch manifest is incomplete.";
                return false;
            }
            if (data == null)
            {
                error = "A33 cannot capture a null SavedData DTO.";
                return false;
            }

            lock (Sync)
            {
                WideNumericStateRecordV1 result = new WideNumericStateRecordV1();
                if (!CaptureTours(data, result, out error) ||
                    !CaptureSingles(data, result, out error) ||
                    !CaptureShows(data, result, out error) ||
                    !CaptureTheaters(data, result, out error) ||
                    !CaptureStats(data, result, out error) ||
                    !CaptureStory(data, result, out error) ||
                    !CaptureLoans(data, result, out error) ||
                    !CaptureCafes(data, result, out error) ||
                    !CaptureBusinessContracts(data, result, out error))
                {
                    return false;
                }
                record = result;
                return true;
            }
        }

        internal static void SetBusinessProposalBasePayment(business._proposal proposal, long value)
        {
            if (proposal == null) return;
            EnsureEpoch();
            lock (Sync)
            {
                WideBusinessProposalRuntime state;
                if (!BusinessProposals.TryGetValue(proposal, out state))
                {
                    state = new WideBusinessProposalRuntime(value);
                    BusinessProposals.Add(proposal, state);
                }
                else
                {
                    state.BasePayment = value;
                }
                proposal._payment = WideNumericMath.ClampToInt32(value);
            }
        }

        internal static long GetBusinessProposalBasePayment(business._proposal proposal)
        {
            if (proposal == null) return 0L;
            EnsureEpoch();
            lock (Sync)
            {
                WideBusinessProposalRuntime state;
                if (!BusinessProposals.TryGetValue(proposal, out state))
                {
                    state = new WideBusinessProposalRuntime(proposal._payment);
                    BusinessProposals.Add(proposal, state);
                }
                return state.BasePayment;
            }
        }

        internal static void SetBusinessContractPayment(business.active_proposal proposal, long value)
        {
            if (proposal == null) return;
            EnsureEpoch();
            lock (Sync)
            {
                WideBusinessContractRuntime state;
                if (!BusinessContracts.TryGetValue(proposal, out state))
                {
                    state = new WideBusinessContractRuntime(value);
                    BusinessContracts.Add(proposal, state);
                }
                else
                {
                    state.PaymentPerWeek = value;
                }
                proposal.Payment_per_week = WideNumericMath.ClampToInt32(value);
            }
        }

        internal static long GetBusinessContractPayment(business.active_proposal proposal)
        {
            if (proposal == null) return 0L;
            EnsureEpoch();
            lock (Sync)
            {
                WideBusinessContractRuntime state;
                if (!BusinessContracts.TryGetValue(proposal, out state))
                {
                    state = new WideBusinessContractRuntime(proposal.Payment_per_week);
                    BusinessContracts.Add(proposal, state);
                }
                return state.PaymentPerWeek;
            }
        }

        internal static long GetTourProductionCost(SEvent_Tour.tour tour)
        {
            return GetTour(tour).ProductionCost;
        }

        internal static long GetTourExpectedRevenue(SEvent_Tour.tour tour)
        {
            return GetTour(tour).ExpectedRevenue;
        }

        internal static long GetTourSaving(SEvent_Tour.tour tour)
        {
            return GetTour(tour).Saving;
        }

        internal static long GetTourRevenue(SEvent_Tour.tour tour)
        {
            return GetTour(tour).Revenue;
        }

        internal static long GetTourNewFans(SEvent_Tour.tour tour)
        {
            return GetTour(tour).NewFans;
        }

        internal static long GetTourTotalAudience(SEvent_Tour.tour tour)
        {
            EnsureEpoch();
            lock (Sync)
            {
                WideTourRuntime state = GetTourUnlocked(tour);
                SynchronizeTourCountriesUnlocked(tour, state);
                long total = 0L;
                foreach (WideTourCountryRuntime country in state.Countries)
                    total = WideNumericRepair.Add(total, country.Audience,
                        "SEvent_Tour.tour.GetTotalAudience");
                return total;
            }
        }

        internal static long GetTourTotalCountryFans(SEvent_Tour.tour tour)
        {
            EnsureEpoch();
            lock (Sync)
            {
                WideTourRuntime state = GetTourUnlocked(tour);
                SynchronizeTourCountriesUnlocked(tour, state);
                long total = 0L;
                foreach (WideTourCountryRuntime country in state.Countries)
                    total = WideNumericRepair.Add(total, country.NewFans,
                        "SEvent_Tour.tour.GetNewFans");
                return total;
            }
        }

        internal static long GetTourCountryAudience(SEvent_Tour.tour tour, int ordinal)
        {
            return GetTourCountryValue(tour, ordinal, 0);
        }

        internal static long GetTourCountryFans(SEvent_Tour.tour tour, int ordinal)
        {
            return GetTourCountryValue(tour, ordinal, 1);
        }

        internal static long GetTourCountryRevenue(SEvent_Tour.tour tour, int ordinal)
        {
            return GetTourCountryValue(tour, ordinal, 2);
        }

        internal static long GetTourProfit(SEvent_Tour.tour tour)
        {
            WideTourRuntime state = GetTour(tour);
            return WideNumericRepair.Subtract(
                WideNumericRepair.Add(state.Revenue, state.Saving, "SEvent_Tour.tour.GetProfit revenue+saving"),
                state.ProductionCost,
                "SEvent_Tour.tour.GetProfit");
        }

        internal static long GetTourNetProductionCost(SEvent_Tour.tour tour)
        {
            WideTourRuntime state = GetTour(tour);
            return WideNumericRepair.Subtract(
                state.ProductionCost,
                state.Saving,
                "SEvent_Tour.tour.GetProductionCost");
        }

        internal static void SetTourComputed(
            SEvent_Tour.tour tour,
            long productionCost,
            long expectedRevenue,
            long saving)
        {
            EnsureEpoch();
            lock (Sync)
            {
                WideTourRuntime state = GetTourUnlocked(tour);
                SynchronizeTourCountriesUnlocked(tour, state);
                state.ProductionCost = productionCost;
                state.ExpectedRevenue = expectedRevenue;
                state.Saving = saving;
                tour.ProductionCost = WideNumericMath.ClampToInt32(productionCost);
                tour.ExpectedRevenue = WideNumericMath.ClampToInt32(expectedRevenue);
                tour.Saving = WideNumericMath.ClampToInt32(saving);
            }
        }

        internal static void AddTourRevenue(SEvent_Tour.tour tour, long value)
        {
            EnsureEpoch();
            lock (Sync)
            {
                WideTourRuntime state = GetTourUnlocked(tour);
                state.Revenue = WideNumericRepair.Add(
                    state.Revenue, value, "SEvent_Tour.tour.AddRevenue");
                tour.Revenue = WideNumericMath.ClampToInt32(state.Revenue);
            }
        }

        internal static void AddTourFans(SEvent_Tour.tour tour, long value)
        {
            EnsureEpoch();
            lock (Sync)
            {
                WideTourRuntime state = GetTourUnlocked(tour);
                state.NewFans = WideNumericRepair.Add(
                    state.NewFans, value, "SEvent_Tour.tour.AddFans");
                tour.NewFans = WideNumericMath.ClampToInt32(state.NewFans);
            }
        }

        internal static void SetTourCountryValues(
            SEvent_Tour.tour tour,
            int ordinal,
            long audience,
            long newFans,
            long revenue)
        {
            EnsureEpoch();
            lock (Sync)
            {
                WideTourRuntime state = GetTourUnlocked(tour);
                SynchronizeTourCountriesUnlocked(tour, state);
                if (ordinal < 0 || ordinal >= tour.SelectedCountries.Count)
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "A33 tour-country ordinal is outside the selected-country set.");
                    throw new ArgumentOutOfRangeException(nameof(ordinal));
                }
                WideTourCountryRuntime value = state.Countries[ordinal];
                value.Audience = audience;
                value.NewFans = newFans;
                value.Revenue = revenue;
                tour.SelectedCountries[ordinal].Audience = WideNumericMath.ClampToInt32(audience);
                tour.SelectedCountries[ordinal].NewFans = WideNumericMath.ClampToInt32(newFans);
                tour.SelectedCountries[ordinal].Revenue = WideNumericMath.ClampToInt32(revenue);
            }
        }

        internal static List<long> GetShowFans(Shows._show show)
        {
            EnsureEpoch();
            lock (Sync)
            {
                List<long> values;
                if (!ShowFanSeries.TryGetValue(show.id, out values))
                {
                    values = Mirror(show.fans);
                    ShowFanSeries.Add(show.id, values);
                }
                return new List<long>(values);
            }
        }

        internal static void AppendShowFans(Shows._show show, long value)
        {
            EnsureEpoch();
            lock (Sync)
            {
                List<long> values;
                if (!ShowFanSeries.TryGetValue(show.id, out values))
                {
                    values = Mirror(show.fans);
                    ShowFanSeries.Add(show.id, values);
                }
                values.Add(value);
                show.fans.Add(WideNumericMath.ClampToInt32(value));
            }
        }

        internal static void SetSingleReleaseFans(
            singles._single single,
            long ordinary,
            long hardcore,
            long casual)
        {
            EnsureEpoch();
            lock (Sync)
            {
                Singles[single.id] = new WideSingleRuntime(ordinary, hardcore, casual);
                if (single.ReleaseData != null)
                {
                    single.ReleaseData.NewFans = WideNumericMath.ClampToInt32(ordinary);
                    single.ReleaseData.NewHardcoreFans = WideNumericMath.ClampToInt32(hardcore);
                    single.ReleaseData.NewCasualFans = WideNumericMath.ClampToInt32(casual);
                }
            }
        }

        internal static WideSingleRuntime GetSingleReleaseFans(singles._single single)
        {
            EnsureEpoch();
            lock (Sync)
            {
                WideSingleRuntime value;
                if (!Singles.TryGetValue(single.id, out value))
                {
                    value = WideSingleRuntime.From(single.ReleaseData);
                    Singles.Add(single.id, value);
                }
                return value;
            }
        }

        internal static long GetTheaterSubscribers(Theaters._theater theater)
        {
            EnsureEpoch();
            lock (Sync)
            {
                long total = 0L;
                for (int index = 0; index < theater.Subscribers.Count; index++)
                {
                    Theaters._theater._subscriber subscriber = theater.Subscribers[index];
                    long value = GetSubscriberUnlocked(theater, subscriber, index);
                    total = WideNumericRepair.Add(total, value, "Theaters._theater.GetSubscribers");
                }
                return total;
            }
        }

        internal static long GetTheaterSubscriber(
            Theaters._theater theater,
            Theaters._theater._subscriber subscriber,
            int ordinal)
        {
            EnsureEpoch();
            lock (Sync)
            {
                return GetSubscriberUnlocked(theater, subscriber, ordinal);
            }
        }

        internal static void SetTheaterSubscriber(
            Theaters._theater theater,
            Theaters._theater._subscriber subscriber,
            int ordinal,
            long value)
        {
            EnsureEpoch();
            lock (Sync)
            {
                TheaterSubscribers[SubscriberKey(theater.ID, ordinal, (int)subscriber.gender,
                    (int)subscriber.hardcoreness, (int)subscriber.age)] = value;
                subscriber.People = WideNumericMath.ClampToInt32(value);
            }
        }

        internal static long GetTheaterStatSubscribers(
            Theaters._theater theater,
            int ordinal,
            Theaters._theater._stat stat)
        {
            EnsureEpoch();
            lock (Sync)
            {
                string key = StatKey(theater.ID, ordinal);
                long value;
                if (!TheaterStats.TryGetValue(key, out value))
                {
                    value = stat.Subscribers;
                    TheaterStats.Add(key, value);
                }
                return value;
            }
        }

        internal static void SetTheaterStatSubscribers(
            Theaters._theater theater,
            int ordinal,
            Theaters._theater._stat stat,
            long value)
        {
            EnsureEpoch();
            lock (Sync)
            {
                TheaterStats[StatKey(theater.ID, ordinal)] = value;
                stat.Subscribers = WideNumericMath.ClampToInt32(value);
            }
        }

        internal static void ReplaceTheaterStatSeries(
            Theaters._theater theater,
            List<long> values)
        {
            if (theater == null) throw new ArgumentNullException(nameof(theater));
            if (values == null) throw new ArgumentNullException(nameof(values));
            EnsureEpoch();
            lock (Sync)
            {
                if (theater.Stats == null || theater.Stats.Count != values.Count)
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "A33 theater stat-series replacement count did not match live state.");
                    throw new InvalidOperationException(
                        "Theater stat-series replacement count mismatch.");
                }

                for (int index = 0; index < values.Count; index++)
                {
                    Theaters._theater._stat stat = theater.Stats[index];
                    if (stat == null || !Mirror(values[index], stat.Subscribers))
                    {
                        WideNumericRepair.LatchInvariantFailure(
                            "A33 theater stat-series replacement failed its compatibility witness.");
                        throw new InvalidOperationException(
                            "Theater stat-series compatibility witness mismatch.");
                    }
                }

                string prefix = theater.ID.ToString(CultureInfo.InvariantCulture) + ":";
                List<string> obsolete = new List<string>();
                foreach (string key in TheaterStats.Keys)
                    if (key.StartsWith(prefix, StringComparison.Ordinal)) obsolete.Add(key);
                foreach (string key in obsolete) TheaterStats.Remove(key);
                for (int index = 0; index < values.Count; index++)
                {
                    TheaterStats.Add(StatKey(theater.ID, index), values[index]);
                    theater.Stats[index].Subscribers = WideNumericMath.ClampToInt32(values[index]);
                }
            }
        }

        internal static List<long> GetStatsTotals()
        {
            EnsureEpoch();
            lock (Sync)
            {
                EnsureStatsUnlocked();
                return new List<long>(statsTotalFans);
            }
        }

        internal static List<long> GetStatsChanges()
        {
            EnsureEpoch();
            lock (Sync)
            {
                EnsureStatsUnlocked();
                return new List<long>(statsFanChanges);
            }
        }

        internal static void AppendStats(long total, long change)
        {
            EnsureEpoch();
            lock (Sync)
            {
                EnsureStatsUnlocked();
                statsTotalFans.Add(total);
                statsFanChanges.Add(change);
                Stats.data.fans.total_fans_per_week.Add(WideNumericMath.ClampToInt32(total));
                Stats.data.fans.fans_change_per_week.Add(WideNumericMath.ClampToInt32(change));
            }
        }

        internal static void SetStoryCh3(long value)
        {
            EnsureEpoch();
            lock (Sync)
            {
                hasStoryCh3 = true;
                storyCh3 = value;
                tasks.Story_Data.ch3_aya_fans = WideNumericMath.ClampToInt32(value);
            }
        }

        internal static void SetStoryCh4(long value)
        {
            EnsureEpoch();
            lock (Sync)
            {
                hasStoryCh4 = true;
                storyCh4 = value;
                tasks.Story_Data.ch4_fans_needed = WideNumericMath.ClampToInt32(value);
            }
        }

        internal static void SetStoryCh4Scandal(long value)
        {
            EnsureEpoch();
            lock (Sync)
            {
                hasStoryCh4Scandal = true;
                storyCh4Scandal = value;
                tasks.Story_Data.ch4_scandal_points = WideNumericMath.ClampToInt32(value);
            }
        }

        internal static long GetStoryCh3()
        {
            EnsureEpoch();
            lock (Sync)
            {
                if (!hasStoryCh3)
                {
                    hasStoryCh3 = true;
                    storyCh3 = tasks.Story_Data.ch3_aya_fans;
                }
                return storyCh3;
            }
        }

        internal static long GetStoryCh4()
        {
            EnsureEpoch();
            lock (Sync)
            {
                if (!hasStoryCh4)
                {
                    hasStoryCh4 = tasks.Story_Data.ch4_fans_needed >= 0;
                    storyCh4 = tasks.Story_Data.ch4_fans_needed;
                }
                return storyCh4;
            }
        }

        internal static long GetStoryCh4Scandal()
        {
            EnsureEpoch();
            lock (Sync)
            {
                if (!hasStoryCh4Scandal)
                {
                    hasStoryCh4Scandal = tasks.Story_Data.ch4_scandal_points >= 0;
                    storyCh4Scandal = tasks.Story_Data.ch4_scandal_points;
                }
                return storyCh4Scandal;
            }
        }

        internal static long GetLoanPayment(loans._loan loan)
        {
            EnsureEpoch();
            lock (Sync)
            {
                long value;
                if (!LoanPayments.TryGetValue(loan.ID, out value))
                {
                    value = loan.PaymentPerWeek;
                    LoanPayments.Add(loan.ID, value);
                }
                return value;
            }
        }

        internal static void SetLoanPayment(loans._loan loan, long value)
        {
            EnsureEpoch();
            lock (Sync)
            {
                LoanPayments[loan.ID] = value;
                loan.PaymentPerWeek = WideNumericMath.ClampToInt32(value);
            }
        }

        internal static long GetCafeProfit(Cafes._cafe cafe, int ordinal, Cafes._cafe._stat stat)
        {
            EnsureEpoch();
            lock (Sync)
            {
                long value;
                string key = CafeKey(cafe.ID, ordinal);
                if (!CafeProfits.TryGetValue(key, out value))
                {
                    value = stat.Profit;
                    CafeProfits.Add(key, value);
                }
                return value;
            }
        }

        internal static long GetCafeNewFans(
            Cafes._cafe cafe,
            int ordinal,
            Cafes._cafe._stat stat)
        {
            EnsureEpoch();
            lock (Sync)
            {
                long value;
                string key = CafeKey(cafe.ID, ordinal);
                if (!CafeNewFans.TryGetValue(key, out value))
                {
                    value = stat.New_Fans;
                    CafeNewFans.Add(key, value);
                }
                return value;
            }
        }

        internal static void SetCafeProfit(
            Cafes._cafe cafe,
            int ordinal,
            Cafes._cafe._stat stat,
            long value)
        {
            EnsureEpoch();
            lock (Sync)
            {
                CafeProfits[CafeKey(cafe.ID, ordinal)] = value;
                stat.Profit = WideNumericMath.ClampToInt32(value);
            }
        }

        internal static void ReplaceCafeStatSeries(
            Cafes._cafe cafe,
            List<long> profits,
            List<long> newFans)
        {
            if (cafe == null) throw new ArgumentNullException(nameof(cafe));
            if (profits == null) throw new ArgumentNullException(nameof(profits));
            if (newFans == null) throw new ArgumentNullException(nameof(newFans));
            EnsureEpoch();
            lock (Sync)
            {
                if (cafe.Stats == null || cafe.Stats.Count != profits.Count ||
                    cafe.Stats.Count != newFans.Count)
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "A33 café stat-series replacement count did not match live state.");
                    throw new InvalidOperationException(
                        "Café stat-series replacement count mismatch.");
                }

                for (int index = 0; index < profits.Count; index++)
                {
                    Cafes._cafe._stat stat = cafe.Stats[index];
                    if (stat == null || !Mirror(profits[index], stat.Profit) ||
                        !Mirror(newFans[index], stat.New_Fans))
                    {
                        WideNumericRepair.LatchInvariantFailure(
                            "A33 café stat-series replacement failed its compatibility witness.");
                        throw new InvalidOperationException(
                            "Café stat-series compatibility witness mismatch.");
                    }
                }

                string prefix = cafe.ID.ToString(CultureInfo.InvariantCulture) + ":";
                List<string> obsolete = new List<string>();
                foreach (string key in CafeProfits.Keys)
                    if (key.StartsWith(prefix, StringComparison.Ordinal)) obsolete.Add(key);
                foreach (string key in CafeNewFans.Keys)
                    if (key.StartsWith(prefix, StringComparison.Ordinal) &&
                        !obsolete.Contains(key)) obsolete.Add(key);
                foreach (string key in obsolete)
                {
                    CafeProfits.Remove(key);
                    CafeNewFans.Remove(key);
                }
                for (int index = 0; index < profits.Count; index++)
                {
                    string key = CafeKey(cafe.ID, index);
                    CafeProfits.Add(key, profits[index]);
                    CafeNewFans.Add(key, newFans[index]);
                    cafe.Stats[index].Profit = WideNumericMath.ClampToInt32(profits[index]);
                    cafe.Stats[index].New_Fans = WideNumericMath.ClampToInt32(newFans[index]);
                }
            }
        }

        internal static void RestoreToursAfterVanillaLoad() { Restore(Subsystem.Tours); }
        internal static void RestoreSinglesAfterVanillaLoad() { Restore(Subsystem.Singles); }
        internal static void RestoreShowsAfterVanillaLoad() { Restore(Subsystem.Shows); }
        internal static void RestoreTheatersAfterVanillaLoad() { Restore(Subsystem.Theaters); }
        internal static void RestoreStatsAfterVanillaLoad() { Restore(Subsystem.Stats); }
        internal static void RestoreStoryAfterVanillaLoad() { Restore(Subsystem.Story); }
        internal static void RestoreLoansAfterVanillaLoad() { Restore(Subsystem.Loans); }
        internal static void RestoreCafesAfterVanillaLoad() { Restore(Subsystem.Cafes); }
        internal static void RestoreBusinessContractsAfterVanillaLoad() { Restore(Subsystem.BusinessContracts); }

        private static void Restore(Subsystem subsystem)
        {
            EnsureEpoch();
            RepairEnvelopeLoadState state;
            SaveManager.SavedData target = GetTargetSavedData();
            if (target == null || !RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                WideNumericRepair.LatchInvariantFailure(
                    "A33.4 could not associate the adopted SavedData object with its repair envelope.");
                return;
            }

            if (!state.Present)
            {
                SeedLegacy(subsystem);
                Interlocked.Increment(ref legacySeedCount);
                SetDiagnostic("A33.4 seeded " + subsystem +
                    " from legacy vanilla compatibility values; unavailable pre-SNLF wide preimages were not claimed as recovered.");
                return;
            }
            if (!state.Valid || state.Envelope == null || state.Envelope.records == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "A33.4 refused an invalid repair envelope instead of treating it as legacy absence.");
                return;
            }
            if (state.Envelope.records.wide_numeric_state_version == 0)
            {
                SeedLegacy(subsystem);
                Interlocked.Increment(ref legacySeedCount);
                SetDiagnostic("A33.4 seeded " + subsystem +
                    " from legacy vanilla compatibility values; unavailable pre-SNLF wide preimages were not claimed as recovered.");
                return;
            }
            if (
                (state.Envelope.records.wide_numeric_state_version != LegacySectionVersion &&
                    state.Envelope.records.wide_numeric_state_version != PreviousSectionVersion &&
                    state.Envelope.records.wide_numeric_state_version != SectionVersion) ||
                state.Envelope.records.wide_numeric_state == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "A33.4 refused an invalid or unsupported wide_numeric_state section.");
                return;
            }

            if (subsystem == Subsystem.BusinessContracts &&
                state.Envelope.records.wide_numeric_state_version < SectionVersion)
            {
                SeedLegacy(subsystem);
                Interlocked.Increment(ref legacySeedCount);
                SetDiagnostic("A33.4 seeded BusinessContracts from pre-v3 vanilla Int32 compatibility values; unavailable pre-v3 wide preimages were not claimed as recovered.");
                return;
            }

            string error;
            bool restored = RestoreSubsystem(
                subsystem,
                state.Envelope.records.wide_numeric_state,
                out error);
            if (!restored)
            {
                WideNumericRepair.LatchInvariantFailure("A33.4 restore failed: " + error);
                return;
            }
            Interlocked.Increment(ref restoredSectionCount);
            if (subsystem == Subsystem.Story &&
                state.Envelope.records.wide_numeric_state_version == LegacySectionVersion)
            {
                lock (Sync)
                {
                    hasStoryCh4Scandal = tasks.Story_Data.ch4_scandal_points >= 0;
                    storyCh4Scandal = tasks.Story_Data.ch4_scandal_points;
                }
                Interlocked.Increment(ref legacySeedCount);
                SetDiagnostic("A33.4 restored the exact version-1 Story wide fields from checkpoint " +
                    state.Envelope.checkpoint_id +
                    "; its then-unrepresented chapter-four scandal baseline was seeded from the vanilla compatibility value and no unavailable preimage was claimed.");
                return;
            }
            SetDiagnostic("A33.4 restored exact " + subsystem + " wide state from checkpoint " +
                state.Envelope.checkpoint_id + ".");
        }

        private static bool RestoreSubsystem(
            Subsystem subsystem,
            WideNumericStateRecordV1 source,
            out string error)
        {
            lock (Sync)
            {
                switch (subsystem)
                {
                    case Subsystem.Tours: return RestoreTours(source, out error);
                    case Subsystem.Singles: return RestoreSingles(source, out error);
                    case Subsystem.Shows: return RestoreShows(source, out error);
                    case Subsystem.Theaters: return RestoreTheaters(source, out error);
                    case Subsystem.Stats: return RestoreStats(source, out error);
                    case Subsystem.Story: return RestoreStory(source, out error);
                    case Subsystem.Loans: return RestoreLoans(source, out error);
                    case Subsystem.Cafes: return RestoreCafes(source, out error);
                    case Subsystem.BusinessContracts: return RestoreBusinessContracts(source, out error);
                    default: error = "unknown subsystem"; return false;
                }
            }
        }

        private static bool CaptureTours(
            SaveManager.SavedData data,
            WideNumericStateRecordV1 result,
            out string error)
        {
            error = string.Empty;
            if (data.SEvent_Tour__Tours == null || SEvent_Tour.Tours == null ||
                data.SEvent_Tour__Tours.Count != SEvent_Tour.Tours.Count)
            {
                error = "A33 tour DTO/live counts do not match.";
                return false;
            }
            Dictionary<int, SEvent_Tour.TourData> saved = new Dictionary<int, SEvent_Tour.TourData>();
            foreach (SEvent_Tour.TourData item in data.SEvent_Tour__Tours)
            {
                if (item == null || item.ID < 0 || saved.ContainsKey(item.ID))
                {
                    error = "A33 tour DTO set has a null, invalid, or duplicate ID.";
                    return false;
                }
                saved.Add(item.ID, item);
            }
            foreach (SEvent_Tour.tour live in SEvent_Tour.Tours)
            {
                SEvent_Tour.TourData dto;
                if (live == null || !saved.TryGetValue(live.ID, out dto))
                {
                    error = "A33 live tour set does not exactly match its DTO set.";
                    return false;
                }
                WideTourRuntime runtime = GetTourUnlocked(live);
                SynchronizeTourCountriesUnlocked(live, runtime);
                if (!Mirror(runtime.ProductionCost, live.ProductionCost, dto.ProductionCost) ||
                    !Mirror(runtime.ExpectedRevenue, live.ExpectedRevenue, dto.ExpectedRevenue) ||
                    !Mirror(runtime.Saving, live.Saving, dto.Saving) ||
                    !Mirror(runtime.Revenue, live.Revenue, dto.Revenue) ||
                    !Mirror(runtime.NewFans, live.NewFans, dto.NewFans) ||
                    live.SelectedCountries == null || dto.SelectedCountries == null ||
                    live.SelectedCountries.Count != dto.SelectedCountries.Count ||
                    runtime.Countries.Count != live.SelectedCountries.Count)
                {
                    error = "A33 tour compatibility mirrors or country counts do not match the frozen DTO.";
                    return false;
                }

                WideTourRecordV1 output = new WideTourRecordV1
                {
                    tour_id = live.ID,
                    production_cost = Format(runtime.ProductionCost),
                    expected_revenue = Format(runtime.ExpectedRevenue),
                    saving = Format(runtime.Saving),
                    revenue = Format(runtime.Revenue),
                    new_fans = Format(runtime.NewFans)
                };
                for (int index = 0; index < live.SelectedCountries.Count; index++)
                {
                    SEvent_Tour.tour.selectedCountry country = live.SelectedCountries[index];
                    SEvent_Tour.Tour_CountryData savedCountry = dto.SelectedCountries[index];
                    WideTourCountryRuntime exact = runtime.Countries[index];
                    if (country == null || country.Country == null || savedCountry == null ||
                        (int)country.Country.Type != (int)savedCountry.Country ||
                        exact.Country != (int)country.Country.Type || exact.Ordinal != index ||
                        country.Level != savedCountry.Level ||
                        country.Attendance != savedCountry.Attendance ||
                        country.Discount != savedCountry.Discount ||
                        !Mirror(exact.Audience, country.Audience, savedCountry.Audience) ||
                        !Mirror(exact.NewFans, country.NewFans, savedCountry.NewFans) ||
                        !Mirror(exact.Revenue, country.Revenue, savedCountry.Revenue))
                    {
                        error = "A33 tour-country identity/witness/mirror validation failed.";
                        return false;
                    }
                    output.countries.Add(new WideTourCountryRecordV1
                    {
                        country = exact.Country,
                        ordinal = index,
                        level = country.Level,
                        attendance = country.Attendance,
                        discount = country.Discount,
                        audience = Format(exact.Audience),
                        new_fans = Format(exact.NewFans),
                        revenue = Format(exact.Revenue)
                    });
                }
                result.tours.Add(output);
            }
            result.tours.Sort((left, right) => left.tour_id.CompareTo(right.tour_id));
            return true;
        }

        private static bool CaptureSingles(SaveManager.SavedData data, WideNumericStateRecordV1 result, out string error)
        {
            error = string.Empty;
            if (data.singles__Singles == null || singles.Singles == null ||
                data.singles__Singles.Count != singles.Singles.Count)
            {
                error = "A33 single DTO/live counts do not match.";
                return false;
            }
            Dictionary<int, singles.SinglesData> saved = new Dictionary<int, singles.SinglesData>();
            foreach (singles.SinglesData item in data.singles__Singles)
            {
                if (item == null || item.id < 0 || saved.ContainsKey(item.id))
                {
                    error = "A33 single DTO set has a null, invalid, or duplicate ID.";
                    return false;
                }
                saved.Add(item.id, item);
            }
            foreach (singles._single live in singles.Singles)
            {
                singles.SinglesData dto;
                if (live == null || live.ReleaseData == null || !saved.TryGetValue(live.id, out dto) ||
                    dto.ReleaseData == null)
                {
                    error = "A33 live single/release set does not exactly match its DTO set.";
                    return false;
                }
                WideSingleRuntime exact;
                if (!Singles.TryGetValue(live.id, out exact))
                {
                    exact = WideSingleRuntime.From(live.ReleaseData);
                    Singles.Add(live.id, exact);
                }
                if (!Mirror(exact.Ordinary, live.ReleaseData.NewFans, dto.ReleaseData.NewFans) ||
                    !Mirror(exact.Hardcore, live.ReleaseData.NewHardcoreFans, dto.ReleaseData.NewHardcoreFans) ||
                    !Mirror(exact.Casual, live.ReleaseData.NewCasualFans, dto.ReleaseData.NewCasualFans))
                {
                    error = "A33 single release compatibility mirror differs from the frozen DTO.";
                    return false;
                }
                result.single_releases.Add(new WideSingleReleaseRecordV1
                {
                    single_id = live.id,
                    new_fans = Format(exact.Ordinary),
                    new_hardcore_fans = Format(exact.Hardcore),
                    new_casual_fans = Format(exact.Casual)
                });
            }
            result.single_releases.Sort((left, right) => left.single_id.CompareTo(right.single_id));
            return true;
        }

        private static bool CaptureShows(SaveManager.SavedData data, WideNumericStateRecordV1 result, out string error)
        {
            error = string.Empty;
            if (data.shows__Shows == null || Shows.shows == null ||
                data.shows__Shows.Count != Shows.shows.Count)
            {
                error = "A33 show DTO/live counts do not match.";
                return false;
            }
            Dictionary<int, Shows.ShowData> saved = new Dictionary<int, Shows.ShowData>();
            foreach (Shows.ShowData item in data.shows__Shows)
            {
                if (item == null || item.id < 0 || saved.ContainsKey(item.id))
                {
                    error = "A33 show DTO set has a null, invalid, or duplicate ID.";
                    return false;
                }
                saved.Add(item.id, item);
            }
            foreach (Shows._show live in Shows.shows)
            {
                Shows.ShowData dto;
                if (live == null || live.fans == null || !saved.TryGetValue(live.id, out dto) ||
                    dto.fans == null)
                {
                    error = "A33 live show set does not exactly match its DTO set.";
                    return false;
                }
                List<long> exact;
                if (!ShowFanSeries.TryGetValue(live.id, out exact))
                {
                    exact = Mirror(live.fans);
                    ShowFanSeries.Add(live.id, exact);
                }
                if (exact.Count != live.fans.Count || exact.Count != dto.fans.Count)
                {
                    error = "A33 show fan-series count differs from the frozen DTO.";
                    return false;
                }
                WideShowFansRecordV1 output = new WideShowFansRecordV1
                {
                    show_id = live.id,
                    episode_count = exact.Count
                };
                for (int index = 0; index < exact.Count; index++)
                {
                    if (!Mirror(exact[index], live.fans[index], dto.fans[index]))
                    {
                        error = "A33 show fan compatibility mirror differs from the frozen DTO.";
                        return false;
                    }
                    output.episode_fans.Add(Format(exact[index]));
                }
                result.show_fans.Add(output);
            }
            result.show_fans.Sort((left, right) => left.show_id.CompareTo(right.show_id));
            return true;
        }

        private static bool CaptureTheaters(SaveManager.SavedData data, WideNumericStateRecordV1 result, out string error)
        {
            error = string.Empty;
            if (data.Theaters__Theaters == null || Theaters.Theaters_ == null ||
                data.Theaters__Theaters.Count != Theaters.Theaters_.Count)
            {
                error = "A33 theater DTO/live counts do not match.";
                return false;
            }
            Dictionary<int, Theaters.TheaterData> saved = new Dictionary<int, Theaters.TheaterData>();
            foreach (Theaters.TheaterData item in data.Theaters__Theaters)
            {
                if (item == null || item.ID < 0 || saved.ContainsKey(item.ID))
                {
                    error = "A33 theater DTO set has a null, invalid, or duplicate ID.";
                    return false;
                }
                saved.Add(item.ID, item);
            }
            foreach (Theaters._theater live in Theaters.Theaters_)
            {
                Theaters.TheaterData dto;
                if (live == null || !saved.TryGetValue(live.ID, out dto) ||
                    live.Subscribers == null || dto.Subscribers == null ||
                    live.Stats == null || dto.Stats == null ||
                    live.Subscribers.Count != dto.Subscribers.Count ||
                    live.Stats.Count != dto.Stats.Count)
                {
                    error = "A33 theater live/DTO set or nested counts do not match.";
                    return false;
                }
                for (int index = 0; index < live.Subscribers.Count; index++)
                {
                    Theaters._theater._subscriber subscriber = live.Subscribers[index];
                    Theaters._theater._subscriber savedSubscriber = dto.Subscribers[index];
                    long exact = GetSubscriberUnlocked(live, subscriber, index);
                    if (subscriber == null || savedSubscriber == null ||
                        subscriber.gender != savedSubscriber.gender ||
                        subscriber.hardcoreness != savedSubscriber.hardcoreness ||
                        subscriber.age != savedSubscriber.age ||
                        !Mirror(exact, subscriber.People, savedSubscriber.People))
                    {
                        error = "A33 theater subscriber identity/mirror differs from the frozen DTO.";
                        return false;
                    }
                    result.theater_subscribers.Add(new WideTheaterSubscriberRecordV1
                    {
                        theater_id = live.ID,
                        ordinal = index,
                        gender = (int)subscriber.gender,
                        hardcoreness = (int)subscriber.hardcoreness,
                        age = (int)subscriber.age,
                        people = Format(exact)
                    });
                }
                for (int index = 0; index < live.Stats.Count; index++)
                {
                    Theaters._theater._stat stat = live.Stats[index];
                    Theaters.TheaterData._stat savedStat = dto.Stats[index];
                    if (stat == null || savedStat == null)
                    {
                        error = "A33 theater stat set contains a null row.";
                        return false;
                    }
                    long exact = GetTheaterStatSubscribers(live, index, stat);
                    string date = ExtensionMethods.ToDataString(stat.Date);
                    if (!string.Equals(date, savedStat.Date, StringComparison.Ordinal) ||
                        !Mirror(exact, stat.Subscribers, savedStat.Subscribers))
                    {
                        error = "A33 theater stat date/mirror differs from the frozen DTO.";
                        return false;
                    }
                    result.theater_stats.Add(new WideTheaterStatRecordV1
                    {
                        theater_id = live.ID,
                        ordinal = index,
                        date = date,
                        subscribers = Format(exact)
                    });
                }
            }
            return true;
        }

        private static bool CaptureStats(SaveManager.SavedData data, WideNumericStateRecordV1 result, out string error)
        {
            error = string.Empty;
            if (data.Stats__data == null || data.Stats__data.total_fans_per_week == null ||
                data.Stats__data.fans_change_per_week == null)
            {
                error = "A33 Stats DTO or fan series is null.";
                return false;
            }
            EnsureStatsUnlocked();
            if (statsTotalFans.Count != Stats.data.fans.total_fans_per_week.Count ||
                statsTotalFans.Count != data.Stats__data.total_fans_per_week.Count ||
                statsFanChanges.Count != Stats.data.fans.fans_change_per_week.Count ||
                statsFanChanges.Count != data.Stats__data.fans_change_per_week.Count ||
                statsTotalFans.Count != statsFanChanges.Count)
            {
                error = "A33 Stats fan-series lengths do not match the frozen DTO.";
                return false;
            }
            for (int index = 0; index < statsTotalFans.Count; index++)
            {
                if (!Mirror(statsTotalFans[index], Stats.data.fans.total_fans_per_week[index],
                        data.Stats__data.total_fans_per_week[index]) ||
                    !Mirror(statsFanChanges[index], Stats.data.fans.fans_change_per_week[index],
                        data.Stats__data.fans_change_per_week[index]))
                {
                    error = "A33 Stats compatibility mirror differs from the frozen DTO.";
                    return false;
                }
                result.stats_total_fans_per_week.Add(Format(statsTotalFans[index]));
                result.stats_fans_change_per_week.Add(Format(statsFanChanges[index]));
            }
            return true;
        }

        private static bool CaptureStory(SaveManager.SavedData data, WideNumericStateRecordV1 result, out string error)
        {
            error = string.Empty;
            if (data.tasks__StoryData == null || tasks.Story_Data == null)
            {
                error = "A33 story DTO/live state is null.";
                return false;
            }
            if (!hasStoryCh3)
            {
                hasStoryCh3 = true;
                storyCh3 = tasks.Story_Data.ch3_aya_fans;
            }
            if (!hasStoryCh4 && tasks.Story_Data.ch4_fans_needed >= 0)
            {
                hasStoryCh4 = true;
                storyCh4 = tasks.Story_Data.ch4_fans_needed;
            }
            if (!hasStoryCh4Scandal && tasks.Story_Data.ch4_scandal_points >= 0)
            {
                hasStoryCh4Scandal = true;
                storyCh4Scandal = tasks.Story_Data.ch4_scandal_points;
            }
            if (!Mirror(storyCh3, tasks.Story_Data.ch3_aya_fans,
                    data.tasks__StoryData.ch3_aya_fans) ||
                (hasStoryCh4 && !Mirror(storyCh4, tasks.Story_Data.ch4_fans_needed,
                    data.tasks__StoryData.ch4_fans_needed)) ||
                (hasStoryCh4Scandal && !Mirror(storyCh4Scandal,
                    tasks.Story_Data.ch4_scandal_points,
                    data.tasks__StoryData.ch4_scandal_points)))
            {
                error = "A33 story compatibility mirror differs from the frozen DTO.";
                return false;
            }
            result.has_story_ch3_aya_fans = hasStoryCh3;
            result.story_ch3_aya_fans = Format(storyCh3);
            result.has_story_ch4_fans_needed = hasStoryCh4;
            result.story_ch4_fans_needed = Format(storyCh4);
            result.has_story_ch4_scandal_points = hasStoryCh4Scandal;
            result.story_ch4_scandal_points = Format(storyCh4Scandal);
            return true;
        }

        private static bool CaptureLoans(SaveManager.SavedData data, WideNumericStateRecordV1 result, out string error)
        {
            error = string.Empty;
            if (data.loans__LoanData == null || loans.Loans == null ||
                data.loans__LoanData.Count != loans.Loans.Count)
            {
                error = "A33 loan DTO/live counts do not match.";
                return false;
            }
            Dictionary<int, loans.LoanData> saved = new Dictionary<int, loans.LoanData>();
            foreach (loans.LoanData item in data.loans__LoanData)
            {
                if (item == null || item.ID < 0 || saved.ContainsKey(item.ID))
                {
                    error = "A33 loan DTO set has a null, invalid, or duplicate ID.";
                    return false;
                }
                saved.Add(item.ID, item);
            }
            foreach (loans._loan live in loans.Loans)
            {
                loans.LoanData dto;
                if (live == null || !saved.TryGetValue(live.ID, out dto))
                {
                    error = "A33 live loan set does not exactly match its DTO set.";
                    return false;
                }
                long exact = GetLoanPayment(live);
                if (!Mirror(exact, live.PaymentPerWeek, dto.PaymentPerWeek) || live.Amount != dto.Amount ||
                    (int)live.Duration != (int)dto.Duration)
                {
                    error = "A33 loan payment/witness differs from the frozen DTO.";
                    return false;
                }
                if (exact < int.MinValue || exact > int.MaxValue)
                {
                    result.loan_payments.Add(new WideLoanPaymentRecordV1
                    {
                        loan_id = live.ID,
                        duration = (int)live.Duration,
                        amount = Format(live.Amount),
                        payment_per_week = Format(exact)
                    });
                }
            }
            result.loan_payments.Sort((left, right) => left.loan_id.CompareTo(right.loan_id));
            return true;
        }

        private static bool CaptureCafes(SaveManager.SavedData data, WideNumericStateRecordV1 result, out string error)
        {
            error = string.Empty;
            if (data.Cafes__Cafes == null || Cafes.Cafes_ == null ||
                data.Cafes__Cafes.Count != Cafes.Cafes_.Count)
            {
                error = "A33 café DTO/live counts do not match.";
                return false;
            }
            Dictionary<int, Cafes.CafeData> saved = new Dictionary<int, Cafes.CafeData>();
            foreach (Cafes.CafeData item in data.Cafes__Cafes)
            {
                if (item == null || item.ID < 0 || saved.ContainsKey(item.ID))
                {
                    error = "A33 café DTO set has a null, invalid, or duplicate ID.";
                    return false;
                }
                saved.Add(item.ID, item);
            }
            foreach (Cafes._cafe live in Cafes.Cafes_)
            {
                Cafes.CafeData dto;
                if (live == null || !saved.TryGetValue(live.ID, out dto) ||
                    live.Stats == null || dto.Stats == null || live.Stats.Count != dto.Stats.Count)
                {
                    error = "A33 café live/DTO set or stat counts do not match.";
                    return false;
                }
                for (int index = 0; index < live.Stats.Count; index++)
                {
                    Cafes._cafe._stat stat = live.Stats[index];
                    Cafes._cafe._stat savedStat = dto.Stats[index];
                    if (stat == null || savedStat == null)
                    {
                        error = "A33 café stat set contains a null row.";
                        return false;
                    }
                    long exact = GetCafeProfit(live, index, stat);
                    long exactFans = GetCafeNewFans(live, index, stat);
                    if (stat.Dish_ID != savedStat.Dish_ID ||
                        !Mirror(exact, stat.Profit, savedStat.Profit) ||
                        !Mirror(exactFans, stat.New_Fans, savedStat.New_Fans))
                    {
                        error = "A33 café stat witness/mirror differs from the frozen DTO.";
                        return false;
                    }
                    if (exact < int.MinValue || exact > int.MaxValue ||
                        exactFans < int.MinValue || exactFans > int.MaxValue)
                    {
                        result.cafe_profits.Add(new WideCafeProfitRecordV1
                        {
                            cafe_id = live.ID,
                            stat_ordinal = index,
                            dish_id = stat.Dish_ID,
                            profit = Format(exact),
                            new_fans = Format(exactFans)
                        });
                    }
                }
            }
            return true;
        }

        private static bool CaptureBusinessContracts(
            SaveManager.SavedData data,
            WideNumericStateRecordV1 result,
            out string error)
        {
            error = string.Empty;
            mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
            business manager = main == null || main.Data == null ? null : main.Data.GetComponent<business>();
            if (manager == null || manager.ActiveProposals == null ||
                data.business__ActiveProposalsData == null ||
                manager.ActiveProposals.Count != data.business__ActiveProposalsData.Count)
            {
                error = "A33 business-contract DTO/live counts do not match.";
                return false;
            }

            for (int index = 0; index < manager.ActiveProposals.Count; index++)
            {
                business.active_proposal live = manager.ActiveProposals[index];
                business.active_proposal_data saved = data.business__ActiveProposalsData[index];
                if (live == null || saved == null)
                {
                    error = "A33 business-contract set contains a null live/DTO row.";
                    return false;
                }
                int girlId = live.Girl == null ? -1 : live.Girl.id;
                if (saved.Girl != girlId || saved.Skill != live.Skill || saved.Type != live.Type ||
                    saved.EndDate != ExtensionMethods.ToDataString(live.EndDate))
                {
                    error = "A33 business-contract identity/witness differs from its frozen DTO.";
                    return false;
                }
                long exact = GetBusinessContractPayment(live);
                if (!Mirror(exact, live.Payment_per_week, saved.Payment_per_week))
                {
                    error = "A33 business-contract payment differs from its Int32 compatibility mirror.";
                    return false;
                }
                if (exact < int.MinValue || exact > int.MaxValue)
                {
                    result.business_contract_payments.Add(new WideBusinessContractPaymentRecordV1
                    {
                        ordinal = index,
                        girl_id = girlId,
                        skill = (int)live.Skill,
                        type = (int)live.Type,
                        end_date = ExtensionMethods.ToDataString(live.EndDate),
                        payment_per_week = Format(exact)
                    });
                }
            }
            return true;
        }

        private static bool RestoreTours(WideNumericStateRecordV1 source, out string error)
        {
            error = string.Empty;
            if (source.tours.Count != SEvent_Tour.Tours.Count)
            {
                error = "tour record/current counts are not one-to-one";
                return false;
            }
            Dictionary<SEvent_Tour.tour, WideTourRuntime> pending =
                new Dictionary<SEvent_Tour.tour, WideTourRuntime>();
            Dictionary<int, SEvent_Tour.tour> live = new Dictionary<int, SEvent_Tour.tour>();
            foreach (SEvent_Tour.tour item in SEvent_Tour.Tours)
            {
                if (item == null || item.ID < 0 || live.ContainsKey(item.ID))
                {
                    error = "current tour set contains a null, invalid, or duplicate ID";
                    return false;
                }
                live.Add(item.ID, item);
            }
            foreach (WideTourRecordV1 item in source.tours)
            {
                SEvent_Tour.tour target;
                if (!live.TryGetValue(item.tour_id, out target) ||
                    item.countries.Count != target.SelectedCountries.Count)
                {
                    error = "tour ID/country set does not match vanilla reconstruction";
                    return false;
                }
                WideTourRuntime value;
                if (!TryTour(item, target, out value, out error)) return false;
                pending.Add(target, value);
            }
            Tours = new ConditionalWeakTable<SEvent_Tour.tour, WideTourRuntime>();
            foreach (KeyValuePair<SEvent_Tour.tour, WideTourRuntime> pair in pending)
            {
                Tours.Add(pair.Key, pair.Value);
                ApplyTourMirrors(pair.Key, pair.Value);
            }
            return true;
        }

        private static bool RestoreSingles(WideNumericStateRecordV1 source, out string error)
        {
            error = string.Empty;
            if (source.single_releases.Count != singles.Singles.Count)
            {
                error = "single release record/current counts are not one-to-one";
                return false;
            }
            Dictionary<int, singles._single> live = new Dictionary<int, singles._single>();
            foreach (singles._single item in singles.Singles)
            {
                if (item == null || item.ReleaseData == null || live.ContainsKey(item.id))
                {
                    error = "current single set is invalid or duplicated";
                    return false;
                }
                live.Add(item.id, item);
            }
            Dictionary<int, WideSingleRuntime> pending = new Dictionary<int, WideSingleRuntime>();
            foreach (WideSingleReleaseRecordV1 item in source.single_releases)
            {
                singles._single target;
                long ordinary, hardcore, casual;
                if (!live.TryGetValue(item.single_id, out target) ||
                    !TryParseCanonical(item.new_fans, out ordinary) ||
                    !TryParseCanonical(item.new_hardcore_fans, out hardcore) ||
                    !TryParseCanonical(item.new_casual_fans, out casual) ||
                    !Mirror(ordinary, target.ReleaseData.NewFans) ||
                    !Mirror(hardcore, target.ReleaseData.NewHardcoreFans) ||
                    !Mirror(casual, target.ReleaseData.NewCasualFans))
                {
                    error = "single release identity/value is inconsistent with its vanilla mirror";
                    return false;
                }
                pending.Add(item.single_id, new WideSingleRuntime(ordinary, hardcore, casual));
            }
            Singles.Clear();
            foreach (KeyValuePair<int, WideSingleRuntime> pair in pending) Singles.Add(pair.Key, pair.Value);
            return true;
        }

        private static bool RestoreShows(WideNumericStateRecordV1 source, out string error)
        {
            error = string.Empty;
            if (source.show_fans.Count != Shows.shows.Count)
            {
                error = "show fan record/current counts are not one-to-one";
                return false;
            }
            Dictionary<int, Shows._show> live = new Dictionary<int, Shows._show>();
            foreach (Shows._show item in Shows.shows)
            {
                if (item == null || live.ContainsKey(item.id))
                {
                    error = "current show set is invalid or duplicated";
                    return false;
                }
                live.Add(item.id, item);
            }
            Dictionary<int, List<long>> pending = new Dictionary<int, List<long>>();
            foreach (WideShowFansRecordV1 item in source.show_fans)
            {
                Shows._show target;
                if (!live.TryGetValue(item.show_id, out target) ||
                    item.episode_fans.Count != target.fans.Count)
                {
                    error = "show identity/fan count does not match vanilla reconstruction";
                    return false;
                }
                List<long> values = new List<long>();
                for (int index = 0; index < item.episode_fans.Count; index++)
                {
                    long value;
                    if (!TryParseCanonical(item.episode_fans[index], out value) ||
                        !Mirror(value, target.fans[index]))
                    {
                        error = "show fan value is inconsistent with its vanilla mirror";
                        return false;
                    }
                    values.Add(value);
                }
                pending.Add(item.show_id, values);
            }
            ShowFanSeries.Clear();
            foreach (KeyValuePair<int, List<long>> pair in pending) ShowFanSeries.Add(pair.Key, pair.Value);
            return true;
        }

        private static bool RestoreTheaters(WideNumericStateRecordV1 source, out string error)
        {
            error = string.Empty;
            Dictionary<int, Theaters._theater> live = new Dictionary<int, Theaters._theater>();
            foreach (Theaters._theater theater in Theaters.Theaters_)
            {
                if (theater == null || live.ContainsKey(theater.ID))
                {
                    error = "current theater set is invalid or duplicated";
                    return false;
                }
                live.Add(theater.ID, theater);
            }
            int subscriberCount = 0;
            int statCount = 0;
            foreach (Theaters._theater theater in live.Values)
            {
                subscriberCount += theater.Subscribers.Count;
                statCount += theater.Stats.Count;
            }
            if (subscriberCount != source.theater_subscribers.Count ||
                statCount != source.theater_stats.Count)
            {
                error = "theater nested record/current counts are not one-to-one";
                return false;
            }
            Dictionary<string, long> subscribers = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (WideTheaterSubscriberRecordV1 item in source.theater_subscribers)
            {
                Theaters._theater theater;
                long value;
                if (!live.TryGetValue(item.theater_id, out theater) ||
                    item.ordinal >= theater.Subscribers.Count ||
                    !TryParseCanonical(item.people, out value))
                {
                    error = "theater subscriber identity/value is invalid";
                    return false;
                }
                Theaters._theater._subscriber target = theater.Subscribers[item.ordinal];
                string key = SubscriberKey(item.theater_id, item.ordinal, item.gender,
                    item.hardcoreness, item.age);
                if ((int)target.gender != item.gender ||
                    (int)target.hardcoreness != item.hardcoreness ||
                    (int)target.age != item.age || !Mirror(value, target.People) ||
                    subscribers.ContainsKey(key))
                {
                    error = "theater subscriber witness/mirror is inconsistent";
                    return false;
                }
                subscribers.Add(key, value);
            }
            Dictionary<string, long> stats = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (WideTheaterStatRecordV1 item in source.theater_stats)
            {
                Theaters._theater theater;
                long value;
                if (!live.TryGetValue(item.theater_id, out theater) ||
                    item.ordinal >= theater.Stats.Count ||
                    !TryParseCanonical(item.subscribers, out value))
                {
                    error = "theater stat identity/value is invalid";
                    return false;
                }
                Theaters._theater._stat target = theater.Stats[item.ordinal];
                string key = StatKey(item.theater_id, item.ordinal);
                if (!string.Equals(item.date, ExtensionMethods.ToDataString(target.Date), StringComparison.Ordinal) ||
                    !Mirror(value, target.Subscribers) || stats.ContainsKey(key))
                {
                    error = "theater stat date/mirror witness is inconsistent";
                    return false;
                }
                stats.Add(key, value);
            }
            TheaterSubscribers.Clear();
            TheaterStats.Clear();
            foreach (KeyValuePair<string, long> pair in subscribers) TheaterSubscribers.Add(pair.Key, pair.Value);
            foreach (KeyValuePair<string, long> pair in stats) TheaterStats.Add(pair.Key, pair.Value);
            return true;
        }

        private static bool RestoreStats(WideNumericStateRecordV1 source, out string error)
        {
            error = string.Empty;
            if (source.stats_total_fans_per_week.Count != Stats.data.fans.total_fans_per_week.Count ||
                source.stats_fans_change_per_week.Count != Stats.data.fans.fans_change_per_week.Count)
            {
                error = "Stats series lengths do not match vanilla reconstruction";
                return false;
            }
            List<long> totals = new List<long>();
            List<long> changes = new List<long>();
            for (int index = 0; index < source.stats_total_fans_per_week.Count; index++)
            {
                long total, change;
                if (!TryParseCanonical(source.stats_total_fans_per_week[index], out total) ||
                    !TryParseCanonical(source.stats_fans_change_per_week[index], out change) ||
                    !Mirror(total, Stats.data.fans.total_fans_per_week[index]) ||
                    !Mirror(change, Stats.data.fans.fans_change_per_week[index]))
                {
                    error = "Stats value is inconsistent with its vanilla mirror";
                    return false;
                }
                totals.Add(total);
                changes.Add(change);
            }
            statsTotalFans = totals;
            statsFanChanges = changes;
            return true;
        }

        private static bool RestoreStory(WideNumericStateRecordV1 source, out string error)
        {
            error = string.Empty;
            long ch3, ch4, ch4Scandal;
            if (!TryParseCanonical(source.story_ch3_aya_fans, out ch3) ||
                !TryParseCanonical(source.story_ch4_fans_needed, out ch4) ||
                !TryParseCanonical(source.story_ch4_scandal_points, out ch4Scandal) ||
                (source.has_story_ch3_aya_fans && !Mirror(ch3, tasks.Story_Data.ch3_aya_fans)) ||
                (source.has_story_ch4_fans_needed && !Mirror(ch4, tasks.Story_Data.ch4_fans_needed)) ||
                (source.has_story_ch4_scandal_points &&
                    !Mirror(ch4Scandal, tasks.Story_Data.ch4_scandal_points)))
            {
                error = "story target value is inconsistent with its vanilla mirror";
                return false;
            }
            hasStoryCh3 = source.has_story_ch3_aya_fans;
            storyCh3 = ch3;
            hasStoryCh4 = source.has_story_ch4_fans_needed;
            storyCh4 = ch4;
            hasStoryCh4Scandal = source.has_story_ch4_scandal_points;
            storyCh4Scandal = ch4Scandal;
            return true;
        }

        private static bool RestoreLoans(WideNumericStateRecordV1 source, out string error)
        {
            error = string.Empty;
            Dictionary<int, loans._loan> live = new Dictionary<int, loans._loan>();
            foreach (loans._loan loan in loans.Loans)
            {
                if (loan == null || live.ContainsKey(loan.ID))
                {
                    error = "current loan set is invalid or duplicated";
                    return false;
                }
                live.Add(loan.ID, loan);
            }
            Dictionary<int, long> pending = new Dictionary<int, long>();
            foreach (loans._loan loan in loans.Loans) pending.Add(loan.ID, loan.PaymentPerWeek);
            foreach (WideLoanPaymentRecordV1 item in source.loan_payments)
            {
                loans._loan target;
                long amount, payment;
                if (!live.TryGetValue(item.loan_id, out target) ||
                    !TryParseCanonical(item.amount, out amount) ||
                    !TryParseCanonical(item.payment_per_week, out payment) ||
                    amount != target.Amount || item.duration != (int)target.Duration ||
                    !Mirror(payment, target.PaymentPerWeek))
                {
                    error = "loan payment identity/witness/mirror is inconsistent";
                    return false;
                }
                pending[item.loan_id] = payment;
            }
            LoanPayments.Clear();
            foreach (KeyValuePair<int, long> pair in pending) LoanPayments.Add(pair.Key, pair.Value);
            return true;
        }

        private static bool RestoreCafes(WideNumericStateRecordV1 source, out string error)
        {
            error = string.Empty;
            Dictionary<int, Cafes._cafe> live = new Dictionary<int, Cafes._cafe>();
            foreach (Cafes._cafe cafe in Cafes.Cafes_)
            {
                if (cafe == null || live.ContainsKey(cafe.ID))
                {
                    error = "current café set is invalid or duplicated";
                    return false;
                }
                live.Add(cafe.ID, cafe);
            }
            Dictionary<string, long> pendingProfits =
                new Dictionary<string, long>(StringComparer.Ordinal);
            Dictionary<string, long> pendingFans =
                new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (Cafes._cafe cafe in Cafes.Cafes_)
            {
                for (int index = 0; index < cafe.Stats.Count; index++)
                {
                    string key = CafeKey(cafe.ID, index);
                    pendingProfits.Add(key, cafe.Stats[index].Profit);
                    pendingFans.Add(key, cafe.Stats[index].New_Fans);
                }
            }
            foreach (WideCafeProfitRecordV1 item in source.cafe_profits)
            {
                Cafes._cafe cafe;
                long value;
                long fans;
                if (!live.TryGetValue(item.cafe_id, out cafe) ||
                    item.stat_ordinal >= cafe.Stats.Count ||
                    !TryParseCanonical(item.profit, out value) ||
                    !TryParseCanonical(item.new_fans, out fans) ||
                    cafe.Stats[item.stat_ordinal].Dish_ID != item.dish_id ||
                    !Mirror(value, cafe.Stats[item.stat_ordinal].Profit) ||
                    !Mirror(fans, cafe.Stats[item.stat_ordinal].New_Fans))
                {
                    error = "café stat identity/witness/mirror is inconsistent";
                    return false;
                }
                string key = CafeKey(item.cafe_id, item.stat_ordinal);
                pendingProfits[key] = value;
                pendingFans[key] = fans;
            }
            CafeProfits.Clear();
            CafeNewFans.Clear();
            foreach (KeyValuePair<string, long> pair in pendingProfits)
                CafeProfits.Add(pair.Key, pair.Value);
            foreach (KeyValuePair<string, long> pair in pendingFans)
                CafeNewFans.Add(pair.Key, pair.Value);
            return true;
        }

        private static bool RestoreBusinessContracts(WideNumericStateRecordV1 source, out string error)
        {
            error = string.Empty;
            mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
            business manager = main == null || main.Data == null ? null : main.Data.GetComponent<business>();
            if (manager == null || manager.ActiveProposals == null)
            {
                error = "business manager/active contract list is unavailable";
                return false;
            }

            ConditionalWeakTable<business.active_proposal, WideBusinessContractRuntime> pending =
                new ConditionalWeakTable<business.active_proposal, WideBusinessContractRuntime>();
            foreach (business.active_proposal live in manager.ActiveProposals)
            {
                if (live == null)
                {
                    error = "current business-contract set contains a null row";
                    return false;
                }
                pending.Add(live, new WideBusinessContractRuntime(live.Payment_per_week));
            }

            foreach (WideBusinessContractPaymentRecordV1 item in source.business_contract_payments)
            {
                if (item == null || item.ordinal < 0 || item.ordinal >= manager.ActiveProposals.Count)
                {
                    error = "business-contract payment ordinal is invalid";
                    return false;
                }
                business.active_proposal live = manager.ActiveProposals[item.ordinal];
                long exact;
                int girlId = live.Girl == null ? -1 : live.Girl.id;
                if (!TryParseCanonical(item.payment_per_week, out exact) ||
                    item.girl_id != girlId || item.skill != (int)live.Skill || item.type != (int)live.Type ||
                    item.end_date != ExtensionMethods.ToDataString(live.EndDate) ||
                    !Mirror(exact, live.Payment_per_week))
                {
                    error = "business-contract payment identity/witness/mirror is inconsistent";
                    return false;
                }
                pending.Remove(live);
                pending.Add(live, new WideBusinessContractRuntime(exact));
                live.Payment_per_week = WideNumericMath.ClampToInt32(exact);
            }
            BusinessContracts = pending;
            return true;
        }

        private static void SeedLegacy(Subsystem subsystem)
        {
            lock (Sync)
            {
                switch (subsystem)
                {
                    case Subsystem.Tours:
                        Tours = new ConditionalWeakTable<SEvent_Tour.tour, WideTourRuntime>();
                        foreach (SEvent_Tour.tour item in SEvent_Tour.Tours)
                            Tours.Add(item, WideTourRuntime.From(item));
                        break;
                    case Subsystem.Singles:
                        Singles.Clear();
                        foreach (singles._single item in singles.Singles) Singles[item.id] = WideSingleRuntime.From(item.ReleaseData);
                        break;
                    case Subsystem.Shows:
                        ShowFanSeries.Clear();
                        foreach (Shows._show item in global::Shows.shows) ShowFanSeries[item.id] = Mirror(item.fans);
                        break;
                    case Subsystem.Theaters:
                        TheaterSubscribers.Clear();
                        TheaterStats.Clear();
                        foreach (Theaters._theater theater in global::Theaters.Theaters_)
                        {
                            for (int index = 0; index < theater.Subscribers.Count; index++)
                            {
                                Theaters._theater._subscriber sub = theater.Subscribers[index];
                                TheaterSubscribers[SubscriberKey(theater.ID, index, (int)sub.gender,
                                    (int)sub.hardcoreness, (int)sub.age)] = sub.People;
                            }
                            for (int index = 0; index < theater.Stats.Count; index++)
                                TheaterStats[StatKey(theater.ID, index)] = theater.Stats[index].Subscribers;
                        }
                        break;
                    case Subsystem.Stats:
                        statsTotalFans = Mirror(Stats.data.fans.total_fans_per_week);
                        statsFanChanges = Mirror(Stats.data.fans.fans_change_per_week);
                        break;
                    case Subsystem.Story:
                        hasStoryCh3 = true;
                        storyCh3 = tasks.Story_Data.ch3_aya_fans;
                        hasStoryCh4 = tasks.Story_Data.ch4_fans_needed >= 0;
                        storyCh4 = tasks.Story_Data.ch4_fans_needed;
                        hasStoryCh4Scandal = tasks.Story_Data.ch4_scandal_points >= 0;
                        storyCh4Scandal = tasks.Story_Data.ch4_scandal_points;
                        break;
                    case Subsystem.Loans:
                        LoanPayments.Clear();
                        foreach (loans._loan loan in loans.Loans) LoanPayments[loan.ID] = loan.PaymentPerWeek;
                        break;
                    case Subsystem.Cafes:
                        CafeProfits.Clear();
                        CafeNewFans.Clear();
                        foreach (Cafes._cafe cafe in global::Cafes.Cafes_)
                            for (int index = 0; index < cafe.Stats.Count; index++)
                            {
                                CafeProfits[CafeKey(cafe.ID, index)] = cafe.Stats[index].Profit;
                                CafeNewFans[CafeKey(cafe.ID, index)] = cafe.Stats[index].New_Fans;
                            }
                        break;
                    case Subsystem.BusinessContracts:
                    {
                        BusinessContracts = new ConditionalWeakTable<business.active_proposal, WideBusinessContractRuntime>();
                        mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
                        business manager = main == null || main.Data == null ? null : main.Data.GetComponent<business>();
                        if (manager != null && manager.ActiveProposals != null)
                            foreach (business.active_proposal proposal in manager.ActiveProposals)
                                if (proposal != null) BusinessContracts.Add(proposal, new WideBusinessContractRuntime(proposal.Payment_per_week));
                        break;
                    }
                }
            }
        }

        private static WideTourRuntime GetTour(SEvent_Tour.tour tour)
        {
            EnsureEpoch();
            lock (Sync) { return GetTourUnlocked(tour); }
        }

        private static WideTourRuntime GetTourUnlocked(SEvent_Tour.tour tour)
        {
            if (tour == null) throw new ArgumentNullException(nameof(tour));
            WideTourRuntime state;
            if (!Tours.TryGetValue(tour, out state))
            {
                state = WideTourRuntime.From(tour);
                Tours.Add(tour, state);
            }
            return state;
        }

        private static long GetTourCountryValue(SEvent_Tour.tour tour, int ordinal, int member)
        {
            EnsureEpoch();
            lock (Sync)
            {
                WideTourRuntime state = GetTourUnlocked(tour);
                SynchronizeTourCountriesUnlocked(tour, state);
                if (ordinal < 0 || ordinal >= state.Countries.Count)
                    throw new ArgumentOutOfRangeException(nameof(ordinal));
                WideTourCountryRuntime country = state.Countries[ordinal];
                if (member == 0) return country.Audience;
                if (member == 1) return country.NewFans;
                return country.Revenue;
            }
        }

        private static void SynchronizeTourCountriesUnlocked(
            SEvent_Tour.tour tour,
            WideTourRuntime state)
        {
            if (tour.SelectedCountries == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "A33 tour selected-country set is null.");
                throw new InvalidOperationException("Tour selected-country set is null.");
            }

            bool alreadyAligned = state.Countries.Count == tour.SelectedCountries.Count;
            if (alreadyAligned)
            {
                for (int index = 0; index < state.Countries.Count; index++)
                {
                    SEvent_Tour.tour.selectedCountry live = tour.SelectedCountries[index];
                    if (live == null || live.Country == null ||
                        state.Countries[index].Country != (int)live.Country.Type ||
                        state.Countries[index].Ordinal != index)
                    {
                        alreadyAligned = false;
                        break;
                    }
                }
            }
            if (alreadyAligned) return;

            List<WideTourCountryRuntime> aligned = new List<WideTourCountryRuntime>();
            for (int index = 0; index < tour.SelectedCountries.Count; index++)
            {
                SEvent_Tour.tour.selectedCountry live = tour.SelectedCountries[index];
                if (live == null || live.Country == null)
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "A33 tour contains a null selected country.");
                    throw new InvalidOperationException("Tour contains a null selected country.");
                }
                WideTourCountryRuntime prior = state.Countries.Find(item =>
                    item.Country == (int)live.Country.Type);
                aligned.Add(prior == null
                    ? new WideTourCountryRuntime((int)live.Country.Type, index,
                        live.Audience, live.NewFans, live.Revenue)
                    : new WideTourCountryRuntime(prior.Country, index,
                        prior.Audience, prior.NewFans, prior.Revenue));
            }
            state.Countries.Clear();
            state.Countries.AddRange(aligned);
        }

        private static long GetSubscriberUnlocked(
            Theaters._theater theater,
            Theaters._theater._subscriber subscriber,
            int ordinal)
        {
            string key = SubscriberKey(theater.ID, ordinal, (int)subscriber.gender,
                (int)subscriber.hardcoreness, (int)subscriber.age);
            long value;
            if (!TheaterSubscribers.TryGetValue(key, out value))
            {
                value = subscriber.People;
                TheaterSubscribers.Add(key, value);
            }
            return value;
        }

        private static void EnsureStatsUnlocked()
        {
            if (statsTotalFans.Count == 0 && Stats.data.fans.total_fans_per_week.Count != 0)
                statsTotalFans = Mirror(Stats.data.fans.total_fans_per_week);
            if (statsFanChanges.Count == 0 && Stats.data.fans.fans_change_per_week.Count != 0)
                statsFanChanges = Mirror(Stats.data.fans.fans_change_per_week);
        }

        private static void EnsureEpoch()
        {
            long current = LoadEpoch.Current;
            lock (Sync)
            {
                if (epoch == current) return;
                Tours = new ConditionalWeakTable<SEvent_Tour.tour, WideTourRuntime>();
                Singles.Clear(); ShowFanSeries.Clear(); TheaterSubscribers.Clear();
                TheaterStats.Clear(); LoanPayments.Clear(); CafeProfits.Clear(); CafeNewFans.Clear();
                BusinessProposals = new ConditionalWeakTable<business._proposal, WideBusinessProposalRuntime>();
                BusinessContracts = new ConditionalWeakTable<business.active_proposal, WideBusinessContractRuntime>();
                statsTotalFans = new List<long>();
                statsFanChanges = new List<long>();
                hasStoryCh3 = false; storyCh3 = 0L; hasStoryCh4 = false; storyCh4 = 0L;
                hasStoryCh4Scandal = false; storyCh4Scandal = 0L;
                epoch = current;
            }
        }

        private static bool TryTour(WideTourRecordV1 item, SEvent_Tour.tour target,
            out WideTourRuntime value, out string error)
        {
            value = null;
            error = string.Empty;
            long production, expected, saving, revenue, fans;
            if (!TryParseCanonical(item.production_cost, out production) ||
                !TryParseCanonical(item.expected_revenue, out expected) ||
                !TryParseCanonical(item.saving, out saving) ||
                !TryParseCanonical(item.revenue, out revenue) ||
                !TryParseCanonical(item.new_fans, out fans) ||
                !Mirror(production, target.ProductionCost) || !Mirror(expected, target.ExpectedRevenue) ||
                !Mirror(saving, target.Saving) || !Mirror(revenue, target.Revenue) ||
                !Mirror(fans, target.NewFans))
            {
                error = "tour aggregate is inconsistent with its vanilla mirror";
                return false;
            }
            value = new WideTourRuntime(production, expected, saving, revenue, fans);
            for (int index = 0; index < item.countries.Count; index++)
            {
                WideTourCountryRecordV1 saved = item.countries[index];
                SEvent_Tour.tour.selectedCountry live = target.SelectedCountries[index];
                long audience, newFans, countryRevenue;
                if (saved.ordinal != index || live == null || live.Country == null ||
                    saved.country != (int)live.Country.Type || saved.level != live.Level ||
                    saved.attendance != live.Attendance || saved.discount != live.Discount ||
                    !TryParseCanonical(saved.audience, out audience) ||
                    !TryParseCanonical(saved.new_fans, out newFans) ||
                    !TryParseCanonical(saved.revenue, out countryRevenue) ||
                    !Mirror(audience, live.Audience) || !Mirror(newFans, live.NewFans) ||
                    !Mirror(countryRevenue, live.Revenue))
                {
                    error = "tour-country record is inconsistent with its identity/witness/mirror";
                    return false;
                }
                value.Countries.Add(new WideTourCountryRuntime(saved.country, index,
                    audience, newFans, countryRevenue));
            }
            return true;
        }

        private static void ApplyTourMirrors(SEvent_Tour.tour tour, WideTourRuntime value)
        {
            tour.ProductionCost = WideNumericMath.ClampToInt32(value.ProductionCost);
            tour.ExpectedRevenue = WideNumericMath.ClampToInt32(value.ExpectedRevenue);
            tour.Saving = WideNumericMath.ClampToInt32(value.Saving);
            tour.Revenue = WideNumericMath.ClampToInt32(value.Revenue);
            tour.NewFans = WideNumericMath.ClampToInt32(value.NewFans);
            for (int index = 0; index < value.Countries.Count; index++)
            {
                tour.SelectedCountries[index].Audience = WideNumericMath.ClampToInt32(value.Countries[index].Audience);
                tour.SelectedCountries[index].NewFans = WideNumericMath.ClampToInt32(value.Countries[index].NewFans);
                tour.SelectedCountries[index].Revenue = WideNumericMath.ClampToInt32(value.Countries[index].Revenue);
            }
        }

        private static bool Mirror(long exact, int live, int saved)
        {
            int mirror = WideNumericMath.ClampToInt32(exact);
            return live == mirror && saved == mirror;
        }
        private static bool Mirror(long exact, int mirror)
        {
            return mirror == WideNumericMath.ClampToInt32(exact);
        }
        private static List<long> Mirror(List<int> values)
        {
            List<long> result = new List<long>();
            if (values != null) foreach (int value in values) result.Add(value);
            return result;
        }
        private static bool Canonical(params string[] values)
        {
            foreach (string value in values) { long ignored; if (!TryParseCanonical(value, out ignored)) return false; }
            return true;
        }
        private static bool Canonical(List<string> values)
        {
            foreach (string value in values) { long ignored; if (!TryParseCanonical(value, out ignored)) return false; }
            return true;
        }
        private static string SubscriberKey(int theater, int ordinal, int gender, int hardcore, int age)
        {
            return theater.ToString(CultureInfo.InvariantCulture) + ":" + ordinal.ToString(CultureInfo.InvariantCulture) +
                ":" + gender.ToString(CultureInfo.InvariantCulture) + ":" + hardcore.ToString(CultureInfo.InvariantCulture) +
                ":" + age.ToString(CultureInfo.InvariantCulture);
        }
        private static string StatKey(int theater, int ordinal)
        {
            return theater.ToString(CultureInfo.InvariantCulture) + ":" + ordinal.ToString(CultureInfo.InvariantCulture);
        }
        private static string CafeKey(int cafe, int ordinal)
        {
            return cafe.ToString(CultureInfo.InvariantCulture) + ":" + ordinal.ToString(CultureInfo.InvariantCulture);
        }
        private static SaveManager.SavedData GetTargetSavedData()
        {
            try
            {
                if (Camera.main == null) return null;
                mainScript main = Camera.main.GetComponent<mainScript>();
                return main == null ? null : main.GetSavedData();
            }
            catch (Exception) { return null; }
        }
        private static void SetDiagnostic(string value)
        {
            lock (Sync) { lastDiagnostic = value ?? string.Empty; }
        }

        private enum Subsystem { Tours, Singles, Shows, Theaters, Stats, Story, Loans, Cafes, BusinessContracts }
    }

    internal sealed class WideBusinessProposalRuntime
    {
        internal long BasePayment;
        internal WideBusinessProposalRuntime(long value) { BasePayment = value; }
    }

    internal sealed class WideBusinessContractRuntime
    {
        internal long PaymentPerWeek;
        internal WideBusinessContractRuntime(long value) { PaymentPerWeek = value; }
    }

    internal sealed class WideSingleRuntime
    {
        internal readonly long Ordinary;
        internal readonly long Hardcore;
        internal readonly long Casual;
        internal WideSingleRuntime(long ordinary, long hardcore, long casual)
        { Ordinary = ordinary; Hardcore = hardcore; Casual = casual; }
        internal static WideSingleRuntime From(singles._single._releaseData data)
        {
            return data == null ? new WideSingleRuntime(0L, 0L, 0L) :
                new WideSingleRuntime(data.NewFans, data.NewHardcoreFans, data.NewCasualFans);
        }
    }

    internal sealed class WideTourRuntime
    {
        internal long ProductionCost;
        internal long ExpectedRevenue;
        internal long Saving;
        internal long Revenue;
        internal long NewFans;
        internal readonly List<WideTourCountryRuntime> Countries = new List<WideTourCountryRuntime>();
        internal WideTourRuntime(long production, long expected, long saving, long revenue, long fans)
        { ProductionCost = production; ExpectedRevenue = expected; Saving = saving; Revenue = revenue; NewFans = fans; }
        internal static WideTourRuntime From(SEvent_Tour.tour tour)
        {
            WideTourRuntime value = new WideTourRuntime(tour.ProductionCost, tour.ExpectedRevenue,
                tour.Saving, tour.Revenue, tour.NewFans);
            for (int index = 0; index < tour.SelectedCountries.Count; index++)
            {
                SEvent_Tour.tour.selectedCountry country = tour.SelectedCountries[index];
                value.Countries.Add(new WideTourCountryRuntime((int)country.Country.Type, index,
                    country.Audience, country.NewFans, country.Revenue));
            }
            return value;
        }
    }

    internal sealed class WideTourCountryRuntime
    {
        internal readonly int Country;
        internal readonly int Ordinal;
        internal long Audience;
        internal long NewFans;
        internal long Revenue;
        internal WideTourCountryRuntime(int country, int ordinal, long audience, long fans, long revenue)
        { Country = country; Ordinal = ordinal; Audience = audience; NewFans = fans; Revenue = revenue; }
    }
}
