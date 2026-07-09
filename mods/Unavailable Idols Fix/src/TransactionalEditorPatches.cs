using System.Collections.Generic;
using HarmonyLib;

namespace UnavailableIdolsFix
{
    internal sealed class ConcertEditSession
    {
        internal SEvent_Concerts._concert Original;
        internal SEvent_Concerts._concert WorkingCopy;
    }

    internal static class ConcertTransactionStore
    {
        private static readonly Dictionary<Concert_New_Popup, ConcertEditSession> Sessions = new Dictionary<Concert_New_Popup, ConcertEditSession>();

        internal static void Begin(Concert_New_Popup popup, SEvent_Concerts._concert original, out SEvent_Concerts._concert workingCopy)
        {
            workingCopy = Clone(original);
            Sessions[popup] = new ConcertEditSession
            {
                Original = original,
                WorkingCopy = workingCopy
            };
        }

        internal static bool TryGet(Concert_New_Popup popup, out ConcertEditSession session)
        {
            return Sessions.TryGetValue(popup, out session);
        }

        internal static void Remove(Concert_New_Popup popup)
        {
            Sessions.Remove(popup);
        }

        internal static void Commit(ConcertEditSession session)
        {
            if (session == null || session.Original == null || session.WorkingCopy == null)
            {
                return;
            }

            SEvent_Concerts._concert source = session.WorkingCopy;
            SEvent_Concerts._concert target = session.Original;
            target.Title = source.Title;
            target.Venue = source.Venue;
            target.SetListItems = CloneSetList(source.SetListItems);
            target.ProjectedValues.TicketPrice = source.ProjectedValues.TicketPrice;
            target.Cast_Changed = false;
            target.RecalcFanAppeal();
            target.RecalcProjectedValues();
        }

        private static SEvent_Concerts._concert Clone(SEvent_Concerts._concert source)
        {
            SEvent_Concerts._concert clone = new SEvent_Concerts._concert
            {
                ID = source.ID,
                Status = source.Status,
                FinishDate = source.FinishDate,
                SetListItems = CloneSetList(source.SetListItems),
                UsedAccidents = source.UsedAccidents,
                SSK = source.SSK,
                Cast_Changed = source.Cast_Changed,
                Title = source.Title,
                Venue = source.Venue,
                Hype = source.Hype,
                Card_Accident_Success_Chance = source.Card_Accident_Success_Chance,
                Card_Accident_Happening = source.Card_Accident_Happening,
                Card_No_Critical_Failure = source.Card_No_Critical_Failure,
                No_Accident_Counter = source.No_Accident_Counter,
                parameters = source.parameters,
                Cards = source.Cards
            };

            clone.ProjectedValues.TicketPrice = source.ProjectedValues.TicketPrice;
            clone.ProjectedValues.Attendance = source.ProjectedValues.Attendance;
            clone.ProjectedValues.Actual_Attendance = source.ProjectedValues.Actual_Attendance;
            clone.ProjectedValues.Actual_Audience = source.ProjectedValues.Actual_Audience;
            clone.ProjectedValues.Actual_Hype = source.ProjectedValues.Actual_Hype;
            clone.ProjectedValues.Actual_Revenue = source.ProjectedValues.Actual_Revenue;
            clone.ProjectedValues.Actual_Cost = source.ProjectedValues.Actual_Cost;
            clone.ProjectedValues.Parent = clone;
            clone.FanAppeal = CloneFanAppeal(source.FanAppeal);
            return clone;
        }

        private static List<SEvent_Concerts._concert.ISetlistItem> CloneSetList(List<SEvent_Concerts._concert.ISetlistItem> source)
        {
            List<SEvent_Concerts._concert.ISetlistItem> clone = new List<SEvent_Concerts._concert.ISetlistItem>();
            if (source == null)
            {
                return clone;
            }

            foreach (SEvent_Concerts._concert.ISetlistItem item in source)
            {
                if (item is SEvent_Concerts._concert._song)
                {
                    SEvent_Concerts._concert._song song = (SEvent_Concerts._concert._song)item;
                    clone.Add(new SEvent_Concerts._concert._song
                    {
                        Single = song.Single,
                        Center = song.Center
                    });
                }
                else if (item is SEvent_Concerts._concert._mc)
                {
                    SEvent_Concerts._concert._mc mc = (SEvent_Concerts._concert._mc)item;
                    SEvent_Concerts._concert._mc mcClone = new SEvent_Concerts._concert._mc
                    {
                        Girls = mc.Girls == null ? new List<data_girls.girls>() : new List<data_girls.girls>(mc.Girls)
                    };
                    mcClone.SetTitle(mc.GetTitle());
                    clone.Add(mcClone);
                }
            }

            return clone;
        }

        internal static List<singles._fanAppeal> CloneFanAppeal(List<singles._fanAppeal> source)
        {
            List<singles._fanAppeal> clone = new List<singles._fanAppeal>();
            if (source == null)
            {
                return clone;
            }

            foreach (singles._fanAppeal appeal in source)
            {
                if (appeal != null)
                {
                    clone.Add(new singles._fanAppeal
                    {
                        type = appeal.type,
                        ratio = appeal.ratio
                    });
                }
            }

            return clone;
        }
    }

    [HarmonyPatch(typeof(Concert_New_Popup), nameof(Concert_New_Popup.Reset))]
    internal static class ConcertEditorBeginTransactionPatch
    {
        private static void Prefix(Concert_New_Popup __instance, ref SEvent_Concerts._concert _Concert)
        {
            ConcertTransactionStore.Remove(__instance);
            if (_Concert != null)
            {
                SEvent_Concerts._concert workingCopy;
                ConcertTransactionStore.Begin(__instance, _Concert, out workingCopy);
                _Concert = workingCopy;
            }
        }
    }

    [HarmonyPatch(typeof(Concert_New_Popup), nameof(Concert_New_Popup.OnContinue))]
    [HarmonyPriority(Priority.First)]
    internal static class ConcertEditorCommitTransactionPatch
    {
        private static void Prefix(Concert_New_Popup __instance)
        {
            ConcertEditSession session;
            if (!ConcertTransactionStore.TryGet(__instance, out session))
            {
                return;
            }

            ConcertTransactionStore.Commit(session);
            __instance.Concert = session.Original;
            ConcertTransactionStore.Remove(__instance);
        }
    }

    [HarmonyPatch(typeof(Concert_New_Popup), nameof(Concert_New_Popup.OnCancel))]
    [HarmonyPriority(Priority.First)]
    internal static class ConcertEditorCancelTransactionPatch
    {
        private static void Prefix(Concert_New_Popup __instance)
        {
            ConcertEditSession session;
            if (!ConcertTransactionStore.TryGet(__instance, out session))
            {
                return;
            }

            Traverse popup = Traverse.Create(__instance);
            bool showingGirls = popup.Field("showing_PanelGirls").GetValue<bool>();
            bool showingSingle = popup.Field("showing_PanelSingle").GetValue<bool>();
            if (showingGirls || showingSingle)
            {
                return;
            }

            __instance.Concert = session.Original;
            ConcertTransactionStore.Remove(__instance);
        }
    }

    [HarmonyPatch(typeof(Show_Popup), nameof(Show_Popup.Reset))]
    internal static class ShowEditorWarningBeginPatch
    {
        private static void Prefix(Shows._show __0, out bool __state)
        {
            __state = __0 != null && __0.Cast_Changed;
        }

        private static void Postfix(Shows._show __0, bool __state)
        {
            // Show_Popup.Reset also names its argument "__Show". Bind by index because
            // Harmony interprets the original name as one of its reserved injections.
            if (__0 != null)
            {
                __0.Cast_Changed = __state;
            }
        }
    }

    [HarmonyPatch(typeof(Show_Popup), nameof(Show_Popup.OnContinue))]
    [HarmonyPriority(Priority.First)]
    internal static class ShowEditorWarningCommitPatch
    {
        private static void Prefix(Show_Popup __instance)
        {
            Shows._show show = Traverse.Create(__instance).Field("_Show").GetValue<Shows._show>();
            if (show != null)
            {
                show.Cast_Changed = false;
            }
        }
    }

    internal sealed class SingleEditorSession
    {
        internal singles._single Single;
        internal bool CastChanged;
        internal List<singles._fanAppeal> FanAppeal;
    }

    internal static class SingleEditorTransactionStore
    {
        private static readonly Dictionary<SinglePopup_Senbatsu, SingleEditorSession> Sessions = new Dictionary<SinglePopup_Senbatsu, SingleEditorSession>();

        internal static void Begin(SinglePopup_Senbatsu popup, singles._single single, bool isNew)
        {
            Sessions.Remove(popup);
            if (isNew || single == null)
            {
                return;
            }

            Sessions[popup] = new SingleEditorSession
            {
                Single = single,
                CastChanged = single.Cast_Changed,
                FanAppeal = ConcertTransactionStore.CloneFanAppeal(single.FanAppeal)
            };
        }

        internal static bool TryTake(SinglePopup_Senbatsu popup, out SingleEditorSession session)
        {
            if (!Sessions.TryGetValue(popup, out session))
            {
                return false;
            }

            Sessions.Remove(popup);
            return true;
        }

        internal static bool TryGet(SinglePopup_Senbatsu popup, out SingleEditorSession session)
        {
            return Sessions.TryGetValue(popup, out session);
        }
    }

    [HarmonyPatch(typeof(SinglePopup_Senbatsu), nameof(SinglePopup_Senbatsu.SetSingle))]
    internal static class SingleEditorBeginTransactionPatch
    {
        private static void Prefix(SinglePopup_Senbatsu __instance, singles._single _single, bool _new)
        {
            SingleEditorTransactionStore.Begin(__instance, _single, _new);
        }

        private static void Postfix(SinglePopup_Senbatsu __instance)
        {
            SingleEditorSession session;
            if (SingleEditorTransactionStore.TryGet(__instance, out session) && session.Single != null)
            {
                session.Single.Cast_Changed = session.CastChanged;
            }
        }
    }

    [HarmonyPatch(typeof(SinglePopup_Senbatsu), nameof(SinglePopup_Senbatsu.OnConfirm))]
    [HarmonyPriority(Priority.First)]
    internal static class SingleEditorCommitTransactionPatch
    {
        private static void Prefix(SinglePopup_Senbatsu __instance)
        {
            SingleEditorSession session;
            if (SingleEditorTransactionStore.TryTake(__instance, out session) && session.Single != null)
            {
                session.Single.Cast_Changed = false;
            }
        }
    }

    [HarmonyPatch(typeof(SinglePopup_Senbatsu), nameof(SinglePopup_Senbatsu.OnCancel))]
    [HarmonyPriority(Priority.First)]
    internal static class SingleEditorCancelTransactionPatch
    {
        private static void Prefix(SinglePopup_Senbatsu __instance)
        {
            SingleEditorSession session;
            if (!SingleEditorTransactionStore.TryTake(__instance, out session) || session.Single == null)
            {
                return;
            }

            session.Single.FanAppeal = ConcertTransactionStore.CloneFanAppeal(session.FanAppeal);
            session.Single.Cast_Changed = session.CastChanged;
            if (session.Single.Update != null)
            {
                session.Single.Update();
            }
        }
    }
}
