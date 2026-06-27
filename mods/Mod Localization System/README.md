# Mod Localization System

`Mod Localization System` is a standalone runtime dependency for Idol Manager mods.
It loads `Localization/<language>/strings.txt` relative to the calling mod's DLL,
with `Localization/en/strings.txt` required as the safe fallback.

It also selects complete language-specific JSON assets for both Harmony and
data-only mods. This allows event, dialogue, trivia, policy, and other text-heavy
JSON mods to use the same language selection and fallback rules as UI strings.

An explicit empty value such as `OpenIdolPrefix=` is preserved as a translation;
it does not fall back to English. This supports languages with different word order.

## Mod author usage

Reference `com.cosmo.modlocalizationsystem.dll` with `Private` set to `false` so the
player installs one shared copy:

```xml
<Reference Include="com.cosmo.modlocalizationsystem">
  <HintPath>path\to\com.cosmo.modlocalizationsystem.dll</HintPath>
  <Private>false</Private>
</Reference>
```

Then:

```csharp
using ModLocalizationSystem;

string title = ModLocalization.Get("menu.title", "My Mod");
string rawText = ModLocalization.GetRaw("POLICY_TITLE");
```

The runtime always follows Idol Manager's selected language. There is no separate
mod-language override. It tries exact, parent-tag, and legacy-folder candidates;
for example, `fr-CA` can use `fr`, and `ko` can use the legacy `kr` folder.

The currently established language folders are `en`, `fr`, `kr`, `cn`, `jp`,
`ru`, and `ptbr`. The resolver is not limited to those codes: another valid
language tag works when a mod supplies a corresponding localization folder.

Tools that render another mod's action definitions can use
`ModLocalization.ForDirectory(mod.Path)` to localize that mod's own assets without
copying the parser.

## Localized JSON assets

Every localized JSON copy, including English, must live under its own
`Localization/<language>/` directory while preserving the original relative path:

```text
My Event Mod/
├── info.json
└── Localization/
    ├── en/
    │   └── JSON/
    │       └── Events/
    │           ├── dialogues.json
    │           ├── random_events.json
    │           └── characters.json
    ├── fr/
    │   └── JSON/
    │       └── Events/
    │           ├── dialogues.json
    │           ├── random_events.json
    │           └── characters.json
    └── kr/
        └── JSON/
            └── Events/
                ├── dialogues.json
                ├── random_events.json
                └── characters.json
```

The framework redirects Idol Manager's standard `JSON/**/*.json` mod lookups to
the best localized copy. This works without a DLL in the content mod: the player
only needs the shared Mod Localization System installed and enabled. It also works
automatically for Harmony mods whose JSON uses the normal Idol Manager directory
layout.

`Localization/en/<relative path>` is required for each automatically localized
JSON asset. Resolution uses Idol Manager's exact language, compatible aliases and
parent language, then the English localized file. A root file such as
`JSON/Events/dialogues.json` is never treated as an English localization.

If a mod has only the vanilla root JSON structure and no matching English file
under `Localization/en/`, the framework leaves that mod completely untouched.
This prevents ordinary single-language content mods from being mistaken for
localized mods.

Localized JSON files are whole-file replacements, not key-level merges. Keep IDs,
conditions, actions, and other non-text data synchronized across every copy.

### Harmony mods with custom JSON loaders

If a DLL reads a JSON file itself instead of asking Idol Manager for a standard
`JSON/...` path, resolve the file before reading it:

```csharp
string path = ModLocalization.GetLocalizedAssetPath("Data/dialogues.json");
if (string.IsNullOrEmpty(path))
    throw new FileNotFoundException("Missing Localization/en/Data/dialogues.json");
string json = File.ReadAllText(path);
```

When the DLL is not stored at the content mod's root, resolve against that root:

```csharp
string path = ModLocalization
    .ForDirectory(modRootDirectory)
    .GetLocalizedAssetPath("Data/dialogues.json");
```

The API supports any relative asset path but still requires its English copy
inside `Localization/en/`. Automatic game interception is
deliberately restricted to JSON below the standard `JSON/` directory so localized
data cannot unexpectedly replace unrelated images, audio, configuration, or mod
metadata.

Idol Manager parses most JSON data during startup, so changing the game's language
requires the same reload or restart that the game normally uses for language data.
