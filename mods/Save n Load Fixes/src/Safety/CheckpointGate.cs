using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using UnityEngine;

namespace SaveNLoadFixes.Safety
{
    /// <summary>
    /// Named transient checkpoint families selected by the persistence audit.
    /// Task 1 defines the registry and enforcement seams only. Later Sprint 1D
    /// tasks acquire these blockers at their source-correct operation lifetimes.
    /// </summary>
    internal enum CheckpointBlockerKind
    {
        EventTemplatesPendingOpen = 1,
        SemanticCallbackFinalFrame = 2,
        BirthdayQueueBetweenPopups = 3,
        SskPostPaymentLaunch = 4
    }

    /// <summary>
    /// Disposable, nesting-safe handle for one active checkpoint blocker.
    /// Multiple instances of the same blocker kind are intentionally allowed.
    /// </summary>
    internal sealed class CheckpointBlockerLease : IDisposable
    {
        private long tokenId;

        internal CheckpointBlockerLease(long tokenId)
        {
            this.tokenId = tokenId;
        }

        public void Dispose()
        {
            long id = Interlocked.Exchange(ref this.tokenId, 0L);
            if (id != 0L)
            {
                CheckpointGate.Release(id);
            }
        }
    }

    /// <summary>
    /// Central checkpoint-safety registry for short, source-verified transitions
    /// that must be atomic with respect to autosave/manual save/in-game load.
    ///
    /// The gate does not serialize semantic state. It only says whether a
    /// checkpoint boundary is currently legal. Blocker producers are introduced
    /// by later Sprint 1D tasks.
    /// </summary>
    internal static class CheckpointGate
    {
        private sealed class BlockerRecord
        {
            internal CheckpointBlockerKind Kind;
            internal string Detail;
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<long, BlockerRecord> ActiveBlockers =
            new Dictionary<long, BlockerRecord>();

        private static long nextTokenId;
        private static string lastBlockedDiagnostic = string.Empty;

        internal static CheckpointBlockerLease Acquire(
            CheckpointBlockerKind kind,
            string detail = null)
        {
            lock (Sync)
            {
                long tokenId = ++nextTokenId;
                if (tokenId == 0L)
                {
                    tokenId = ++nextTokenId;
                }

                ActiveBlockers.Add(
                    tokenId,
                    new BlockerRecord
                    {
                        Kind = kind,
                        Detail = detail ?? string.Empty
                    });

                return new CheckpointBlockerLease(tokenId);
            }
        }

        internal static bool HasActiveBlockers
        {
            get
            {
                lock (Sync)
                {
                    return ActiveBlockers.Count != 0;
                }
            }
        }

        internal static int ActiveBlockerCount
        {
            get
            {
                lock (Sync)
                {
                    return ActiveBlockers.Count;
                }
            }
        }

        internal static string ActiveBlockerDescription
        {
            get
            {
                lock (Sync)
                {
                    return BuildBlockerDescriptionLocked();
                }
            }
        }

        internal static string LastBlockedDiagnostic
        {
            get
            {
                lock (Sync)
                {
                    return lastBlockedDiagnostic;
                }
            }
        }

        internal static bool ShouldAllowManualSave(out string reason)
        {
            return !TryBuildUnsafeLiveCheckpointReason(out reason);
        }

        internal static bool ShouldAllowInGameLoad(out string reason)
        {
            if (IsMainMenu())
            {
                reason = string.Empty;
                return true;
            }

            return !TryBuildUnsafeLiveCheckpointReason(out reason);
        }

        internal static void RecordBlockedOperation(string operation, string reason)
        {
            string diagnostic = string.Concat(
                operation,
                " blocked because the current gameplay checkpoint is unsafe: ",
                reason);

            lock (Sync)
            {
                lastBlockedDiagnostic = diagnostic;
            }

            Debug.LogWarning(
                SaveNLoadFixesConstants.LogPrefix + diagnostic);
        }

        internal static void Release(long tokenId)
        {
            lock (Sync)
            {
                ActiveBlockers.Remove(tokenId);
            }
        }

        /// <summary>
        /// Drops transient blocker leases that belong to a discarded process-local
        /// timeline. Token IDs are intentionally not reset, so a late Dispose() from
        /// an old lease can never release a blocker acquired by a newer timeline.
        /// </summary>
        internal static void ResetForNewTimeline()
        {
            lock (Sync)
            {
                ActiveBlockers.Clear();
            }
        }

        private static bool TryBuildUnsafeLiveCheckpointReason(out string reason)
        {
            List<string> reasons = new List<string>();

            if (SaveManager.PauseAutosave)
            {
                reasons.Add("SaveManager.PauseAutosave is active");
            }

            if (PopupManager.PopupCounter > 0)
            {
                reasons.Add(
                    "PopupManager.PopupCounter=" +
                    PopupManager.PopupCounter.ToString(CultureInfo.InvariantCulture));
            }

            if (ActiveDialogueController.ShowingDialogue)
            {
                reasons.Add("ActiveDialogueController.ShowingDialogue is active");
            }

            lock (Sync)
            {
                if (ActiveBlockers.Count != 0)
                {
                    reasons.Add("SNLF blockers: " + BuildBlockerDescriptionLocked());
                }
            }

            reason = string.Join("; ", reasons.ToArray());
            return reasons.Count != 0;
        }

        private static bool IsMainMenu()
        {
            try
            {
                return mainScript.IsMainMenu();
            }
            catch (Exception exception)
            {
                // A missing scene singleton should not silently disable protection.
                // Treat an indeterminate scene as gameplay and retain the gate.
                Debug.LogWarning(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Could not determine main-menu state while evaluating CheckpointGate. " +
                    "The in-game load guard remains enabled. " +
                    exception.GetType().Name +
                    ": " +
                    exception.Message);
                return false;
            }
        }

        private static string BuildBlockerDescriptionLocked()
        {
            if (ActiveBlockers.Count == 0)
            {
                return string.Empty;
            }

            List<long> tokenIds = new List<long>(ActiveBlockers.Keys);
            tokenIds.Sort();

            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < tokenIds.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(", ");
                }

                long tokenId = tokenIds[index];
                BlockerRecord blocker = ActiveBlockers[tokenId];
                builder.Append(blocker.Kind.ToString());
                builder.Append('#');
                builder.Append(tokenId.ToString(CultureInfo.InvariantCulture));

                if (!string.IsNullOrEmpty(blocker.Detail))
                {
                    builder.Append('(');
                    builder.Append(blocker.Detail);
                    builder.Append(')');
                }
            }

            return builder.ToString();
        }
    }
}
