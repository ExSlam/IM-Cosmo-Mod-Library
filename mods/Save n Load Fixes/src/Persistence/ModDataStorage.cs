using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using SaveNLoadFixes.Transport;
using JsonValue = SaveNLoadFixes.Persistence.RepairEnvelopeCodec.FiniteJsonValue;
using JsonKind = SaveNLoadFixes.Persistence.RepairEnvelopeCodec.FiniteJsonKind;

namespace SaveNLoadFixes.Persistence
{
    internal sealed class ModDataContainer
    {
        public ModDataContainer() { }
        internal string OpaqueRoot;
        internal readonly SortedDictionary<string, string> Records = new SortedDictionary<string, string>(StringComparer.Ordinal);
        internal readonly Dictionary<string, ModDataApi.Registration> Accepted = new Dictionary<string, ModDataApi.Registration>(StringComparer.Ordinal);
        internal bool Loaded;
        internal bool NeedsRestore;
    }

    internal static class ModDataStorage
    {
        internal const string RootKey = "__cosmo_snlf_mod_data";
        private const string Format = "snlf-mod-data";
        private static readonly ConditionalWeakTable<SaveManager.SavedData, ModDataContainer> States = new ConditionalWeakTable<SaveManager.SavedData, ModDataContainer>();
        private static WeakReference active = new WeakReference(null);

        internal static bool TryExtract(string fullJson, out string stripped, out ModDataContainer state, out string error)
        {
            state = new ModDataContainer { Loaded = true };
            string raw;
            if (!TryRemoveRoot(fullJson, RootKey, out stripped, out raw, out error)) return false;
            if (raw == null) return true;
            JsonValue root, format, version, mods;
            string parseError;
            if (!RepairEnvelopeCodec.FiniteJsonParser.TryParse(raw, out root, out parseError) || root.Kind != JsonKind.Object ||
                root.ObjectValues.Count != 3 || !root.ObjectValues.TryGetValue("format", out format) || format.Kind != JsonKind.String || format.Text != Format ||
                !root.ObjectValues.TryGetValue("version", out version) || version.Kind != JsonKind.Number || version.Text != "1" ||
                !root.ObjectValues.TryGetValue("mods", out mods) || mods.Kind != JsonKind.Object)
            {
                // Future/invalid container is isolated and preserved in full. No
                // current writer can silently turn it into an empty v1 container.
                state.OpaqueRoot = raw;
                return true;
            }
            string modsRaw = RootValue(raw, "mods");
            foreach (string owner in mods.ObjectValues.Keys) state.Records.Add(owner, RootValue(modsRaw, owner));
            return true;
        }

        internal static void Associate(SaveManager.SavedData data, ModDataContainer state) { States.Add(data, state); }
        internal static void MarkAdopted(SaveManager.SavedData data) { States.GetOrCreateValue(data).NeedsRestore = true; }
        internal static void RestoreLoaded(SaveManager.SavedData data)
        {
            if (data == null) return;
            ModDataContainer state = States.GetOrCreateValue(data);
            if (!state.NeedsRestore) return;
            state.NeedsRestore = false;
            state.Accepted.Clear();
            state.Loaded = true;
            active = new WeakReference(data);
            foreach (ModDataApi.Registration owner in ModDataApi.Snapshot()) Restore(state, owner);
        }
        internal static void RestoreLateRegistration(ModDataApi.Registration owner)
        {
            SaveManager.SavedData data = active.Target as SaveManager.SavedData;
            ModDataContainer state;
            if (data != null && States.TryGetValue(data, out state)) Restore(state, owner);
        }
        internal static void ClearActive() { active = new WeakReference(null); }

        private static void Restore(ModDataContainer state, ModDataApi.Registration owner)
        {
            state.Accepted.Remove(owner.Owner);
            if (state.OpaqueRoot != null) return;
            string raw;
            ModPayload payload = null;
            if (state.Records.TryGetValue(owner.Owner, out raw) && !TryReadRecord(raw, out payload))
            { SaveShutdownCoordinator.ReportFailure(owner.Owner + ": unsupported mod-data record preserved."); return; }
            try { if (owner.Restore(payload)) state.Accepted[owner.Owner] = owner; }
            catch (Exception exception) { SaveShutdownCoordinator.ReportFailure(owner.Owner + " restore failed; payload preserved: " + exception); }
        }

        internal static string Inject(SaveManager.SavedData data, string vanillaJson)
        {
            ModDataContainer state = States.GetOrCreateValue(data);
            if (state.OpaqueRoot == null)
            {
                foreach (ModDataApi.Registration owner in ModDataApi.Snapshot())
                {
                    ModDataApi.Registration accepted;
                    if (state.Loaded && (!state.Accepted.TryGetValue(owner.Owner, out accepted) || !ReferenceEquals(owner, accepted))) continue;
                    try
                    {
                        ModPayload payload = owner.Capture();
                        if (payload != null) state.Records[owner.Owner] = WriteRecord(payload);
                    }
                    catch (Exception exception) { SaveShutdownCoordinator.ReportFailure(owner.Owner + " capture failed; payload preserved: " + exception); }
                }
            }
            if (state.OpaqueRoot == null && state.Records.Count == 0) return vanillaJson;
            string root = state.OpaqueRoot;
            if (root == null)
            {
                StringBuilder builder = new StringBuilder("{\"format\":\"" + Format + "\",\"version\":1,\"mods\":{");
                bool comma = false;
                foreach (KeyValuePair<string, string> record in state.Records)
                {
                    if (comma) builder.Append(',');
                    comma = true;
                    RepairEnvelopeCodec.AppendJsonString(builder, record.Key);
                    builder.Append(':').Append(record.Value);
                }
                root = builder.Append("}}").ToString();
            }
            int open = RepairEnvelopeCodec.SkipWhitespaceForRawMigration(vanillaJson, 0);
            int first = RepairEnvelopeCodec.SkipWhitespaceForRawMigration(vanillaJson, open + 1);
            return vanillaJson.Insert(open + 1, "\"" + RootKey + "\":" + root + (vanillaJson[first] == '}' ? "" : ","));
        }

        private static string WriteRecord(ModPayload payload)
        {
            StringBuilder builder = new StringBuilder("{\"schema\":" + payload.SchemaVersion.ToString(CultureInfo.InvariantCulture) + ",\"encoding\":\"json\",\"payload\":");
            RepairEnvelopeCodec.AppendJsonString(builder, payload.Json);
            return builder.Append('}').ToString();
        }
        private static bool TryReadRecord(string raw, out ModPayload payload)
        {
            payload = null;
            JsonValue node, schema, encoding, content;
            string error;
            int version;
            if (!RepairEnvelopeCodec.FiniteJsonParser.TryParse(raw, out node, out error) || node.Kind != JsonKind.Object || node.ObjectValues.Count != 3 ||
                !node.ObjectValues.TryGetValue("schema", out schema) || schema.Kind != JsonKind.Number || !int.TryParse(schema.Text, NumberStyles.None, CultureInfo.InvariantCulture, out version) || version < 1 ||
                !node.ObjectValues.TryGetValue("encoding", out encoding) || encoding.Kind != JsonKind.String || encoding.Text != "json" ||
                !node.ObjectValues.TryGetValue("payload", out content) || content.Kind != JsonKind.String) return false;
            try { payload = new ModPayload(version, content.Text); return true; }
            catch (ArgumentException) { return false; }
        }

        private static string RootValue(string json, string key)
        {
            bool found; int ps, pe, vs, ve; string error;
            if (!RepairEnvelopeCodec.TryFindRootPropertyForRawMigration(json, key, out found, out ps, out pe, out vs, out ve, out error) || !found)
                throw new InvalidOperationException("Validated mod-data member disappeared: " + key);
            return json.Substring(vs, ve - vs);
        }
        private static bool TryRemoveRoot(string json, string key, out string stripped, out string raw, out string error)
        {
            stripped = json; raw = null;
            bool found; int ps, pe, vs, ve;
            if (!RepairEnvelopeCodec.TryFindRootPropertyForRawMigration(json, key, out found, out ps, out pe, out vs, out ve, out error)) return false;
            if (!found) return true;
            raw = json.Substring(vs, ve - vs);
            int previous = ps - 1;
            while (previous >= 0 && char.IsWhiteSpace(json[previous])) previous--;
            int next = RepairEnvelopeCodec.SkipWhitespaceForRawMigration(json, pe);
            if (previous >= 0 && json[previous] == ',') ps = previous;
            else if (next < json.Length && json[next] == ',') pe = next + 1;
            stripped = json.Remove(ps, pe - ps);
            if (!RepairEnvelopeCodec.TryFindRootPropertyForRawMigration(stripped, key, out found, out ps, out pe, out vs, out ve, out error)) return false;
            if (found) { error = "Duplicate SNLF mod-data root."; return false; }
            return true;
        }
    }
}
