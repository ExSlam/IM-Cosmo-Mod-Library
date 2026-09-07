using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace ClinicRecoveryPriorityFix
{
    /// <summary>
    /// Vanilla ticks rooms in floor/build order. Each auto-practice room immediately claims an
    /// eligible idle idol, so a studio or salon encountered before a clinic can take a low-stamina
    /// idol before the clinic sorts its candidates. Track one agency tick and run every clinic
    /// before the first competing room without replacing the rest of agency.onTimeTick.
    /// </summary>
    internal static class ClinicPriorityScheduler
    {
        [ThreadStatic]
        private static agency activeAgency;

        [ThreadStatic]
        private static HashSet<agency._room> processedClinics;

        [ThreadStatic]
        private static agency._room forcedClinic;

        internal static void BeginAgencyTick(agency agencyInstance)
        {
            activeAgency = agencyInstance;
            processedClinics = new HashSet<agency._room>();
            forcedClinic = null;
        }

        internal static void EndAgencyTick()
        {
            forcedClinic = null;
            processedClinics = null;
            activeAgency = null;
        }

        internal static agency ActiveAgency
        {
            get { return activeAgency; }
        }

        internal static bool BeforeRoomTick(agency._room room)
        {
            if (activeAgency == null || processedClinics == null || room == null)
            {
                return true;
            }

            if (room.type == agency._type.doctorsOffice)
            {
                if (room == forcedClinic)
                {
                    processedClinics.Add(room);
                    return true;
                }

                // The outer vanilla loop will still encounter a clinic that was ticked early.
                // Skip that second call so progress and completion are calculated only once.
                if (processedClinics.Contains(room))
                {
                    return false;
                }

                processedClinics.Add(room);
                return true;
            }

            ProcessRemainingClinics();
            return true;
        }

        internal static void AfterRoomTick(agency._room room)
        {
            if (activeAgency == null || processedClinics == null || room == null ||
                room.type != agency._type.doctorsOffice || !processedClinics.Contains(room) ||
                room.status != agency._room._status.normal)
            {
                return;
            }

            // A completed recovery leaves the clinic normal until its next vanilla tick. Refill
            // it now so later auto-practice rooms in this same tick cannot take the next patient.
            // AutoActivitiesCheck still respects all vanilla recovery toggles and eligibility.
            room.AutoActivitiesCheck();
        }

        internal static void RefillIdleClinicsAfterAgencyTick(agency agencyInstance)
        {
            if (agencyInstance == null || activeAgency == null || activeAgency != agencyInstance)
            {
                return;
            }

            // Clinics run early so competing auto-practice rooms cannot steal low-stamina idols.
            // That also means a later room/job in the same agency tick can release an idol or change
            // her stamina after every clinic has already had its normal scheduling chance. Perform
            // one final auto-activity pass on clinics that are still idle. Calling
            // AutoActivitiesCheck instead of OnTimeTick avoids a second progress/completion tick and
            // preserves vanilla's autoPractice toggle plus the Step 3 occupation-aware candidate
            // filtering that DoAutoPractice reaches through this method.
            List<agency._room> clinics = agencyInstance.allRooms(agency._type.doctorsOffice);
            for (int i = 0; i < clinics.Count; i++)
            {
                agency._room clinic = clinics[i];
                if (clinic == null || clinic.status != agency._room._status.normal)
                {
                    continue;
                }

                clinic.AutoActivitiesCheck();
            }
        }

        private static void ProcessRemainingClinics()
        {
            List<agency._room> clinics = activeAgency.allRooms(agency._type.doctorsOffice);
            for (int i = 0; i < clinics.Count; i++)
            {
                agency._room clinic = clinics[i];
                if (clinic == null || processedClinics.Contains(clinic))
                {
                    continue;
                }

                forcedClinic = clinic;
                try
                {
                    clinic.OnTimeTick();
                }
                finally
                {
                    forcedClinic = null;
                }
            }
        }
    }

    /// <summary>
    /// Vanilla uses girl.status == normal as a proxy for whether an idol is free for automatic
    /// practice. That proxy is too coarse for clinics: room-backed activities can own an idol
    /// while her status remains normal (dates), and a stale/orphan scene or practice status can
    /// survive even when no room owns her. Preserve the original status gate for every non-clinic
    /// room, but let clinics derive availability from the live agency room graph.
    /// </summary>
    internal static class ClinicRecoveryOccupationGate
    {
        internal static data_girls._status GetStatusForAutoPracticeGate(
            data_girls.girls girl,
            agency._room destination)
        {
            if (girl == null)
            {
                return data_girls._status.practice;
            }

            if (destination == null || destination.type != agency._type.doctorsOffice)
            {
                return girl.status;
            }

            // Only work-state statuses are eligible for ownership-based recovery. Keep vanilla's
            // block for lifecycle/health states such as hiatus, graduation, injury, or depression.
            if (girl.status != data_girls._status.normal &&
                girl.status != data_girls._status.scene &&
                girl.status != data_girls._status.practice)
            {
                return girl.status;
            }

            agency agencyInstance = ClinicPriorityScheduler.ActiveAgency;
            if (agencyInstance == null)
            {
                // Auto-practice normally runs inside agency.onTimeTick, where the scheduler scope
                // supplies the live agency. Outside that scope, fail conservatively to vanilla's
                // status behavior rather than guessing whether a scene/practice marker is orphaned.
                return girl.status;
            }

            return IsActuallyOccupied(agencyInstance, girl)
                ? data_girls._status.practice
                : data_girls._status.normal;
        }

        internal static bool IsActuallyOccupied(agency agencyInstance, data_girls.girls girl)
        {
            if (agencyInstance == null || girl == null)
            {
                return true;
            }

            List<agency._room> rooms = agencyInstance.allRooms(true, true);
            for (int i = 0; i < rooms.Count; i++)
            {
                if (RoomActivelyOwnsOrQueuesGirl(rooms[i], girl))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RoomActivelyOwnsOrQueuesGirl(agency._room room, data_girls.girls girl)
        {
            if (room == null)
            {
                return false;
            }

            // Training, treatment, and dates use room.girl. Dates notably do not set girl.room or
            // change girl.status, so checking only either reverse pointer would miss real work.
            if (room.girl == girl && room.status != agency._room._status.normal)
            {
                return true;
            }

            // PauseTraining deliberately leaves a resumable girl queued after FinishPractice has
            // returned her status to normal. Do not let a clinic steal that queued assignment.
            if (room.girl_paused == girl)
            {
                return true;
            }

            // Cafe assignments are multi-slot and normally leave the room itself in normal status.
            // The cafe system can represent ownership either through girl.room/WorkingGirls or the
            // room's persisted cafe slots, so honor both live representations.
            if (room.type == agency._type.cafeAndShop &&
                (girl.room == room ||
                 room.cafe_girl_1 == girl || room.cafe_girl_2 == girl || room.cafe_girl_3 == girl))
            {
                return true;
            }

            // A scene status is authoritative only when an active scene room actually contains the
            // idol. This is the key distinction that lets orphan scene markers recover in clinics.
            return room.status == agency._room._status.scene &&
                   room.sceneGirls != null &&
                   room.sceneGirls.Contains(girl);
        }
    }

    /// <summary>
    /// A saved idol can retain girl.status == scene even when no reconstructed room actually owns
    /// her. That stale marker survives indefinitely because vanilla clears scene status only through
    /// FinishScene(). Associate each asynchronous agency loader with its exact agency instance and
    /// repair only after that loader reaches terminal completion, when rooms/queues are reconstructed.
    /// </summary>
    internal static class ClinicRecoveryLoadRepair
    {
        private sealed class AgencyLoadLease
        {
            internal agency Agency;
            internal long Generation;
        }

        private static readonly object Sync = new object();
        private static readonly ConditionalWeakTable<object, AgencyLoadLease> AgencyLoads =
            new ConditionalWeakTable<object, AgencyLoadLease>();
        private static long nextLoadGeneration;
        private static long currentLoadGeneration;

        internal static void RegisterAgencyLoadIterator(agency agencyInstance, IEnumerator iterator)
        {
            if (agencyInstance == null || iterator == null)
            {
                return;
            }

            lock (Sync)
            {
                long generation = ++nextLoadGeneration;
                currentLoadGeneration = generation;

                AgencyLoadLease existing;
                if (AgencyLoads.TryGetValue(iterator, out existing))
                {
                    AgencyLoads.Remove(iterator);
                }

                AgencyLoads.Add(iterator, new AgencyLoadLease
                {
                    Agency = agencyInstance,
                    Generation = generation
                });
            }
        }

        internal static void ObserveAgencyLoadMoveNext(object iterator, bool result)
        {
            if (result || iterator == null)
            {
                return;
            }

            AgencyLoadLease lease;
            long currentGeneration;
            lock (Sync)
            {
                if (!AgencyLoads.TryGetValue(iterator, out lease))
                {
                    return;
                }

                AgencyLoads.Remove(iterator);
                currentGeneration = currentLoadGeneration;
            }

            // A second load can start before an older agency coroutine reaches its terminal
            // MoveNext. Never let completion from that stale loader mutate the newer runtime state.
            if (lease.Generation != currentGeneration)
            {
                return;
            }

            NormalizeOrphanSceneStatuses(lease.Agency);
        }

        internal static int NormalizeOrphanSceneStatuses(agency agencyInstance)
        {
            if (agencyInstance == null || data_girls.girl == null)
            {
                return 0;
            }

            int repaired = 0;
            for (int i = 0; i < data_girls.girl.Count; i++)
            {
                data_girls.girls girl = data_girls.girl[i];
                if (girl == null || girl.status != data_girls._status.scene)
                {
                    continue;
                }

                // Be conservative around inconsistent saves: if any reconstructed room/job/queue
                // still owns the idol, leave the status untouched. The clinic occupation gate will
                // continue protecting her until that real ownership is gone.
                if (ClinicRecoveryOccupationGate.IsActuallyOccupied(agencyInstance, girl))
                {
                    continue;
                }

                // Mirror vanilla FinishScene semantics. Because the live room graph proves this
                // scene marker is orphaned, SetStatus(normal) cannot terminate a legitimate scene;
                // it does preserve previous_status and the idol UI update behavior vanilla expects.
                girl.SetStatus(data_girls._status.normal);
                repaired++;
            }

            if (repaired > 0)
            {
                UnityEngine.Debug.Log(
                    "[Clinic Recovery Priority Fix] Normalized " + repaired +
                    " orphan scene idol status marker(s) after agency load reconstruction.");
            }

            return repaired;
        }
    }

    /// <summary>
    /// Register the exact asynchronous agency loader so terminal MoveNext can distinguish current
    /// reconstruction from a stale loader that completed after the player started another load.
    /// </summary>
    [HarmonyPatch]
    internal static class AgencyLoadFactoryClinicRecoveryPatch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = typeof(agency).GetMethod(
                "LoadData",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
            if (method == null || !typeof(IEnumerator).IsAssignableFrom(method.ReturnType))
            {
                throw new MissingMethodException(typeof(agency).FullName, "LoadData");
            }

            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(agency __instance, ref IEnumerator __result)
        {
            ClinicRecoveryLoadRepair.RegisterAgencyLoadIterator(__instance, __result);
        }
    }

    /// <summary>
    /// Normalize orphan scene markers only after vanilla's agency loader has waited for idols and
    /// groups, reconstructed all room state, rendered floors, and rebound saved staff assignments.
    /// </summary>
    [HarmonyPatch]
    internal static class AgencyLoadCompleteClinicRecoveryPatch
    {
        private static MethodBase TargetMethod()
        {
            Type iteratorType = typeof(agency).GetNestedType(
                "<LoadData>d__80",
                BindingFlags.NonPublic);
            if (iteratorType == null)
            {
                throw new MissingMemberException(typeof(agency).FullName, "<LoadData>d__80");
            }

            MethodInfo[] methods = iteratorType.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                bool nameMatches =
                    string.Equals(method.Name, "MoveNext", StringComparison.Ordinal) ||
                    method.Name.EndsWith(".MoveNext", StringComparison.Ordinal);
                if (nameMatches && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
                {
                    return method;
                }
            }

            throw new MissingMethodException(iteratorType.FullName, "MoveNext");
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(object __instance, bool __result)
        {
            ClinicRecoveryLoadRepair.ObserveAgencyLoadMoveNext(__instance, __result);
        }
    }

    /// <summary>
    /// Change only the status value consumed by vanilla DoAutoPractice. Returning normal means the
    /// existing comparison admits the idol; any non-normal value preserves the block. All remaining
    /// vanilla candidate filters and clinic selection logic execute exactly as before.
    /// </summary>
    [HarmonyPatch(typeof(agency._room), "DoAutoPractice", new Type[0])]
    internal static class ClinicRecoveryOccupationGatePatch
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            FieldInfo statusField = AccessTools.Field(typeof(data_girls.girls), nameof(data_girls.girls.status));
            MethodInfo gateMethod = AccessTools.Method(
                typeof(ClinicRecoveryOccupationGate),
                nameof(ClinicRecoveryOccupationGate.GetStatusForAutoPracticeGate));

            if (statusField == null || gateMethod == null)
            {
                UnityEngine.Debug.LogError(
                    "[Clinic Recovery Priority Fix] Could not resolve the DoAutoPractice occupation gate; preserving vanilla status filtering.");
                return codes;
            }

            int statusLoadIndex = -1;
            int statusLoadCount = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].LoadsField(statusField))
                {
                    continue;
                }

                statusLoadIndex = i;
                statusLoadCount++;
            }

            if (statusLoadCount != 1)
            {
                UnityEngine.Debug.LogError(
                    "[Clinic Recovery Priority Fix] Expected exactly one girl.status gate in DoAutoPractice but found " +
                    statusLoadCount + "; preserving vanilla status filtering.");
                return codes;
            }

            CodeInstruction originalStatusLoad = codes[statusLoadIndex];
            CodeInstruction loadDestinationRoom = new CodeInstruction(OpCodes.Ldarg_0);
            loadDestinationRoom.labels.AddRange(originalStatusLoad.labels);
            loadDestinationRoom.blocks.AddRange(originalStatusLoad.blocks);

            // The original stack already contains the candidate girl for ldfld girl.status. Push
            // this room as the second helper argument and return a status-shaped gate value so the
            // original equality/branch IL remains unchanged.
            codes[statusLoadIndex] = loadDestinationRoom;
            codes.Insert(statusLoadIndex + 1, new CodeInstruction(OpCodes.Call, gateMethod));
            return codes;
        }
    }

    [HarmonyPatch(typeof(agency._room), "CanAutoTrain", new Type[] { typeof(data_girls.girls) })]
    internal static class ClinicRecoveryEligibilityPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(agency._room __instance, data_girls.girls _girl, ref bool __result)
        {
            if (__instance == null || __instance.type != agency._type.doctorsOffice)
            {
                return true;
            }

            // Vanilla reuses training-specialization eligibility for clinic recovery. That
            // excludes every idol whose preference is don't train, vocal, dance, or styling,
            // even though medical recovery is not training. The separate occupation-gate patch
            // preserves actual room/scene/queue ownership while allowing orphan scene/practice
            // markers to reach these remaining vanilla clinic checks. Preserve sickness and the
            // temporary manual-cancel ban here.
            __result = _girl != null &&
                       !_girl.IsSick() &&
                       !agency.GirlsBannedFromAutoTasks.Contains(_girl);
            return false;
        }
    }

    [HarmonyPatch(typeof(agency), "onTimeTick", new Type[0])]
    internal static class AgencyTimeTickPriorityScopePatch
    {
        [HarmonyPrefix]
        private static void Prefix(agency __instance)
        {
            ClinicPriorityScheduler.BeginAgencyTick(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(agency __instance)
        {
            ClinicPriorityScheduler.RefillIdleClinicsAfterAgencyTick(__instance);
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception)
        {
            ClinicPriorityScheduler.EndAgencyTick();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.OnTimeTick), new Type[0])]
    internal static class RoomTimeTickClinicPriorityPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(agency._room __instance)
        {
            return ClinicPriorityScheduler.BeforeRoomTick(__instance);
        }

        [HarmonyPostfix]
        private static void Postfix(agency._room __instance)
        {
            ClinicPriorityScheduler.AfterRoomTick(__instance);
        }
    }
}
