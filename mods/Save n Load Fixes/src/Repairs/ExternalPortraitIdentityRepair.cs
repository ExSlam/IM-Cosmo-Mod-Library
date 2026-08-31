using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using SaveNLoadFixes.Persistence;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A31 / regression 46: vanilla silently substitutes the first loaded
    /// same-type portrait asset when a saved external asset ID is unavailable. If
    /// the career is then saved, idol/staff save code writes the substitute GetID(),
    /// permanently destroying the unresolved external identity.
    ///
    /// SNLF keeps only unresolved identities, keyed by stable entity ID + sprite
    /// type. Before a current-format target is reconstructed, section records put
    /// the original ID back into the exact target DTO slot so vanilla can naturally
    /// rebind it when the provider returns. After vanilla reconstruction, a mismatch
    /// between the target DTO ID and the loaded asset ID proves fallback substitution
    /// and keeps the repair token alive. Save postfixes rewrite the vanilla DTO back
    /// to the unresolved original before caller-thread envelope freeze.
    ///
    /// No title/type similarity or rename guessing is performed. A renamed external
    /// definition needs an explicit future alias/migration contract.
    /// </summary>
    internal static class ExternalPortraitIdentityRepair
    {
        internal const int SectionVersion = 1;
        internal const string IdolKind = "idol";
        internal const string StaffKind = "staff";

        private sealed class LoadContext
        {
            internal SaveManager.SavedData Target;
            internal string Kind = string.Empty;
            internal bool ObserveAfterLoad;
            internal HashSet<string> ExpectedKeys = new HashSet<string>(StringComparer.Ordinal);
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, ExternalPortraitIdentityRecordV1> Unresolved =
            new Dictionary<string, ExternalPortraitIdentityRecordV1>(StringComparer.Ordinal);

        private static LoadContext idolLoadContext;
        private static LoadContext staffLoadContext;

        private static long detectedFallbackCount;
        private static long exactRebindCount;
        private static long savedDtoRewriteCount;
        private static long capturedCheckpointCount;
        private static long capturedIdentityCount;
        private static long legacySectionAbsentCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static long runtimeResolutionFailureCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return ExternalPortraitIdentityPatchHealth.IsHealthy; }
        }

        internal static int UnresolvedIdentityCount
        {
            get { lock (Sync) { return Unresolved.Count; } }
        }

        internal static long DetectedFallbackCount { get { return Interlocked.Read(ref detectedFallbackCount); } }
        internal static long ExactRebindCount { get { return Interlocked.Read(ref exactRebindCount); } }
        internal static long SavedDtoRewriteCount { get { return Interlocked.Read(ref savedDtoRewriteCount); } }
        internal static long CapturedCheckpointCount { get { return Interlocked.Read(ref capturedCheckpointCount); } }
        internal static long CapturedIdentityCount { get { return Interlocked.Read(ref capturedIdentityCount); } }
        internal static long LegacySectionAbsentCount { get { return Interlocked.Read(ref legacySectionAbsentCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static long RuntimeResolutionFailureCount { get { return Interlocked.Read(ref runtimeResolutionFailureCount); } }
        internal static string LastDiagnostic { get { lock (Sync) { return lastDiagnostic; } } }

        internal static void BeginIdolLoad()
        {
            idolLoadContext = BeginLoad(IdolKind);
        }

        internal static void EndIdolLoad()
        {
            LoadContext context = idolLoadContext;
            idolLoadContext = null;
            EndLoad(context);
        }

        internal static void BeginStaffLoad()
        {
            staffLoadContext = BeginLoad(StaffKind);
        }

        internal static void EndStaffLoad()
        {
            LoadContext context = staffLoadContext;
            staffLoadContext = null;
            EndLoad(context);
        }

        internal static void RewriteIdolSaveData()
        {
            RewriteSavedDto(IdolKind);
        }

        internal static void RewriteStaffSaveData()
        {
            RewriteSavedDto(StaffKind);
        }

        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out List<ExternalPortraitIdentityRecordV1> records,
            out string error)
        {
            records = new List<ExternalPortraitIdentityRecordV1>();
            error = string.Empty;
            if (dataToSave == null)
            {
                return CaptureFailed("A31 cannot capture against a null SavedData request.", out error);
            }

            List<ExternalPortraitIdentityRecordV1> snapshot;
            lock (Sync)
            {
                snapshot = new List<ExternalPortraitIdentityRecordV1>(Unresolved.Count);
                foreach (ExternalPortraitIdentityRecordV1 record in Unresolved.Values)
                {
                    snapshot.Add(Clone(record));
                }
            }

            snapshot.Sort(CompareRecords);
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < snapshot.Count; index++)
            {
                ExternalPortraitIdentityRecordV1 record = snapshot[index];
                string key;
                if (!TryValidateRecordShape(record, out key, out error) || !seen.Add(key))
                {
                    return CaptureFailed(
                        string.IsNullOrEmpty(error) ? "A31 runtime registry contains a duplicate identity key." : error,
                        out error);
                }

                data_girls.GirlData._textureAsset slot;
                if (!TryGetSavedSlot(dataToSave, record, out slot, out error))
                {
                    return CaptureFailed("A31 save DTO no longer matches unresolved identity: " + error, out error);
                }
                if (slot == null || !string.Equals(slot.asset_id, record.asset_id, StringComparison.Ordinal))
                {
                    return CaptureFailed(
                        "A31 save DTO did not retain the unresolved original portrait ID before envelope freeze.",
                        out error);
                }

                records.Add(Clone(record));
            }

            Interlocked.Increment(ref capturedCheckpointCount);
            Interlocked.Add(ref capturedIdentityCount, records.Count);
            SetDiagnostic(
                "A31 captured " + records.Count.ToString(CultureInfo.InvariantCulture) +
                " unresolved external portrait identity token(s) for this checkpoint.");
            return true;
        }

        private static LoadContext BeginLoad(string kind)
        {
            ClearKind(kind);
            LoadContext context = new LoadContext { Kind = kind };

            SaveManager.SavedData target = GetTargetSavedData();
            context.Target = target;
            if (target == null)
            {
                Interlocked.Increment(ref associationMissingCount);
                SetDiagnostic("A31 target SavedData is unavailable before " + kind + " portrait reconstruction.");
                return context;
            }

            RepairEnvelopeLoadState state;
            if (!RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                SetDiagnostic("A31 target SavedData has no repair-envelope read association.");
                return context;
            }

            if (!state.Present)
            {
                context.ObserveAfterLoad = true;
                Interlocked.Increment(ref legacySectionAbsentCount);
                SetDiagnostic("A31 is observing a pre-envelope target for unresolved portrait fallback.");
                return context;
            }
            if (!state.Valid || state.Envelope == null || state.Envelope.records == null)
            {
                RecordInvalid("A31 loaded a present but invalid repair envelope; portrait identity state was not treated as legacy/empty.");
                return context;
            }

            RepairEnvelopeRecordsV1 envelopeRecords = state.Envelope.records;
            if (envelopeRecords.external_portrait_identities_version == 0)
            {
                context.ObserveAfterLoad = true;
                Interlocked.Increment(ref legacySectionAbsentCount);
                SetDiagnostic("A31 is observing a pre-A31 envelope for unresolved portrait fallback.");
                return context;
            }
            if (envelopeRecords.external_portrait_identities_version != SectionVersion ||
                envelopeRecords.external_portrait_identities == null)
            {
                RecordInvalid("A31 repair section is invalid or unsupported.");
                return context;
            }

            string error;
            HashSet<string> expected;
            if (!TryValidateAndApplyCurrentRecords(
                    target,
                    envelopeRecords.external_portrait_identities,
                    kind,
                    out expected,
                    out error))
            {
                RecordInvalid("A31 exact repair section failed validation: " + error);
                return context;
            }

            context.ExpectedKeys = expected;
            context.ObserveAfterLoad = true;
            SetDiagnostic(
                "A31 prepared " + expected.Count.ToString(CultureInfo.InvariantCulture) +
                " current-format " + kind + " unresolved identity token(s) before vanilla reconstruction.");
            return context;
        }

        private static void EndLoad(LoadContext context)
        {
            if (context == null || !context.ObserveAfterLoad || context.Target == null)
            {
                return;
            }

            List<ExternalPortraitIdentityRecordV1> observed;
            string error;
            if (!TryObserveLoadedKind(context.Target, context.Kind, out observed, out error))
            {
                Interlocked.Increment(ref runtimeResolutionFailureCount);
                SetDiagnostic("A31 could not observe loaded " + context.Kind + " portrait state: " + error);
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + LastDiagnostic);
                return;
            }

            HashSet<string> observedKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < observed.Count; index++)
            {
                ExternalPortraitIdentityRecordV1 record = observed[index];
                string key = BuildKey(record.entity_kind, record.entity_id, record.sprite_type);
                observedKeys.Add(key);
                lock (Sync)
                {
                    Unresolved[key] = Clone(record);
                }
                Interlocked.Increment(ref detectedFallbackCount);
            }

            foreach (string expectedKey in context.ExpectedKeys)
            {
                if (!observedKeys.Contains(expectedKey))
                {
                    Interlocked.Increment(ref exactRebindCount);
                }
            }

            SetDiagnostic(
                "A31 completed " + context.Kind + " portrait reconstruction with " +
                observed.Count.ToString(CultureInfo.InvariantCulture) +
                " unresolved same-type fallback token(s); " +
                (context.ExpectedKeys.Count - CountIntersection(context.ExpectedKeys, observedKeys)).ToString(CultureInfo.InvariantCulture) +
                " previously unresolved token(s) rebound exactly.");
        }

        private static bool TryValidateAndApplyCurrentRecords(
            SaveManager.SavedData target,
            List<ExternalPortraitIdentityRecordV1> records,
            string kindToApply,
            out HashSet<string> expectedKeys,
            out string error)
        {
            expectedKeys = new HashSet<string>(StringComparer.Ordinal);
            error = string.Empty;
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0; index < records.Count; index++)
            {
                ExternalPortraitIdentityRecordV1 record = records[index];
                string key;
                if (!TryValidateRecordShape(record, out key, out error) || !seen.Add(key))
                {
                    if (string.IsNullOrEmpty(error))
                    {
                        error = "duplicate unresolved portrait identity key";
                    }
                    return false;
                }

                data_girls.GirlData._textureAsset slot;
                if (!TryGetSavedSlot(target, record, out slot, out error))
                {
                    return false;
                }

                if (string.Equals(record.entity_kind, kindToApply, StringComparison.Ordinal))
                {
                    slot.asset_id = record.asset_id;
                    expectedKeys.Add(key);
                }
            }

            return true;
        }

        private static bool TryObserveLoadedKind(
            SaveManager.SavedData target,
            string kind,
            out List<ExternalPortraitIdentityRecordV1> observed,
            out string error)
        {
            observed = new List<ExternalPortraitIdentityRecordV1>();
            error = string.Empty;

            if (string.Equals(kind, IdolKind, StringComparison.Ordinal))
            {
                if (target.data_girls__Girls == null || data_girls.girl == null)
                {
                    error = "idol target/runtime list is null";
                    return false;
                }

                Dictionary<int, data_girls.girls> live = BuildLiveGirlMap(data_girls.girl, out error);
                if (live == null)
                {
                    return false;
                }

                HashSet<int> savedIds = new HashSet<int>();
                for (int index = 0; index < target.data_girls__Girls.Count; index++)
                {
                    data_girls.GirlData saved = target.data_girls__Girls[index];
                    if (saved == null || saved.id < 0 || !savedIds.Add(saved.id) || saved.textureAssets == null)
                    {
                        error = "idol target contains a null/duplicate entity or null texture list";
                        return false;
                    }

                    data_girls.girls loaded;
                    if (!live.TryGetValue(saved.id, out loaded) || loaded == null)
                    {
                        error = "saved idol did not resolve to exactly one loaded idol";
                        return false;
                    }
                    if (!ObserveEntity(IdolKind, saved.id, saved.textureAssets, loaded.textureAssets, observed, out error))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (string.Equals(kind, StaffKind, StringComparison.Ordinal))
            {
                if (target.staff__Staff == null || staff.Staff == null)
                {
                    error = "staff target/runtime list is null";
                    return false;
                }

                Dictionary<int, staff._staff> live = BuildLiveStaffMap(staff.Staff, out error);
                if (live == null)
                {
                    return false;
                }

                HashSet<int> savedIds = new HashSet<int>();
                for (int index = 0; index < target.staff__Staff.Count; index++)
                {
                    staff.StaffData saved = target.staff__Staff[index];
                    if (saved == null || saved.id < 0 || !savedIds.Add(saved.id) || saved.textureAssets == null)
                    {
                        error = "staff target contains a null/duplicate entity or null texture list";
                        return false;
                    }

                    staff._staff loaded;
                    if (!live.TryGetValue(saved.id, out loaded) || loaded == null)
                    {
                        error = "saved staff member did not resolve to exactly one loaded staff member";
                        return false;
                    }
                    if (!ObserveEntity(StaffKind, saved.id, saved.textureAssets, loaded.textureAssets, observed, out error))
                    {
                        return false;
                    }
                }

                return true;
            }

            error = "unsupported entity kind";
            return false;
        }

        private static bool ObserveEntity(
            string kind,
            int entityId,
            List<data_girls.GirlData._textureAsset> savedSlots,
            List<data_girls.girls._textureAsset> liveSlots,
            List<ExternalPortraitIdentityRecordV1> observed,
            out string error)
        {
            error = string.Empty;
            if (savedSlots == null || liveSlots == null)
            {
                error = "saved/live texture list is null";
                return false;
            }

            HashSet<int> savedTypes = new HashSet<int>();
            for (int index = 0; index < savedSlots.Count; index++)
            {
                data_girls.GirlData._textureAsset saved = savedSlots[index];
                int typeValue = saved == null ? -1 : (int)saved.type;
                if (saved == null || !IsSupportedSpriteType(typeValue) || !savedTypes.Add(typeValue))
                {
                    error = "saved entity contains a null, unsupported, or duplicate portrait slot";
                    return false;
                }
                if (string.IsNullOrEmpty(saved.asset_id))
                {
                    // There is no original identity to preserve. Do not fabricate one from
                    // the fallback asset, because that would canonize the wrong definition.
                    continue;
                }

                data_girls.girls._textureAsset live;
                if (!TryGetLiveSlot(liveSlots, saved.type, out live, out error))
                {
                    return false;
                }

                string liveId = live == null || live.asset == null ? string.Empty : live.asset.GetID();
                if (!string.Equals(liveId, saved.asset_id, StringComparison.Ordinal))
                {
                    observed.Add(
                        new ExternalPortraitIdentityRecordV1
                        {
                            entity_kind = kind,
                            entity_id = entityId,
                            sprite_type = typeValue,
                            asset_id = saved.asset_id
                        });
                }
            }

            return true;
        }

        private static void RewriteSavedDto(string kind)
        {
            SaveManager.SavedData target = GetTargetSavedData();
            if (target == null)
            {
                SetDiagnostic("A31 could not rewrite " + kind + " save DTO because SavedData is unavailable.");
                return;
            }

            List<ExternalPortraitIdentityRecordV1> snapshot = SnapshotKind(kind);
            int rewritten = 0;
            for (int index = 0; index < snapshot.Count; index++)
            {
                ExternalPortraitIdentityRecordV1 record = snapshot[index];
                data_girls.GirlData._textureAsset slot;
                string error;
                if (!TryGetOrCreateSavedSlotForRewrite(target, record, out slot, out error) || slot == null)
                {
                    SetDiagnostic("A31 could not rewrite unresolved portrait identity into save DTO: " + error);
                    continue;
                }

                slot.asset_id = record.asset_id;
                rewritten++;
            }

            Interlocked.Add(ref savedDtoRewriteCount, rewritten);
            if (rewritten > 0)
            {
                SetDiagnostic(
                    "A31 rewrote " + rewritten.ToString(CultureInfo.InvariantCulture) +
                    " unresolved " + kind + " portrait ID(s) into the vanilla save DTO.");
            }
        }

        private static bool TryGetOrCreateSavedSlotForRewrite(
            SaveManager.SavedData target,
            ExternalPortraitIdentityRecordV1 record,
            out data_girls.GirlData._textureAsset slot,
            out string error)
        {
            slot = null;
            error = string.Empty;
            if (target == null || record == null)
            {
                error = "target or record is null";
                return false;
            }

            List<data_girls.GirlData._textureAsset> slots;
            if (string.Equals(record.entity_kind, IdolKind, StringComparison.Ordinal))
            {
                data_girls.GirlData entity;
                if (!TryGetSavedGirl(target.data_girls__Girls, record.entity_id, out entity, out error))
                {
                    return false;
                }
                slots = entity.textureAssets;
            }
            else if (string.Equals(record.entity_kind, StaffKind, StringComparison.Ordinal))
            {
                staff.StaffData entity;
                if (!TryGetSavedStaff(target.staff__Staff, record.entity_id, out entity, out error))
                {
                    return false;
                }
                slots = entity.textureAssets;
            }
            else
            {
                error = "unsupported entity kind";
                return false;
            }

            if (slots == null)
            {
                error = "saved texture list is null";
                return false;
            }

            int matches = 0;
            for (int index = 0; index < slots.Count; index++)
            {
                data_girls.GirlData._textureAsset candidate = slots[index];
                if (candidate != null && (int)candidate.type == record.sprite_type)
                {
                    slot = candidate;
                    matches++;
                }
            }
            if (matches > 1)
            {
                slot = null;
                error = "sprite type occurs more than once in save DTO";
                return false;
            }
            if (matches == 1)
            {
                return true;
            }

            slot = new data_girls.GirlData._textureAsset
            {
                type = (data_girls_textures._spriteType)record.sprite_type,
                asset_id = record.asset_id
            };
            slots.Add(slot);
            return true;
        }

        private static bool TryGetSavedSlot(
            SaveManager.SavedData target,
            ExternalPortraitIdentityRecordV1 record,
            out data_girls.GirlData._textureAsset slot,
            out string error)
        {
            slot = null;
            error = string.Empty;
            if (target == null || record == null)
            {
                error = "target or record is null";
                return false;
            }

            if (string.Equals(record.entity_kind, IdolKind, StringComparison.Ordinal))
            {
                data_girls.GirlData entity;
                if (!TryGetSavedGirl(target.data_girls__Girls, record.entity_id, out entity, out error))
                {
                    return false;
                }
                return TryGetSavedTextureSlot(entity.textureAssets, record.sprite_type, out slot, out error);
            }

            if (string.Equals(record.entity_kind, StaffKind, StringComparison.Ordinal))
            {
                staff.StaffData entity;
                if (!TryGetSavedStaff(target.staff__Staff, record.entity_id, out entity, out error))
                {
                    return false;
                }
                return TryGetSavedTextureSlot(entity.textureAssets, record.sprite_type, out slot, out error);
            }

            error = "unsupported entity kind";
            return false;
        }

        private static bool TryGetSavedGirl(
            List<data_girls.GirlData> rows,
            int id,
            out data_girls.GirlData result,
            out string error)
        {
            result = null;
            error = string.Empty;
            if (rows == null)
            {
                error = "saved idol list is null";
                return false;
            }

            int matches = 0;
            for (int index = 0; index < rows.Count; index++)
            {
                data_girls.GirlData row = rows[index];
                if (row != null && row.id == id)
                {
                    result = row;
                    matches++;
                }
            }
            if (matches != 1)
            {
                error = "idol ID does not occur exactly once in target save";
                result = null;
                return false;
            }
            return true;
        }

        private static bool TryGetSavedStaff(
            List<staff.StaffData> rows,
            int id,
            out staff.StaffData result,
            out string error)
        {
            result = null;
            error = string.Empty;
            if (rows == null)
            {
                error = "saved staff list is null";
                return false;
            }

            int matches = 0;
            for (int index = 0; index < rows.Count; index++)
            {
                staff.StaffData row = rows[index];
                if (row != null && row.id == id)
                {
                    result = row;
                    matches++;
                }
            }
            if (matches != 1)
            {
                error = "staff ID does not occur exactly once in target save";
                result = null;
                return false;
            }
            return true;
        }

        private static bool TryGetSavedTextureSlot(
            List<data_girls.GirlData._textureAsset> slots,
            int spriteType,
            out data_girls.GirlData._textureAsset result,
            out string error)
        {
            result = null;
            error = string.Empty;
            if (slots == null)
            {
                error = "saved texture list is null";
                return false;
            }

            int matches = 0;
            for (int index = 0; index < slots.Count; index++)
            {
                data_girls.GirlData._textureAsset slot = slots[index];
                if (slot != null && (int)slot.type == spriteType)
                {
                    result = slot;
                    matches++;
                }
            }
            if (matches != 1)
            {
                error = "sprite type does not occur exactly once for the target entity";
                result = null;
                return false;
            }
            return true;
        }

        private static bool TryGetLiveSlot(
            List<data_girls.girls._textureAsset> slots,
            data_girls_textures._spriteType type,
            out data_girls.girls._textureAsset result,
            out string error)
        {
            result = null;
            error = string.Empty;
            int matches = 0;
            for (int index = 0; index < slots.Count; index++)
            {
                data_girls.girls._textureAsset slot = slots[index];
                if (slot != null && slot.type == type)
                {
                    result = slot;
                    matches++;
                }
            }
            if (matches != 1)
            {
                error = "loaded sprite type does not occur exactly once for the target entity";
                result = null;
                return false;
            }
            return true;
        }

        private static Dictionary<int, data_girls.girls> BuildLiveGirlMap(
            List<data_girls.girls> rows,
            out string error)
        {
            error = string.Empty;
            Dictionary<int, data_girls.girls> map = new Dictionary<int, data_girls.girls>();
            for (int index = 0; index < rows.Count; index++)
            {
                data_girls.girls row = rows[index];
                if (row == null || row.id < 0 || map.ContainsKey(row.id))
                {
                    error = "loaded idol list contains a null/invalid/duplicate ID";
                    return null;
                }
                map.Add(row.id, row);
            }
            return map;
        }

        private static Dictionary<int, staff._staff> BuildLiveStaffMap(
            List<staff._staff> rows,
            out string error)
        {
            error = string.Empty;
            Dictionary<int, staff._staff> map = new Dictionary<int, staff._staff>();
            for (int index = 0; index < rows.Count; index++)
            {
                staff._staff row = rows[index];
                if (row == null || row.id < 0 || map.ContainsKey(row.id))
                {
                    error = "loaded staff list contains a null/invalid/duplicate ID";
                    return null;
                }
                map.Add(row.id, row);
            }
            return map;
        }

        private static bool TryValidateRecordShape(
            ExternalPortraitIdentityRecordV1 record,
            out string key,
            out string error)
        {
            key = string.Empty;
            error = string.Empty;
            if (record == null ||
                (!string.Equals(record.entity_kind, IdolKind, StringComparison.Ordinal) &&
                 !string.Equals(record.entity_kind, StaffKind, StringComparison.Ordinal)) ||
                record.entity_id < 0 ||
                !IsSupportedSpriteType(record.sprite_type) ||
                string.IsNullOrEmpty(record.asset_id))
            {
                error = "record contains an unsupported kind, invalid stable ID/type, or empty original asset ID";
                return false;
            }

            key = BuildKey(record.entity_kind, record.entity_id, record.sprite_type);
            return true;
        }

        private static bool IsSupportedSpriteType(int value)
        {
            return value == (int)data_girls_textures._spriteType.body ||
                   value == (int)data_girls_textures._spriteType.hair ||
                   value == (int)data_girls_textures._spriteType.face ||
                   value == (int)data_girls_textures._spriteType.acc;
        }

        private static void ClearKind(string kind)
        {
            lock (Sync)
            {
                List<string> remove = new List<string>();
                foreach (KeyValuePair<string, ExternalPortraitIdentityRecordV1> pair in Unresolved)
                {
                    if (pair.Value != null && string.Equals(pair.Value.entity_kind, kind, StringComparison.Ordinal))
                    {
                        remove.Add(pair.Key);
                    }
                }
                for (int index = 0; index < remove.Count; index++)
                {
                    Unresolved.Remove(remove[index]);
                }
            }
        }

        private static List<ExternalPortraitIdentityRecordV1> SnapshotKind(string kind)
        {
            List<ExternalPortraitIdentityRecordV1> result = new List<ExternalPortraitIdentityRecordV1>();
            lock (Sync)
            {
                foreach (ExternalPortraitIdentityRecordV1 record in Unresolved.Values)
                {
                    if (record != null && string.Equals(record.entity_kind, kind, StringComparison.Ordinal))
                    {
                        result.Add(Clone(record));
                    }
                }
            }
            result.Sort(CompareRecords);
            return result;
        }

        private static ExternalPortraitIdentityRecordV1 Clone(ExternalPortraitIdentityRecordV1 record)
        {
            return new ExternalPortraitIdentityRecordV1
            {
                entity_kind = record == null ? string.Empty : record.entity_kind,
                entity_id = record == null ? -1 : record.entity_id,
                sprite_type = record == null ? -1 : record.sprite_type,
                asset_id = record == null ? string.Empty : record.asset_id
            };
        }

        private static int CompareRecords(ExternalPortraitIdentityRecordV1 left, ExternalPortraitIdentityRecordV1 right)
        {
            int kind = string.CompareOrdinal(left == null ? string.Empty : left.entity_kind, right == null ? string.Empty : right.entity_kind);
            if (kind != 0)
            {
                return kind;
            }
            int id = (left == null ? -1 : left.entity_id).CompareTo(right == null ? -1 : right.entity_id);
            if (id != 0)
            {
                return id;
            }
            return (left == null ? -1 : left.sprite_type).CompareTo(right == null ? -1 : right.sprite_type);
        }

        private static string BuildKey(string kind, int entityId, int spriteType)
        {
            return (kind ?? string.Empty) + ":" +
                entityId.ToString(CultureInfo.InvariantCulture) + ":" +
                spriteType.ToString(CultureInfo.InvariantCulture);
        }

        private static int CountIntersection(HashSet<string> first, HashSet<string> second)
        {
            int count = 0;
            foreach (string value in first)
            {
                if (second.Contains(value))
                {
                    count++;
                }
            }
            return count;
        }

        private static SaveManager.SavedData GetTargetSavedData()
        {
            try
            {
                mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
                return main == null ? null : main.GetSavedData();
            }
            catch (Exception exception)
            {
                SetDiagnostic("A31 could not resolve the target SavedData: " + exception.Message);
                return null;
            }
        }

        private static bool CaptureFailed(string diagnostic, out string error)
        {
            error = diagnostic ?? "unknown A31 capture failure";
            SetDiagnostic(error);
            return false;
        }

        private static void RecordInvalid(string diagnostic)
        {
            Interlocked.Increment(ref invalidSectionCount);
            SetDiagnostic(diagnostic);
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + diagnostic);
        }

        private static void SetDiagnostic(string value)
        {
            lock (Sync)
            {
                lastDiagnostic = value ?? string.Empty;
            }
        }
    }

    internal static class ExternalPortraitIdentityPatchHealth
    {
        private static int resolvedTargetMethodCount;
        private static int failureCount;
        private static string lastDiagnostic = string.Empty;

        internal static int ResolvedTargetMethodCount { get { return Volatile.Read(ref resolvedTargetMethodCount); } }
        internal static int FailureCount { get { return Volatile.Read(ref failureCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }
        internal static bool IsHealthy { get { return ResolvedTargetMethodCount == 4 && FailureCount == 0; } }

        internal static void ReportTargetResolved()
        {
            Interlocked.Increment(ref resolvedTargetMethodCount);
        }

        internal static void ReportFailure(string diagnostic)
        {
            Interlocked.Increment(ref failureCount);
            lastDiagnostic = diagnostic ?? string.Empty;
        }
    }
}
