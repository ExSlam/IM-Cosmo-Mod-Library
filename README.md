# Cosmo Mod Library

Source repository for Cosmo's mods for [Idol Manager](https://store.steampowered.com/app/821880/Idol_Manager/).

This repository contains gameplay mods, bug fixes, shared frameworks, localization tools, and developer utilities built with Harmony for Idol Manager.

* [Download compiled mods from the Steam Workshop](https://steamcommunity.com/workshop/filedetails/?id=3763440928)
* [Install IM-HarmonyIntegration](https://github.com/ui3TD/IM-HarmonyIntegration)
* [Read the localization guide](LOCALIZATION.md)
* [Read the contribution guidelines](CONTRIBUTING.md)

> [!IMPORTANT]
> Most compiled mods in the Steam Workshop collection require **IM-HarmonyIntegration**.
>
> Subscribing to the mods through Steam Workshop does not replace the separate Harmony installation. Install IM-HarmonyIntegration from GitHub before using Harmony-based mods.

## Installing Compiled Mods

Players who do not need to modify or compile the source should install the published versions through Steam Workshop.

1. Open the [Cosmo Mods Steam Workshop collection](https://steamcommunity.com/workshop/filedetails/?id=3763440928).
2. Subscribe to the mods you want to use.
3. Install [IM-HarmonyIntegration](https://github.com/ui3TD/IM-HarmonyIntegration).
4. Start Idol Manager.
5. Open the in-game mod manager and enable the subscribed mods.

Some mods depend on other Cosmo mods or frameworks. Check the requirements shown on each Workshop page before enabling a mod.

### Installing IM-HarmonyIntegration on Windows

IM-HarmonyIntegration is a BepInEx plugin that allows Idol Manager to load Harmony mod DLLs.

1. Open the [IM-HarmonyIntegration repository](https://github.com/ui3TD/IM-HarmonyIntegration).
2. Download the latest Windows x64 release.
3. In Steam, right-click **Idol Manager**.
4. Select **Manage > Browse local files**.
5. Extract the contents of the IM-HarmonyIntegration ZIP directly into the Idol Manager installation directory.

A typical Steam installation directory is:

```text
C:\Program Files (x86)\Steam\steamapps\common\Idol Manager\
```

The exact location may differ when Steam uses another library folder.

The IM-HarmonyIntegration Windows package includes the files needed to install BepInEx. After installation, the game directory should contain folders and files such as:

```text
Idol Manager\
├── BepInEx\
├── IM_Data\
├── Idol Manager.exe
├── doorstop_config.ini
└── winhttp.dll
```

The Harmony library used when building these mods is located at:

```text
Idol Manager\BepInEx\core\0Harmony.dll
```

## Mods Included

The repository currently contains 20 buildable mod projects.

### Gameplay and Interface Mods

* [Assistant Manager](mods/Assistant%20Manager/) - Adds Assistant Manager Offices and allows Assistant Managers to share eligible office work with the Producer.
* [Cheats Mod](mods/Cheats%20Mod/) - Adds configurable cheat and testing actions to the Mod Buttons Action Hub.
* [Graduation Calendar](mods/Graduation%20Calendar/) - Shows idol graduation dates in Extras and adds a graduation calendar popup.
* [Graduation Details](mods/Graduation%20Details/) - Adds a graduated-idol details popup containing career earnings, singles, marriage information, and other history.
* [Graduation Rebalances](mods/Graduation%20Rebalances/) - Pushes graduation dates back for qualifying high-performing idols.
* [Idol Career Diary](mods/Idol%20Career%20Diary/) - Adds a profile-integrated career timeline powered by IM Data Core and IM UI Framework.
* [Monthly Ledger](mods/Monthly%20Ledger/) - Adds a localized monthly income and expense ledger to the Action Hub.
* [No Bullying Policy](mods/No%20Bullying%20Policy/) - Adds a policy option that can disable bullying while preserving the default behavior when the policy is not active.
* [UI Recovery Tools](mods/UI%20Recovery%20Tools/) - Adds configurable recovery hotkeys and an error overlay for recovering from stuck interface states.

### Frameworks and Developer Tools

* [IM Data Core](mods/IM%20Data%20Core/) - Provides reusable persistent data storage and an event-ledger backend for Idol Manager mods.
* [IM UI Framework](mods/IM%20UI%20Framework/) - Provides reusable game-style controls, buttons, popups, and other interface utilities.
* [Mod Buttons](mods/Mod%20Buttons/) - Provides a centralized Action Hub that lets mods add interactive actions through configuration files.
* [Mod Localization System](mods/Mod%20Localization%20System/) - Provides shared language-aware JSON loading and localization support for Idol Manager mods.

### Bug Fixes and Compatibility Mods

* [Clinic Recovery Priority Fix](mods/Clinic%20Recovery%20Priority%20Fix/) - Makes automated clinics claim eligible low-stamina idols before competing auto-practice rooms.
* [Divorce Fix](mods/Divorce%20Fix/) - Clears stale marriage state after divorce events so affected idols can flirt again.
* [Room Assignment Fix](mods/Room%20Assignment%20Fix/) - Prevents manual and automatic room assignment from placing one idol in multiple rooms or tasks at once.
* [Show Cast Assignment Fix](mods/Show%20Cast%20Assignment%20Fix/) - Prevents one idol from occupying multiple permanent-cast slots in radio, internet, and television shows.
* [Singles Marketing Checkbox Fix](mods/Singles%20Marketing%20Checkbox%20Fix/) - Keeps the Marketing checklist row visible for in-development singles when no marketing campaign was selected.
* [Staff Firing Freeze Fix](mods/Staff%20Firing%20Freeze%20Fix/) - Prevents room and business-proposal state from becoming stuck when busy staff members are fired.
* [Unavailable Idols Fix](mods/Unavailable%20Idols%20Fix/) - Protects show casts, concerts, assignments, and related persistent state when idols become temporarily unavailable, announce graduation, or leave the agency.

## Repository Layout

```text
IM-Cosmo-Mod-Library\
├── mods\
│   └── <Mod Name>\
│       ├── src\            # C# source files
│       ├── assets\         # Files copied into the packaged mod
│       ├── docs\           # Screenshots and extended documentation
│       └── <Mod Name>.csproj
├── scripts\                # Build, packaging, and deployment scripts
├── artifacts\              # Generated packaged builds
├── Cosmo Mod Library.sln
├── Directory.Build.props
├── LOCALIZATION.md
├── CONTRIBUTING.md
└── LICENSE.md
```

Not every mod needs every optional directory.

* `mods/<Mod Name>/src` contains C# source files.
* `mods/<Mod Name>/assets` contains `info.json`, localization files, images, and other runtime assets copied into packaged builds.
* `mods/<Mod Name>/docs` contains screenshots or extended documentation when needed.
* `Directory.Build.props` defines shared compiler settings, game DLL references, and packaging behavior.
* `scripts/Build-And-Deploy-Mods.ps1` builds and deploys the mods into a local Idol Manager installation.

## Building From Source

### Requirements

To build the complete solution, you need:

* Windows
* Git
* A current .NET SDK capable of building .NET Framework 4.6 projects
* Idol Manager installed locally
* [IM-HarmonyIntegration](https://github.com/ui3TD/IM-HarmonyIntegration) installed in the Idol Manager directory
* Local copies of the required Idol Manager, Unity, DOTween, and Harmony DLLs

The projects target:

```text
.NET Framework 4.6
```

The required game and Harmony DLLs are not included in this repository. Copy them from your own Idol Manager installation after installing IM-HarmonyIntegration.

Do not commit or publicly redistribute the copied game DLLs.

### 1. Clone the Repository

Choose a parent development directory and clone the repository inside it:

```powershell
cd C:\Development
mkdir IdolManagerMods
cd IdolManagerMods

git clone https://github.com/ExSlam/IM-Cosmo-Mod-Library.git
```

This produces:

```text
C:\Development\IdolManagerMods\
└── IM-Cosmo-Mod-Library\
```

### 2. Create the Sibling DLL Directory

By default, `Directory.Build.props` expects a directory named `dll` beside the repository.

Create it in the same parent directory where `IM-Cosmo-Mod-Library` is located:

```powershell
cd C:\Development\IdolManagerMods
mkdir dll
```

The expected structure is:

```text
C:\Development\IdolManagerMods\
├── dll\
└── IM-Cosmo-Mod-Library\
```

In other words, the default DLL path is:

```text
IM-Cosmo-Mod-Library\..\dll
```

You may use a different location by overriding the `dllDir` MSBuild property or editing `Directory.Build.props`.

### 3. Locate the Idol Manager Directory

In Steam:

1. Open your Library.
2. Right-click **Idol Manager**.
3. Select **Manage > Browse local files**.

The directory normally resembles:

```text
<Steam Library>\steamapps\common\Idol Manager\
```

After IM-HarmonyIntegration is installed, the two relevant source directories are:

```text
Idol Manager\BepInEx\core\
Idol Manager\IM_Data\Managed\
```

### 4. Copy the Harmony DLL

Copy:

```text
Idol Manager\BepInEx\core\0Harmony.dll
```

Into the sibling development DLL directory:

```text
C:\Development\IdolManagerMods\dll\
```

Result:

```text
C:\Development\IdolManagerMods\dll\0Harmony.dll
```

### 5. Copy the Game and Unity DLLs

Copy the following files from:

```text
Idol Manager\IM_Data\Managed\
```

Into:

```text
C:\Development\IdolManagerMods\dll\
```

Required files:

```text
Assembly-CSharp.dll
Assembly-CSharp-firstpass.dll
DOTween.dll
UnityEngine.dll
UnityEngine.CoreModule.dll
UnityEngine.ImageConversionModule.dll
UnityEngine.InputLegacyModule.dll
UnityEngine.JSONSerializeModule.dll
UnityEngine.TextRenderingModule.dll
UnityEngine.UI.dll
UnityEngine.UIModule.dll
UnityEngine.UnityWebRequestModule.dll
UnityEngine.UnityWebRequestTextureModule.dll
Unity.TextMeshPro.dll
```

Together with `0Harmony.dll`, the complete sibling DLL directory should contain:

```text
dll\
├── 0Harmony.dll
├── Assembly-CSharp.dll
├── Assembly-CSharp-firstpass.dll
├── DOTween.dll
├── UnityEngine.dll
├── UnityEngine.CoreModule.dll
├── UnityEngine.ImageConversionModule.dll
├── UnityEngine.InputLegacyModule.dll
├── UnityEngine.JSONSerializeModule.dll
├── UnityEngine.TextRenderingModule.dll
├── UnityEngine.UI.dll
├── UnityEngine.UIModule.dll
├── UnityEngine.UnityWebRequestModule.dll
├── UnityEngine.UnityWebRequestTextureModule.dll
└── Unity.TextMeshPro.dll
```

The full development layout should now resemble:

```text
C:\Development\IdolManagerMods\
├── dll\
│   ├── 0Harmony.dll
│   ├── Assembly-CSharp.dll
│   ├── Assembly-CSharp-firstpass.dll
│   ├── DOTween.dll
│   ├── UnityEngine.dll
│   ├── UnityEngine.CoreModule.dll
│   ├── UnityEngine.ImageConversionModule.dll
│   ├── UnityEngine.InputLegacyModule.dll
│   ├── UnityEngine.JSONSerializeModule.dll
│   ├── UnityEngine.TextRenderingModule.dll
│   ├── UnityEngine.UI.dll
│   ├── UnityEngine.UIModule.dll
│   ├── UnityEngine.UnityWebRequestModule.dll
│   ├── UnityEngine.UnityWebRequestTextureModule.dll
│   └── Unity.TextMeshPro.dll
│
└── IM-Cosmo-Mod-Library\
    ├── mods\
    ├── scripts\
    ├── Cosmo Mod Library.sln
    └── Directory.Build.props
```

### 6. Restore Packages

Open PowerShell in the repository directory:

```powershell
cd C:\Development\IdolManagerMods\IM-Cosmo-Mod-Library
```

Restore the NuGet dependencies:

```powershell
dotnet restore
```

### 7. Build the Complete Solution

To compile every project:

```powershell
dotnet build "Cosmo Mod Library.sln" -c Release
```

Normal compiler output is placed under each project's `bin\Release\net46` directory.

### 8. Create Packaged Mod Folders

To compile the complete solution and copy each mod DLL and its assets into `artifacts\mods\Release`, run:

```powershell
$artifactRoot = Join-Path (Get-Location) "artifacts\mods\Release"

dotnet build "Cosmo Mod Library.sln" `
    -c Release `
    "-p:ModOutputDir=$artifactRoot"
```

Packaged mods will be placed under:

```text
artifacts\mods\Release\<Mod Name>\
```

For example:

```text
artifacts\mods\Release\Room Assignment Fix\
├── com.cosmo.roomassignmentfix.dll
├── info.json
└── Localization\
```

### 9. Build an Individual Mod

For example, to build and package IM UI Framework:

```powershell
$artifactRoot = Join-Path (Get-Location) "artifacts\mods\Release"

dotnet build "mods\IM UI Framework\IM UI Framework.csproj" `
    -c Release `
    "-p:ModOutputDir=$artifactRoot"
```

Replace the project path with the project you want to build.

## Building and Deploying to Idol Manager

The included PowerShell script can build all deployable projects and copy them into the local Idol Manager mod directory:

```powershell
.\scripts\Build-And-Deploy-Mods.ps1 -Configuration Release
```

The default deployment directory is:

```text
%USERPROFILE%\AppData\LocalLow\Glitch Pitch\Idol Manager\Mods
```

For Debug builds:

```powershell
.\scripts\Build-And-Deploy-Mods.ps1 -Configuration Debug
```

The deployment script:

* Finds all deployable `.csproj` files under `mods`.
* Builds each project.
* Creates packaged output under `artifacts\mods\<Configuration>`.
* Reads each packaged mod's `HarmonyID`.
* Matches existing installations by `HarmonyID`.
* Copies the DLL and runtime assets into the appropriate local mod directory.
* Avoids deploying `.pdb` debugging files.

A custom deployment directory can be supplied with `-DeployRoot`:

```powershell
.\scripts\Build-And-Deploy-Mods.ps1 `
    -Configuration Release `
    -DeployRoot "D:\IdolManagerTestMods"
```

To deploy previously built artifacts without rebuilding:

```powershell
.\scripts\Build-And-Deploy-Mods.ps1 `
    -Configuration Release `
    -SkipBuild
```

## Troubleshooting

### Missing `HarmonyLib` or `HarmonyPatch`

Confirm that this file exists:

```text
..\dll\0Harmony.dll
```

The source file should have been copied from:

```text
Idol Manager\BepInEx\core\0Harmony.dll
```

If the `BepInEx` directory does not exist, install IM-HarmonyIntegration before collecting the build dependencies.

### Missing Idol Manager Types

Errors involving types such as `staff`, `agency`, `data_girls`, or other game classes usually mean that one or both of these files are missing or from an incompatible game version:

```text
Assembly-CSharp.dll
Assembly-CSharp-firstpass.dll
```

Copy fresh versions from:

```text
Idol Manager\IM_Data\Managed\
```

### Missing Unity Types

Errors involving `UnityEngine`, user-interface classes, image conversion, text rendering, web requests, or TextMesh Pro usually mean that one of the Unity DLLs is absent from the sibling `dll` directory.

Compare your directory against the complete required DLL list above.

### Missing DOTween

Confirm that this file exists:

```text
..\dll\DOTween.dll
```

Copy it from:

```text
Idol Manager\IM_Data\Managed\DOTween.dll
```

### Build Breaks After an Idol Manager Update

When Idol Manager updates, its managed assemblies may change.

Copy fresh versions of the required DLLs from the current game installation into the sibling `dll` directory and rebuild the solution.

All copied DLLs should come from the same installed game version.

### The Repository and DLL Directory Are Not Siblings

Either move the directories into the expected layout:

```text
Parent Directory\
├── dll\
└── IM-Cosmo-Mod-Library\
```

Or override `dllDir` when building:

```powershell
dotnet build "Cosmo Mod Library.sln" `
    -c Release `
    "-p:dllDir=D:\Libraries\IdolManager"
```

## Localization

See [LOCALIZATION.md](LOCALIZATION.md) for instructions on:

* Adding translations to existing mods
* Adding support for languages not shipped by the base game
* Working with localized JSON assets
* Using the shared localization runtime
* Providing player-facing text through `strings.txt` files

## Contributing

Pull requests are welcome. Keep them focused, explain what changed and why, and follow these rules:

1. Add in-code comments that explain your changes in relation to the original behavior or implementation.
2. Use named variables and constants. Do not leave loose string literals or magic numbers in the code.
3. Put player-facing text in the proper localizable `strings.txt` files for each mod instead of hardcoding it in code.
4. Localization translations for mods in this repo are appreciated.

If your change affects behavior, UI, balance, persistence, or compatibility, include a short explanation in the PR description.

Keep changes focused and explain what changed and why. Player-facing text should be placed in the appropriate localization files rather than hardcoded into C# source.

## License

This repository is **source-available, not open source**.

Personal reference, personal gameplay, private local modification, interoperability, and contributions through pull requests are permitted under the terms in [LICENSE.md](LICENSE.md).

Redistribution, alternate public releases, mirrors, binary reuploads, and Steam Workshop reuploads are not permitted without prior written permission from Cosmo.

Idol Manager, Unity, Harmony, BepInEx, DOTween, Steam, and any related names or assets belong to their respective owners.
