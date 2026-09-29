# Cosmo Mod Library: mandatory agent rules

These requirements apply to every mod in this repository, including frameworks, gameplay mods, fixes, and data-only content. Apply them to code, assets, localization, documentation, and release packages. Existing files that do not meet these requirements are not examples to copy. Follow the user's current task scope; this policy alone does not authorize an unrelated repository-wide rewrite or deployment.

## 1. Scope, naming, and explicit values

- Give every constant, variable, field, parameter, and data structure a name that explains its purpose in the specific mod and operation. Include units or meaning where ambiguity is possible, such as a duration in seconds or a percentage coefficient.
- Use the narrowest appropriate visibility, ownership, and lifetime. Keep implementation details local or internal; expose only intentional APIs. Shared or static mutable state must have a clear owner and initialization/reset lifecycle.
- Do not scatter magic numbers, loose string literals, or unexplained values through implementation code. Define meaningful numeric values, thresholds, defaults, identifiers, paths, localization keys, and format specifications in appropriately scoped named constants, configuration, enums, or documented data structures.
- Literal values belong at their named definition or in the relevant declared configuration/data field. Reuse that definition at call sites; do not invent a generic constants dump or meaningless names just to conceal literals.
- Player-facing text belongs in localization assets. Moving a hardcoded message into a C# constant does not satisfy the localization requirement.
- Comments must explain the reason for a change and, for patches, the original behavior and the specific condition being corrected.

## 2. Complete localization through the embedded localization runtime

Every player-facing string and displayed value must support all seven standard languages:

| Language | Required folder |
| --- | --- |
| English | `en` |
| Simplified Chinese | `cn` |
| French | `fr` |
| Japanese | `jp` |
| Korean | `kr` |
| Brazilian Portuguese | `ptbr` |
| Russian | `ru` |

- This includes new and existing UI affected by the change, buttons, labels, tooltips, settings, notifications, error messages shown to players, JSON content, story text, dialogue, and descriptions. Localize the labels, units, and surrounding text for displayed values, and format numbers, dates, and currencies appropriately for the game's selected language.
- Keep the established folder names above. Use Idol Manager's language selection and English fallback; do not invent a separate language preference for each mod.
- Each mod with player-facing content must use its own embedded copy of the shared localization implementation used by the other Cosmo Mod Library mods. Follow [LOCALIZATION.md](LOCALIZATION.md), [Mod Localization System](<mods/Mod Localization System/README.md>), and the shared-source inclusion in [Directory.Build.props](Directory.Build.props).
- Store UI translations in `assets/Localization/<language>/strings.txt`. Keep keys identical across languages and translate their values. Preserve placeholders, formatting tokens, and intentional empty values. English fallback is recovery behavior; it does not count as completing a missing translation.
- Store translated JSON under `assets/Localization/<language>/` with the same relative path as the original asset, including the English copy. These are whole-file replacements: synchronize IDs, conditions, actions, numeric gameplay values, and other structural data across all languages. Translate the player-facing text, not identifiers or game logic.
- Verify the loading path for every localized asset. The current embedded string helper alone does not install the standalone mod's JSON interception patches. A mod using localized JSON must also integrate the required loader/interception behavior through the localization system; it must not silently depend on an optional standalone installation to satisfy this rule. If otherwise data-only content requires an embedded runtime adapter, include the necessary C# project and source.
- Embedded helpers must coexist with other embedded copies and the standalone localization mod. Scope implementation types appropriately and avoid duplicate global patches or ambiguous public helper types.
- Player-facing metadata and Workshop descriptions require translated versions through the host's supported localization mechanisms. Keep `info.json` valid for Idol Manager; do not invent unsupported metadata fields to simulate translation. Identify any unsupported display path instead of claiming it is localized.
- Check key coverage and placeholder compatibility in all seven languages. For changed UI, verify font coverage, wrapping, clipping, and layout with translated text. Report any checks that could not be performed.

## 3. Authorship and precise documentation

- Credit **Cosmo** as the author of every mod in this library. Keep author metadata, project/package authorship where present, README credits, and Workshop credits consistent with that attribution.
- **Cosmo is a person and the namesake author.** Do not in documentation use the name as a coding methodology, algorithm, storage format, or substitute for the name of the software component performing an action.
- Explain which mod does what, under which specific conditions, and what the player observes. In technical documentation, name the actual method, component, data, and destination when those details matter.
- Avoid wording such as "Cosmo saves the data to diary and transports it to side car envelope." It misattributes software behavior and does not explain the operation.
- Prefer concrete wording such as "When the player opens the graduation calendar, Graduation Calendar displays upcoming graduation dates using Idol Manager-styled controls." Describe persistence with the actual responsible mod, trigger, and storage location verified from the implementation.
- Preserve required third-party credits and license notices alongside Cosmo's authorship. Do not attribute third-party assets or libraries to Cosmo.

## 4. UI must follow the game's controls and framework patterns

Before implementing or substantially changing a mod UI:

1. Read [Graduation Calendar's README](<mods/Graduation Calendar/README.md>) and inspect [its UI implementation](<mods/Graduation Calendar/src/GraduationCalendar.cs>) as the reference for proportionate sizing, layout, spacing, and native-looking controls. As well as Monthly Ledger's. UI control text must have contrast with its background's image or bg color to be **VISIBLE** to the players. You have access to the game's decompiled assets at F:\SteamLibrary\steamapps\workshop\content\821880\Idol Manager\Asset Ripper Exported\ExportedProject, copy of game files for testing at F:\SteamLibrary\steamapps\workshop\content\821880\Idol Manager, decompiled vanilla Idol Manager source code at F:\SteamLibrary\steamapps\workshop\content\821880\Decompiled Idol Manager Source Code\IM Source Code, and Idol Manager dlls that may be needed by Cosmo Mod Library mods at F:\SteamLibrary\steamapps\workshop\content\821880\dll.
2. Review [IM UI Framework's README and system inventory](<mods/IM UI Framework/README.md>) and its relevant documentation/source. Consider all available framework systems before choosing an implementation, including composable elements, scene templates, native buttons, scrolling, popup shells, popup registration/queues, themes, and game fonts.
3. Reuse the framework's appropriate controls and lifecycle behavior. Preserve native interaction, keyboard/mouse behavior, clipping, scrolling, popup ordering, close behavior, pause/input blocking, and backdrop/blur cleanup where applicable.
4. Use the game's current fonts and the framework's font handling. Size and test the UI for its actual content, supported resolutions/scaling, and all required languages.

A mod may depend explicitly on the external **IM UI Framework** mod, or embed the framework functions it needs to avoid that external dependency. For embedding, follow [Graduation Calendar's embedded implementation](<mods/Graduation Calendar/src/EmbeddedIMUiFramework>): use a mod-specific namespace, include the dependencies of the selected functions, initialize them through the mod's lifecycle, and avoid copying framework-global patches that would run twice. The embedded UI must work both with and without the standalone framework installed.

## 5. Required mod files, release copy, and licensing

Every mod must have this minimum source layout. `src/` and the project file are required when the mod needs C# code, including any embedded runtime integration.

```text
mods/<Mod Name>/
  README.md
  CHANGELOG.md
  LICENSE.md
  <Mod Name>.csproj       # When C# is required; declares the mod version
  src/                   # C# implementation compiled into the mod DLL
  assets/
    info.json
    thumb.png
    steam description.txt
    Localization/
      en/
      cn/
      fr/
      jp/
      kr/
      ptbr/
      ru/
    ...other assets required by the mod
```

### Metadata and player-facing descriptions

- `assets/info.json` must use Idol Manager Harmony Integration (locally at F:\SteamLibrary\steamapps\workshop\content\821880\IM-HarmonyIntegration) supported metadata schema, identify the mod and author, and describe its behavior in simple player-friendly terms. Idol Manager terminology is appropriate. Keep implementation jargon out of the description unless a framework genuinely needs a brief explanation for mod authors.
- Keep the declared mod version consistent between `info.json` and the `.csproj` when present. Use the correct assembly identity and actual dependencies.
- `assets/thumb.png` must be exactly **512 x 512 pixels** and **under 1 MB**. Use **fewer than 1,000,000 bytes** as the repository's validation threshold.
- `assets/steam description.txt` must explain the actual behavior, activation conditions, requirements, and relevant player-visible limitations in plain language. Framework descriptions may include a brief technical overview; detailed implementation documentation belongs in `README.md`.
- Use Steam's supported BBCode-style markup, such as `[h1]`, `[b]`, `[list]`, and `[url=...]`, following [Steam's formatting reference](https://steamcommunity.com/comment/Announcement/formattinghelp).
- Validate the final Workshop description by encoded size, including markup and line breaks. Valve documents `k_cchPublishedDocumentDescriptionMax` as **8,000 bytes**; keep the UTF-8 description **below 8,000 bytes** as a conservative publishing budget. Unicode character count alone is insufficient. Validate each localized description separately against the current publishing limit. See [Steamworks description limits](https://partner.steamgames.com/doc/api/ISteamRemoteStorage#k_cchPublishedDocumentDescriptionMax) and [SetItemDescription](https://partner.steamgames.com/doc/api/ISteamUGC#SetItemDescription).

### README and changelog timing

- Keep technical architecture, implementation details, persistence formats, APIs, compatibility details, and build instructions in the mod's `README.md`.
- Record change history in the mod's `CHANGELOG.md`, using accurate versions/dates and describing the final implemented behavior. Do not present unfinished work as a released feature.
- Perform final README release reconciliation **after the user explicitly gives the manual go-ahead that the mod is ready for release and should be deployed**. At that point, reconcile the README with the actual code, dependencies, metadata, behavior, and changelog before completing the authorized deployment.
- Development/build work alone does not establish release readiness. An existing go-ahead for the current release is sufficient; do not ask for the same authorization again or invent a second approval step for the documentation update.

### License copies and runtime packages

- Include a **byte-for-byte copy of the current repository-root `LICENSE.md` in every mod**, including data-only mods. Place it at `mods/<Mod Name>/LICENSE.md` and include it in that mod's distributed package. Refresh those copies when the root license changes; do not rewrite its terms or substitute another license.
- Deploy the freshly compiled mod DLL, `info.json`, current localizations, required runtime assets, the primary thumbnail, and the license. Keep source files, `src/`, project files, PDBs, `bin/`, `obj/`, and alternate thumbnails out of runtime packages unless the user explicitly requests a source distribution.
- Copy compiled assemblies from the verified build output. An older DLL stored among assets must not overwrite a fresh build during packaging.
- Preserve existing player configuration when updating an installation unless the user asks to replace it or a documented migration requires a change. Include any explicitly requested standalone tool as its own self-contained folder, without carrying its source/build parent directories into the mod package.
- Before reporting release/deployment completion, check the affected build, metadata/version consistency, seven-language coverage, thumbnail dimensions/size, description byte limits, license equality, and the final runtime file list. Distinguish completed checks from in-game validation that remains unperformed.
