using System;
using System.Globalization;

namespace IMDataCore
{
    internal sealed partial class IMDataCoreController
    {
        /// <summary>
        /// Persists a successful name edit performed by Madxis' optional identity editor.
        /// This is a durable idol event and intentionally records both sides of the edit.
        /// </summary>
        internal void CaptureMadxisIdolNameChanged(
            int idolId,
            string oldFirstName,
            string oldLastName,
            string newFirstName,
            string newLastName,
            string oldDisplayName,
            string newDisplayName,
            string nationalityCode,
            string nationalityName)
        {
            if (idolId < CoreConstants.MinimumValidIdolIdentifier ||
                (string.Equals(oldFirstName ?? string.Empty, newFirstName ?? string.Empty, StringComparison.Ordinal) &&
                 string.Equals(oldLastName ?? string.Empty, newLastName ?? string.Empty, StringComparison.Ordinal)))
            {
                return;
            }

            lock (runtimeLock)
            {
                if (saveLoadPreparationActive)
                {
                    return;
                }

                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                IdolNameChangedEventPayload payload = new IdolNameChangedEventPayload
                {
                    idol_id = idolId,
                    idol_old_first_name = oldFirstName ?? string.Empty,
                    idol_old_last_name = oldLastName ?? string.Empty,
                    idol_new_first_name = newFirstName ?? string.Empty,
                    idol_new_last_name = newLastName ?? string.Empty,
                    idol_old_display_name = oldDisplayName ?? string.Empty,
                    idol_new_display_name = newDisplayName ?? string.Empty,
                    idol_nationality_code = nationalityCode ?? string.Empty,
                    idol_nationality_name = nationalityName ?? string.Empty,
                    event_date = CoreDateTimeUtility.ToRoundTripString(gameDate)
                };

                EnqueueEventRecordLocked(
                    gameDate,
                    idolId,
                    CoreConstants.EventEntityKindIdol,
                    idolId.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeIdolNameChanged,
                    CoreConstants.EventSourceMadxisIdolNameEditorPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));
                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Persists a successful nationality edit performed by Madxis' optional identity editor.
        /// Country codes and display names are stored together so history remains readable even if
        /// the external mod's catalog changes later.
        /// </summary>
        internal void CaptureMadxisIdolNationalityChanged(
            int idolId,
            string firstName,
            string lastName,
            string oldNationalityCode,
            string newNationalityCode,
            string oldNationalityName,
            string newNationalityName,
            string oldDisplayName,
            string newDisplayName)
        {
            string oldCode = string.IsNullOrWhiteSpace(oldNationalityCode) ? string.Empty : oldNationalityCode.Trim();
            string newCode = string.IsNullOrWhiteSpace(newNationalityCode) ? string.Empty : newNationalityCode.Trim();
            if (idolId < CoreConstants.MinimumValidIdolIdentifier ||
                string.Equals(oldCode, newCode, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            lock (runtimeLock)
            {
                if (saveLoadPreparationActive)
                {
                    return;
                }

                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                IdolNationalityChangedEventPayload payload = new IdolNationalityChangedEventPayload
                {
                    idol_id = idolId,
                    idol_old_first_name = firstName ?? string.Empty,
                    idol_old_last_name = lastName ?? string.Empty,
                    idol_old_nationality_code = oldCode,
                    idol_new_nationality_code = newCode,
                    idol_old_nationality_name = oldNationalityName ?? string.Empty,
                    idol_new_nationality_name = newNationalityName ?? string.Empty,
                    idol_old_display_name = oldDisplayName ?? string.Empty,
                    idol_new_display_name = newDisplayName ?? string.Empty,
                    event_date = CoreDateTimeUtility.ToRoundTripString(gameDate)
                };

                EnqueueEventRecordLocked(
                    gameDate,
                    idolId,
                    CoreConstants.EventEntityKindIdol,
                    idolId.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeIdolNationalityChanged,
                    CoreConstants.EventSourceMadxisIdolNationalityEditorPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));
                FlushAfterCaptureLocked();
            }
        }
    }

    [Serializable]
    internal sealed class IdolNameChangedEventPayload
    {
        public int idol_id;
        public string idol_old_first_name = string.Empty;
        public string idol_old_last_name = string.Empty;
        public string idol_new_first_name = string.Empty;
        public string idol_new_last_name = string.Empty;
        public string idol_old_display_name = string.Empty;
        public string idol_new_display_name = string.Empty;
        public string idol_nationality_code = string.Empty;
        public string idol_nationality_name = string.Empty;
        public string event_date = string.Empty;
    }

    [Serializable]
    internal sealed class IdolNationalityChangedEventPayload
    {
        public int idol_id;
        public string idol_old_first_name = string.Empty;
        public string idol_old_last_name = string.Empty;
        public string idol_old_nationality_code = string.Empty;
        public string idol_new_nationality_code = string.Empty;
        public string idol_old_nationality_name = string.Empty;
        public string idol_new_nationality_name = string.Empty;
        public string idol_old_display_name = string.Empty;
        public string idol_new_display_name = string.Empty;
        public string event_date = string.Empty;
    }
}
