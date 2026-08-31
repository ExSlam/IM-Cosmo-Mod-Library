using System;
using System.Threading;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A19. SaveManager can deserialize GlobalData in Awake before staticVars.Awake
    /// subscribes LoadSettings to LoadGlobalDataEvent. Keep one process-local pending delivery
    /// for the exact successfully loaded object and replay only the vanilla staticVars consumer
    /// after it has subscribed. This is consumer-readiness repair, not transport or persistence.
    /// </summary>
    internal static class GlobalDataDeliveryRepair
    {
        private static readonly object Sync = new object();

        private static SaveManager pendingManager;
        private static SaveManager.GlobalData pendingData;
        private static long deliveryInvocationSerial;

        private static long loadAttemptCount;
        private static long successfulLoadCount;
        private static long normalDeliveryObservedCount;
        private static long pendingDeliveryCreatedCount;
        private static long pendingDeliverySupersededCount;
        private static long deferredReplayCount;
        private static long consumerInvocationCount;
        private static long nullLoadCount;
        private static long stalePendingDiscardCount;
        private static long consumerReadyNoPendingCount;
        private static long runtimeFailureCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return GlobalDataDeliveryPatchHealth.IsHealthy; }
        }

        internal static long LoadAttemptCount { get { return Interlocked.Read(ref loadAttemptCount); } }
        internal static long SuccessfulLoadCount { get { return Interlocked.Read(ref successfulLoadCount); } }
        internal static long NormalDeliveryObservedCount { get { return Interlocked.Read(ref normalDeliveryObservedCount); } }
        internal static long PendingDeliveryCreatedCount { get { return Interlocked.Read(ref pendingDeliveryCreatedCount); } }
        internal static long PendingDeliverySupersededCount { get { return Interlocked.Read(ref pendingDeliverySupersededCount); } }
        internal static long DeferredReplayCount { get { return Interlocked.Read(ref deferredReplayCount); } }
        internal static long ConsumerInvocationCount { get { return Interlocked.Read(ref consumerInvocationCount); } }
        internal static long NullLoadCount { get { return Interlocked.Read(ref nullLoadCount); } }
        internal static long StalePendingDiscardCount { get { return Interlocked.Read(ref stalePendingDiscardCount); } }
        internal static long ConsumerReadyNoPendingCount { get { return Interlocked.Read(ref consumerReadyNoPendingCount); } }
        internal static long RuntimeFailureCount { get { return Interlocked.Read(ref runtimeFailureCount); } }
        internal static string LastDiagnostic { get { lock (Sync) { return lastDiagnostic; } } }

        /// <summary>
        /// Starts one synchronous LoadGlobalData attempt. A newer attempt supersedes an older
        /// undelivered object because vanilla also replaces SaveManager._GlobalData.
        /// The returned serial lets the Postfix prove whether LoadSettings ran during this call.
        /// </summary>
        internal static long BeginLoadAttempt()
        {
            Interlocked.Increment(ref loadAttemptCount);
            lock (Sync)
            {
                if (pendingData != null)
                {
                    pendingManager = null;
                    pendingData = null;
                    Interlocked.Increment(ref pendingDeliverySupersededCount);
                    lastDiagnostic = "A19 superseded an older pending GlobalData delivery with a newer load attempt.";
                }

                return deliveryInvocationSerial;
            }
        }

        /// <summary>
        /// Runs after vanilla has assigned _GlobalData and, when a subscriber already exists,
        /// invoked LoadGlobalDataEvent. A successful read with no observed LoadSettings call is
        /// retained as exactly one pending delivery for staticVars.Awake.
        /// </summary>
        internal static void CompleteLoadAttempt(SaveManager manager, long deliverySerialAtStart)
        {
            SaveManager.GlobalData loaded = manager == null ? null : manager._GlobalData;
            if (loaded == null)
            {
                Interlocked.Increment(ref nullLoadCount);
                lock (Sync)
                {
                    pendingManager = null;
                    pendingData = null;
                    lastDiagnostic = "A19 observed a null GlobalData load; no delivery is pending.";
                }
                return;
            }

            Interlocked.Increment(ref successfulLoadCount);
            lock (Sync)
            {
                if (deliveryInvocationSerial > deliverySerialAtStart)
                {
                    Interlocked.Increment(ref normalDeliveryObservedCount);
                    pendingManager = null;
                    pendingData = null;
                    lastDiagnostic = "A19 observed vanilla LoadGlobalDataEvent deliver GlobalData normally.";
                    return;
                }

                pendingManager = manager;
                pendingData = loaded;
                Interlocked.Increment(ref pendingDeliveryCreatedCount);
                lastDiagnostic = "A19 retained one successfully loaded GlobalData object pending staticVars consumer readiness.";
            }
        }

        /// <summary>
        /// Prefix observation on the vanilla consumer. It is deliberately passive: normal event
        /// delivery remains vanilla-owned. The monotonically increasing serial only proves that
        /// the consumer was actually invoked during a LoadGlobalData call.
        /// </summary>
        internal static void ObserveLoadSettingsInvocation()
        {
            Interlocked.Increment(ref consumerInvocationCount);
            lock (Sync)
            {
                deliveryInvocationSerial++;
            }
        }

        /// <summary>
        /// Called after staticVars.Awake has executed its vanilla event subscription. Claim the
        /// exact pending object under the lock, then invoke LoadSettings outside the lock so its
        /// observation Prefix cannot deadlock. Claim-before-call makes the replay at-most-once.
        /// </summary>
        internal static void ReplayPendingAfterConsumerReady(staticVars consumer)
        {
            if (consumer == null)
            {
                Interlocked.Increment(ref runtimeFailureCount);
                SetDiagnostic("A19 could not replay pending GlobalData because the staticVars consumer was null.");
                return;
            }

            SaveManager manager;
            SaveManager.GlobalData data;
            lock (Sync)
            {
                if (pendingData == null)
                {
                    Interlocked.Increment(ref consumerReadyNoPendingCount);
                    return;
                }

                manager = pendingManager;
                data = pendingData;

                if (manager == null || !object.ReferenceEquals(manager._GlobalData, data))
                {
                    pendingManager = null;
                    pendingData = null;
                    Interlocked.Increment(ref stalePendingDiscardCount);
                    lastDiagnostic = "A19 discarded a pending GlobalData object that was no longer the manager's current loaded object.";
                    return;
                }

                // Claim before executing the consumer. Never arm the same object twice.
                pendingManager = null;
                pendingData = null;
            }

            try
            {
                consumer.LoadSettings();
                Interlocked.Increment(ref deferredReplayCount);
                SetDiagnostic("A19 replayed the pending GlobalData object exactly once after staticVars subscribed.");
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref runtimeFailureCount);
                SetDiagnostic("A19 pending GlobalData replay failed after being claimed: " + ex.GetType().Name);
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + LastDiagnostic);
            }
        }

        private static void SetDiagnostic(string value)
        {
            lock (Sync)
            {
                lastDiagnostic = value ?? string.Empty;
            }
        }
    }

    internal static class GlobalDataDeliveryPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 3;

        private static readonly object Sync = new object();
        private static int resolvedTargetMethodCount;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return resolvedTargetMethodCount == ExpectedTargetMethodCount &&
                        string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static int ResolvedTargetMethodCount { get { lock (Sync) { return resolvedTargetMethodCount; } } }
        internal static string Failure { get { lock (Sync) { return failure; } } }

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                resolvedTargetMethodCount++;
                if (resolvedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure = "A19 resolved more target methods than the frozen three-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A19 patch failure";
            }
        }
    }
}
