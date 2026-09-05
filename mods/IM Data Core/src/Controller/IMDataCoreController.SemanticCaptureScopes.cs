using System;
using System.Collections.Generic;

namespace IMDataCore
{
    internal sealed partial class IMDataCoreController
    {
        private readonly Stack<SemanticCaptureScope> semanticCaptureScopes =
            new Stack<SemanticCaptureScope>();

        /// <summary>
        /// Opens a semantic settlement boundary. Nested rows are held without a
        /// capture sequence until the outer prerequisite row has been observed.
        /// </summary>
        internal SemanticCaptureScope BeginSemanticCaptureScope()
        {
            SemanticCaptureScope scope = new SemanticCaptureScope();
            lock (runtimeLock)
            {
                semanticCaptureScopes.Push(scope);
            }
            return scope;
        }

        /// <summary>
        /// Closes the scope before the owner emits its prerequisite row.
        /// Deferred children are committed explicitly afterwards.
        /// </summary>
        internal void EndSemanticCaptureScope(SemanticCaptureScope scope)
        {
            if (scope == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                if (scope.IsClosed)
                {
                    return;
                }
                if (semanticCaptureScopes.Count == 0 ||
                    !object.ReferenceEquals(semanticCaptureScopes.Peek(), scope))
                {
                    CoreLog.Warn("Semantic capture scope ended out of order; deferred rows were discarded to protect chronology.");
                    scope.Events.Clear();
                    scope.IsClosed = true;
                    return;
                }
                semanticCaptureScopes.Pop();
                scope.IsClosed = true;
            }
        }

        /// <summary>
        /// Replays deferred child rows after the prerequisite row. If an outer
        /// semantic scope is still active, replay naturally becomes deferred into
        /// that parent scope, preserving recursive-call mutation order.
        /// </summary>
        internal void CommitSemanticCaptureScope(SemanticCaptureScope scope)
        {
            if (scope == null || !scope.IsClosed || scope.WasAborted)
            {
                return;
            }

            lock (runtimeLock)
            {
                for (int index = 0; index < scope.Events.Count; index++)
                {
                    DeferredSemanticEvent deferredEvent = scope.Events[index];
                    if (deferredEvent == null)
                    {
                        continue;
                    }
                    EnqueueEventRecordLocked(
                        deferredEvent.GameDate,
                        deferredEvent.IdolId,
                        deferredEvent.EntityKind,
                        deferredEvent.EntityId,
                        deferredEvent.EventType,
                        deferredEvent.SourcePatch,
                        deferredEvent.PayloadJson);
                }
                scope.Events.Clear();
                FlushAfterCaptureLocked();
            }
        }

        internal void AbortSemanticCaptureScope(SemanticCaptureScope scope)
        {
            if (scope == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                if (!scope.IsClosed && semanticCaptureScopes.Count > 0 &&
                    object.ReferenceEquals(semanticCaptureScopes.Peek(), scope))
                {
                    semanticCaptureScopes.Pop();
                }
                scope.Events.Clear();
                scope.WasAborted = true;
                scope.IsClosed = true;
            }
        }
    }

    internal sealed class SemanticCaptureScope
    {
        internal readonly List<DeferredSemanticEvent> Events =
            new List<DeferredSemanticEvent>();
        internal bool IsClosed;
        internal bool WasAborted;
    }

    internal sealed class DeferredSemanticEvent
    {
        internal DateTime GameDate;
        internal int IdolId;
        internal string EntityKind = string.Empty;
        internal string EntityId = string.Empty;
        internal string EventType = string.Empty;
        internal string SourcePatch = string.Empty;
        internal string PayloadJson = CoreConstants.EmptyJsonObject;
    }
}
