using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using ModLocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GraduationDetails
{
    // Optional in older records. A missing career snapshot must never be backfilled
    // from live idol data after graduation, because those values can change.
    [Serializable]
    internal sealed class GraduationCareerSnapshot
    {
        public string HireDate = string.Empty;
        public string GraduationDate = string.Empty;
        public int FinalTenureDays = CareerProfile.UnknownTenureDays;
        public long TotalEarnings;
        public long TotalPaidByAgency;
        public bool AgencyPayAvailable;
        public bool AgencyPayComplete;
        public bool HasRecordedPayments;

        internal GraduationCareerSnapshot Clone()
        {
            return (GraduationCareerSnapshot)MemberwiseClone();
        }

        internal static bool Same(GraduationCareerSnapshot first, GraduationCareerSnapshot second)
        {
            if (ReferenceEquals(first, second)) return true;
            return first != null && second != null &&
                first.HireDate == second.HireDate && first.GraduationDate == second.GraduationDate &&
                first.FinalTenureDays == second.FinalTenureDays && first.TotalEarnings == second.TotalEarnings &&
                first.TotalPaidByAgency == second.TotalPaidByAgency &&
                first.AgencyPayAvailable == second.AgencyPayAvailable &&
                first.AgencyPayComplete == second.AgencyPayComplete &&
                first.HasRecordedPayments == second.HasRecordedPayments;
        }

        internal bool IsValid()
        {
            DateTime graduation;
            if (!CareerProfile.TryReadDate(GraduationDate, out graduation) ||
                TotalPaidByAgency < 0L || (AgencyPayComplete && !AgencyPayAvailable) ||
                (HasRecordedPayments && !AgencyPayAvailable) ||
                (!AgencyPayAvailable && TotalPaidByAgency != 0L))
                return false;
            if (string.IsNullOrEmpty(HireDate))
                return FinalTenureDays == CareerProfile.UnknownTenureDays;
            DateTime hire;
            return CareerProfile.TryReadDate(HireDate, out hire) && hire <= graduation &&
                FinalTenureDays == (graduation - hire).Days;
        }
    }

    internal static partial class GraduationDetailsImDataCoreBridge
    {
        private const string CareerPayMethodName = "TryGetIdolCareerPay";
        private const int PaidAmountArgument = 1;
        private const int CompleteArgument = 2;
        private const int RecordedArgument = 3;

        internal static bool TryGetCareerPay(int idolId, out long totalPaid,
            out bool complete, out bool recorded)
        {
            totalPaid = 0L;
            complete = false;
            recorded = false;
            lock (BridgeLock)
            {
                try
                {
                    bool available;
                    string error;
                    if (!TryEnsureSessionLocked(out available, out error) || !available)
                        return false;
                    // This optional API must not make existing namespace persistence depend
                    // on an upgraded IMDC. Older cores still retain graduation snapshots.
                    MethodInfo method = interopApiType.GetMethod(CareerPayMethodName,
                        BindingFlags.Public | BindingFlags.Static, null,
                        new Type[] { typeof(int), typeof(long).MakeByRefType(),
                            typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType(),
                            typeof(string).MakeByRefType() }, null);
                    if (method == null || method.ReturnType != typeof(bool)) return false;
                    object[] arguments = { idolId, 0L, false, false, string.Empty };
                    if (!(bool)method.Invoke(null, arguments)) return false;
                    totalPaid = (long)arguments[PaidAmountArgument];
                    complete = (bool)arguments[CompleteArgument];
                    recorded = (bool)arguments[RecordedArgument];
                    return totalPaid >= 0L;
                }
                catch
                {
                    // A missing/incompatible optional provider is an unknown amount, never zero pay.
                    totalPaid = 0L;
                    complete = false;
                    recorded = false;
                    return false;
                }
            }
        }
    }

    internal static class CareerProfile
    {
        internal const int UnknownTenureDays = -1;
        private const string StoredDateFormat = "yyyy-MM-dd";
        private const string DisplayDateFormat = "d";
        private const string DisplayNumberFormat = "N0";
        private const string CareerRowName = "GraduationDetails_CareerValues";
        private const string LineSeparator = "\n";
        private const string NotRecordedKey = "career.not_recorded";
        private const string HireDateKey = "career.hire_date";
        private const string GraduationDateKey = "career.graduation_date";
        private const string TenureKey = "career.tenure";
        private const string TenureDurationKey = "career.tenure_duration";
        private const string PaidKey = "career.total_paid";
        private const string PartialPayKey = "career.partial_pay";
        private const string PayUnavailableKey = "career.pay_unavailable";
        private const string EarningsPrefixKey = "jobs.total_earnings_prefix";

        internal static GraduationCareerSnapshot CaptureBeforeGraduation(data_girls.girls girl)
        {
            if (girl == null || girl.status == data_girls._status.graduated) return null;
            DateTime graduation = staticVars.dateTime.Date;
            DateTime hire = girl.Hiring_Date.Date;
            bool hireKnown = hire > DateTime.MinValue && hire <= graduation;
            var snapshot = new GraduationCareerSnapshot
            {
                HireDate = hireKnown ? WriteDate(hire) : string.Empty,
                GraduationDate = WriteDate(graduation),
                FinalTenureDays = hireKnown ? (graduation - hire).Days : UnknownTenureDays,
                TotalEarnings = girl.GetTotalEarnings()
            };
            snapshot.AgencyPayAvailable = GraduationDetailsImDataCoreBridge.TryGetCareerPay(girl.id,
                out snapshot.TotalPaidByAgency, out snapshot.AgencyPayComplete, out snapshot.HasRecordedPayments);
            return snapshot;
        }

        internal static void CommitGraduation(data_girls.girls girl, GraduationCareerSnapshot career)
        {
            if (girl == null || career == null || girl.status != data_girls._status.graduated) return;
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(girl.id);
            if (snapshot == null || snapshot.Career != null) return;
            // Commit only after vanilla completed graduation. Capture earnings before its
            // destructive cleanup, but freeze the actual final graduation date afterward.
            DateTime graduation = girl.Graduation_Date.Date;
            career.GraduationDate = WriteDate(graduation);
            DateTime hire;
            if (TryReadDate(career.HireDate, out hire) && hire <= graduation)
                career.FinalTenureDays = (graduation - hire).Days;
            else
            {
                career.HireDate = string.Empty;
                career.FinalTenureDays = UnknownTenureDays;
            }
            snapshot.Career = career.Clone();
            GraduationDetailsPersistenceController.UpsertGraduationSnapshot(snapshot);
        }

        internal static string GetArchivedEarnings(data_girls.girls girl)
        {
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(girl.id);
            string amount = snapshot != null && snapshot.Career != null
                ? FormatMoney(snapshot.Career.TotalEarnings) : Text(NotRecordedKey);
            return Text(EarningsPrefixKey) + amount;
        }

        internal static void Render(Profile_Popup profile)
        {
            if (profile == null || profile.Jobs_Container == null) return;
            Transform existing = profile.Jobs_Container.transform.Find(CareerRowName);
            if (profile.Girl == null)
            {
                if (existing != null) existing.gameObject.SetActive(false);
                return;
            }

            // Plain native text uses the existing Jobs tab's fonts, material, color,
            // layout and scrolling. No new popup, copied game scripts, or input handlers.
            TextMeshProUGUI reference = profile.Jobs_Salary == null ? null :
                profile.Jobs_Salary.GetComponentInChildren<TextMeshProUGUI>(true);
            if (reference == null) return;
            GameObject row = existing == null ? new GameObject(CareerRowName,
                typeof(RectTransform), typeof(TextMeshProUGUI), typeof(ContentSizeFitter)) : existing.gameObject;
            if (existing == null)
            {
                row.layer = profile.Jobs_Container.layer;
                row.transform.SetParent(profile.Jobs_Container.transform, false);
                row.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            if (profile.Jobs_Earnings != null &&
                profile.Jobs_Earnings.transform.parent == profile.Jobs_Container.transform)
                row.transform.SetSiblingIndex(profile.Jobs_Earnings.transform.GetSiblingIndex() + 1);

            // Vanilla's Jobs layout positions children but does not size their width.
            // Match its anchors/pivot and use the available column width before wrapping.
            RectTransform rowRect = row.GetComponent<RectTransform>();
            RectTransform containerRect = profile.Jobs_Container.GetComponent<RectTransform>();
            rowRect.anchorMin = reference.rectTransform.anchorMin;
            rowRect.anchorMax = reference.rectTransform.anchorMax;
            rowRect.pivot = reference.rectTransform.pivot;
            float columnWidth = containerRect.rect.width > 0f
                ? containerRect.rect.width : reference.rectTransform.rect.width;
            rowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, columnWidth);
            TextMeshProUGUI text = row.GetComponent<TextMeshProUGUI>();
            text.font = reference.font;
            text.fontSharedMaterial = reference.fontSharedMaterial;
            text.fontSize = reference.fontSize;
            text.fontStyle = reference.fontStyle;
            text.color = reference.color;
            text.alignment = reference.alignment;
            text.margin = reference.margin;
            text.lineSpacing = reference.lineSpacing;
            text.paragraphSpacing = reference.paragraphSpacing;
            text.enableWordWrapping = true;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
            text.text = BuildCareerText(profile.Girl);
            row.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rowRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }

        private static string BuildCareerText(data_girls.girls girl)
        {
            bool graduated = girl.status == data_girls._status.graduated || GraduationDetailsState.IsFor(girl);
            var lines = new List<string>();
            if (graduated)
            {
                GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(girl.id);
                GraduationCareerSnapshot career = snapshot == null ? null : snapshot.Career;
                DateTime hire;
                DateTime graduation;
                // Missing historical fields never fall through to live idol dates.
                bool hireKnown = TryReadDate(career == null ? string.Empty : career.HireDate, out hire);
                bool graduationKnown = TryReadDate(career == null ? string.Empty : career.GraduationDate, out graduation);
                lines.Add(Format(HireDateKey, hireKnown ? FormatDate(hire) : Text(NotRecordedKey)));
                lines.Add(Format(GraduationDateKey, graduationKnown ? FormatDate(graduation) : Text(NotRecordedKey)));
                lines.Add(Format(TenureKey, hireKnown && graduationKnown && career.FinalTenureDays >= 0
                    ? FormatTenure(hire, graduation) : Text(NotRecordedKey)));
                lines.Add(Format(PaidKey, career == null ? Text(NotRecordedKey) :
                    FormatPay(career.TotalPaidByAgency, career.AgencyPayAvailable,
                        career.AgencyPayComplete, career.HasRecordedPayments, true)));
            }
            else
            {
                DateTime hire = girl.Hiring_Date.Date;
                DateTime current = staticVars.dateTime.Date;
                bool hireKnown = hire > DateTime.MinValue && hire <= current;
                lines.Add(Format(HireDateKey, hireKnown ? FormatDate(hire) : Text(NotRecordedKey)));
                lines.Add(Format(TenureKey, hireKnown ? FormatTenure(hire, current) : Text(NotRecordedKey)));
                long totalPaid;
                bool complete;
                bool recorded;
                bool available = GraduationDetailsImDataCoreBridge.TryGetCareerPay(girl.id,
                    out totalPaid, out complete, out recorded);
                lines.Add(Format(PaidKey, FormatPay(totalPaid, available, complete, recorded, false)));
            }
            return string.Join(LineSeparator, lines.ToArray());
        }

        private static string FormatPay(long amount, bool available, bool complete, bool recorded, bool archived)
        {
            if (!available) return Text(archived ? NotRecordedKey : PayUnavailableKey);
            if (!complete && !recorded) return Text(NotRecordedKey);
            return complete ? FormatMoney(amount) : Format(PartialPayKey, FormatMoney(amount));
        }

        private static string FormatTenure(DateTime hire, DateTime end)
        {
            int years = end.Year - hire.Year;
            if (hire.AddYears(years) > end) years--;
            DateTime anniversary = hire.AddYears(years);
            int months = (end.Year - anniversary.Year) * MonthsPerYear + end.Month - anniversary.Month;
            if (anniversary.AddMonths(months) > end) months--;
            int days = (end - anniversary.AddMonths(months)).Days;
            return Format(TenureDurationKey, years.ToString(DisplayNumberFormat, staticVars.GetCulture()),
                months.ToString(DisplayNumberFormat, staticVars.GetCulture()),
                days.ToString(DisplayNumberFormat, staticVars.GetCulture()));
        }

        private const int MonthsPerYear = 12;
        private static string FormatMoney(long amount)
        {
            return ExtensionMethods.formatMoney(amount, false, false, false);
        }
        private static string FormatDate(DateTime date)
        {
            return date.ToString(DisplayDateFormat, staticVars.GetCulture());
        }
        private static string WriteDate(DateTime date)
        {
            return date.ToString(StoredDateFormat, CultureInfo.InvariantCulture);
        }
        internal static bool TryReadDate(string value, out DateTime date)
        {
            return DateTime.TryParseExact(value, StoredDateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date);
        }
        private static string Text(string key)
        {
            return ModLocalization.Get(key, string.Empty);
        }
        private static string Format(string key, params object[] values)
        {
            return string.Format(staticVars.GetCulture(), Text(key), values);
        }
    }
}

