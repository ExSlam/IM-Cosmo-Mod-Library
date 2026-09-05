using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace IMDataCore
{
    /// <summary>
    /// Wave-1 generated-task occurrence identity. Custom/scripted task ids keep
    /// their definition-stream semantics. Only non-custom task occurrences get
    /// an IMDC-owned generation, with exact v6 checkpoint rebinding by the
    /// serialized tasks__TaskData ordinal plus a full saved-row witness.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        private const string TaskOccurrenceGenerationPrefix = "t:";
        private const string TaskIdentityWitnessPrefix = "sha256:";

        private sealed class TaskIdentityRuntimeBinding
        {
            internal string EntityId = string.Empty;
            internal long CoverageStartSequence;
            internal string Origin = LightweightIdentityBindingSchema.OriginNative;
            internal string LegacyCandidateKey = string.Empty;
        }

        private readonly Dictionary<tasks._task, TaskIdentityRuntimeBinding>
            generatedTaskIdentityByReference =
                new Dictionary<tasks._task, TaskIdentityRuntimeBinding>();

        private List<LightweightIdentityBindingRecord> pendingLoadedTaskBindings;
        private bool pendingLoadedTaskBindingsComplete;

        private static bool CanonicalTaskIdentityRuntimeEnabled
        {
            get
            {
                return LightweightCoreStorageEngine.DurableV6RuntimeEnabled;
            }
        }

        private static string CreateTaskOccurrenceGenerationId()
        {
            return TaskOccurrenceGenerationPrefix + Guid.NewGuid().ToString("N");
        }

        private long ResolveNextTaskCoverageSequenceLocked()
        {
            return captureSequence == long.MaxValue
                ? long.MaxValue
                : captureSequence + 1L;
        }

        private static bool IsGeneratedNonCustomTask(tasks._task task)
        {
            return task != null && string.IsNullOrEmpty(task.Custom ?? string.Empty);
        }

        private TaskIdentityRuntimeBinding EnsureGeneratedTaskIdentityBindingLocked(
            tasks._task task,
            string origin,
            string legacyCandidateKey)
        {
            if (!IsGeneratedNonCustomTask(task))
            {
                return null;
            }

            TaskIdentityRuntimeBinding existing;
            if (generatedTaskIdentityByReference.TryGetValue(task, out existing) &&
                existing != null &&
                !string.IsNullOrEmpty(existing.EntityId))
            {
                if (string.IsNullOrEmpty(existing.LegacyCandidateKey) &&
                    !string.IsNullOrEmpty(legacyCandidateKey))
                {
                    existing.LegacyCandidateKey = legacyCandidateKey;
                }
                return existing;
            }

            TaskIdentityRuntimeBinding created = new TaskIdentityRuntimeBinding
            {
                EntityId = CreateTaskOccurrenceGenerationId(),
                CoverageStartSequence = ResolveNextTaskCoverageSequenceLocked(),
                Origin = string.IsNullOrEmpty(origin)
                    ? LightweightIdentityBindingSchema.OriginNative
                    : origin,
                LegacyCandidateKey = legacyCandidateKey ?? string.Empty
            };
            generatedTaskIdentityByReference[task] = created;
            RegisterIdentityCandidateLocked(
                CoreConstants.EventEntityKindTask,
                created.LegacyCandidateKey,
                CoreConstants.EventEntityKindTask,
                created.EntityId,
                LightweightIdentityBindingSchema.CandidateSourceNativeBinding);
            return created;
        }

        internal GeneratedTaskBirthSnapshot CreateGeneratedTaskBirthSnapshot(
            tasks._task._type requestedType)
        {
            GeneratedTaskBirthSnapshot snapshot = new GeneratedTaskBirthSnapshot
            {
                RequestedTaskType = requestedType
            };

            if (tasks.ActiveTasks != null)
            {
                for (int index = CoreConstants.ZeroBasedListStartIndex;
                    index < tasks.ActiveTasks.Count;
                    index++)
                {
                    snapshot.ActiveTaskReferencesBefore.Add(tasks.ActiveTasks[index]);
                }
            }
            return snapshot;
        }

        private static bool ContainsTaskReference(
            IReadOnlyList<tasks._task> tasksBefore,
            tasks._task candidate)
        {
            if (tasksBefore == null || candidate == null)
            {
                return false;
            }

            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < tasksBefore.Count;
                index++)
            {
                if (ReferenceEquals(tasksBefore[index], candidate))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Resolves the one object inserted by GenerateTask, allocates its
        /// occurrence identity, and emits finding #27's missing task_added row.
        /// On the live v5 writer the row keeps the legacy coarse key; once v6 is
        /// live the exact same hook emits under the canonical t: generation.
        /// </summary>
        internal void CaptureGeneratedTaskAdded(GeneratedTaskBirthSnapshot snapshotBefore)
        {
            if (snapshotBefore == null || tasks.ActiveTasks == null)
            {
                return;
            }

            tasks._task createdTask = null;
            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < tasks.ActiveTasks.Count;
                index++)
            {
                tasks._task candidate = tasks.ActiveTasks[index];
                if (candidate == null ||
                    ContainsTaskReference(snapshotBefore.ActiveTaskReferencesBefore, candidate))
                {
                    continue;
                }

                if (createdTask != null)
                {
                    // More than one appended object is not an exact birth
                    // observation. Refuse to guess which task GenerateTask made.
                    return;
                }
                createdTask = candidate;
            }

            if (createdTask == null ||
                createdTask.Type != snapshotBefore.RequestedTaskType ||
                !IsGeneratedNonCustomTask(createdTask))
            {
                return;
            }

            bool activeAfter = tasks.ActiveTasks.Contains(createdTask);
            if (!activeAfter)
            {
                return;
            }

            int taskGirlId = createdTask.Girl != null
                ? createdTask.Girl.id
                : CoreConstants.InvalidIdValue;
            TaskLifecycleEventPayload payload = BuildTaskLifecyclePayload(
                createdTask,
                CoreConstants.TaskLifecycleActionAdded,
                false,
                createdTask.Fulfilled,
                false,
                true,
                false,
                CoreEnumNameMapping.ToTaskRouteCode(
                    tasks.Story_Data != null ? tasks.Story_Data.Route : tasks._route.NONE),
                createdTask.AvailableFrom.HasValue
                    ? CoreDateTimeUtility.ToRoundTripString(createdTask.AvailableFrom.Value)
                    : string.Empty);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                string legacyCandidateKey = BuildTaskIdentifier(createdTask, null);
                TaskIdentityRuntimeBinding binding =
                    EnsureGeneratedTaskIdentityBindingLocked(
                        createdTask,
                        LightweightIdentityBindingSchema.OriginNative,
                        legacyCandidateKey);
                string entityIdentifier = CanonicalTaskIdentityRuntimeEnabled &&
                    binding != null &&
                    !string.IsNullOrEmpty(binding.EntityId)
                        ? binding.EntityId
                        : legacyCandidateKey;

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    taskGirlId >= CoreConstants.MinimumValidIdolIdentifier
                        ? taskGirlId
                        : CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindTask,
                    entityIdentifier,
                    CoreConstants.EventTypeTaskAdded,
                    CoreConstants.EventSourceTasksGenerateTaskPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        private string ResolveTaskHistoryEntityIdentifierLocked(
            tasks._task task,
            TaskLifecycleSnapshot snapshotBefore)
        {
            string legacyIdentifier = BuildTaskIdentifier(task, snapshotBefore);
            if (!CanonicalTaskIdentityRuntimeEnabled || !IsGeneratedNonCustomTask(task))
            {
                return legacyIdentifier;
            }

            TaskIdentityRuntimeBinding binding;
            if (generatedTaskIdentityByReference.TryGetValue(task, out binding) &&
                binding != null &&
                !string.IsNullOrEmpty(binding.EntityId))
            {
                return binding.EntityId;
            }

            // Only active objects may receive a fresh native generation here.
            // Removed objects without a binding must remain legacy/unknown rather
            // than receiving a fabricated terminal-only occurrence identity.
            if (tasks.ActiveTasks != null && tasks.ActiveTasks.Contains(task))
            {
                binding = EnsureGeneratedTaskIdentityBindingLocked(
                    task,
                    LightweightIdentityBindingSchema.OriginNative,
                    legacyIdentifier);
                if (binding != null && !string.IsNullOrEmpty(binding.EntityId))
                {
                    return binding.EntityId;
                }
            }

            return legacyIdentifier;
        }

        private static string BuildTaskLegacyCandidateKey(tasks.TaskData savedRow)
        {
            if (savedRow == null)
            {
                return string.Empty;
            }

            return string.Concat(
                CoreEnumNameMapping.ToTaskTypeCode(savedRow.Type),
                CoreConstants.SaveKeyJoinSeparator,
                CoreEnumNameMapping.ToTaskGoalCode(savedRow.Goal),
                CoreConstants.SaveKeyJoinSeparator,
                savedRow.Girl.ToString(CultureInfo.InvariantCulture));
        }

        internal static string BuildTaskIdentityValidationFingerprint(tasks.TaskData savedRow)
        {
            if (savedRow == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            AppendTaskWitnessField(builder, ((int)savedRow.Type).ToString(CultureInfo.InvariantCulture));
            AppendTaskWitnessField(builder, ((int)savedRow.Goal).ToString(CultureInfo.InvariantCulture));
            AppendTaskWitnessField(builder, savedRow.Substory ? "1" : "0");
            AppendTaskWitnessField(builder, savedRow.Single_Genre.ToString(CultureInfo.InvariantCulture));
            AppendTaskWitnessField(builder, savedRow.Single_Lyrics.ToString(CultureInfo.InvariantCulture));
            AppendTaskWitnessField(builder, savedRow.Show_Genre.ToString(CultureInfo.InvariantCulture));
            AppendTaskWitnessField(builder, savedRow.Show_Medium.ToString(CultureInfo.InvariantCulture));
            AppendTaskWitnessField(builder, savedRow.Girl.ToString(CultureInfo.InvariantCulture));
            AppendTaskWitnessField(builder, savedRow.AgentName ?? string.Empty);
            AppendTaskWitnessField(builder, ((int)savedRow.Skill).ToString(CultureInfo.InvariantCulture));
            AppendTaskWitnessField(builder, savedRow.Custom ?? string.Empty);
            AppendTaskWitnessField(builder, savedRow.AvailableFrom ?? string.Empty);
            AppendTaskWitnessField(builder, savedRow.Fulfilled ? "1" : "0");

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] digest = sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(builder.ToString()));
                return TaskIdentityWitnessPrefix + ToLowerHexForTaskIdentity(digest);
            }
        }

        private static void AppendTaskWitnessField(StringBuilder builder, string value)
        {
            string normalized = value ?? string.Empty;
            builder.Append(normalized.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(normalized);
            builder.Append('|');
        }

        private static string ToLowerHexForTaskIdentity(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            for (int index = 0; index < bytes.Length; index++)
            {
                builder.Append(bytes[index].ToString("x2", CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        private static int ResolveLiveTaskSingleGenreId(tasks._task task)
        {
            return task != null && task.Single_Genre != null
                ? task.Single_Genre.id
                : CoreConstants.InvalidIdValue;
        }

        private static int ResolveLiveTaskSingleLyricsId(tasks._task task)
        {
            return task != null && task.Single_Lyrics != null
                ? task.Single_Lyrics.id
                : CoreConstants.InvalidIdValue;
        }

        private static int ResolveLiveTaskShowGenreId(tasks._task task)
        {
            return task != null && task.Show_Genre != null
                ? task.Show_Genre.id
                : CoreConstants.InvalidIdValue;
        }

        private static int ResolveLiveTaskShowMediumId(tasks._task task)
        {
            return task != null && task.Show_Medium != null
                ? task.Show_Medium.id
                : CoreConstants.InvalidIdValue;
        }

        private static string ResolveLiveTaskAvailableFrom(tasks._task task)
        {
            if (task == null || !task.AvailableFrom.HasValue)
            {
                return string.Empty;
            }

            try
            {
                return ExtensionMethods.ToDataString(task.AvailableFrom.Value) ?? string.Empty;
            }
            catch
            {
                return null;
            }
        }

        private static bool TaskSavedRowMatchesLive(
            tasks.TaskData savedRow,
            tasks._task liveTask)
        {
            if (savedRow == null || liveTask == null)
            {
                return false;
            }

            string liveAvailableFrom = ResolveLiveTaskAvailableFrom(liveTask);
            if (liveAvailableFrom == null)
            {
                return false;
            }

            int liveGirlId = liveTask.Girl != null
                ? liveTask.Girl.id
                : CoreConstants.InvalidIdValue;
            return savedRow.Type == liveTask.Type &&
                savedRow.Goal == liveTask.Goal &&
                savedRow.Substory == liveTask.Substory &&
                savedRow.Single_Genre == ResolveLiveTaskSingleGenreId(liveTask) &&
                savedRow.Single_Lyrics == ResolveLiveTaskSingleLyricsId(liveTask) &&
                savedRow.Show_Genre == ResolveLiveTaskShowGenreId(liveTask) &&
                savedRow.Show_Medium == ResolveLiveTaskShowMediumId(liveTask) &&
                savedRow.Girl == liveGirlId &&
                string.Equals(savedRow.AgentName ?? string.Empty, liveTask.AgentName ?? string.Empty, StringComparison.Ordinal) &&
                savedRow.Skill == liveTask.Skill &&
                string.Equals(savedRow.Custom ?? string.Empty, liveTask.Custom ?? string.Empty, StringComparison.Ordinal) &&
                string.Equals(savedRow.AvailableFrom ?? string.Empty, liveAvailableFrom, StringComparison.Ordinal) &&
                savedRow.Fulfilled == liveTask.Fulfilled;
        }

        internal List<LightweightIdentityBindingRecord>
            CaptureTaskIdentityBindingsForCheckpointLocked(
                SaveManager.SavedData savedData)
        {
            List<LightweightIdentityBindingRecord> result =
                new List<LightweightIdentityBindingRecord>();
            if (!CanonicalTaskIdentityRuntimeEnabled ||
                savedData == null ||
                savedData.tasks__TaskData == null ||
                tasks.ActiveTasks == null ||
                tasks.ActiveTasks.Count != savedData.tasks__TaskData.Count)
            {
                return result;
            }

            List<tasks._task> stale = new List<tasks._task>();
            foreach (tasks._task task in generatedTaskIdentityByReference.Keys)
            {
                if (!tasks.ActiveTasks.Contains(task))
                {
                    stale.Add(task);
                }
            }
            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < stale.Count;
                index++)
            {
                generatedTaskIdentityByReference.Remove(stale[index]);
            }

            for (int ordinal = CoreConstants.ZeroBasedListStartIndex;
                ordinal < savedData.tasks__TaskData.Count;
                ordinal++)
            {
                tasks.TaskData savedRow = savedData.tasks__TaskData[ordinal];
                tasks._task liveTask = tasks.ActiveTasks[ordinal];
                if (savedRow == null ||
                    !string.IsNullOrEmpty(savedRow.Custom ?? string.Empty) ||
                    !TaskSavedRowMatchesLive(savedRow, liveTask))
                {
                    continue;
                }

                string legacyCandidateKey = BuildTaskLegacyCandidateKey(savedRow);
                TaskIdentityRuntimeBinding runtimeBinding =
                    EnsureGeneratedTaskIdentityBindingLocked(
                        liveTask,
                        LightweightIdentityBindingSchema.OriginNative,
                        legacyCandidateKey);
                if (runtimeBinding == null || string.IsNullOrEmpty(runtimeBinding.EntityId))
                {
                    continue;
                }

                List<string> legacyCandidates = new List<string>();
                string candidate = !string.IsNullOrEmpty(runtimeBinding.LegacyCandidateKey)
                    ? runtimeBinding.LegacyCandidateKey
                    : legacyCandidateKey;
                if (!string.IsNullOrEmpty(candidate))
                {
                    legacyCandidates.Add(candidate);
                }

                result.Add(new LightweightIdentityBindingRecord
                {
                    EntityKind = CoreConstants.EventEntityKindTask,
                    EntityId = runtimeBinding.EntityId,
                    ContainerKind = LightweightIdentityBindingSchema.ContainerTasksTaskData,
                    ContainerOrdinal = ordinal,
                    ParentEntityKind = string.Empty,
                    ParentEntityId = string.Empty,
                    ChildLocator = string.Empty,
                    ValidationFingerprint = BuildTaskIdentityValidationFingerprint(savedRow),
                    Origin = runtimeBinding.Origin,
                    CoverageStartSequence = Math.Min(runtimeBinding.CoverageStartSequence, captureSequence),
                    LegacyCandidateKeys = legacyCandidates
                });
            }
            return result;
        }

        internal void PrepareTaskIdentityBindingsForLoad(
            bool exactCheckpointSelected,
            bool identityBindingsComplete,
            IReadOnlyList<LightweightIdentityBindingRecord> identityBindings)
        {
            lock (runtimeLock)
            {
                generatedTaskIdentityByReference.Clear();
                pendingLoadedTaskBindings = null;
                pendingLoadedTaskBindingsComplete = false;

                if (!CanonicalTaskIdentityRuntimeEnabled ||
                    !exactCheckpointSelected ||
                    !identityBindingsComplete ||
                    identityBindings == null)
                {
                    return;
                }

                List<LightweightIdentityBindingRecord> taskBindings =
                    new List<LightweightIdentityBindingRecord>();
                for (int index = CoreConstants.ZeroBasedListStartIndex;
                    index < identityBindings.Count;
                    index++)
                {
                    LightweightIdentityBindingRecord binding = identityBindings[index];
                    if (binding != null &&
                        string.Equals(
                            binding.EntityKind,
                            CoreConstants.EventEntityKindTask,
                            StringComparison.Ordinal))
                    {
                        taskBindings.Add(CloneTaskIdentityBinding(binding));
                    }
                }

                pendingLoadedTaskBindings = taskBindings;
                pendingLoadedTaskBindingsComplete = true;
            }
        }

        internal void AssociateLoadedTaskIdentities()
        {
            if (!CanonicalTaskIdentityRuntimeEnabled)
            {
                return;
            }

            SaveManager.SavedData savedData = ResolveCurrentSavedDataForTaskIdentity();
            lock (runtimeLock)
            {
                if (TryAdoptLoadedTaskIdentitiesLocked())
                {
                    pendingLoadedTaskBindings = null;
                    pendingLoadedTaskBindingsComplete = false;
                    return;
                }

                if (!pendingLoadedTaskBindingsComplete ||
                    pendingLoadedTaskBindings == null ||
                    savedData == null ||
                    savedData.tasks__TaskData == null ||
                    tasks.ActiveTasks == null)
                {
                    return;
                }

                for (int index = CoreConstants.ZeroBasedListStartIndex;
                    index < pendingLoadedTaskBindings.Count;
                    index++)
                {
                    LightweightIdentityBindingRecord binding =
                        pendingLoadedTaskBindings[index];
                    if (binding == null ||
                        binding.ContainerOrdinal < CoreConstants.ZeroBasedListStartIndex ||
                        binding.ContainerOrdinal >= savedData.tasks__TaskData.Count ||
                        binding.ContainerOrdinal >= tasks.ActiveTasks.Count)
                    {
                        continue;
                    }

                    tasks.TaskData savedRow =
                        savedData.tasks__TaskData[binding.ContainerOrdinal];
                    tasks._task liveTask = tasks.ActiveTasks[binding.ContainerOrdinal];
                    if (savedRow == null ||
                        !string.IsNullOrEmpty(savedRow.Custom ?? string.Empty) ||
                        !string.Equals(
                            BuildTaskIdentityValidationFingerprint(savedRow),
                            binding.ValidationFingerprint,
                            StringComparison.Ordinal) ||
                        !TaskSavedRowMatchesLive(savedRow, liveTask))
                    {
                        continue;
                    }

                    generatedTaskIdentityByReference[liveTask] =
                        new TaskIdentityRuntimeBinding
                        {
                            EntityId = binding.EntityId ?? string.Empty,
                            CoverageStartSequence = binding.CoverageStartSequence,
                            Origin = binding.Origin ?? string.Empty,
                            LegacyCandidateKey = binding.LegacyCandidateKeys != null &&
                                binding.LegacyCandidateKeys.Count > 0
                                    ? binding.LegacyCandidateKeys[0]
                                    : string.Empty
                        };
                }

                pendingLoadedTaskBindings = null;
                pendingLoadedTaskBindingsComplete = false;
            }
        }

        private void RetireGeneratedTaskIdentityIfRemovedLocked(tasks._task task)
        {
            if (task == null)
            {
                return;
            }

            if (tasks.ActiveTasks != null && tasks.ActiveTasks.Contains(task))
            {
                return;
            }
            generatedTaskIdentityByReference.Remove(task);
        }

        private static LightweightIdentityBindingRecord CloneTaskIdentityBinding(
            LightweightIdentityBindingRecord source)
        {
            return new LightweightIdentityBindingRecord
            {
                EntityKind = source.EntityKind ?? string.Empty,
                EntityId = source.EntityId ?? string.Empty,
                ContainerKind = source.ContainerKind ?? string.Empty,
                ContainerOrdinal = source.ContainerOrdinal,
                ParentEntityKind = source.ParentEntityKind ?? string.Empty,
                ParentEntityId = source.ParentEntityId ?? string.Empty,
                ChildLocator = source.ChildLocator ?? string.Empty,
                ValidationFingerprint = source.ValidationFingerprint ?? string.Empty,
                Origin = source.Origin ?? string.Empty,
                CoverageStartSequence = source.CoverageStartSequence,
                LegacyCandidateKeys = source.LegacyCandidateKeys != null
                    ? new List<string>(source.LegacyCandidateKeys)
                    : new List<string>()
            };
        }

        private static SaveManager.SavedData ResolveCurrentSavedDataForTaskIdentity()
        {
            if (Camera.main == null)
            {
                return null;
            }

            mainScript main = Camera.main.GetComponent<mainScript>();
            return main != null ? main.GetSavedData() : null;
        }

        private void ResetTaskIdentityRuntimeStateLocked()
        {
            generatedTaskIdentityByReference.Clear();
            pendingLoadedTaskBindings = null;
            pendingLoadedTaskBindingsComplete = false;
        }
    }
}
