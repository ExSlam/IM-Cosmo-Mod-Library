# Show Cast Assignment Fix

Prevents the same idol from occupying more than one permanent-cast slot in radio, internet, and TV shows.

## Behavior

- Rejects a duplicate idol selected through the show-cast picker.
- Validates new and edited shows before the cast is saved.
- Repairs existing permanent casts by preserving the first occurrence and clearing later duplicate slots.
- Deduplicates the cast returned to gameplay logic as a safety net, so a malformed old save cannot apply per-cast rewards or penalties multiple times.
- Rechecks casts after the vanilla replacement path used when a cast member leaves.

## Existing saves

Open an affected show once or let it air an episode. The mod keeps the first slot and clears repeated copies; assign different idols to any now-empty slots in the show editor.

## Build

```powershell
dotnet build "mods/Show Cast Assignment Fix/Show Cast Assignment Fix.csproj" -c Release
```
