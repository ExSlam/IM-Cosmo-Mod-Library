# Localization Guide

This guide explains how to create a new translation for both the base game `Idol Manager` and the `Idol Career Diary` mod, including languages that are not shipped with the game by default.

`Idol Career Diary` uses the language currently selected in the base game. If the base game does not have that language installed and selected, the mod will not switch to it.

## 1. Find the `Idol Manager` install folder

The default Steam install path is:

```text
C:\Program Files (x86)\Steam\steamapps\common\Idol Manager
```

If your Steam library is in a different location:

1. Open Steam.
2. Open your Library.
3. Right-click `Idol Manager`.
4. Click `Manage`.
5. Click `Browse local files`.

That opens the game install folder.

## 2. Important rule

To make `Idol Career Diary` use a language, you must do both of these:

1. Add that language to the base game and select it in the game's `Settings` menu.
2. Add the matching translation file to `Idol Career Diary`.

The base game settings menu discovers languages by scanning:

```text
Idol Manager\IM_Data\StreamingAssets\Languages
```

The base game does not automatically detect arbitrary custom languages on first launch. New languages such as German, Croatian, Latvian, or custom `zh-Hans` / `zh-Hant` folders must be selected manually in the `Settings` menu after you add them.

## 3. Folder naming standard

For new translations, use BCP 47 language tags.

Examples:

| Language name in English | Recommended folder name |
| --- | --- |
| English | `en` |
| German | `de` |
| Croatian | `hr` |
| Japanese | `ja` |
| Russian | `ru` |
| Korean | `ko` |
| Portuguese (Brazil) | `pt-BR` |
| Chinese, Simplified | `zh-Hans` |
| Chinese, Traditional | `zh-Hant` |
| Spanish (Latin America) | `es-419` |

Formatting rules:

- language subtag: lowercase, for example `en`, `de`, `hr`, `lv`, `zh`
- script subtag: Title Case, for example `Hans`, `Hant`, `Latn`, `Cyrl`
- region subtag: uppercase, for example `US`, `BR`, `TW`

Chinese note:

- `Hans` and `Hant` are script codes, not language names.
- `zh-Hans` means Chinese written in Simplified characters.
- `zh-Hant` means Chinese written in Traditional characters.

Use these references when you need the correct code for a language or script:

- IANA Script codes: [Language and script code examples, including `zh-Hans` and `zh-Hant`](https://www.iana.org/help/idn-repository-procedure)
- Unicode CLDR: [Languages and Scripts table](https://www.unicode.org/cldr/charts/latest/supplemental/languages_and_scripts.html)
- IANA Language Codes: [Language Subtag Registry](https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry)

Important details:

- The folder name and the base game's `info.json` `ID` must match exactly.
- For new translations submitted to this repository, use the recommended BCP 47 folder names shown in this guide.
- The base game ships with a few older folder names such as `jp`, `cn`, and `ptbr`. Those are legacy game folders, not the naming standard for new mod translation pull requests.

## 4. Add the language to the base game

Open this folder inside the `Idol Manager` install directory:

```text
Idol Manager\IM_Data\StreamingAssets\Languages
```

You will see the shipped base game language folders there, such as `en`, `jp`, `cn`, `ru`, and `ptbr`.

### Step-by-step

1. Copy the existing language folder that is closest to your target language.
2. Paste it in the same `Languages` directory.
3. Rename the copied folder to your target language code.
4. Open the copied folder and edit `info.json`.
5. Change the `ID` value so it exactly matches the folder name.
6. Change the `Language` value to the display name you want the game to show in the settings menu.
7. Translate the JSON files under the `JSON` subfolder.
8. Start the game.
9. Open `Settings`.
10. Select your new language from the language list.

Example for a German base-game language folder:

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

Notes:

- For Latin-alphabet languages, `Linotte` is a reasonable starting point because the shipped English folder already uses it.
- For Japanese, Chinese, or other scripts that need different glyph coverage, copy `info.json` from a shipped language that already renders that script correctly and keep or adapt its `Font` value.
- Shipped examples:
  - Japanese uses `Rounded M+ 1p`
  - Chinese uses `WenQuanYi Micro Hei`
  - Russian uses `Hero Light`
- If the game does not show your new language in `Settings`, the usual problem is that the folder name and `info.json` `ID` do not match.

## 5. Add the language to `Idol Career Diary`

For `Idol Career Diary`, the translation file lives in this repository at:

```text
mods\Idol Career Diary\assets\Localization\<language-tag>\strings.txt
```

### Step-by-step

1. Go to this folder in the repository:

```text
mods\Idol Career Diary\assets\Localization
```

2. Copy the English source file:

```text
mods\Idol Career Diary\assets\Localization\en\strings.txt
```

3. Create a new folder using the recommended BCP 47 language tag.
4. Paste the copied file into that folder.
5. Translate only the text on the right side of each `=` line.
6. Do not change the keys on the left side of `=`.
7. Preserve numbered placeholders such as `{0}` exactly, and move them as needed for your language's word order.
8. Save the file as:

```text
mods\Idol Career Diary\assets\Localization\<language-tag>\strings.txt
```

Examples:

```text
mods\Idol Career Diary\assets\Localization\de\strings.txt
mods\Idol Career Diary\assets\Localization\hr\strings.txt
mods\Idol Career Diary\assets\Localization\lv\strings.txt
mods\Idol Career Diary\assets\Localization\zh-Hans\strings.txt
mods\Idol Career Diary\assets\Localization\zh-Hant\strings.txt
mods\Idol Career Diary\assets\Localization\pt-BR\strings.txt
```

Important details:

- For new translation pull requests, use `ja`, `zh-Hans`, `zh-Hant`, and `pt-BR` for the mod, not `jp`, `cn`, or `ptbr`.
- If you are translating the game's existing shipped Chinese folder `cn`, the mod treats that legacy code as Simplified Chinese and will look for `zh-Hans`.
- If you want a separate Traditional Chinese translation, create and select a separate base-game language such as `zh-Hant`, then add `mods\Idol Career Diary\assets\Localization\zh-Hant\strings.txt`.
- Missing mod keys fall back to English, so partial translations are safe while you work.

## 6. Publish a translation to GitHub

To contribute a translation back to the repository:

1. Go to the repository on GitHub: `https://github.com/ExSlam/IM-Cosmo-Mod-Library`
2. Fork the repository to your own GitHub account.
3. Clone your fork.
4. Add or update the translation files in your fork.
5. Commit your changes.
6. Push your branch.
7. Open a pull request against `ExSlam/IM-Cosmo-Mod-Library`.

What to include in the pull request description:

- The language in English
- The folder name you used
- Whether the base game language is one of the shipped folders or a new custom folder
- Any known missing or untranslated strings

## 7. Exhaustive reference: fixed base game behavior

This table is exhaustive for the language folders currently shipped with the base game.

| Language name in English | Base game folder name |
| --- | --- |
| English | `en` |
| Japanese | `jp` |
| Chinese | `cn` |
| Russian | `ru` |
| Portuguese (Brazil) | `ptbr` |

This table is exhaustive for the base game's hardcoded first-launch Steam language mapping.

| Steam language reported by the game | Base game folder selected automatically |
| --- | --- |
| English and everything not matched below | `en` |
| Japanese | `jp` |
| Chinese, Simplified | `cn` |
| Chinese, Traditional | `cn` |
| Russian | `ru` |
| Ukrainian | `ru` |
| Portuguese | `ptbr` |
| Portuguese (Brazil) | `ptbr` |

Important:

- New custom languages such as `de`, `hr`, `lv`, `ja`, `zh-Hans`, or `zh-Hant` can still be added manually by creating a folder in `IM_Data\StreamingAssets\Languages` with a matching `info.json`.
- Custom languages must be selected manually in the base game settings menu.
- The base game's language menu is dynamic.
- The base game's first-launch auto-selection is not dynamic.

## 8. Practical script support

The base game and `Idol Career Diary` are not limited to a fixed list of languages, but they are limited by text rendering behavior and available fonts.

What is clearly supported by the shipped game setup:

- Latin-script languages, because the shipped game already includes Latin fonts such as `Linotte`
- Cyrillic-script languages, because the shipped game already includes Russian with `Hero Light`
- Japanese, because the shipped game already includes Japanese with `Rounded M+ 1p`
- Chinese, because the shipped game already includes Chinese with `WenQuanYi Micro Hei`

What this means in practice:

- Languages that use Latin script are the safest to add
- Languages that use Cyrillic script are also likely to work well
- Simplified and Traditional Chinese can be supported with separate folders such as `zh-Hans` and `zh-Hant`
- Japanese is already supported by the shipped font setup

What is not reliably supported as shipped:

- Arabic-script languages such as Arabic, Persian, and Urdu
- Other scripts that need right-to-left layout, bidirectional text handling, or contextual shaping

Why:

- The game can load a language-selected font from `info.json`
- The game can also fall back to an OS-installed font if the named font exists on the user's system
- But complex-script support is not just a font problem
- Arabic-style scripts usually need right-to-left handling and character shaping support in addition to glyph coverage
- No right-to-left, bidirectional, Arabic, or text-shaping support exists in this game

Safe guidance for translators:

- If your language uses Latin, Cyrillic, Japanese, or Chinese characters, it is a reasonable candidate for a translation pack
- If your language uses Arabic script or another complex right-to-left script, do not assume it will most likely not work

## 9. Dynamic support beyond the hardcoded alias table

`Idol Career Diary` is dynamic.

There is no finite language list for the mod, because the loader accepts syntactically valid BCP 47-style tags and then tries exact and fallback folders.

Examples that work even though they are not explicitly hardcoded in the alias table:

- `hr`
- `lv`
- `ca`
- `ga`
- `sr-Latn`
- `fr-CA`
- `pt-PT`
- `zh-Hant-HK`

What the loader does:

- it tries the exact selected tag first
- then it tries sensible fallback folders
- for example `fr-CA` falls back to `fr`
- for example `zh-TW` falls back through Traditional Chinese forms such as `zh-Hant`

What this means in practice:

- You can add Croatian with base game folder `hr` and mod folder `hr`.
- You can add other languages the same way by using the correct BCP 47 tag in both places.

Do not use folder names such as `german` or `croatian` for new base-game translations. Use the actual tag, such as `de` or `hr`.

## 10. Recommended mapping for common cases

Use this table when you want the simplest answer for where to put files.

| Language name in English | Base game folder if editing the shipped game translation | Base game folder if creating a new translation | `Idol Career Diary` folder |
| --- | --- | --- | --- |
| English | `en` | `en` | `en` |
| Japanese | `jp` | `ja` | `ja` |
| Chinese, Simplified | `cn` | `zh-Hans` | `zh-Hans` |
| Chinese, Traditional | not shipped separately | `zh-Hant` | `zh-Hant` |
| Russian | `ru` | `ru` | `ru` |
| Portuguese (Brazil) | `ptbr` | `pt-BR` | `pt-BR` |

If your language is not in the table above, use the correct BCP 47 language tag for both the new base-game folder and the new `Idol Career Diary` folder.
