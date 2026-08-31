using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using SaveNLoadFixes.Persistence;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// N03: preserves the bounded semantic meaning of the 13 supplied vanilla
    /// Substories_Manager BeforeStart callbacks. The delegate/closure object itself
    /// is never serialized. Capture classifies one of six audited setup kinds and
    /// stores only stable idol IDs plus the one source-specific string/flag payload.
    /// Load reattaches a fresh Action to the exact reconstructed queue row.
    /// </summary>
    internal static class QueuedBeforeStartRepair
    {
        internal const int SectionVersion = 1;

        internal enum SetupKind
        {
            None = 0,
            DatingSetGirlSprite = 1,
            AgnosticCenter = 2,
            GenericDate = 3,
            FujimotoWarning = 4,
            DateFriend = 5,
            DateEx = 6
        }

        private static long captureFailureCount;
        private static long classifiedCallbackCount;
        private static long restoredDescriptorCount;
        private static long legacyMissingCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static long runtimeResolutionFailureCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented { get { return QueuedBeforeStartPatchHealth.IsHealthy; } }
        internal static long CaptureFailureCount { get { return Interlocked.Read(ref captureFailureCount); } }
        internal static long ClassifiedCallbackCount { get { return Interlocked.Read(ref classifiedCallbackCount); } }
        internal static long RestoredDescriptorCount { get { return Interlocked.Read(ref restoredDescriptorCount); } }
        internal static long LegacyMissingCount { get { return Interlocked.Read(ref legacyMissingCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static long RuntimeResolutionFailureCount { get { return Interlocked.Read(ref runtimeResolutionFailureCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out List<QueuedBeforeStartRecordV1> records,
            out string error)
        {
            records = null;
            error = string.Empty;

            if (dataToSave == null || dataToSave.Substories_Manager__dialogueQueue == null ||
                Substories_Manager.dialogueQueue == null)
            {
                return CaptureFailed("N03 target/live dialogue queue is unavailable.", out error);
            }

            List<Substories_Manager.QueueData> savedQueue = dataToSave.Substories_Manager__dialogueQueue;
            List<Substories_Manager._dialogueQueue> liveQueue = Substories_Manager.dialogueQueue;
            if (savedQueue.Count != liveQueue.Count)
            {
                return CaptureFailed("N03 serialized/live dialogue queue cardinality diverges at checkpoint capture.", out error);
            }

            List<QueuedBeforeStartRecordV1> result = new List<QueuedBeforeStartRecordV1>();
            for (int index = 0; index < liveQueue.Count; index++)
            {
                Substories_Manager.QueueData saved = savedQueue[index];
                Substories_Manager._dialogueQueue live = liveQueue[index];
                if (!QueueWitnessMatches(saved, live))
                {
                    return CaptureFailed("N03 serialized/live dialogue queue witness mismatch at ordinal " + index + ".", out error);
                }

                if (live.BeforeStart == null)
                {
                    continue;
                }

                QueuedBeforeStartRecordV1 record;
                if (!TryClassifyVanillaCallback(index, saved, live.BeforeStart, out record, out error))
                {
                    Interlocked.Increment(ref captureFailureCount);
                    lastDiagnostic = error;
                    return false;
                }

                result.Add(record);
                Interlocked.Increment(ref classifiedCallbackCount);
            }

            records = result;
            return true;
        }

        internal static void RestoreAfterSubstoriesLoad(Substories_Manager manager)
        {
            SaveManager.SavedData target = GetTargetSavedData(manager);
            if (target == null)
            {
                RecordInvalid("N03 cannot resolve target SavedData after Substories_Manager.LoadFunction().");
                return;
            }

            RepairEnvelopeLoadState state;
            if (!RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                lastDiagnostic = "N03 failed closed because target SavedData has no repair-envelope association.";
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
                return;
            }

            if (!state.Present || state.Envelope == null || state.Envelope.records == null ||
                state.Envelope.records.queued_before_start_version == 0)
            {
                Interlocked.Increment(ref legacyMissingCount);
                lastDiagnostic = "N03 legacy/pre-section queue rows keep vanilla null BeforeStart callbacks; no semantic callback is fabricated.";
                return;
            }

            if (!state.Valid || state.Envelope.records.queued_before_start_version != SectionVersion ||
                state.Envelope.records.queued_before_start == null)
            {
                RecordInvalid("N03 repair section is invalid, unsupported, or missing its descriptor list.");
                return;
            }

            List<Substories_Manager.QueueData> savedQueue = target.Substories_Manager__dialogueQueue;
            List<Substories_Manager._dialogueQueue> liveQueue = Substories_Manager.dialogueQueue;
            if (savedQueue == null || liveQueue == null || savedQueue.Count != liveQueue.Count)
            {
                RecordInvalid("N03 target/reconstructed dialogue queue cardinality diverges.");
                return;
            }

            Action[] staged = new Action[liveQueue.Count];
            HashSet<int> seenOrdinals = new HashSet<int>();
            List<QueuedBeforeStartRecordV1> descriptors = state.Envelope.records.queued_before_start;
            for (int index = 0; index < descriptors.Count; index++)
            {
                QueuedBeforeStartRecordV1 record = descriptors[index];
                if (record == null || record.queue_ordinal < 0 || record.queue_ordinal >= liveQueue.Count ||
                    !seenOrdinals.Add(record.queue_ordinal))
                {
                    RecordInvalid("N03 descriptor list contains a null, duplicate, or out-of-range queue ordinal.");
                    return;
                }

                Substories_Manager.QueueData saved = savedQueue[record.queue_ordinal];
                Substories_Manager._dialogueQueue live = liveQueue[record.queue_ordinal];
                if (!RecordMatchesSavedQueue(record, saved) || !QueueWitnessMatches(saved, live) ||
                    live.BeforeStart != null)
                {
                    RecordInvalid("N03 descriptor queue witness does not match the exact reconstructed vanilla row.");
                    return;
                }

                Action rebuilt;
                if (!TryBuildRuntimeAction(record, out rebuilt))
                {
                    RecordInvalid("N03 descriptor contains an unsupported setup kind or malformed stable arguments.");
                    return;
                }
                staged[record.queue_ordinal] = rebuilt;
            }

            for (int index = 0; index < staged.Length; index++)
            {
                if (staged[index] == null)
                {
                    continue;
                }
                liveQueue[index].BeforeStart = staged[index];
                Interlocked.Increment(ref restoredDescriptorCount);
            }

            lastDiagnostic = "N03 reattached " + descriptors.Count + " semantic BeforeStart descriptor(s) to the reconstructed queue.";
        }

        private static SaveManager.SavedData GetTargetSavedData(Substories_Manager manager)
        {
            if (manager == null)
            {
                return null;
            }
            mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
            return main == null ? null : main.GetSavedData();
        }

        private static bool QueueWitnessMatches(Substories_Manager.QueueData saved, Substories_Manager._dialogueQueue live)
        {
            if (saved == null || live == null || live.dialogue == null)
            {
                return false;
            }
            return string.Equals(saved.dialogue, live.dialogue.id, StringComparison.Ordinal) &&
                string.Equals(saved.launchTime, ExtensionMethods.ToDataString(live.launchTime), StringComparison.Ordinal) &&
                saved.delay == live.delay && saved.debug == live.debug;
        }

        private static bool RecordMatchesSavedQueue(QueuedBeforeStartRecordV1 record, Substories_Manager.QueueData saved)
        {
            return record != null && saved != null &&
                string.Equals(record.dialogue_id, saved.dialogue, StringComparison.Ordinal) &&
                string.Equals(record.launch_time, saved.launchTime, StringComparison.Ordinal) &&
                record.delay == saved.delay && record.debug == saved.debug;
        }

        private static bool TryClassifyVanillaCallback(
            int queueOrdinal,
            Substories_Manager.QueueData saved,
            Action callback,
            out QueuedBeforeStartRecordV1 record,
            out string error)
        {
            record = null;
            error = string.Empty;
            if (callback == null || callback.Method == null || callback.Method.DeclaringType == null)
            {
                error = "N03 encountered an unreadable non-null BeforeStart callback.";
                return false;
            }

            string nested = callback.Method.DeclaringType.Name;
            string method = callback.Method.Name;
            Type owner = callback.Method.DeclaringType.DeclaringType;
            QueuedBeforeStartRecordV1 descriptor = BaseRecord(queueOrdinal, saved);

            if (owner == typeof(Event_Overlord) && nested == "<>c__DisplayClass19_0" && method == "<OnSingleWorkComplete>b__0")
            {
                data_girls.girls center;
                if (!TryReadField(callback.Target, "Center", out center) || !TrySetGirlId(center, descriptor, true))
                {
                    error = "N03 could not extract the agnostic center idol ID.";
                    return false;
                }
                descriptor.setup_kind = (int)SetupKind.AgnosticCenter;
                record = descriptor;
                return true;
            }

            if (owner == typeof(Dating) && nested == "<>c__DisplayClass27_0" && method == "<GoOnSpecificDate>b__0")
            {
                data_girls.girls girl;
                if (!TryReadField(callback.Target, "Girl", out girl) || !TrySetGirlId(girl, descriptor, true))
                {
                    error = "N03 could not extract the public-date idol ID.";
                    return false;
                }
                descriptor.setup_kind = (int)SetupKind.DatingSetGirlSprite;
                descriptor.set_mask = true;
                record = descriptor;
                return true;
            }

            if (owner == typeof(Dating) && nested == "<>c__DisplayClass28_0" && IsGoOnDateSpriteMethod(method))
            {
                data_girls.girls girl;
                if (!TryReadField(callback.Target, "Girl", out girl) || !TrySetGirlId(girl, descriptor, true))
                {
                    error = "N03 could not extract the queued GoOnDate idol ID.";
                    return false;
                }
                descriptor.setup_kind = (int)SetupKind.DatingSetGirlSprite;
                descriptor.set_mask = method == "<GoOnDate>b__7" || method == "<GoOnDate>b__8" || method == "<GoOnDate>b__9";
                record = descriptor;
                return true;
            }

            if (owner == typeof(Dating) && nested == "<>c__DisplayClass38_0" && method == "<GenerateGenericDate>b__0")
            {
                data_girls.girls girl;
                string questionId;
                if (!TryReadField(callback.Target, "Girl", out girl) || !TrySetGirlId(girl, descriptor, true) ||
                    !TryReadField(callback.Target, "QuestionID", out questionId) || questionId == null)
                {
                    error = "N03 could not extract generic-date idol/question arguments.";
                    return false;
                }
                descriptor.setup_kind = (int)SetupKind.GenericDate;
                descriptor.semantic_arg = questionId;
                record = descriptor;
                return true;
            }

            if (owner == typeof(Dating) && nested == "<>c" && method == "<OnNewDay>b__75_0")
            {
                if (Dating.Data == null || Dating.Data.Caught_GirlID < 0)
                {
                    error = "N03 could not extract date_fujimoto_warning Caught_GirlID.";
                    return false;
                }
                descriptor.setup_kind = (int)SetupKind.FujimotoWarning;
                descriptor.girl_id_a = Dating.Data.Caught_GirlID;
                record = descriptor;
                return true;
            }

            if (owner == typeof(Dating) && nested == "<>c__DisplayClass75_0" && method == "<OnNewDay>b__1")
            {
                data_girls.girls casual;
                if (!TryReadField(callback.Target, "Casual_Girl", out casual) || !TrySetGirlId(casual, descriptor, true))
                {
                    error = "N03 could not extract date_break_off_casual idol ID.";
                    return false;
                }
                descriptor.setup_kind = (int)SetupKind.DatingSetGirlSprite;
                descriptor.set_mask = false;
                record = descriptor;
                return true;
            }

            if (owner == typeof(Dating) && nested == "<>c__DisplayClass75_2" && method == "<OnNewDay>b__2")
            {
                data_girls.girls bestFriend;
                object locals;
                Dating._partner partner;
                if (!TryReadField(callback.Target, "BestFriend", out bestFriend) ||
                    !TryReadObjectField(callback.Target, "CS$<>8__locals1", out locals) ||
                    !TryReadField(locals, "Partner", out partner) || partner == null || partner.Girl == null ||
                    bestFriend == null || partner.Girl.id < 0 || bestFriend.id < 0)
                {
                    error = "N03 could not extract date_friend partner/best-friend IDs.";
                    return false;
                }
                descriptor.setup_kind = (int)SetupKind.DateFriend;
                descriptor.girl_id_a = partner.Girl.id;
                descriptor.girl_id_b = bestFriend.id;
                record = descriptor;
                return true;
            }

            if (owner == typeof(Dating) && nested == "<>c__DisplayClass75_3" && method == "<OnNewDay>b__4")
            {
                data_girls.girls girlfriend;
                data_girls.girls ex;
                if (!TryReadField(callback.Target, "Gf", out girlfriend) || !TryReadField(callback.Target, "Ex", out ex) ||
                    girlfriend == null || ex == null || girlfriend.id < 0 || ex.id < 0)
                {
                    error = "N03 could not extract date_ex current/ex idol IDs.";
                    return false;
                }
                descriptor.setup_kind = (int)SetupKind.DateEx;
                descriptor.girl_id_a = girlfriend.id;
                descriptor.girl_id_b = ex.id;
                record = descriptor;
                return true;
            }

            error = "N03 refuses to serialize unsupported opaque BeforeStart delegate " +
                callback.Method.DeclaringType.FullName + "." + method + ".";
            return false;
        }

        private static QueuedBeforeStartRecordV1 BaseRecord(int queueOrdinal, Substories_Manager.QueueData saved)
        {
            return new QueuedBeforeStartRecordV1
            {
                queue_ordinal = queueOrdinal,
                dialogue_id = saved.dialogue ?? string.Empty,
                launch_time = saved.launchTime ?? string.Empty,
                delay = saved.delay,
                debug = saved.debug,
                setup_kind = (int)SetupKind.None,
                girl_id_a = -1,
                girl_id_b = -1,
                semantic_arg = string.Empty
            };
        }

        private static bool IsGoOnDateSpriteMethod(string method)
        {
            return method == "<GoOnDate>b__4" || method == "<GoOnDate>b__6" ||
                method == "<GoOnDate>b__7" || method == "<GoOnDate>b__8" ||
                method == "<GoOnDate>b__9" || method == "<GoOnDate>b__10";
        }

        private static bool TrySetGirlId(data_girls.girls girl, QueuedBeforeStartRecordV1 record, bool first)
        {
            if (girl == null || girl.id < 0)
            {
                return false;
            }
            if (first)
            {
                record.girl_id_a = girl.id;
            }
            else
            {
                record.girl_id_b = girl.id;
            }
            return true;
        }

        private static bool TryReadField<T>(object target, string name, out T value)
        {
            value = default(T);
            if (target == null)
            {
                return false;
            }
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                return false;
            }
            object raw = field.GetValue(target);
            if (raw == null)
            {
                return !typeof(T).IsValueType;
            }
            if (!(raw is T))
            {
                return false;
            }
            value = (T)raw;
            return true;
        }

        private static bool TryReadObjectField(object target, string name, out object value)
        {
            value = null;
            if (target == null)
            {
                return false;
            }
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                return false;
            }
            value = field.GetValue(target);
            return value != null;
        }

        private static bool TryBuildRuntimeAction(QueuedBeforeStartRecordV1 record, out Action action)
        {
            action = null;
            if (record == null || record.setup_kind <= (int)SetupKind.None || record.setup_kind > (int)SetupKind.DateEx)
            {
                return false;
            }

            SetupKind kind = (SetupKind)record.setup_kind;
            int first = record.girl_id_a;
            int second = record.girl_id_b;
            string semanticArg = record.semantic_arg ?? string.Empty;
            bool setMask = record.set_mask;

            if ((kind == SetupKind.DatingSetGirlSprite || kind == SetupKind.AgnosticCenter ||
                 kind == SetupKind.GenericDate || kind == SetupKind.FujimotoWarning) && first < 0)
            {
                return false;
            }
            if ((kind == SetupKind.DateFriend || kind == SetupKind.DateEx) && (first < 0 || second < 0 || first == second))
            {
                return false;
            }

            switch (kind)
            {
                case SetupKind.DatingSetGirlSprite:
                    action = delegate { ExecuteDatingSprite(first, setMask); };
                    return true;
                case SetupKind.AgnosticCenter:
                    action = delegate { ExecuteAgnosticCenter(first); };
                    return true;
                case SetupKind.GenericDate:
                    action = delegate { ExecuteGenericDate(first, semanticArg); };
                    return true;
                case SetupKind.FujimotoWarning:
                    action = delegate { ExecuteFujimotoWarning(first); };
                    return true;
                case SetupKind.DateFriend:
                    action = delegate { ExecuteDateFriend(first, second); };
                    return true;
                case SetupKind.DateEx:
                    action = delegate { ExecuteDateEx(first, second); };
                    return true;
                default:
                    return false;
            }
        }

        private static void ExecuteDatingSprite(int girlId, bool setMask)
        {
            data_girls.girls girl = ResolveGirl(girlId, "dating sprite");
            if (girl == null)
            {
                return;
            }
            ActiveDialogueController.DoBeforeDialogue(
                delegate
                {
                    vn_actorObject actorObject = Dating.GetADC().getActorObject("g");
                    if (actorObject == null || actorObject.actor == null)
                    {
                        RuntimeFailure("N03 dating sprite could not resolve actor tag g.");
                        return;
                    }
                    actorObject.actor.girl = ResolveGirl(girlId, "dating sprite deferred");
                    if (actorObject.actor.girl == null)
                    {
                        return;
                    }
                    actorObject.SetSpriteGirl();
                    if (setMask && Dating.Wear_Masks)
                    {
                        actorObject.SetMask();
                    }
                    Dating.GetADC().RenderNameBox();
                });
        }

        private static void ExecuteAgnosticCenter(int girlId)
        {
            data_girls.girls girl = ResolveGirl(girlId, "agnostic center");
            Substories_Manager._substoryData data = Substories_Manager.GetSubstoryData("agnostic");
            if (girl == null || data == null || data.GetActor("girl1") == null)
            {
                RuntimeFailure("N03 agnostic callback could not resolve loaded substory actor girl1.");
                return;
            }
            data.GetActor("girl1").girl = girl;
            ActiveDialogueController.DoBeforeDialogue(
                delegate
                {
                    data_girls.girls loaded = ResolveGirl(girlId, "agnostic deferred");
                    vn_actorObject actorObject = Dating.GetADC().getActorObject("girl1");
                    if (loaded == null || actorObject == null || actorObject.actor == null)
                    {
                        RuntimeFailure("N03 agnostic callback could not resolve loaded VN actor girl1.");
                        return;
                    }
                    actorObject.actor.girl = loaded;
                    actorObject.SetSpriteGirl();
                });
        }

        private static void ExecuteGenericDate(int girlId, string questionId)
        {
            if (ResolveGirl(girlId, "generic date") == null)
            {
                return;
            }
            ActiveDialogueController.DoBeforeDialogue(
                delegate
                {
                    data_girls.girls girl = ResolveGirl(girlId, "generic date deferred");
                    ActiveDialogueController adc = Dating.GetADC();
                    vn_actorObject actorObject = adc == null ? null : adc.getActorObject("g");
                    if (girl == null || actorObject == null || actorObject.actor == null)
                    {
                        RuntimeFailure("N03 generic-date callback could not resolve actor tag g.");
                        return;
                    }
                    actorObject.actor.girl = girl;
                    actorObject.SetSpriteGirl();
                    if (Dating.Wear_Masks)
                    {
                        actorObject.SetMask();
                    }
                    if (questionId == "date_generic_question_y")
                    {
                        vn_actorObject actorObject2 = adc.getActorObject("girl2");
                        if (actorObject2 == null || actorObject2.actor == null)
                        {
                            RuntimeFailure("N03 generic-date callback could not resolve actor tag girl2.");
                            return;
                        }
                        if (actorObject2.actor.girl == girl)
                        {
                            actorObject2.actor.girl = data_girls.GetRandomActiveGirl(girl, null);
                        }
                        if (actorObject2.actor.girl == girl)
                        {
                            actorObject2.actor.girl = null;
                            actorObject2.actor.type = data_dialogues._dialogue._actors._type.staff;
                            if (staff.Staff == null || staff.Staff.Count <= 1)
                            {
                                RuntimeFailure("N03 generic-date staff fallback is unavailable.");
                                return;
                            }
                            actorObject2.actor.staff = staff.Staff[1];
                            // Preserve the supplied source's exact target object for SetSpriteStaff().
                            actorObject.SetSpriteStaff();
                        }
                    }
                    adc.RenderNameBox();
                });
        }

        private static void ExecuteFujimotoWarning(int girlId)
        {
            data_girls.girls girl = ResolveGirl(girlId, "Fujimoto warning");
            Substories_Manager._substoryData data = Substories_Manager.GetSubstoryData("date_fujimoto_warning");
            if (girl == null || data == null || data.GetActor("girl1") == null)
            {
                RuntimeFailure("N03 Fujimoto-warning callback could not resolve loaded substory actor girl1.");
                return;
            }
            data.GetActor("girl1").girl = girl;
        }

        private static void ExecuteDateFriend(int partnerGirlId, int bestFriendId)
        {
            data_girls.girls partner = ResolveGirl(partnerGirlId, "date_friend partner");
            data_girls.girls bestFriend = ResolveGirl(bestFriendId, "date_friend best friend");
            if (partner == null || bestFriend == null)
            {
                return;
            }
            ActiveDialogueController.DoBeforeDialogue(
                delegate
                {
                    data_girls.girls loadedPartner = ResolveGirl(partnerGirlId, "date_friend partner deferred");
                    data_girls.girls loadedFriend = ResolveGirl(bestFriendId, "date_friend best friend deferred");
                    Substories_Manager._substoryData data = Substories_Manager.GetSubstoryData("date_friend");
                    ActiveDialogueController adc = Dating.GetADC();
                    vn_actorObject actorObject = adc == null ? null : adc.getActorObject("girl2");
                    if (loadedPartner == null || loadedFriend == null || data == null || data.GetActor("girl1") == null ||
                        actorObject == null || actorObject.actor == null)
                    {
                        RuntimeFailure("N03 date_friend callback could not resolve loaded actor bindings.");
                        return;
                    }
                    data.GetActor("girl1").girl = loadedPartner;
                    actorObject.actor.girl = loadedFriend;
                    actorObject.SetSpriteGirl();
                    adc.RenderText();
                    adc.RenderNameBox();
                });
        }

        private static void ExecuteDateEx(int girlfriendId, int exId)
        {
            data_girls.girls girlfriend = ResolveGirl(girlfriendId, "date_ex current partner");
            data_girls.girls ex = ResolveGirl(exId, "date_ex former partner");
            if (girlfriend == null || ex == null)
            {
                return;
            }
            ActiveDialogueController.DoBeforeDialogue(
                delegate
                {
                    data_girls.girls loadedGirlfriend = ResolveGirl(girlfriendId, "date_ex current partner deferred");
                    data_girls.girls loadedEx = ResolveGirl(exId, "date_ex former partner deferred");
                    Substories_Manager._substoryData data = Substories_Manager.GetSubstoryData("date_ex");
                    ActiveDialogueController adc = Dating.GetADC();
                    vn_actorObject actorObject = adc == null ? null : adc.getActorObject("girl2");
                    if (loadedGirlfriend == null || loadedEx == null || data == null || data.GetActor("girl1") == null ||
                        actorObject == null || actorObject.actor == null)
                    {
                        RuntimeFailure("N03 date_ex callback could not resolve loaded actor bindings.");
                        return;
                    }
                    data.GetActor("girl1").girl = loadedGirlfriend;
                    actorObject.actor.girl = loadedEx;
                    actorObject.SetSpriteGirl();
                    adc.RenderNameBox();
                });
        }

        private static data_girls.girls ResolveGirl(int girlId, string context)
        {
            data_girls.girls girl = data_girls.GetGirlByID(girlId);
            if (girl == null)
            {
                RuntimeFailure("N03 could not resolve loaded idol " + girlId + " for " + context + ".");
            }
            return girl;
        }

        private static void RuntimeFailure(string diagnostic)
        {
            Interlocked.Increment(ref runtimeResolutionFailureCount);
            lastDiagnostic = diagnostic;
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + diagnostic);
        }

        private static bool CaptureFailed(string diagnostic, out string error)
        {
            Interlocked.Increment(ref captureFailureCount);
            lastDiagnostic = diagnostic;
            error = diagnostic;
            return false;
        }

        private static void RecordInvalid(string diagnostic)
        {
            Interlocked.Increment(ref invalidSectionCount);
            lastDiagnostic = diagnostic;
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + diagnostic);
        }
    }

    internal static class QueuedBeforeStartPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 1;
        private static readonly object Sync = new object();
        private static int resolvedTargetMethodCount;
        private static string failure = string.Empty;

        internal static int ResolvedTargetMethodCount { get { lock (Sync) { return resolvedTargetMethodCount; } } }
        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return resolvedTargetMethodCount == ExpectedTargetMethodCount && string.IsNullOrEmpty(failure);
                }
            }
        }
        internal static string Failure { get { lock (Sync) { return failure; } } }

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                resolvedTargetMethodCount++;
                if (resolvedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure = "N03 resolved more patch targets than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync) { failure = diagnostic ?? "unknown N03 patch failure"; }
        }
    }
}
