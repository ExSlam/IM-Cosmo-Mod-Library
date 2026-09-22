using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SaveNLoadFixes.Transport;

namespace SaveNLoadFixes
{
    /// <summary>A concrete SavedData write, shared by every participating sidecar writer.</summary>
    public sealed class SaveRequest
    {
        public string RequestId { get; private set; }
        public string AbsoluteSavePath { get; private set; }
        internal SaveRequest(string path) { RequestId = Guid.NewGuid().ToString("N"); AbsoluteSavePath = path; }
    }

    /// <summary>
    /// Register once during mod initialization. The callback runs on the save caller
    /// thread; freeze Unity state there and return a Task covering ALL asynchronous
    /// work. Completed, faulted and cancelled tasks all finish the attempt. Returning
    /// before untracked work ends violates the contract. A null task is a failure.
    /// </summary>
    public static class SaveParticipationApi
    {
        public const int Version = 1;
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, Registration> Writers = new Dictionary<string, Registration>(StringComparer.Ordinal);
        private static int pending;
        private static bool exitSealed;
        public static int PendingAttemptCount { get { lock (Sync) return pending; } }

        public static IDisposable RegisterSidecarWriter(string owner, Func<SaveRequest, Task> writer)
        {
            ModDataApi.ValidateOwner(owner);
            if (writer == null) throw new ArgumentNullException("writer");
            lock (Sync)
            {
                if (exitSealed) throw new InvalidOperationException("SNLF shutdown has committed.");
                if (Writers.ContainsKey(owner)) throw new InvalidOperationException("Sidecar writer already registered: " + owner);
                Registration registration = new Registration(owner, writer);
                Writers.Add(owner, registration);
                return registration;
            }
        }

        // For mods whose existing save hooks already launch their own writers.
        // Acquire BEFORE launching work. Complete only after all IO has ended.
        public static SaveAttempt BeginSidecarAttempt(string owner, string absoluteSavePath)
        {
            ModDataApi.ValidateOwner(owner);
            string path;
            if (!SavePathResolver.TryNormalizeAbsolutePath(absoluteSavePath, out path))
                throw new ArgumentException("An absolute save path is required.", "absoluteSavePath");
            lock (Sync)
            {
                if (exitSealed) throw new InvalidOperationException("SNLF shutdown has committed.");
                pending++;
                return new SaveAttempt(owner, path);
            }
        }

        internal static void Dispatch(string path)
        {
            Registration[] registrations;
            lock (Sync)
            {
                registrations = new List<Registration>(Writers.Values).ToArray();
            }
            SaveRequest request = new SaveRequest(path);
            foreach (Registration registration in registrations)
            {
                SaveAttempt attempt = BeginSidecarAttempt(registration.Owner, path);
                try
                {
                    Task task = registration.Writer(request);
                    if (task == null) throw new InvalidOperationException("Sidecar callback returned no completion task.");
                    task.ContinueWith(completed =>
                    {
                        string error = completed.IsCanceled ? "Sidecar attempt cancelled." :
                            completed.IsFaulted ? completed.Exception.Flatten().ToString() : string.Empty;
                        attempt.Complete(!completed.IsCanceled && !completed.IsFaulted, error);
                    }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
                catch (Exception exception) { attempt.Complete(false, exception.ToString()); }
            }
        }

        internal static bool TrySealExit()
        {
            lock (Sync)
            {
                if (pending != 0) return false;
                exitSealed = true;
                return true;
            }
        }
        internal static void ResumeAfterMenu() { lock (Sync) exitSealed = false; }
        internal static void EndAttempt(string owner, bool succeeded, string error)
        {
            // Publish terminal state only after diagnostics: shutdown may start as
            // soon as pending reaches zero. Never invoke Unity from a worker here.
            if (!succeeded) SaveShutdownCoordinator.ReportFailure(owner + ": " + error);
            lock (Sync) pending--;
        }

        private sealed class Registration : IDisposable
        {
            internal readonly string Owner;
            internal readonly Func<SaveRequest, Task> Writer;
            internal Registration(string owner, Func<SaveRequest, Task> writer) { Owner = owner; Writer = writer; }
            public void Dispose()
            {
                lock (Sync)
                {
                    Registration current;
                    if (Writers.TryGetValue(Owner, out current) && ReferenceEquals(current, this)) Writers.Remove(Owner);
                }
            }
        }
    }

    public sealed class SaveAttempt
    {
        private int completed;
        public string Owner { get; private set; }
        public string AbsoluteSavePath { get; private set; }
        internal SaveAttempt(string owner, string path) { Owner = owner; AbsoluteSavePath = path; }
        public void Complete(bool succeeded, string error)
        {
            if (Interlocked.Exchange(ref completed, 1) == 0)
                SaveParticipationApi.EndAttempt(Owner, succeeded, error ?? string.Empty);
        }
    }
}
