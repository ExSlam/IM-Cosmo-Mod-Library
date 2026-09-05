using System;
using System.Collections.Generic;

namespace IMDataCore
{
    /// <summary>
    /// Wave-2 Task-5 task/substory history extensions. Scene occurrence state is
    /// history-only correlation bookkeeping and is never used to restore gameplay.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        private const string SubstorySceneOccurrencePrefix = "ss:";

        private readonly Dictionary<Substories_Manager._dialogueQueue, string>
            substorySceneOccurrenceByQueueEntry =
                new Dictionary<Substories_Manager._dialogueQueue, string>();
        private readonly Dictionary<agency._room, string>
            substorySceneOccurrenceByRoom =
                new Dictionary<agency._room, string>();

        private static string CreateSubstorySceneOccurrenceId()
        {
            return SubstorySceneOccurrencePrefix + Guid.NewGuid().ToString("N");
        }

        private void ResetTaskSubstoryHistoryRuntimeStateLocked()
        {
            substorySceneOccurrenceByQueueEntry.Clear();
            substorySceneOccurrenceByRoom.Clear();
        }

        private static Substories_Manager._dialogueQueue ResolveNewSubstoryQueueEntry(
            data_dialogues._dialogue dialogue,
            SubstoryStartSnapshot snapshotBefore)
        {
            if (dialogue == null || snapshotBefore == null ||
                Substories_Manager.dialogueQueue == null)
            {
                return null;
            }

            Substories_Manager._dialogueQueue created = null;
            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < Substories_Manager.dialogueQueue.Count;
                index++)
            {
                Substories_Manager._dialogueQueue candidate =
                    Substories_Manager.dialogueQueue[index];
                if (candidate == null ||
                    !ReferenceEquals(candidate.dialogue, dialogue) ||
                    ContainsSubstoryQueueReference(
                        snapshotBefore.QueueReferencesBefore,
                        candidate))
                {
                    continue;
                }

                if (created != null)
                {
                    return null;
                }
                created = candidate;
            }

            return created;
        }

        private static bool ContainsSubstoryQueueReference(
            IReadOnlyList<Substories_Manager._dialogueQueue> existing,
            Substories_Manager._dialogueQueue candidate)
        {
            if (existing == null || candidate == null)
            {
                return false;
            }

            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < existing.Count;
                index++)
            {
                if (ReferenceEquals(existing[index], candidate))
                {
                    return true;
                }
            }
            return false;
        }

        private string TrackQueuedSubstorySceneOccurrenceLocked(
            Substories_Manager._dialogueQueue queueEntry,
            data_dialogues._dialogue dialogue)
        {
            if (queueEntry == null || dialogue == null ||
                dialogue.type != data_dialogues._dialogue._type.scene)
            {
                return string.Empty;
            }

            string occurrenceId;
            if (substorySceneOccurrenceByQueueEntry.TryGetValue(
                    queueEntry,
                    out occurrenceId) &&
                !string.IsNullOrEmpty(occurrenceId))
            {
                return occurrenceId;
            }

            occurrenceId = CreateSubstorySceneOccurrenceId();
            substorySceneOccurrenceByQueueEntry[queueEntry] = occurrenceId;
            return occurrenceId;
        }

        internal SubstoryScenePresentationSnapshot CreateSubstoryScenePresentationSnapshot(
            data_dialogues._dialogue dialogue)
        {
            SubstoryScenePresentationSnapshot snapshot =
                new SubstoryScenePresentationSnapshot
                {
                    Dialogue = dialogue
                };
            if (dialogue == null ||
                dialogue.type != data_dialogues._dialogue._type.scene ||
                string.IsNullOrEmpty(dialogue.id))
            {
                return snapshot;
            }

            agency agencySystem = ResolveAgencySystemForIdentity();
            if (agencySystem != null)
            {
                List<agency._room> danceStudios =
                    agencySystem.allRooms(agency._type.danceStudio);
                if (danceStudios != null &&
                    danceStudios.Count > CoreConstants.ZeroBasedListStartIndex)
                {
                    snapshot.ExpectedRoom =
                        danceStudios[CoreConstants.ZeroBasedListStartIndex];
                }
            }

            lock (runtimeLock)
            {
                if (Substories_Manager.dialogueQueue != null)
                {
                    for (int index = Substories_Manager.dialogueQueue.Count - 1;
                        index >= CoreConstants.ZeroBasedListStartIndex;
                        index--)
                    {
                        Substories_Manager._dialogueQueue queued =
                            Substories_Manager.dialogueQueue[index];
                        if (queued == null ||
                            !ReferenceEquals(queued.dialogue, dialogue) ||
                            queued.launchTime > staticVars.dateTime)
                        {
                            continue;
                        }

                        snapshot.QueueEntry = queued;
                        string occurrenceId;
                        if (substorySceneOccurrenceByQueueEntry.TryGetValue(
                                queued,
                                out occurrenceId))
                        {
                            snapshot.OccurrenceId = occurrenceId ?? string.Empty;
                        }
                        break;
                    }
                }

                if (string.IsNullOrEmpty(snapshot.OccurrenceId))
                {
                    snapshot.OccurrenceId =
                        RecoverOpenSubstorySceneOccurrenceLocked(dialogue.id);
                }
            }

            return snapshot;
        }

        internal void CaptureSubstoryScenePresented(
            data_dialogues._dialogue dialogue,
            SubstoryScenePresentationSnapshot snapshotBefore)
        {
            if (dialogue == null || snapshotBefore == null ||
                !ReferenceEquals(dialogue, snapshotBefore.Dialogue) ||
                snapshotBefore.ExpectedRoom == null ||
                !ReferenceEquals(snapshotBefore.ExpectedRoom.substoryScene, dialogue) ||
                snapshotBefore.ExpectedRoom.status != agency._room._status.substoryScene)
            {
                return;
            }

            Substories_Manager._substoryData substoryData =
                Substories_Manager.GetSubstoryData(dialogue.id);
            string actorSummary = BuildSubstoryActorSummary(substoryData);
            List<int> idolIds = ResolveDistinctSubstoryIdolIdentifiers(substoryData);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                string occurrenceId = snapshotBefore.OccurrenceId;
                if (string.IsNullOrEmpty(occurrenceId))
                {
                    occurrenceId = CreateSubstorySceneOccurrenceId();
                }

                int queueCount = Substories_Manager.dialogueQueue != null
                    ? Substories_Manager.dialogueQueue.Count
                    : CoreConstants.ZeroBasedListStartIndex;
                int delayedCount = Substories_Manager.Delayed_Queue != null
                    ? Substories_Manager.Delayed_Queue.Count
                    : CoreConstants.ZeroBasedListStartIndex;
                SubstoryLifecycleEventPayload payload = BuildSubstoryLifecyclePayload(
                    dialogue.id,
                    dialogue.parent ?? string.Empty,
                    actorSummary,
                    ResolveSubstoryTypeCode(dialogue),
                    CoreConstants.SubstoryLifecycleActionPresented,
                    Substories_Manager.IsUsed(dialogue.id),
                    Substories_Manager.IsUsed(dialogue.id),
                    queueCount,
                    queueCount,
                    delayedCount,
                    delayedCount,
                    snapshotBefore.QueueEntry != null
                        ? CoreDateTimeUtility.ToRoundTripString(snapshotBefore.QueueEntry.launchTime)
                        : string.Empty,
                    snapshotBefore.QueueEntry != null && snapshotBefore.QueueEntry.debug,
                    snapshotBefore.QueueEntry != null && snapshotBefore.QueueEntry.BeforeStart != null);
                payload.substory_occurrence_id = occurrenceId;

                EnqueueSubstoryLifecycleEventLocked(
                    payload,
                    CoreConstants.EventTypeSubstoryPresented,
                    CoreConstants.EventSourceSubstoryScenePresentedPatch,
                    idolIds);

                substorySceneOccurrenceByRoom[snapshotBefore.ExpectedRoom] =
                    occurrenceId;
                if (snapshotBefore.QueueEntry != null)
                {
                    substorySceneOccurrenceByQueueEntry.Remove(
                        snapshotBefore.QueueEntry);
                }
                FlushAfterCaptureLocked();
            }
        }

        internal SubstorySceneCompletionSnapshot CreateSubstorySceneCompletionSnapshot(
            agency._room room)
        {
            SubstorySceneCompletionSnapshot snapshot =
                new SubstorySceneCompletionSnapshot
                {
                    Room = room
                };
            if (room == null ||
                room.status != agency._room._status.substoryScene ||
                room.substoryScene == null ||
                room.substoryScene.type != data_dialogues._dialogue._type.scene)
            {
                return snapshot;
            }

            data_dialogues._dialogue dialogue = room.substoryScene;
            snapshot.DialogueId = dialogue.id ?? string.Empty;
            snapshot.ParentDialogueId = dialogue.parent ?? string.Empty;
            snapshot.DialogueTypeCode = ResolveSubstoryTypeCode(dialogue);
            snapshot.WasSceneActiveBefore = !string.IsNullOrEmpty(snapshot.DialogueId);

            Substories_Manager._substoryData substoryData =
                Substories_Manager.GetSubstoryData(snapshot.DialogueId);
            snapshot.ActorSummary = BuildSubstoryActorSummary(substoryData);
            snapshot.IdolIds = ResolveDistinctSubstoryIdolIdentifiers(substoryData);

            lock (runtimeLock)
            {
                string occurrenceId;
                if (substorySceneOccurrenceByRoom.TryGetValue(room, out occurrenceId))
                {
                    snapshot.OccurrenceId = occurrenceId ?? string.Empty;
                }

                if (string.IsNullOrEmpty(snapshot.OccurrenceId))
                {
                    snapshot.OccurrenceId =
                        RecoverOpenSubstorySceneOccurrenceLocked(snapshot.DialogueId);
                }

                if (string.IsNullOrEmpty(snapshot.OccurrenceId))
                {
                    snapshot.OccurrenceId = CreateSubstorySceneOccurrenceId();
                }
            }

            return snapshot;
        }

        internal void CaptureSubstorySceneCompleted(
            SubstorySceneCompletionSnapshot snapshotBefore)
        {
            if (snapshotBefore == null ||
                !snapshotBefore.WasSceneActiveBefore ||
                snapshotBefore.Room == null ||
                string.IsNullOrEmpty(snapshotBefore.DialogueId) ||
                string.IsNullOrEmpty(snapshotBefore.OccurrenceId) ||
                snapshotBefore.Room.status == agency._room._status.substoryScene ||
                snapshotBefore.Room.substoryScene != null)
            {
                return;
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                int queueCount = Substories_Manager.dialogueQueue != null
                    ? Substories_Manager.dialogueQueue.Count
                    : CoreConstants.ZeroBasedListStartIndex;
                int delayedCount = Substories_Manager.Delayed_Queue != null
                    ? Substories_Manager.Delayed_Queue.Count
                    : CoreConstants.ZeroBasedListStartIndex;
                SubstoryLifecycleEventPayload payload =
                    BuildSubstoryLifecyclePayload(
                        snapshotBefore.DialogueId,
                        snapshotBefore.ParentDialogueId,
                        snapshotBefore.ActorSummary,
                        snapshotBefore.DialogueTypeCode,
                        CoreConstants.SubstoryLifecycleActionCompleted,
                        Substories_Manager.IsUsed(snapshotBefore.DialogueId),
                        Substories_Manager.IsUsed(snapshotBefore.DialogueId),
                        queueCount,
                        queueCount,
                        delayedCount,
                        delayedCount,
                        string.Empty,
                        false,
                        false);
                payload.substory_occurrence_id = snapshotBefore.OccurrenceId;

                EnqueueSubstoryLifecycleEventLocked(
                    payload,
                    CoreConstants.EventTypeSubstoryCompleted,
                    CoreConstants.EventSourceRoomSubstoryFinishPatch,
                    snapshotBefore.IdolIds);
                FlushAfterCaptureLocked();
                substorySceneOccurrenceByRoom.Remove(snapshotBefore.Room);
            }
        }

        private string RecoverOpenSubstorySceneOccurrenceLocked(string dialogueId)
        {
            if (string.IsNullOrEmpty(dialogueId) || storageEngine == null)
            {
                return string.Empty;
            }

            string occurrenceId;
            if (storageEngine.TryFindLatestOpenSubstoryOccurrence(
                    dialogueId,
                    out occurrenceId))
            {
                return occurrenceId ?? string.Empty;
            }
            return string.Empty;
        }
    }
}
