using System;
using System.Collections.Generic;
using HarmonyLib;

namespace UnavailableIdolsFix
{
    internal sealed class RemovalSnapshot
    {
        internal data_girls.girls Girl;
        internal bool Temporary;
        internal RemovalSnapshot Previous;
        internal readonly List<singles._single> Singles = new List<singles._single>();
        internal readonly List<Shows._show> ShowProjects = new List<Shows._show>();
        internal readonly List<SEvent_Concerts._concert> Concerts = new List<SEvent_Concerts._concert>();
        internal readonly List<Girls_Mentors._mentor> Mentorships = new List<Girls_Mentors._mentor>();
        internal readonly List<int> PushSlots = new List<int>();

        internal static RemovalSnapshot Capture(data_girls.girls girl, bool temporary)
        {
            RemovalSnapshot snapshot = new RemovalSnapshot
            {
                Girl = girl,
                Temporary = temporary
            };

            if (girl == null)
            {
                return snapshot;
            }

            if (singles.Singles != null)
            {
                foreach (singles._single single in singles.Singles)
                {
                    if (single != null && single.status != singles._single._status.released && AvailabilityRules.SingleContains(single, girl))
                    {
                        snapshot.Singles.Add(single);
                    }
                }
            }

            if (Shows.shows != null)
            {
                foreach (Shows._show show in Shows.shows)
                {
                    if (show != null && show.status != Shows._show._status.canceled && AvailabilityRules.ShowContains(show, girl))
                    {
                        snapshot.ShowProjects.Add(show);
                    }
                }
            }

            if (SEvent_Concerts.Concerts != null)
            {
                foreach (SEvent_Concerts._concert concert in SEvent_Concerts.Concerts)
                {
                    if (concert != null && concert.Status != SEvent_Tour.tour._status.finished && AvailabilityRules.ConcertContains(concert, girl))
                    {
                        snapshot.Concerts.Add(concert);
                    }
                }
            }

            if (Girls_Mentors.Mentors != null)
            {
                foreach (Girls_Mentors._mentor mentor in Girls_Mentors.Mentors)
                {
                    if (mentor != null && (mentor.Senpai == girl || mentor.Kohai == girl))
                    {
                        snapshot.Mentorships.Add(mentor);
                    }
                }
            }

            if (Pushes.Girls != null)
            {
                for (int i = 0; i < Pushes.Girls.Count; i++)
                {
                    if (Pushes.Girls[i] == girl)
                    {
                        snapshot.PushSlots.Add(i);
                    }
                }
            }

            return snapshot;
        }

        internal void NotifyCompletedRemovals()
        {
            if (Girl == null)
            {
                return;
            }

            if (!Temporary)
            {
                foreach (singles._single single in Singles)
                {
                    if (!AvailabilityRules.SingleContains(single, Girl))
                    {
                        LocalizedNotifications.NotifySingleRemoved(Girl, single);
                    }
                }

                foreach (SEvent_Concerts._concert concert in Concerts)
                {
                    if (!AvailabilityRules.ConcertContains(concert, Girl))
                    {
                        LocalizedNotifications.NotifyConcertRemoved(Girl, concert);
                    }
                }
            }

            foreach (Shows._show show in ShowProjects)
            {
                bool running = show.episodeCount > 0;
                if ((!Temporary || running) && !AvailabilityRules.ShowContains(show, Girl))
                {
                    LocalizedNotifications.NotifyShowRemoved(Girl, show, running);
                }
            }

            if (!Temporary)
            {
                foreach (Girls_Mentors._mentor mentor in Mentorships)
                {
                    if (Girls_Mentors.Mentors == null || !Girls_Mentors.Mentors.Contains(mentor))
                    {
                        data_girls.girls other = mentor.Senpai == Girl ? mentor.Kohai : mentor.Senpai;
                        LocalizedNotifications.NotifyMentorshipRemoved(Girl, other);
                    }
                }

                foreach (int slot in PushSlots)
                {
                    if (Pushes.Girls == null || slot < 0 || slot >= Pushes.Girls.Count || Pushes.Girls[slot] != Girl)
                    {
                        LocalizedNotifications.NotifyPushRemoved(Girl);
                    }
                }
            }
        }
    }

    internal static class RemovalScope
    {
        [ThreadStatic]
        private static RemovalSnapshot current;

        internal static RemovalSnapshot Current
        {
            get { return current; }
        }

        internal static void Enter(RemovalSnapshot snapshot)
        {
            snapshot.Previous = current;
            current = snapshot;
        }

        internal static void Exit(RemovalSnapshot snapshot)
        {
            if (current == snapshot)
            {
                current = snapshot.Previous;
            }
        }

        internal static bool AppliesTo(data_girls.girls girl)
        {
            return current != null && current.Girl == girl;
        }
    }

    internal static class MedicalTransitionScope
    {
        [ThreadStatic]
        internal static data_girls.girls Girl;
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.IsActive))]
    internal static class AnnouncedGraduationIsActivePatch
    {
        private static void Postfix(data_girls.girls __instance, ref bool __result)
        {
            if (__instance == null)
            {
                return;
            }

            if (__instance.status == data_girls._status.depressed)
            {
                __result = false;
            }
            else if (__instance.status == data_girls._status.announced_graduation)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.RemoveFromEverything))]
    [HarmonyPriority(Priority.First)]
    internal static class RemoveFromEverythingLifecyclePatch
    {
        private static void Prefix(data_girls.girls __instance, bool temporary, out RemovalSnapshot __state)
        {
            __state = RemovalSnapshot.Capture(__instance, temporary);
            RemovalScope.Enter(__state);
        }

        private static void Postfix(RemovalSnapshot __state)
        {
            if (__state == null)
            {
                return;
            }

            __state.NotifyCompletedRemovals();
            RemovalScope.Exit(__state);
        }

        private static Exception Finalizer(Exception __exception, RemovalSnapshot __state)
        {
            if (__state != null)
            {
                RemovalScope.Exit(__state);
            }

            return __exception;
        }
    }

    [HarmonyPatch(typeof(singles._single), nameof(singles._single.RemoveGirl))]
    internal static class SingleRemovalPolicyPatch
    {
        private static bool Prefix(data_girls.girls _girl)
        {
            RemovalSnapshot scope = RemovalScope.Current;
            return scope == null || !RemovalScope.AppliesTo(_girl) || !scope.Temporary;
        }
    }

    [HarmonyPatch(typeof(SEvent_Concerts._concert), nameof(SEvent_Concerts._concert.RemoveGirl))]
    [HarmonyAfter(new[] { "com.cosmo.imdatacore" })]
    internal static class ConcertRemovalPolicyPatch
    {
        private static bool Prefix(SEvent_Concerts._concert __instance, data_girls.girls Girl)
        {
            RemovalSnapshot scope = RemovalScope.Current;
            if (scope == null || !RemovalScope.AppliesTo(Girl))
            {
                return true;
            }

            if (scope.Temporary)
            {
                return false;
            }

            if (__instance == null || __instance.SetListItems == null)
            {
                return false;
            }

            bool changed = false;
            foreach (SEvent_Concerts._concert.ISetlistItem item in __instance.SetListItems)
            {
                if (item == null)
                {
                    continue;
                }

                if (item is SEvent_Concerts._concert._song)
                {
                    SEvent_Concerts._concert._song song = (SEvent_Concerts._concert._song)item;
                    if (song.Center == Girl)
                    {
                        song.Center = null;
                        changed = true;
                    }
                }
                else if (item is SEvent_Concerts._concert._mc)
                {
                    SEvent_Concerts._concert._mc mc = (SEvent_Concerts._concert._mc)item;
                    for (int i = 0; i < mc.Girls.Count; i++)
                    {
                        if (mc.Girls[i] == Girl)
                        {
                            mc.Girls[i] = null;
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
            {
                __instance.Cast_Changed = true;
                if (__instance.Update != null)
                {
                    __instance.Update();
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Shows._show), nameof(Shows._show.RemoveGirl))]
    [HarmonyAfter(new[] { "com.cosmo.imdatacore" })]
    internal static class ShowRemovalPolicyPatch
    {
        private static bool Prefix(Shows._show __instance, data_girls.girls _Girl)
        {
            RemovalSnapshot scope = RemovalScope.Current;
            if (scope == null || !RemovalScope.AppliesTo(_Girl))
            {
                return true;
            }

            if (scope.Temporary && (__instance == null || __instance.episodeCount == 0))
            {
                return false;
            }

            if (__instance == null || __instance.castType != Shows._show._castType.permanentCast || __instance.girls == null)
            {
                return false;
            }

            bool changed = false;
            for (int i = 0; i < __instance.girls.Length; i++)
            {
                if (__instance.girls[i] == _Girl)
                {
                    __instance.girls[i] = null;
                    changed = true;
                }
            }

            if (changed)
            {
                __instance.Cast_Changed = true;
                if (__instance.Update != null)
                {
                    __instance.Update();
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Girls_Mentors), nameof(Girls_Mentors.Remove))]
    internal static class MentorshipRemovalPolicyPatch
    {
        private static bool Prefix(data_girls.girls Girl)
        {
            RemovalSnapshot scope = RemovalScope.Current;
            return scope == null || !RemovalScope.AppliesTo(Girl) || !scope.Temporary;
        }
    }

    [HarmonyPatch(typeof(Pushes), nameof(Pushes.RemovePush))]
    internal static class PushRemovalPolicyPatch
    {
        private static bool Prefix(data_girls.girls Girl)
        {
            RemovalSnapshot scope = RemovalScope.Current;
            return scope == null || !RemovalScope.AppliesTo(Girl) || !scope.Temporary;
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Set_Injured))]
    internal static class InjuryLifecyclePatch
    {
        private static bool Prefix(data_girls.girls __instance, out data_girls._status __state)
        {
            __state = __instance.status;
            if (__instance.status == data_girls._status.announced_graduation)
            {
                return false;
            }

            MedicalTransitionScope.Girl = __instance;
            return true;
        }

        private static void Postfix(data_girls.girls __instance, data_girls._status __state)
        {
            if (__state != data_girls._status.injured && __instance.status == data_girls._status.injured)
            {
                LocalizedNotifications.NotifyProjectsBlockedBy(__instance);
            }

            MedicalTransitionScope.Girl = null;
        }

        private static Exception Finalizer(Exception __exception)
        {
            MedicalTransitionScope.Girl = null;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Set_Depressed))]
    internal static class DepressionLifecyclePatch
    {
        private static bool Prefix(data_girls.girls __instance, out data_girls._status __state)
        {
            __state = __instance.status;
            if (__instance.status == data_girls._status.announced_graduation)
            {
                return false;
            }

            MedicalTransitionScope.Girl = __instance;
            return true;
        }

        private static void Postfix(data_girls.girls __instance, data_girls._status __state)
        {
            if (__instance.status == data_girls._status.depressed)
            {
                __instance.BreakContracts();
                __instance.RemoveFromEverything(true);
                if (__state != data_girls._status.depressed)
                {
                    LocalizedNotifications.NotifyProjectsBlockedBy(__instance);
                }
            }

            MedicalTransitionScope.Girl = null;
        }

        private static Exception Finalizer(Exception __exception)
        {
            MedicalTransitionScope.Girl = null;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.SendOnHiatus))]
    internal static class HiatusLifecyclePatch
    {
        private static bool Prefix(data_girls.girls __instance, out data_girls._status __state)
        {
            __state = __instance.status;
            return __instance.status != data_girls._status.announced_graduation;
        }

        private static void Postfix(data_girls.girls __instance, data_girls._status __state)
        {
            if (__state != data_girls._status.hiatus && __instance.status == data_girls._status.hiatus)
            {
                LocalizedNotifications.NotifyProjectsBlockedBy(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.CancelJob))]
    [HarmonyPriority(Priority.First)]
    internal static class PreserveClinicAssignmentPatch
    {
        private static bool Prefix(agency._room __instance)
        {
            data_girls.girls transitioningGirl = MedicalTransitionScope.Girl;
            return transitioningGirl == null
                || __instance == null
                || __instance.type != agency._type.doctorsOffice
                || __instance.girl != transitioningGirl;
        }
    }

    [HarmonyPatch(typeof(Girls_Mentors._mentor), nameof(Girls_Mentors._mentor.GetBonus))]
    internal static class PauseMentorshipBonusPatch
    {
        private static void Postfix(Girls_Mentors._mentor __instance, ref float __result)
        {
            if (__instance != null && (AvailabilityRules.IsTemporarilyUnavailable(__instance.Senpai) || AvailabilityRules.IsTemporarilyUnavailable(__instance.Kohai)))
            {
                __result = 1f;
            }
        }
    }

    internal sealed class PushPauseState
    {
        internal sealed class SlotState
        {
            internal int Index;
            internal data_girls.girls Girl;
            internal data_girls.girls LastDayGirl;
            internal int Days;
        }

        internal readonly List<SlotState> Slots = new List<SlotState>();
    }

    [HarmonyPatch(typeof(Pushes), "OnNewDay")]
    internal static class PauseUnavailablePushesPatch
    {
        private static void Prefix(out PushPauseState __state)
        {
            __state = new PushPauseState();
            if (Pushes.Girls == null || Pushes.Days == null)
            {
                return;
            }

            for (int i = 0; i < Pushes.Girls.Count; i++)
            {
                if (!AvailabilityRules.IsTemporarilyUnavailable(Pushes.Girls[i]))
                {
                    continue;
                }

                PushPauseState.SlotState slot = new PushPauseState.SlotState
                {
                    Index = i,
                    Girl = Pushes.Girls[i],
                    LastDayGirl = GetLastDayGirl(i),
                    Days = i < Pushes.Days.Count ? Pushes.Days[i] : 0
                };
                __state.Slots.Add(slot);
                Pushes.Girls[i] = null;
            }
        }

        private static void Postfix(PushPauseState __state)
        {
            Restore(__state);
        }

        private static Exception Finalizer(Exception __exception, PushPauseState __state)
        {
            Restore(__state);
            return __exception;
        }

        private static data_girls.girls GetLastDayGirl(int index)
        {
            List<data_girls.girls> lastDay = Traverse.Create(typeof(Pushes)).Field("GirlsLastDay").GetValue<List<data_girls.girls>>();
            return lastDay != null && index >= 0 && index < lastDay.Count ? lastDay[index] : null;
        }

        private static void Restore(PushPauseState state)
        {
            if (state == null || state.Slots.Count == 0 || Pushes.Girls == null || Pushes.Days == null)
            {
                return;
            }

            List<data_girls.girls> lastDay = Traverse.Create(typeof(Pushes)).Field("GirlsLastDay").GetValue<List<data_girls.girls>>();
            foreach (PushPauseState.SlotState slot in state.Slots)
            {
                if (slot.Index >= 0 && slot.Index < Pushes.Girls.Count)
                {
                    Pushes.Girls[slot.Index] = slot.Girl;
                }
                if (slot.Index >= 0 && slot.Index < Pushes.Days.Count)
                {
                    Pushes.Days[slot.Index] = slot.Days;
                }
                if (lastDay != null && slot.Index >= 0 && slot.Index < lastDay.Count)
                {
                    lastDay[slot.Index] = slot.LastDayGirl;
                }
            }
        }
    }
}
