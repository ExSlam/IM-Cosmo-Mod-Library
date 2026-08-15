# Faster Skip Dialogue

`Faster Skip Dialogue` upgrades Idol Manager's existing VN **Skip** button. It keeps the vanilla control and state indicator, but replaces the slow/seen-only skip coroutine with a fast runner that works through normal dialogue and data-driven mod cutscenes.

The intended behavior is:

- Press the vanilla **Skip** button once to arm fast skipping for the current VN/dialogue scene.
- Seen and unseen message nodes are both advanced.
- Typewriter text completes immediately.
- VN fades, actor movements, sprite fades, CG/BG transitions, transition delays, and other DOTween-driven presentation are accelerated.
- Fixed waits owned by `ActiveDialogueController` are compressed, including dramatic-CG holds and vanilla scripted VN timing such as `Finale_Rival_Script`.
- A choice pauses automatic advancement without turning Skip off. After the player selects a choice, fast skipping resumes automatically on the selected branch.
- Interactive VN popups are treated as choice-equivalent boundaries and also pause advancement until the popup is gone.
- Internal `instant_transition` dialogue changes keep the current fast-skip session armed. This is important for large data-driven cutscenes that split one scene across many dialogue IDs.
- When the VN scene actually calls `ActiveDialogueController.Hide()`, Skip is disarmed. The closing transition receives a bounded accelerated grace window, then the global tween speed is restored even if a post-dialogue chapter/title prompt remains visible.
- `ActiveDialogueController.Set()` and controller initialization perform defensive cleanup so the next dialogue/VN scene always starts with Skip off and can be enabled again by the player.

## Why vanilla Skip is not enough

Vanilla `ActiveDialogueController._Skip()` has three behaviors that make it unsuitable for this use case:

1. It waits `0.15` seconds between attempts.
2. It advances a message only when that node's unique ID is already present in `staticVars.Log` (unless debug mode is active), so unseen dialogue is protected from skipping.
3. It waits while `Transitioning` or `DisableClick` is set and does not accelerate the transition or timed hold that clears those gates.

Vanilla VN actions can also clear `ActiveDialogueController.Skip` themselves. In particular, `fade_to_black` sets Skip to false, and the `stop_skipping` action exists specifically to terminate vanilla skip. Some hard-coded `vn_actions.DoCustom` cinematics also force Skip off.

Faster Skip preserves those actions and cinematics but prevents them from silently disarming an already-armed Faster Skip session. A true `Hide()` scene end still disarms it.

## Technical design

### 1. Replace only the skip coroutine

The mod leaves `ActiveDialogueController.OnSkip()` untouched. Vanilla still toggles `ActiveDialogueController.Skip` and renders the Skip button state/color.

A Harmony postfix replaces the result of the private `_Skip()` iterator factory with `FasterSkipLoop.Run()`.

The replacement runner advances through `ActiveDialogueController.OnScreenClick()` rather than traversing the dialogue tree itself. This is deliberate: normal `Next()`, requirements, checks, actions, random nodes, logging, Harmony patches from other mods, `instant_transition`, and choice selection keep using the game's own execution path.

### 2. Choice-aware state machine

While Skip is armed, the runner has three effective phases:

- **Fast run:** accelerate presentation and feed normal screen-click advancement.
- **Interactive pause:** when `activeNode.type == choice` or `ActiveDialogueController.ShowingPopup` is true, stop feeding clicks and restore normal DOTween speed. Skip remains armed.
- **Scene ending:** `Hide()` turns the Skip flag off immediately but keeps the tween multiplier for a short grace period so closing fades finish quickly. Then all runtime state is restored.

Because the coroutine remains alive during a choice, `OnChoiceSelected()` changes `activeNode` normally and the runner sees the new non-choice node on the next frame, re-engages acceleration, and continues without another press of the Skip button.

### 3. Do not bypass sequencing gates

The runner never calls through these states:

- `activeNode == null`
- `Transitioning == true`
- `DisableClick == true`
- `ActiveDialogueController.IsLoadingBG == true`

Instead, it accelerates the mechanism responsible for completing the transition/hold and waits for the gate to clear.

The `IsLoadingBG` check is especially important for modded loose textures. Idol Manager may first attempt an Addressables key and then asynchronously fall back to `Mods.GetFilePaths(...)` for a mod PNG. Faster Skip does not advance through a chain of CG changes while that load is unresolved, so mod assets are not intentionally raced against one another.

### 4. Visual acceleration through DOTween

Most Idol Manager VN presentation uses DOTween: text reveal, actor movement, actor/sprite fades, background/CG fades, black overlays, transition vignettes, and many hard-coded cinematics.

While fast-run is active, the mod temporarily multiplies `DG.Tweening.DOTween.timeScale` by `30x`.

It records the previous DOTween scale and restores it at choices, manual Skip-off, and scene cleanup. If another mod changes `DOTween.timeScale` while Faster Skip is active, the runtime treats that new value as the baseline rather than blindly restoring an obsolete value.

The mod does **not** change `UnityEngine.Time.timeScale`.

### 5. Timed controller coroutine compression

DOTween acceleration does not affect `WaitForSeconds` directly. Instead of patching compiler-generated iterator classes such as `<EnableClick>d__77`, the mod discovers every instance method declared directly on `ActiveDialogueController` that returns `IEnumerator`, excluding `_Skip()` itself.

A Harmony postfix wraps the returned enumerator. While fast skip is active, yielded `WaitForSeconds` and `WaitForSecondsRealtime` objects are replaced with short realtime waits. Other yield instructions are passed through unchanged.

This means:

- dramatic-CG `EnableClick` holds are compressed;
- `WaitTillSpritesLoad` still waits for its `WaitUntil(CheckSprites)` predicate, but its fixed post-load pause is compressed;
- `Finale_Rival_Script` keeps all of its scripted `Next()` calls and state changes in the original order while its fixed waits become short;
- resource/predicate waits are not faked complete.

### 6. Scripted Skip resets

`vn_actions.DoMeta` is patched only to preserve an already-armed session across:

- `fade_to_black`
- `stop_skipping`

The original action still runs in full. Only the transient `Skip = false` side effect is restored afterward.

`vn_actions.DoCustom` is similarly guarded because several vanilla custom cinematics directly set Skip to false. If the custom sequence actually ends the VN by calling `Hide()`, the Hide patch marks the real scene end and the postfix does not re-arm Skip.

## Modded dialogue and EroEvents-style cutscenes

The compatibility target is any mod that uses Idol Manager's normal VN/data system: `data_dialogues`, `vn_actions`, `ActiveDialogueController`, normal choice nodes, and standard VN action parameters.

That includes the style used by EroEvents, which heavily relies on normal actions such as `set_cg`, `set_cg_dramatic`, `set_bg`, `transition`, sprite changes/fades, and `instant_transition`. Faster Skip does not need EroEvents-specific IDs or hard-coded dialogue names.

A mod that implements a completely separate cutscene engine outside `ActiveDialogueController` is outside this patch's scope. Its content is not automatically driven because it is not using the vanilla Skip button or VN controller in the first place.

## Compatibility choices

- No dependency on IM Data Core or IM UI Framework.
- No patch to dialogue JSON and no dependency on a specific dialogue mod.
- No synthetic choice selection. Choices remain entirely player-controlled.
- No direct calls to `data_dialogues.GetNextNode()` from the fast runner.
- No forced clearing of `Transitioning`, `DisableClick`, or `IsLoadingBG`.
- No Unity global timescale manipulation.
- Uses the existing vanilla Skip flag and button instead of adding another input or UI control.

## Tunable constants

The timing policy is centralized in `src/FasterSkipRuntime.cs`:

- `StepIntervalSeconds = 0.025f`
- `GatePollSeconds = 0.01f`
- `CompressedTimedYieldSeconds = 0.02f`
- `EndTransitionGraceSeconds = 0.25f`
- `TweenSpeedMultiplier = 30f`

These are source constants rather than player-facing settings in version 1.0.0.

## Build

This project follows the Cosmo Mod Library conventions and inherits the shared `Directory.Build.props` configuration.

Place the directory at:

```text
Cosmo-Mod-Library/
└── mods/
    └── Faster Skip Dialogue/
```

Build with:

```text
dotnet build "mods/Faster Skip Dialogue/Faster Skip Dialogue.csproj" -c Release
```

The project adds only the `DOTween.dll` reference that is not already supplied by the repository-level `Directory.Build.props`.
