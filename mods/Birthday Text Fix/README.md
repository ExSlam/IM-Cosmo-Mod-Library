# Birthday Text Fix

`Birthday Text Fix` fixes incorrect English ordinal suffixes in idol birthday popup titles.

## Player-facing behavior

- Corrects birthday ages such as `21th`, `22st`, or `23th` to `21st`, `22nd`, and `23rd`.
- Correctly handles later ages such as `31st`, `32nd`, `33rd`, `41st`, `42nd`, and `43rd`.
- Preserves the special English endings for `11th`, `12th`, and `13th`.
- Changes only the English birthday popup title.
- Does not change idol ages, birthday timing, stat changes, graduation dates, save data, or other gameplay state.
- Has no dependency on other Cosmo Mod Library mods.

## Implementation

Vanilla `Birthday_Popup.RenderTitle()` inserts the idol's numeric age into `BD__TITLE_2`, whose English text contains a fixed ordinal suffix. The game already has a correct ordinal helper, `ExtensionMethods.GetOrdinal(int)`, but the birthday popup does not use it.

The mod applies a postfix to `Birthday_Popup.RenderTitle()`. When the active language is English, it lets vanilla build the localized birthday text first, finds the existing `st`, `nd`, `rd`, or `th` immediately after the inserted age, and replaces only that two-letter suffix with the value returned by vanilla `ExtensionMethods.GetOrdinal(age)`.

This preserves the rest of the game's English birthday wording and leaves every non-English language on the original vanilla path.

## Build

Project file:
- `mods/Birthday Text Fix/Birthday Text Fix.csproj`

Example command:
- `dotnet build "mods/Birthday Text Fix/Birthday Text Fix.csproj" -c Release`
