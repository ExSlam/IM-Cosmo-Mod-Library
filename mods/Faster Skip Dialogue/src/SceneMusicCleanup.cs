using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DG.Tweening;
using HarmonyLib;
using UnityEngine;

namespace FasterSkipDialogue
{
    internal sealed class SceneMusicTicket
    {
        internal ActiveDialogueController Controller;
        internal bool WasSkipped;
        internal bool Cancelled;
    }

    internal static class SceneMusicCleanup
    {
        private const string PhantasmHandoffDialogue = "story_chapter_3_event_1";
        private const string CustomMusicDirectory = "Audio/Music";
        private const string CustomMusicExtension = ".wav";
        private static readonly FieldInfo BlockStopping = AccessTools.Field(typeof(MusicManager), "blockStopping");
        private static readonly MethodInfo PlayLoadedSong = AccessTools.Method(typeof(MusicManager), "_PlayStorySong");
        private static SceneMusicTicket current;

        internal static SceneMusicTicket Current { get { return current; } }

        internal static void BeginDialogue(ActiveDialogueController controller)
        {
            current = controller == null ? null : new SceneMusicTicket { Controller = controller };
        }

        internal static void Arm(ActiveDialogueController controller)
        {
            if (current == null || current.Controller != controller) BeginDialogue(controller);
            if (current != null) current.WasSkipped = true;
        }

        internal static void EndDialogue(ActiveDialogueController controller, MusicManager manager)
        {
            SceneMusicTicket ticket = current;
            if (ticket == null || ticket.Controller != controller || !ticket.WasSkipped || ticket.Cancelled) return;
            // Vanilla explicitly carries this track into gameplay via Resume_Phantasm.
            if (controller.dialogue != null && controller.dialogue.id == PhantasmHandoffDialogue) return;
            ticket.Cancelled = true;
            if (manager == null) return;

            // A pending _PlayStorySong fade has an OnComplete which starts another
            // track and re-arms blockStopping. Completing that tween, or merely
            // clearing the guard once, lets music restart after Hide/Resume.
            if (manager.StoryAudioSource != null)
            {
                DOTween.Kill(manager.StoryAudioSource, false);
                manager.StoryAudioSource.Stop();
                manager.StoryAudioSource.volume = 0f;
            }
            BlockStopping.SetValue(manager, false);
            manager.StopStorySong(false); // Also stops the separate Introloop player.
        }

        internal static IEnumerator RunDelayed(IEnumerator original, SceneMusicTicket ticket)
        {
            try
            {
                while ((ticket == null || !ticket.Cancelled) && original.MoveNext())
                    yield return original.Current;
            }
            finally
            {
                IDisposable disposable = original as IDisposable;
                if (disposable != null) disposable.Dispose();
            }
        }

        internal static async Task LoadCustomSong(MusicManager manager, SceneMusicTicket ticket,
            string id, bool loop, bool quickStart, bool sharpStart, float fadeInTime, float fadeTime)
        {
            // Same path precedence and decoder as vanilla PlayCustomSong. Carry
            // this request's ticket across await so an old WAV load cannot restart
            // music, even if a different dialogue has begun in the meantime.
            List<string> paths = Mods.GetFilePaths(Path.Combine(CustomMusicDirectory,
                id + CustomMusicExtension), true, true);
            foreach (string path in paths)
            {
                if (!File.Exists(path)) continue;
                AudioClip clip = await mainScript.LoadClip(path, true);
                if (manager != null && clip != null && !ticket.Cancelled)
                    PlayLoadedSong.Invoke(manager, new object[] { clip, loop, quickStart, sharpStart, fadeInTime, fadeTime });
                return;
            }
        }
    }

    [HarmonyPatch(typeof(MusicManager), "PlayStorySong_Delay")]
    internal static class DelayedStoryMusicScenePatch
    {
        private static void Postfix(ref IEnumerator __result)
        {
            __result = SceneMusicCleanup.RunDelayed(__result, SceneMusicCleanup.Current);
        }
    }

    [HarmonyPatch(typeof(MusicManager), "PlayCustomSong")]
    internal static class CustomStoryMusicScenePatch
    {
        private static bool Prefix(MusicManager __instance, string id, bool loop, bool quick_start,
            bool sharp_start, float fade_in_time, float fade_time, ref Task __result)
        {
            SceneMusicTicket ticket = SceneMusicCleanup.Current;
            if (ticket == null) return true;
            __result = SceneMusicCleanup.LoadCustomSong(__instance, ticket, id, loop,
                quick_start, sharp_start, fade_in_time, fade_time);
            return false;
        }
    }
}
