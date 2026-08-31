using System;
using System.Collections.Generic;
using System.Threading;
using SaveNLoadFixes.Persistence;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Unity lays out SNS_Manager._message recursively because each message embeds a
    /// List&lt;_message&gt; Replies field. Preserve the actual finite object tree in a
    /// non-recursive node table, then replace Unity's depth-limited reconstruction
    /// only after vanilla SNS_Manager.LoadFunction has completed.
    /// </summary>
    internal static class SnsMessageRepair
    {
        internal const int SectionVersion = 1;

        private static long capturedCheckpointCount;
        private static long capturedNodeCount;
        private static long restoredLoadCount;
        private static long restoredNodeCount;
        private static long legacySectionAbsentCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return SnsMessagePatchHealth.IsHealthy; }
        }

        internal static long CapturedCheckpointCount { get { return Interlocked.Read(ref capturedCheckpointCount); } }
        internal static long CapturedNodeCount { get { return Interlocked.Read(ref capturedNodeCount); } }
        internal static long RestoredLoadCount { get { return Interlocked.Read(ref restoredLoadCount); } }
        internal static long RestoredNodeCount { get { return Interlocked.Read(ref restoredNodeCount); } }
        internal static long LegacySectionAbsentCount { get { return Interlocked.Read(ref legacySectionAbsentCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out List<SnsMessageNodeRecordV1> records,
            out string error)
        {
            records = new List<SnsMessageNodeRecordV1>();
            error = string.Empty;

            if (!IsImplemented)
            {
                error = "SNS exact-state restore patch is not healthy.";
                return false;
            }

            if (dataToSave == null || dataToSave.SNS_Manager__Messages == null)
            {
                error = "SavedData SNS message list is null after SaveEvent population.";
                return false;
            }

            HashSet<SNS_Manager._message> seen =
                new HashSet<SNS_Manager._message>(MessageReferenceComparer.Instance);
            List<CaptureFrame> pending = new List<CaptureFrame>();

            for (int index = dataToSave.SNS_Manager__Messages.Count - 1; index >= 0; index--)
            {
                pending.Add(
                    new CaptureFrame
                    {
                        Message = dataToSave.SNS_Manager__Messages[index],
                        ParentNodeId = -1,
                        SiblingOrdinal = index
                    });
            }

            while (pending.Count != 0)
            {
                int pendingIndex = pending.Count - 1;
                CaptureFrame frame = pending[pendingIndex];
                pending.RemoveAt(pendingIndex);

                SNS_Manager._message message = frame.Message;
                if (message == null)
                {
                    error = "SavedData SNS tree contains a null message node.";
                    return false;
                }

                if (!seen.Add(message))
                {
                    error = "SavedData SNS tree contains a cycle or shared message reference.";
                    return false;
                }

                int chara = (int)message.Chara;
                if (chara < (int)SNS_Manager._chara.NONE ||
                    chara > (int)SNS_Manager._chara.Fujimoto)
                {
                    error = "SavedData SNS tree contains a character enum outside the vanilla domain.";
                    return false;
                }

                if (message.User == null || message.Message == null || message.Replies == null)
                {
                    error = "SavedData SNS tree contains a null user, message, or Replies field.";
                    return false;
                }

                int nodeId = records.Count;
                records.Add(
                    new SnsMessageNodeRecordV1
                    {
                        node_id = nodeId,
                        parent_node_id = frame.ParentNodeId,
                        sibling_ordinal = frame.SiblingOrdinal,
                        chara = chara,
                        user = message.User,
                        message = message.Message
                    });

                for (int replyIndex = message.Replies.Count - 1; replyIndex >= 0; replyIndex--)
                {
                    pending.Add(
                        new CaptureFrame
                        {
                            Message = message.Replies[replyIndex],
                            ParentNodeId = nodeId,
                            SiblingOrdinal = replyIndex
                        });
                }
            }

            Interlocked.Increment(ref capturedCheckpointCount);
            Interlocked.Add(ref capturedNodeCount, records.Count);
            lastDiagnostic = "Captured exact SNS state as " + records.Count +
                " non-recursive message node(s).";
            return true;
        }

        /// <summary>
        /// Called only after the complete repair envelope has serialized, passed its
        /// strict value round trip, and been injected into the frozen vanilla payload.
        /// This deliberately follows Unity's possible recursive-schema warning so the
        /// log states whether SNLF's independent non-recursive SNS payload succeeded.
        /// </summary>
        internal static void ReportSerializedCheckpointSuccess(
            string checkpointId,
            int messageCount)
        {
            lastDiagnostic = "SNS Fix successfully serialized " + messageCount +
                " SNS message(s) into the non-recursive exact-state payload for SNLF checkpoint " +
                checkpointId + ". Unity's recursive SNS depth warning does not affect this payload.";
            Debug.Log(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
        }

        internal static void RestoreAfterVanillaLoad(SNS_Manager manager)
        {
            SaveManager.SavedData target = GetTargetSavedData();
            RepairEnvelopeLoadState state;
            if (target == null || !RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                lastDiagnostic = "SNS exact-state restore skipped because the adopted SavedData object has no repair-envelope read association.";
                return;
            }

            if (!state.Present)
            {
                Interlocked.Increment(ref legacySectionAbsentCount);
                lastDiagnostic = "Loaded a pre-envelope save; vanilla SNS reconstruction was left untouched.";
                return;
            }

            if (!state.Valid || state.Envelope == null || state.Envelope.records == null)
            {
                RecordInvalid("SNS exact-state restore failed closed because the repair envelope is invalid.");
                return;
            }

            RepairEnvelopeRecordsV1 envelopeRecords = state.Envelope.records;
            if (envelopeRecords.sns_messages_version == 0)
            {
                Interlocked.Increment(ref legacySectionAbsentCount);
                lastDiagnostic = "Loaded a pre-SNS-section envelope; vanilla SNS reconstruction was left untouched.";
                return;
            }

            if (envelopeRecords.sns_messages_version != SectionVersion ||
                envelopeRecords.sns_message_nodes == null)
            {
                RecordInvalid("SNS exact-state restore failed closed because the section is missing or unsupported.");
                return;
            }

            List<SNS_Manager._message> restored;
            string validationError;
            if (!TryReconstruct(
                    envelopeRecords.sns_message_nodes,
                    out restored,
                    out validationError))
            {
                RecordInvalid("SNS exact-state restore failed closed: " + validationError);
                return;
            }

            // Apply only after the complete node table validates and reconstructs.
            SNS_Manager.Messages = restored;
            target.SNS_Manager__Messages = restored;

            Interlocked.Increment(ref restoredLoadCount);
            Interlocked.Add(ref restoredNodeCount, envelopeRecords.sns_message_nodes.Count);
            lastDiagnostic = "SNS Fix successfully deserialized and restored " +
                envelopeRecords.sns_message_nodes.Count +
                " SNS message(s) from SNLF checkpoint " +
                state.Envelope.checkpoint_id + ".";
            Debug.Log(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);

            try
            {
                if (manager != null)
                {
                    manager.RenderMessages();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Exact SNS state was restored, but its presentation refresh failed: " +
                    exception.Message);
            }
        }

        private static bool TryReconstruct(
            List<SnsMessageNodeRecordV1> records,
            out List<SNS_Manager._message> roots,
            out string error)
        {
            roots = new List<SNS_Manager._message>();
            error = string.Empty;
            SNS_Manager._message[] nodes = new SNS_Manager._message[records.Count];
            int[] nextChildOrdinals = new int[records.Count];
            int nextRootOrdinal = 0;

            for (int index = 0; index < records.Count; index++)
            {
                SnsMessageNodeRecordV1 record = records[index];
                if (record == null || record.node_id != index ||
                    record.sibling_ordinal < 0 ||
                    record.chara < (int)SNS_Manager._chara.NONE ||
                    record.chara > (int)SNS_Manager._chara.Fujimoto ||
                    record.user == null || record.message == null)
                {
                    error = "the node table contains a null, non-canonical, or invalid-valued row.";
                    return false;
                }

                if (record.parent_node_id < -1 || record.parent_node_id >= index)
                {
                    error = "the node table contains an invalid parent relationship.";
                    return false;
                }

                SNS_Manager._message message = new SNS_Manager._message
                {
                    Chara = (SNS_Manager._chara)record.chara,
                    User = record.user,
                    Message = record.message,
                    Replies = new List<SNS_Manager._message>()
                };
                nodes[index] = message;

                if (record.parent_node_id == -1)
                {
                    if (record.sibling_ordinal != nextRootOrdinal)
                    {
                        error = "the root message ordinals are not contiguous and ordered.";
                        return false;
                    }

                    nextRootOrdinal++;
                    roots.Add(message);
                }
                else
                {
                    int parentId = record.parent_node_id;
                    if (record.sibling_ordinal != nextChildOrdinals[parentId])
                    {
                        error = "one or more reply ordinals are not contiguous and ordered.";
                        return false;
                    }

                    nextChildOrdinals[parentId]++;
                    nodes[parentId].Replies.Add(message);
                }
            }

            return true;
        }

        private static SaveManager.SavedData GetTargetSavedData()
        {
            try
            {
                mainScript main = Camera.main == null
                    ? null
                    : Camera.main.GetComponent<mainScript>();
                return main == null ? null : main.GetSavedData();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void RecordInvalid(string diagnostic)
        {
            Interlocked.Increment(ref invalidSectionCount);
            lastDiagnostic = diagnostic ?? "SNS exact-state section validation failed.";
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
        }

        private sealed class CaptureFrame
        {
            internal SNS_Manager._message Message;
            internal int ParentNodeId;
            internal int SiblingOrdinal;
        }

        private sealed class MessageReferenceComparer : IEqualityComparer<SNS_Manager._message>
        {
            internal static readonly MessageReferenceComparer Instance =
                new MessageReferenceComparer();

            public bool Equals(SNS_Manager._message left, SNS_Manager._message right)
            {
                return ReferenceEquals(left, right);
            }

            public int GetHashCode(SNS_Manager._message value)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
            }
        }
    }

    internal static class SnsMessagePatchHealth
    {
        private static readonly object Sync = new object();
        private static bool targetResolved;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return targetResolved && string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                // HarmonyX may resolve the same logical target again while composing
                // another mod. Re-observing this one exact method is idempotent.
                targetResolved = true;
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown SNS exact-state patch failure";
            }
        }
    }
}
