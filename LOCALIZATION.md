# Localization Guide

Cosmo Mod Library localization follows the language selected in Idol Manager itself.

The shared localization code reads Idol Manager's current language (`staticVars.Settings.Language`), resolves compatible folder aliases, loads English as the fallback, and then overlays the best matching translation when one exists.

For implementation details, see [`mods/Mod Localization System/README.md`](mods/Mod%20Localization%20System/README.md).

## Current repository language folders

The established Cosmo localization folders are:

| Language | Repository folder |
| --- | --- |
| English | `en` |
| French | `fr` |
| Korean | `kr` |
| Chinese (game-compatible Simplified Chinese) | `cn` |
| Japanese | `jp` |
| Russian | `ru` |
| Portuguese (Brazil) | `ptbr` |

Keep those established names for existing bundled translations. Renaming only one mod from `jp` to `ja`, `cn` to `zh-Hans`, or `ptbr` to `pt-BR` creates two competing conventions without improving runtime behavior.

The resolver also understands common aliases and parent tags. Examples include:

- `ja` and `jp`
- `ko` and `kr`
- `zh-Hans` / Simplified Chinese and `cn`
- `zh-Hant` / Traditional Chinese forms
- `pt-BR` and `ptbr`
- regional tags such as `fr-CA`, which can fall back to `fr`

For a genuinely new language, use the exact language code selected by Idol Manager and provide a matching localization folder. A BCP 47-style tag such as `de`, `hr`, `fr-CA`, or `zh-Hant` is appropriate when you are creating a new custom language rather than renaming an established repository pack.

## How language selection works

Cosmo mods do not choose a language independently. To make a translation active:

1. Add or install the language in Idol Manager if the base game does not already provide it.
2. Select that language in Idol Manager's **Settings** menu.
3. Provide the matching Cosmo localization folder for the mod.
4. Restart the game if the language or font was added while the game was running.

The base game's language folders are under:

```text
Idol Manager\IM_Data\StreamingAssets\Languages
```

The game ships with legacy folder IDs including `en`, `jp`, `cn`, `ru`, and `ptbr`. A custom language folder must contain an `info.json` whose `ID` matches that folder name exactly so Idol Manager can select it.

Example custom German base-game folder:

```text
Idol Manager\IM_Data\StreamingAssets\Languages\de
```

Example `info.json`:

```json
{
  "ID": "de",
  "Language": "German",
  "Author": "Your Name",
  "Version": "1",
  "Font": "Linotte"
}
```

After adding a new custom language, select it manually in Idol Manager's Settings. First-launch Steam-language auto-selection is limited to the game's own hardcoded mappings and should not be treated as discovery for new custom languages.

## UI-string localization in Cosmo mods

Cosmo projects that contain `assets/Localization` compile the shared localization helper into their DLL. Their UI strings therefore retain English fallback and language resolution even when the standalone Mod Localization System is not installed.

The current projects with embedded localization assets include:

- Assistant Manager
- Cheats Mod
- Graduation Calendar
- Graduation Details
- Graduation Rebalances
- IM UI Framework
- Idol Career Diary
- Mod Buttons
- Monthly Ledger
- No Bullying Policy
- UI Recovery Tools
- Unavailable Idols Fix

String files use this layout:

```text
mods\<Mod Name>\assets\Localization\<language>\strings.txt
```

English is the required fallback:

```text
mods\<Mod Name>\assets\Localization\en\strings.txt
```

To add a translation:

1. Copy the mod's `assets/Localization/en/strings.txt`.
2. Create the target language folder.
3. Translate only the values on the right side of each `=`.
4. Keep the keys on the left unchanged.
5. Preserve numbered placeholders such as `{0}` exactly, moving them only as required by grammar.
6. Leave intentional empty values empty. An explicit empty translation is not the same as a missing key.

Missing translated keys fall back to English.

## Localized JSON assets

The standalone **Mod Localization System** is required for automatic language-specific JSON interception, especially for data-only mods.

Localized JSON copies live under `Localization/<language>/` while preserving the original relative path. Example:

```text
My Event Mod/
├── info.json
└── Localization/
    ├── en/
    │   └── JSON/
    │       └── Events/
    │           └── dialogues.json
    └── fr/
        └── JSON/
            └── Events/
                └── dialogues.json
```

`Localization/en/<relative path>` is required for each automatically localized JSON asset. The framework tries the selected Idol Manager language, compatible aliases/parent language folders, and finally English.

Localized JSON files are **whole-file replacements**, not key-level merges. Keep IDs, conditions, actions, and other non-text data synchronized across every language copy.

If a mod has only vanilla root `JSON/...` files and no matching English copy under `Localization/en/`, Mod Localization System leaves that mod alone rather than guessing that it is localized.

## Custom-language naming guidance

Use one convention consistently within a translation pack:

- For the repository's existing bundled languages, keep the established folder names (`jp`, `cn`, `ptbr`, `kr`, and so on).
- For a new custom language, use the language code Idol Manager actually selects and use that same code for the new mod folder when possible.
- Do not create duplicate alias folders solely because the resolver understands both spellings.

The loader's alias support is for compatibility, not an invitation to maintain duplicate copies of the same translation.

## Fonts and script support

A correct language code does not guarantee that every script renders correctly. Font coverage and text-layout support still come from Idol Manager/Unity.

The shipped game already demonstrates working Latin, Cyrillic, Japanese, and Chinese font paths. Cosmo's shared localization helper also attempts to register a Korean TextMeshPro fallback from installed Windows fonts such as `Malgun Gothic`, `NanumGothic`, or `Nanum Gothic` when Korean is selected.

Right-to-left and shaping-heavy scripts require more than glyph coverage. Do not assume a font alone will provide correct bidirectional layout or contextual shaping in Idol Manager's existing UI.

## Contributing a translation

When submitting a translation:

1. Start from the current English strings/JSON so every current key is represented.
2. Keep placeholders, IDs, conditions, actions, and non-text data intact.
3. Use the repository's established folder name for an existing bundled language, or a consistent new language tag for a genuinely new language.
4. Test with that language selected in Idol Manager itself.
5. Mention the language code and any known missing strings or font limitations in the pull request.

Repository: `https://github.com/ExSlam/IM-Cosmo-Mod-Library`
