# Cosmo Mod Library

Source repository for Cosmo's Idol Manager mods.

- [Localization Guide](LOCALIZATION.md) - How to add base-game and mod translations, including new languages not shipped by default.

## Mods Included

- [Divorce Fix](mods/Divorce%20Fix/README.md) - Clears marriage state on divorce events so flirting works again.
- [Graduation Calendar](mods/Graduation%20Calendar/README.md) - Shows graduation dates in Extras and adds a calendar popup.
- [Graduation Details](mods/Graduation%20Details/README.md) - Adds a graduated idol details popup with earnings, singles, and marriage info.
- [Graduation Rebalances](mods/Graduation%20Rebalances/README.md) - Pushes graduation dates back for high-performing idols.
- [Idol Career Diary](mods/Idol%20Career%20Diary/README.md) - Profile-integrated career timeline UI powered by IM Data Core and IM UI Framework.
- [IM Data Core](mods/IM%20Data%20Core/README.md) - Reusable persistent data and event ledger backend for Idol Manager mods.
- [IM UI Framework](mods/IM%20UI%20Framework/README.md) - Reusable UI toolkit for modders building game-style controls and popups.
- [Staff Firing Freeze Fix](mods/Staff%20Firing%20Freeze%20Fix/README.md) - Prevents room and business proposal state from getting stuck when staff are fired mid-task.
- [UI Recovery Tools](mods/UI%20Recovery%20Tools/README.md) - Adds configurable recovery hotkeys and an error overlay for stuck UI states.

## Repository Layout

- `mods/<Mod Name>/src` contains source files.
- `mods/<Mod Name>/assets` contains runtime assets copied into the built mod folder.
- `mods/<Mod Name>/docs` is used for screenshots or deeper documentation when a mod needs it.
- `Directory.Build.props` centralizes game DLL references and local build output behavior.

## Build

1. Make sure the game DLLs are available in the sibling `../dll` folder, or update `dllDir` in `Directory.Build.props`.
2. By default, packaged builds are copied into `artifacts/mods/Debug` or `artifacts/mods/Release`.
3. For live developer installs, deploy the packaged artifact into `%USERPROFILE%\AppData\LocalLow\Glitch Pitch\Idol Manager\Mods` instead of pointing package output at the game folder. Use `.\scripts\Build-And-Deploy-Mods.ps1 -Configuration Debug` or `Release`.
4. Live mod folder names may differ from repo folder names. The deploy script matches existing installs by `HarmonyID` and can also honor a project-level `DeployFolderName`.
5. Restore packages with `dotnet restore`.
6. Build everything with `dotnet build "Cosmo Mod Library.sln" -c Release`.

You can also build an individual mod directly, for example:

`dotnet build "mods/IM UI Framework/IM UI Framework.csproj" -c Release`

## Publishing Note

Choose and add a license before publishing this repository publicly on GitHub.
