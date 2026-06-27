# Mod Localization System

`Mod Localization System` is a standalone runtime dependency for Idol Manager mods.
It loads `Localization/<language>/strings.txt` relative to the calling mod's DLL,
with English loaded first as a safe fallback.

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

The runtime resolves the active Idol Manager language, then tries sensible exact,
parent-tag, and legacy-folder candidates. For example, `fr-CA` can use `fr`, and
`ko` can use the bundled legacy `kr` folder.

Use `ModLocalization.LanguageChanged` to refresh runtime UI that can update without
a restart. `SetSelectedLanguage("game")` follows Idol Manager; any valid BCP 47-like
tag such as `fr`, `kr`, or `de` can be selected as a mod-only override.
The selected-language override is saved as `localization.ini` beside the installed
`Mod Localization System` DLL.

Tools that render another mod's action definitions can use
`ModLocalization.ForDirectory(mod.Path)` to localize that mod's own assets without
copying the parser.

## Localized JSON assets

Keep the normal JSON file as the compatibility fallback. Put translated copies
under `Localization/<language>/` and preserve the entire original relative path:

```text
My Event Mod/
├── info.json
├── JSON/
│   └── Events/
│       ├── dialogues.json
│       ├── random_events.json
│       └── characters.json
└── Localization/
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

Resolution order is the selected exact language, its compatible aliases and
parent language, `Localization/en/`, then the normal file at the mod root. For
example, `fr-CA` can use `Localization/fr/`, and `ko` can use the legacy
`Localization/kr/` folder.

Localized JSON files are whole-file replacements, not key-level merges. Keep IDs,
conditions, actions, and other non-text data synchronized across every copy.

### Harmony mods with custom JSON loaders

If a DLL reads a JSON file itself instead of asking Idol Manager for a standard
`JSON/...` path, resolve the file before reading it:

```csharp
string path = ModLocalization.GetLocalizedAssetPath("Data/dialogues.json");
string json = File.ReadAllText(path);
```

When the DLL is not stored at the content mod's root, resolve against that root:

```csharp
string path = ModLocalization
    .ForDirectory(modRootDirectory)
    .GetLocalizedAssetPath("Data/dialogues.json");
```

The API supports any relative asset path. Automatic game interception is
deliberately restricted to JSON below the standard `JSON/` directory so localized
data cannot unexpectedly replace unrelated images, audio, configuration, or mod
metadata.

Idol Manager parses most JSON data during startup. Changing the mod-language
override while a save is already open therefore takes effect for JSON after the
game is restarted; UI strings that subscribe to `LanguageChanged` can still
refresh immediately.
