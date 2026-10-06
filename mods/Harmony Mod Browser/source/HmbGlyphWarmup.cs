using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HarmonyModBrowser
{
    // Warm only characters found in installed mod metadata. Small batches avoid
    // replacing first-render glyph stalls with one large synchronous atlas build.
    internal sealed class HmbGlyphWarmup : MonoBehaviour
    {
        private const int GlyphsPerBatch = 32;
        private const int ScanCharactersPerFrame = 512;
        private readonly Queue<string> pending = new Queue<string>();
        private readonly HashSet<string> queuedTexts = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<uint> attemptedGlyphs = new HashSet<uint>();
        private readonly HashSet<TMP_FontAsset> failedFonts = new HashSet<TMP_FontAsset>();
        private Coroutine work;
        private TMP_FontAsset primary;
        internal int WarmedBatchCount { get; private set; }

        internal static void QueueLoadedMods()
        {
            mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
            if (main == null || main.Data == null || Mods._Mods == null) return;
            HmbGlyphWarmup warmer = main.Data.GetComponent<HmbGlyphWarmup>();
            if (warmer == null) warmer = main.Data.AddComponent<HmbGlyphWarmup>();
            // Titles first, so the first visible lines are prepared ahead of long descriptions.
            foreach (Mods._mod mod in Mods._Mods)
                if (mod != null) warmer.Queue(mod.Title);
            foreach (Mods._mod mod in Mods._Mods)
            {
                if (mod == null) continue;
                warmer.Queue(mod.Author);
                warmer.Queue(mod.Version);
                warmer.Queue(mod.Description);
            }
            if (warmer.work == null && warmer.pending.Count > 0)
                warmer.work = warmer.StartCoroutine(warmer.Warm());
        }

        private void Awake() { Language.onReset += OnLanguageReset; }

        private void Queue(string text)
        {
            if (!string.IsNullOrEmpty(text) && queuedTexts.Add(text)) pending.Enqueue(text);
        }

        private IEnumerator Warm()
        {
            yield return null;
            while ((primary = HmbGameFont.GetCurrent()) == null) yield return null;
            List<uint> batch = new List<uint>(GlyphsPerBatch);
            int scanned = 0;
            while (pending.Count > 0)
            {
                string text = pending.Dequeue();
                for (int index = 0; index < text.Length; index++)
                {
                    char character = text[index];
                    uint codepoint = character;
                    if (char.IsHighSurrogate(character) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
                        codepoint = (uint)char.ConvertToUtf32(character, text[++index]);
                    else if (char.IsSurrogate(character) || char.IsControl(character))
                        continue;
                    if (attemptedGlyphs.Add(codepoint)) batch.Add(codepoint);
                    scanned++;
                    if (batch.Count >= GlyphsPerBatch || scanned >= ScanCharactersPerFrame)
                    {
                        WarmBatch(batch);
                        batch.Clear();
                        scanned = 0;
                        yield return null;
                    }
                }
            }
            WarmBatch(batch);
            work = null;
        }

        private void WarmBatch(List<uint> batch)
        {
            if (batch.Count == 0 || primary == null) return;
            List<TMP_FontAsset> fonts = new List<TMP_FontAsset>();
            HashSet<TMP_FontAsset> visited = new HashSet<TMP_FontAsset>();
            AddFont(primary, fonts, visited);
            if (TMP_Settings.fallbackFontAssets != null)
                foreach (TMP_FontAsset font in TMP_Settings.fallbackFontAssets) AddFont(font, fonts, visited);
            AddFont(TMP_Settings.defaultFontAsset, fonts, visited);
            uint[] missing = batch.ToArray();
            foreach (TMP_FontAsset font in fonts)
            {
                if (missing.Length == 0) break;
                List<uint> unresolved = new List<uint>();
                foreach (uint glyph in missing)
                    if (!font.characterLookupTable.ContainsKey(glyph)) unresolved.Add(glyph);
                missing = unresolved.ToArray();
                if (missing.Length == 0 || font.atlasPopulationMode != AtlasPopulationMode.Dynamic || failedFonts.Contains(font))
                    continue;
                try
                {
                    uint[] stillMissing;
                    font.TryAddCharacters(missing, out stillMissing, false);
                    missing = stillMissing ?? new uint[0];
                }
                catch (Exception error)
                {
                    failedFonts.Add(font);
                    HmbLog.Warning("Glyph warming skipped font '" + font.name + "': " + error.Message);
                }
            }
            WarmedBatchCount++;
        }

        private static void AddFont(TMP_FontAsset font, List<TMP_FontAsset> fonts, HashSet<TMP_FontAsset> visited)
        {
            if (font == null || !visited.Add(font)) return;
            fonts.Add(font);
            if (font.fallbackFontAssetTable != null)
                foreach (TMP_FontAsset fallback in font.fallbackFontAssetTable) AddFont(fallback, fonts, visited);
        }

        private void OnLanguageReset()
        {
            if (work != null) StopCoroutine(work);
            work = null;
            primary = null;
            pending.Clear();
            queuedTexts.Clear();
            attemptedGlyphs.Clear();
            failedFonts.Clear();
            QueueLoadedMods();
        }

        private void OnDestroy()
        {
            Language.onReset -= OnLanguageReset;
            if (work != null) StopCoroutine(work);
        }
    }
}
