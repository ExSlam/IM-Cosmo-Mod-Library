using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RoomAssignmentFix
{
    /// <summary>
    /// Automatic-room paths can reach agency._room.assign directly. Keep an idol owned by at
    /// most one room even when a caller bypasses DragAndDropManager's availability checks.
    /// </summary>
    internal static class IdolAssignmentGuard
    {
        internal static bool IsAvailableForRoom(agency._room room, data_girls.girls girl)
        {
            if (room == null || girl == null)
            {
                return false;
            }

            if (girl.room != null && girl.room != room)
            {
                if (IsActivelyOwnedByRoom(girl.room, girl))
                {
                    return false;
                }

                ReleaseStaleRoomPointer(girl);
            }

            if (girl.status == data_girls._status.normal || girl.status == data_girls._status.scene)
            {
                return true;
            }

            // Vanilla considers an announced graduate inactive. Unavailable Idols Fix
            // deliberately changes IsActive() to true until graduation is finalized. Keying
            // compatibility to the effective result avoids a hard dependency and does not
            // change standalone Room Assignment Fix behavior.
            if (girl.status == data_girls._status.announced_graduation && girl.IsActive())
            {
                return true;
            }

            // Doctors' offices are the vanilla exception: they must be able to receive an
            // injured or depressed idol, provided she is not already assigned to another room.
            return room.type == agency._type.doctorsOffice && girl.IsSick();
        }

        internal static bool IsAvailableForAmbientTheater(data_girls.girls girl)
        {
            return girl != null &&
                   girl.status == data_girls._status.normal &&
                   girl.room == null;
        }

        private static bool IsActivelyOwnedByRoom(agency._room owner, data_girls.girls girl)
        {
            if (owner.girl == girl && owner.status != agency._room._status.normal)
            {
                return true;
            }

            if (owner.type == agency._type.cafeAndShop && girl.status == data_girls._status.practice)
            {
                Cafes._cafe cafe = owner.GetCafe();
                if ((cafe != null && cafe.WorkingGirls.Contains(girl)) ||
                    owner.cafe_girl_1 == girl ||
                    owner.cafe_girl_2 == girl ||
                    owner.cafe_girl_3 == girl)
                {
                    return true;
                }
            }

            // Practice and scene are unavailable states even if another vanilla state bug has
            // left the owning room's reverse pointer incomplete.
            return girl.status == data_girls._status.practice ||
                   girl.status == data_girls._status.scene;
        }

        private static void ReleaseStaleRoomPointer(data_girls.girls girl)
        {
            agency._room owner = girl.room;
            if (owner == null)
            {
                return;
            }

            if (owner.girl == girl && owner.status == agency._room._status.normal)
            {
                owner.girl = null;
            }

            if (owner.type == agency._type.cafeAndShop)
            {
                Cafes._cafe cafe = owner.GetCafe();
                if (cafe != null)
                {
                    cafe.WorkingGirls.Remove(girl);
                }
                if (owner.cafe_girl_1 == girl)
                {
                    owner.cafe_girl_1 = null;
                }
                if (owner.cafe_girl_2 == girl)
                {
                    owner.cafe_girl_2 = null;
                }
                if (owner.cafe_girl_3 == girl)
                {
                    owner.cafe_girl_3 = null;
                }
            }

            girl.room = null;
        }
    }

    /// <summary>
    /// Vanilla room progress is already game-time based through TimeInterval. The visual
    /// mismatch comes from real-time DOTween fades/pop-ins that keep running at normal
    /// wall-clock speed while Tel's FastForward advances the game clock beyond vanilla
    /// fast speed.
    /// </summary>
    internal static class RoomVisualTiming
    {
        private const double VanillaFastMinutesPerSecond = 200.0;
        private const float MinimumTweenSeconds = 0.05f;

        internal static bool IsSuperFast()
        {
            return staticVars.dateTimeAddMinutesPerSecond > VanillaFastMinutesPerSecond;
        }

        internal static float ScaleTweenSeconds(float seconds)
        {
            if (seconds <= 0f || !IsSuperFast())
            {
                return seconds;
            }

            double currentSpeed = staticVars.dateTimeAddMinutesPerSecond;
            if (currentSpeed <= VanillaFastMinutesPerSecond)
            {
                return seconds;
            }

            float scaled = (float)(seconds * VanillaFastMinutesPerSecond / currentSpeed);
            return Math.Max(MinimumTweenSeconds, Math.Min(seconds, scaled));
        }
    }

    /// <summary>
    /// Theater and recreation-room scenes are visual systems, not room assignments. They
    /// can keep showing an idol after she has been assigned elsewhere because their sprites
    /// are rendered once and then fade out on real-time tweens. Tel's Fast Forward makes
    /// that desynchronization much more visible, so keep those ambient visuals tied to the
    /// current authoritative idol state.
    /// </summary>
    internal static class AmbientRoomVisualGuard
    {
        private static readonly FieldInfo StatusButtonGirlField =
            AccessTools.Field(typeof(StatusButton), "Girl");

        internal static void ReconcileAllTheaters()
        {
            mainScript main = Camera.main != null ? Camera.main.GetComponent<mainScript>() : null;
            if (main == null || main.Data == null)
            {
                return;
            }

            agency agencyComponent = main.Data.GetComponent<agency>();
            if (agencyComponent == null)
            {
                return;
            }

            List<agency._room> rooms = agencyComponent.allRooms(true, true);
            foreach (agency._room room in rooms)
            {
                if (room == null || room.type != agency._type.theatre || room.roomObj == null)
                {
                    continue;
                }

                Room_Theater theaterRoom = room.roomObj.GetComponent<Room_Theater>();
                ReconcileTheater(theaterRoom);
            }
        }

        internal static void ReconcileTheater(Room_Theater theaterRoom)
        {
            if (theaterRoom == null || theaterRoom.Sprites == null)
            {
                return;
            }

            Groups._group group = theaterRoom.GetGroup();
            HashSet<data_girls.girls> used = new HashSet<data_girls.girls>();
            foreach (Room_Theater.Room_Sprite sprite in theaterRoom.Sprites)
            {
                if (!IsIdolTheaterSprite(sprite))
                {
                    continue;
                }

                ReconcileTheaterSprite(sprite, group, used);
            }
        }

        internal static void ReconcileTheaterContaining(StatusButton statusButton)
        {
            if (statusButton == null)
            {
                return;
            }

            Room_Theater theaterRoom = statusButton.GetComponentInParent<Room_Theater>();
            if (theaterRoom != null)
            {
                ReconcileTheater(theaterRoom);
            }
        }

        internal static void HideImmediately(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            CanvasGroup canvas = obj.GetComponent<CanvasGroup>();
            if (canvas != null)
            {
                canvas.alpha = 0f;
            }
            obj.SetActive(false);
        }

        private static bool IsIdolTheaterSprite(Room_Theater.Room_Sprite sprite)
        {
            if (sprite == null || sprite.Obj == null || sprite.StatusButton == null)
            {
                return false;
            }

            return sprite.Type == Room_Theater.Room_Sprite._type.performance ||
                   sprite.Type == Room_Theater.Room_Sprite._type.manzai;
        }

        private static void ReconcileTheaterSprite(
            Room_Theater.Room_Sprite sprite,
            Groups._group group,
            HashSet<data_girls.girls> used)
        {
            if (sprite.Obj == null || !sprite.Obj.activeInHierarchy || sprite.StatusButton == null)
            {
                return;
            }

            StatusButton button = sprite.StatusButton.GetComponent<StatusButton>();
            data_girls.girls current = GetButtonGirl(button);
            if (IdolAssignmentGuard.IsAvailableForAmbientTheater(current) && !used.Contains(current))
            {
                used.Add(current);
                return;
            }

            data_girls.girls replacement = FindAmbientTheaterReplacement(group, used);
            if (replacement == null)
            {
                sprite.StatusButton.SetActive(false);
                return;
            }

            sprite.StatusButton.SetActive(true);
            button.Set(replacement);
            used.Add(replacement);
        }

        private static data_girls.girls FindAmbientTheaterReplacement(
            Groups._group group,
            HashSet<data_girls.girls> used)
        {
            if (group == null)
            {
                return null;
            }

            List<data_girls.girls> girls = group.GetGirls(true, true, null);
            foreach (data_girls.girls girl in girls)
            {
                if (IdolAssignmentGuard.IsAvailableForAmbientTheater(girl) && !used.Contains(girl))
                {
                    return girl;
                }
            }
            return null;
        }

        private static data_girls.girls GetButtonGirl(StatusButton button)
        {
            if (button == null || StatusButtonGirlField == null)
            {
                return null;
            }

            try
            {
                return StatusButtonGirlField.GetValue(button) as data_girls.girls;
            }
            catch
            {
                return null;
            }
        }
    }

    [HarmonyPatch(typeof(Room), nameof(Room.onMouseUp))]
    internal static class ObsoleteRoomMouseUpAssignmentPatch
    {
        private static bool Prefix()
        {
            // Room.onMouseUp belongs to the game's obsolete drag path. It always looks up
            // staticVars.dragAndDrop_id, but the current GirlDraggable/DragAndDropManager path
            // never writes that field. It therefore remains zero and can assign idol 0 instead
            // of the idol being dragged, consuming a clinic before the real drop is processed.
            // DragAndDropManager.OnMouseUp is the canonical path and performs canAssign/canTrain
            // checks before calling agency._room.assign with the actual dragged idol.
            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.assign), new Type[] { typeof(data_girls.girls), typeof(Nullable<data_girls._paramType>) })]
    internal static class RoomAssignIdolOwnershipGuardPatch
    {
        private static bool Prefix(agency._room __instance, data_girls.girls _girl)
        {
            return IdolAssignmentGuard.IsAvailableForRoom(__instance, _girl);
        }

        private static void Postfix(data_girls.girls _girl)
        {
            if (_girl != null)
            {
                AmbientRoomVisualGuard.ReconcileAllTheaters();
            }
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.SetStatus))]
    internal static class GirlStatusAmbientVisualReconcilePatch
    {
        private static void Postfix()
        {
            AmbientRoomVisualGuard.ReconcileAllTheaters();
        }
    }

    [HarmonyPatch(typeof(StatusButton), nameof(StatusButton.Set), new Type[] { typeof(data_girls.girls) })]
    internal static class TheaterStatusButtonSetPatch
    {
        private static void Postfix(StatusButton __instance)
        {
            AmbientRoomVisualGuard.ReconcileTheaterContaining(__instance);
        }
    }

    [HarmonyPatch(typeof(Room_Theater), "Render_Performance")]
    internal static class TheaterRenderPerformanceVisualGuardPatch
    {
        private static void Postfix(Room_Theater __instance)
        {
            AmbientRoomVisualGuard.ReconcileTheater(__instance);
        }
    }

    [HarmonyPatch(typeof(Room_Theater), "Render_Manzai")]
    internal static class TheaterRenderManzaiVisualGuardPatch
    {
        private static void Postfix(Room_Theater __instance)
        {
            AmbientRoomVisualGuard.ReconcileTheater(__instance);
        }
    }

    [HarmonyPatch(typeof(Room), nameof(Room.RemoveSceneSprite))]
    internal static class SceneSpriteImmediateHidePatch
    {
        private static void Prefix(List<GameObject> ___girlSprite)
        {
            if (___girlSprite == null)
            {
                return;
            }

            foreach (GameObject sprite in ___girlSprite)
            {
                AmbientRoomVisualGuard.HideImmediately(sprite);
            }
        }
    }

    [HarmonyPatch(typeof(Room), nameof(Room.RemoveSpriteGirl))]
    internal static class RoomGirlSpriteImmediateHidePatch
    {
        private static void Prefix(GameObject ___GirlSprite)
        {
            AmbientRoomVisualGuard.HideImmediately(___GirlSprite);
        }
    }

    [HarmonyPatch(typeof(AgencySprite), nameof(AgencySprite.Hide))]
    internal static class AgencySpriteHideFastForwardTimingPatch
    {
        private static void Prefix(ref float duration, ref float delay)
        {
            duration = RoomVisualTiming.ScaleTweenSeconds(duration);
            delay = RoomVisualTiming.ScaleTweenSeconds(delay);
        }
    }

    [HarmonyPatch(typeof(AgencySprite), nameof(AgencySprite.Show))]
    internal static class AgencySpriteShowFastForwardTimingPatch
    {
        private static bool Prefix(AgencySprite __instance)
        {
            if (!RoomVisualTiming.IsSuperFast())
            {
                return true;
            }

            CanvasGroup canvas = __instance.GetComponent<CanvasGroup>();
            if (canvas != null)
            {
                canvas.alpha = 1f;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Room), nameof(Room.PopIn))]
    internal static class RoomPopInFastForwardTimingPatch
    {
        private static bool Prefix(Room __instance)
        {
            if (!RoomVisualTiming.IsSuperFast())
            {
                return true;
            }

            RectTransform transform = __instance.GetComponent<RectTransform>();
            if (transform != null)
            {
                transform.localScale = Vector2.one;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(DragAndDropManager), nameof(DragAndDropManager.OnMouseUp),
        new Type[] { typeof(data_girls.girls), typeof(staticVars.DragAndDrop), typeof(UnityEngine.GameObject) })]
    internal static class ClinicRecoverySlotDropFallbackPatch
    {
        private struct DropState
        {
            internal agency._room Room;
            internal data_girls.girls Girl;
            internal data_girls._paramType Parameter;
        }

        [HarmonyPrefix]
        private static void Prefix(
            data_girls.girls girl,
            staticVars.DragAndDrop type,
            agency._room ___hovering_room,
            out DropState __state)
        {
            __state = new DropState();
            if (type != staticVars.DragAndDrop.room || girl == null || ___hovering_room == null ||
                ___hovering_room.type != agency._type.doctorsOffice ||
                ___hovering_room.hovering_style == null)
            {
                return;
            }

            data_girls._paramType parameter = ___hovering_room.hovering_style.Value;
            if (parameter != data_girls._paramType.physicalStamina &&
                parameter != data_girls._paramType.mentalStamina)
            {
                return;
            }

            __state.Room = ___hovering_room;
            __state.Girl = girl;
            __state.Parameter = parameter;
        }

        [HarmonyPostfix]
        private static void Postfix(DropState __state)
        {
            agency._room room = __state.Room;
            data_girls.girls girl = __state.Girl;
            if (room == null || girl == null || room.girl == girl || room.girl != null ||
                room.status != agency._room._status.normal)
            {
                return;
            }

            data_girls._paramType? parameter = new data_girls._paramType?(__state.Parameter);
            if (room.canAssign(girl, parameter) && room.canTrain(girl, parameter))
            {
                room.assign(girl, parameter);
            }
        }
    }
}
