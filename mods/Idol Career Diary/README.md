# Idol Career Diary

`Idol Career Diary` adds a profile-integrated timeline view that turns IM Data Core events into readable career history.

## Dependencies

- `IM Data Core` (`com.cosmo.imdatacore`) version `1.0.0` or higher
- `IM UI Framework` (`com.cosmo.imuiframework`) version `1.0.0` or higher

This mod does not ship a separate persistence backend. It reads timeline events from IM Data Core and renders UI with IM UI Framework.

## Player-facing behavior

- Adds a dedicated career diary view in idol profile flow.
- Reads recent timeline events for the selected idol.
- Groups and formats event types (career, contracts, singles, shows, finance, relationships, and more).
- Searches player-facing timeline text and event payload keywords live from the timeline toolbar.
- Shows complete election rankings (including votes and points), including rankings extended by Tel's Extended SSK.
- Promotes election-attached single and concert events into the idol timeline and links to them from election details.
- Keeps timeline rendering stable when dependencies are present; shows dependency errors when required mods are missing.
- Lets content-only mods override diary text by shipping JSON files in an `Idol Career Diary` folder.

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

Supported text fields: `title`, `with_whom`, `description` or `details`, `outcome_lines`.

Supported general tokens: `{idols}`, `{idol}`, `{focused_idol}`, `{story}`, `{substory}`, `{parent_story}`, `{action}`.

Supported actor tokens:

- `{girl1}`, `{girl2}`, `{girl3}` when those actor tags exist in the captured story event
- `{girl1_possessive}` or `{girl1's}` for possessive prose
- `{actor:girl1}` and `{actor:girl1:possessive}` as explicit actor-tag forms
- `{idol1}`, `{idol2}`, `{idol3}` for first/second/third idol actors in capture order

Harmony/API mods that append events through IM Data Core are also attributed when the event namespace or source hook matches an installed mod's `info.json` `HarmonyID`, DLL name, folder name, or title.

## Installation

1. Install `IM Data Core` first.
2. Install `IM UI Framework` second.
3. Install `Idol Career Diary`.
4. Launch game and open an idol profile to verify diary UI appears.

## 1.0 release contract

- Runtime behavior and user-facing diary feature set are considered stable in `1.x`.
- Dependency requirement remains hard: missing IM Data Core or IM UI Framework is an install error.
- Save compatibility is inherited from IM Data Core event schema and namespace usage.

## Troubleshooting

- If diary UI is missing, confirm both dependency mods are installed and loaded.
- If timeline is empty on older saves, continue gameplay to generate new captured events.
- If dependency errors appear, check `info.json` Harmony IDs and matching DLL names in mod folders.

## Build

Project file:
- `mods/Idol Career Diary/Idol Career Diary.csproj`

Example command:
- `dotnet build "mods/Idol Career Diary/Idol Career Diary.csproj" -c Release`
