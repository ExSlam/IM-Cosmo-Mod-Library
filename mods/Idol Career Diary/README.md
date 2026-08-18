# Idol Career Diary

`Idol Career Diary` adds a profile-integrated timeline view that turns IM Data Core events into readable career history.

## Dependencies

- `IM Data Core` (`com.cosmo.imdatacore`) version `3.2.0` or higher
- `IM UI Framework` (`com.cosmo.imuiframework`) version `2.0.3` or higher

This mod does not ship a separate persistence backend. It reads timeline events from IM Data Core and renders UI with IM UI Framework.

Idol Career Diary uses IM Data Core's public API only. With IM Data Core 3.2,
entity identifiers are read from the event's `EntityId`, and new timeline data
uses the canonical `single_released`, `show_episode_released`,
`contract_cancelled`, and `idol_status_changed` event names. Readers for their
old aliases remain so imported or third-party events can still be displayed.

The last-selected diary entry is supplemental state stored through IM Data
Core. It is immediately visible in the active session and becomes durable at
the next vanilla save boundary, following IM Data Core 3.2's persistence model.

## Player-facing behavior

- Adds a dedicated career diary view in idol profile flow.
- Loads the selected idol's timeline in cursor-based pages: short saves stay on the first 500-row page, while Show More fetches older pages only when needed.
- A committed search automatically walks remaining pages cooperatively across Unity frames so old diary entries remain searchable without blocking one frame or rendering the whole career at once.
- Groups and formats event types (career, contracts, singles, shows, finance, relationships, and more).
- Reconstructs released-single senbatsu from recorded slot order, so later graduation does not remove an idol from that historical formation.
- Searches player-facing timeline text and event payload keywords from the timeline toolbar, with a cached per-event search corpus so repeated long-save searches avoid rebuilding presentation text.
- Shows complete election rankings (including votes and points), including rankings extended by Tel's Extended SSK.
- Promotes election-attached single and concert events into the idol timeline and links to them from election details.
- Keeps timeline rendering stable when dependencies are present; shows dependency errors when required mods are missing.
- Lets content-only mods override diary text by shipping JSON files in an `Idol Career Diary` folder.

## Timeline UI and long-save behavior

The diary keeps the vanilla-cloned profile panel as the single scroll owner. Search controls are laid out as a full-width input with a separate shrinkable action row, so narrow profile widths do not force the text field and two buttons to compete on one horizontal line. Result content continues to use `VerticalLayoutGroup` plus `ContentSizeFitter`, allowing the inherited vanilla `ScrollRect` to calculate height normally.

Only the visible result window is instantiated as Unity UI rows (300 initially, +100 through Show More). Loading/searching older IMDC pages therefore increases data available to the diary without creating thousands of off-screen buttons. Search indexing is query-independent and cached per event; on very long saves it is warmed in batches across frames.

## Custom Diary Entries For Content Mods

JSON-only mods can add player-facing diary text without Harmony or an IM Data Core DLL reference.
The diary infers the source mod from the same folder's `info.json` and displays it as `From mod: <Title>`.

Put one or more `.json` files in either folder inside your mod:

- `Idol Career Diary`
- `IdolCareerDiary`

Example:

```json
{
  "entries": [
    {
      "event_types": ["substory_completed"],
      "substory_ids": ["ee_culture_1a"],
      "title": "Personal Story Completed",
      "with_whom": "{idols}",
      "description": "{idols} finished {story}.",
      "outcome_lines": ["Story progress recorded for {focused_idol}."]
    },
    {
      "event_types": ["substory_completed"],
      "substory_ids": ["party_after_rehearsal"],
      "title": "Party Together",
      "with_whom": "{idols}",
      "description": "{idols} went to a party together."
    },
    {
      "event_types": ["substory_completed"],
      "substory_ids": ["house_invite"],
      "title": "Visit After Work",
      "with_whom": "{girl2}",
      "description": "{girl1} invited {girl2} over to {girl1_possessive} house."
    }
  ]
}
```

Supported match fields: `event_type`, `event_types`, `entity_kind`, `entity_id`, `entity_ids`, `substory_id`, `substory_ids`, `substory_id_prefix`, `substory_id_prefixes`.

When multiple custom entries match one event, selection is deterministic: exact entity/substory IDs outrank prefixes, longer matching prefixes outrank shorter prefixes, constrained event type/entity-kind rules add specificity, and remaining ties are resolved by source mod title, JSON file path, then entry index. Filesystem enumeration order does not decide the displayed text.

Supported text fields: `title`, `with_whom`, `description` or `details`, `outcome_lines`.

Supported general tokens: `{idols}`, `{idol}`, `{focused_idol}`, `{story}`, `{substory}`, `{parent_story}`, `{action}`.

Supported actor tokens:

- `{girl1}`, `{girl2}`, `{girl3}` when those actor tags exist in the captured story event
- `{girl1_possessive}` or `{girl1's}` for possessive prose
- `{actor:girl1}` and `{actor:girl1:possessive}` as explicit actor-tag forms
- `{idol1}`, `{idol2}`, `{idol3}` for first/second/third idol actors in capture order

Harmony/API mods that append events through IM Data Core are also attributed when the event namespace or source hook matches an installed mod's `info.json` `HarmonyID`, DLL name, folder name, or title.

## Installation

1. Install `IM Data Core` 3.2 or newer first.
2. Install `IM UI Framework` second.
3. Install `Idol Career Diary`.
4. Launch game and open an idol profile to verify diary UI appears.

## 1.0 release contract

- Runtime behavior and user-facing diary feature set are considered stable in `1.x`.
- Dependency requirement remains hard: missing IM Data Core or IM UI Framework is an install error.
- Timeline and supplemental-state rollback follow the vanilla save selected by IM Data Core 3.2.

## Troubleshooting

- If diary UI is missing, confirm both dependency mods are installed and loaded.
- If timeline is empty on older saves, continue gameplay to generate new captured events.
- If dependency errors appear, check `info.json` Harmony IDs and matching DLL names in mod folders.

## Build

Project file:
- `mods/Idol Career Diary/Idol Career Diary.csproj`

Example command:
- `dotnet build "mods/Idol Career Diary/Idol Career Diary.csproj" -c Release`


## 1.2.2 election numbering

- Election labels now always use IM Data Core's persisted `election_number`, never the event `EntityId`/vanilla `_SSK.ID`.
- Removed the partial-cache ordinal clamp that could turn a later election into `Election #1` when older diary pages had not been loaded yet.
- Rows missing `election_number` fall back to another row for the same election or to vanilla `_SSK.Count` / `SEvent_SSK.CountElections() + 1`; the number of loaded diary pages no longer affects election numbering.

## 1.2.1 font consistency

Timeline toolbar buttons and the manually-created TMP search field now ask IM UI Framework 2.0.3 to apply Idol Manager's currently selected game font after they are constructed. This fixes the mixed-font timeline UI that could otherwise leave framework/fallback button labels and search text on a MUIP/TMP default while the rest of the diary followed the game font.
