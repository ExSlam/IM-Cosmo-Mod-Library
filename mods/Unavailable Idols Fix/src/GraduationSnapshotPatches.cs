using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace UnavailableIdolsFix
{
    internal static class GraduationSnapshotJson
    {
        internal static string Build(data_girls.girls girl)
        {
            StringBuilder json = new StringBuilder(4096);
            json.Append('{');
            bool first = true;
            Add(json, ref first, "idol_id", girl.id);
            Add(json, ref first, "name", SafeName(girl));
            Add(json, ref first, "first_name", girl.firstName);
            Add(json, ref first, "last_name", girl.lastName);
            Add(json, ref first, "nickname", girl.nickname);
            Add(json, ref first, "status", girl.status.ToString());
            Add(json, ref first, "age", SafeAge(girl));
            Add(json, ref first, "birthday", FormatDate(girl.birthday));
            Add(json, ref first, "hiring_date", FormatDate(girl.Hiring_Date));
            Add(json, ref first, "scheduled_graduation_date", FormatDate(girl.Graduation_Date));
            Add(json, ref first, "snapshot_game_date", FormatDate(staticVars.dateTime));
            Add(json, ref first, "salary", girl.salary);
            Add(json, ref first, "trait", girl.trait.ToString());
            Add(json, ref first, "fan_total", SafeFanTotal(girl));

            Groups._group group = girl.GetGroup();
            Add(json, ref first, "group_id", group == null ? -1 : group.ID);
            Add(json, ref first, "group_title", group == null ? string.Empty : group.Title);

            AddFanBuckets(json, ref first, girl);
            AddStats(json, ref first, girl);
            AddRelationships(json, ref first, girl);
            AddAssignments(json, ref first, girl);
            json.Append('}');
            return json.ToString();
        }

        private static void AddFanBuckets(StringBuilder json, ref bool first, data_girls.girls girl)
        {
            PropertyName(json, ref first, "fan_buckets");
            json.Append('[');
            bool firstFan = true;
            if (girl.Fans != null)
            {
                foreach (resources._fan fan in girl.Fans)
                {
                    if (fan == null)
                    {
                        continue;
                    }

                    if (!firstFan)
                    {
                        json.Append(',');
                    }
                    firstFan = false;
                    json.Append('{');
                    bool firstProperty = true;
                    Add(json, ref firstProperty, "gender", fan.gender.ToString());
                    Add(json, ref firstProperty, "hardcoreness", fan.hardcoreness.ToString());
                    Add(json, ref firstProperty, "age", fan.age.ToString());
                    Add(json, ref firstProperty, "people", fan.people);
                    Add(json, ref firstProperty, "appeal", fan.appeal);
                    Add(json, ref firstProperty, "opinion", fan.Ratio);
                    json.Append('}');
                }
            }
            json.Append(']');
        }

        private static void AddStats(StringBuilder json, ref bool first, data_girls.girls girl)
        {
            PropertyName(json, ref first, "parameters");
            json.Append('{');
            bool firstParameter = true;
            if (girl.parameters != null)
            {
                foreach (data_girls.girls.param parameter in girl.parameters)
                {
                    if (parameter != null)
                    {
                        Add(json, ref firstParameter, parameter.type.ToString(), parameter.val);
                    }
                }
            }
            json.Append('}');
        }

        private static void AddAssignments(StringBuilder json, ref bool first, data_girls.girls girl)
        {
            PropertyName(json, ref first, "assignments");
            json.Append('{');
            bool firstAssignment = true;

            PropertyName(json, ref firstAssignment, "unreleased_single_ids");
            json.Append('[');
            bool firstItem = true;
            if (singles.Singles != null)
            {
                foreach (singles._single single in singles.Singles)
                {
                    if (single != null && single.status != singles._single._status.released && AvailabilityRules.SingleContains(single, girl))
                    {
                        AppendNumber(json, ref firstItem, single.id);
                    }
                }
            }
            json.Append(']');

            PropertyName(json, ref firstAssignment, "show_ids");
            json.Append('[');
            firstItem = true;
            if (Shows.shows != null)
            {
                foreach (Shows._show show in Shows.shows)
                {
                    if (show != null && show.status != Shows._show._status.canceled && AvailabilityRules.ShowContains(show, girl))
                    {
                        AppendNumber(json, ref firstItem, show.id);
                    }
                }
            }
            json.Append(']');

            PropertyName(json, ref firstAssignment, "concert_ids");
            json.Append('[');
            firstItem = true;
            if (SEvent_Concerts.Concerts != null)
            {
                foreach (SEvent_Concerts._concert concert in SEvent_Concerts.Concerts)
                {
                    if (concert != null && concert.Status != SEvent_Tour.tour._status.finished && AvailabilityRules.ConcertContains(concert, girl))
                    {
                        AppendNumber(json, ref firstItem, concert.ID);
                    }
                }
            }
            json.Append(']');

            Girls_Mentors._mentor mentor = Girls_Mentors.GetSenpai_Object(girl, girl);
            data_girls.girls other = mentor == null ? null : (mentor.Senpai == girl ? mentor.Kohai : mentor.Senpai);
            Add(json, ref firstAssignment, "mentor_partner_id", other == null ? -1 : other.id);

            PropertyName(json, ref firstAssignment, "push_slots");
            json.Append('[');
            firstItem = true;
            if (Pushes.Girls != null)
            {
                for (int i = 0; i < Pushes.Girls.Count; i++)
                {
                    if (Pushes.Girls[i] == girl)
                    {
                        AppendNumber(json, ref firstItem, i);
                    }
                }
            }
            json.Append(']');
            json.Append('}');
        }

        private static void AddRelationships(StringBuilder json, ref bool first, data_girls.girls girl)
        {
            PropertyName(json, ref first, "relationships");
            json.Append('[');
            bool firstRelationship = true;
            List<Relationships._relationship> relationships = Relationships.GetAllRelationships(girl, false);
            if (relationships != null)
            {
                foreach (Relationships._relationship relationship in relationships)
                {
                    if (relationship == null)
                    {
                        continue;
                    }

                    data_girls.girls other = relationship.GetOtherGirl(girl);
                    if (!firstRelationship)
                    {
                        json.Append(',');
                    }
                    firstRelationship = false;
                    json.Append('{');
                    bool firstProperty = true;
                    Add(json, ref firstProperty, "idol_id", other == null ? -1 : other.id);
                    Add(json, ref firstProperty, "idol_name", other == null ? string.Empty : SafeName(other));
                    Add(json, ref firstProperty, "ratio", relationship.Ratio);
                    Add(json, ref firstProperty, "status", relationship.Status.ToString());
                    Add(json, ref firstProperty, "dating", relationship.Dating ? 1 : 0);
                    json.Append('}');
                }
            }
            json.Append(']');

            if (girl.DatingData != null)
            {
                Add(json, ref first, "dating_partner_status", girl.DatingData.Partner_Status.ToString());
                Add(json, ref first, "dating_status_known", girl.DatingData.Is_Partner_Status_Known ? 1 : 0);
            }
        }

        private static string SafeName(data_girls.girls girl)
        {
            try
            {
                return girl.GetName(true);
            }
            catch
            {
                return (girl.firstName ?? string.Empty) + " " + (girl.lastName ?? string.Empty);
            }
        }

        private static int SafeAge(data_girls.girls girl)
        {
            try
            {
                return girl.GetAge();
            }
            catch
            {
                return 0;
            }
        }

        private static long SafeFanTotal(data_girls.girls girl)
        {
            try
            {
                return girl.GetFans_Total(null);
            }
            catch
            {
                return 0L;
            }
        }

        private static string FormatDate(DateTime date)
        {
            return date.ToString("o", CultureInfo.InvariantCulture);
        }

        private static void Add(StringBuilder json, ref bool first, string name, string value)
        {
            PropertyName(json, ref first, name);
            AppendQuoted(json, value ?? string.Empty);
        }

        private static void Add(StringBuilder json, ref bool first, string name, int value)
        {
            PropertyName(json, ref first, name);
            json.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void Add(StringBuilder json, ref bool first, string name, long value)
        {
            PropertyName(json, ref first, name);
            json.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void Add(StringBuilder json, ref bool first, string name, float value)
        {
            PropertyName(json, ref first, name);
            json.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void PropertyName(StringBuilder json, ref bool first, string name)
        {
            if (!first)
            {
                json.Append(',');
            }
            first = false;
            AppendQuoted(json, name);
            json.Append(':');
        }

        private static void AppendNumber(StringBuilder json, ref bool first, int value)
        {
            if (!first)
            {
                json.Append(',');
            }
            first = false;
            json.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendQuoted(StringBuilder json, string value)
        {
            json.Append('"');
            if (value != null)
            {
                foreach (char character in value)
                {
                    switch (character)
                    {
                        case '"': json.Append("\\\""); break;
                        case '\\': json.Append("\\\\"); break;
                        case '\b': json.Append("\\b"); break;
                        case '\f': json.Append("\\f"); break;
                        case '\n': json.Append("\\n"); break;
                        case '\r': json.Append("\\r"); break;
                        case '\t': json.Append("\\t"); break;
                        default:
                            if (character < 32)
                            {
                                json.Append("\\u");
                                json.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                json.Append(character);
                            }
                            break;
                    }
                }
            }
            json.Append('"');
        }
    }

    internal static class IMDataCoreSnapshotBridge
    {
        private const string HarmonyId = "com.cosmo.imdatacore";
        private const string ApiTypeName = "IMDataCore.IMDataCoreApi";
        private const string NamespaceIdentifier = "com.cosmo.unavailableidolsfix";
        private static MethodInfo registerMethod;
        private static MethodInfo appendMethod;
        private static object session;
        private static bool resolved;
        private static bool registrationAttempted;
        private static bool loggedFailure;

        internal static void TryAppend(data_girls.girls girl, string payload)
        {
            if (girl == null || string.IsNullOrEmpty(payload) || !Harmony.HasAnyPatches(HarmonyId))
            {
                return;
            }

            try
            {
                if (!EnsureSession())
                {
                    return;
                }

                object[] args =
                {
                    session,
                    girl.id,
                    "idol",
                    girl.id.ToString(CultureInfo.InvariantCulture),
                    "graduating_idol_final_snapshot",
                    payload,
                    "data_girls.girls.Graduate.prefix",
                    string.Empty
                };
                object result = appendMethod.Invoke(null, args);
                if (!(result is bool) || !(bool)result)
                {
                    LogFailure(args[7] as string);
                }
            }
            catch (Exception exception)
            {
                LogFailure(exception.GetBaseException().Message);
            }
        }

        private static bool EnsureSession()
        {
            if (session != null)
            {
                return true;
            }
            if (registrationAttempted)
            {
                return false;
            }
            registrationAttempted = true;

            if (!Resolve())
            {
                return false;
            }

            object[] args = { NamespaceIdentifier, null, string.Empty };
            object result = registerMethod.Invoke(null, args);
            if (result is bool && (bool)result && args[1] != null)
            {
                session = args[1];
                return true;
            }

            LogFailure(args[2] as string);
            return false;
        }

        private static bool Resolve()
        {
            if (resolved)
            {
                return registerMethod != null && appendMethod != null;
            }
            resolved = true;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type apiType = assembly.GetType(ApiTypeName, false);
                if (apiType == null)
                {
                    continue;
                }

                registerMethod = FindMethod(apiType, "TryRegisterNamespace", 3);
                appendMethod = FindMethod(apiType, "TryAppendCustomEvent", 8);
                break;
            }

            if (registerMethod == null || appendMethod == null)
            {
                LogFailure("IM Data Core API methods were not found.");
                return false;
            }

            return true;
        }

        private static MethodInfo FindMethod(Type type, string name, int parameterCount)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name == name && method.GetParameters().Length == parameterCount)
                {
                    return method;
                }
            }
            return null;
        }

        private static void LogFailure(string message)
        {
            if (loggedFailure)
            {
                return;
            }
            loggedFailure = true;
            Debug.LogWarning("[Unavailable Idols Fix] IM Data Core snapshot was not appended: " + (message ?? "unknown error"));
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Graduate))]
    [HarmonyPriority(Priority.First)]
    internal static class GraduationFinalSnapshotPatch
    {
        private static void Prefix(data_girls.girls __instance)
        {
            if (__instance == null)
            {
                return;
            }

            string payload = GraduationSnapshotJson.Build(__instance);
            IMDataCoreSnapshotBridge.TryAppend(__instance, payload);
            // Graduation Details has its own Graduate prefix. This prefix does not mutate the
            // idol, so its snapshot still observes the complete pre-cleanup state.
        }
    }
}
