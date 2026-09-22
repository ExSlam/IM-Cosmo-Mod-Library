# Divorce Fix

`Divorce Fix` clears marriage state on divorce events so flirting works again.

## Player-facing behavior

- Repairs divorce cleanup so relationship state does not stay incorrectly married afterward.
- Restores normal flirting behavior after divorce-related events fire.
- Repairs recognized older divorce outcomes after the complete save load, regardless of loader order. Happy marriages are preserved.
- Works independently and alongside Save n Load Fixes (SNLF). SNLF's forced-breakup repair handles idol-to-idol relationships, not the player's divorce state.

Legacy repair requires the saved graduation text to exactly match the current language's divorce outcome for the player's current name/nickname. Unrecognized text is left intact. New divorce events are repaired from the outcome flags and completed graduation, without parsing text.

Repairs change the loaded relationship state; they do not immediately rewrite save files or create sidecars. The game persists that state on its next normal save.

## Build

Project file:
- `mods/Divorce Fix/Divorce Fix.csproj`

Example command:
- `dotnet build "mods/Divorce Fix/Divorce Fix.csproj" -c Release`
