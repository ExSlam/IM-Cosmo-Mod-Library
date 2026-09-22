using System;
using System.Collections.Generic;
using SaveNLoadFixes.Persistence;

namespace SaveNLoadFixes
{
    /// <summary>Schema version belongs to the mod, independently of its DLL version.</summary>
    public sealed class ModPayload
    {
        public int SchemaVersion { get; private set; }
        public string Json { get; private set; }
        public ModPayload(int schemaVersion, string json)
        {
            if (schemaVersion < 1) throw new ArgumentOutOfRangeException("schemaVersion");
            RepairEnvelopeCodec.FiniteJsonValue parsed;
            string error;
            if (!RepairEnvelopeCodec.FiniteJsonParser.TryParse(json, out parsed, out error))
                throw new ArgumentException(error, "json");
            SchemaVersion = schemaVersion;
            Json = json;
        }
    }

    /// <summary>
    /// Register on the Unity thread. Restore returns true only if the owner accepts
    /// the stored schema (null means no stored payload). It may migrate in memory.
    /// Capture returns a fully frozen payload, or null to preserve stored bytes.
    /// A rejected/throwing restore prevents that owner's capture from overwriting
    /// incompatible data. Other owners and vanilla remain independent.
    /// </summary>
    public static class ModDataApi
    {
        public const int Version = 1;
        private static readonly Dictionary<string, Registration> Owners = new Dictionary<string, Registration>(StringComparer.Ordinal);

        public static IDisposable Register(string owner, Func<ModPayload, bool> restore, Func<ModPayload> capture)
        {
            ValidateOwner(owner);
            if (restore == null || capture == null) throw new ArgumentNullException(restore == null ? "restore" : "capture");
            if (Owners.ContainsKey(owner)) throw new InvalidOperationException("Payload owner already registered: " + owner);
            Registration registration = new Registration(owner, restore, capture);
            Owners.Add(owner, registration);
            ModDataStorage.RestoreLateRegistration(registration);
            return registration;
        }

        internal static Registration[] Snapshot() { return new List<Registration>(Owners.Values).ToArray(); }
        internal static void ValidateOwner(string owner)
        {
            if (string.IsNullOrEmpty(owner) || owner.Length > 200 || owner.IndexOf('.') < 1)
                throw new ArgumentException("Use a stable reverse-domain owner ID.", "owner");
            foreach (char c in owner)
                if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '.' && c != '-' && c != '_')
                    throw new ArgumentException("Owner IDs use lowercase ASCII letters, digits, dot, underscore and hyphen.", "owner");
        }

        internal sealed class Registration : IDisposable
        {
            internal readonly string Owner;
            internal readonly Func<ModPayload, bool> Restore;
            internal readonly Func<ModPayload> Capture;
            internal Registration(string owner, Func<ModPayload, bool> restore, Func<ModPayload> capture)
            { Owner = owner; Restore = restore; Capture = capture; }
            public void Dispose()
            {
                Registration current;
                if (Owners.TryGetValue(Owner, out current) && ReferenceEquals(current, this)) Owners.Remove(Owner);
            }
        }
    }
}
