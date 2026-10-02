#!/usr/bin/env python3
"""Static repository contract checks for Staff Portraits."""
from pathlib import Path
import json
import sys

MOD_ROOT = Path(__file__).resolve().parents[1]
REPO_ROOT = MOD_ROOT.parents[1]
LANGUAGES = ("en", "cn", "fr", "jp", "kr", "ptbr", "ru")


def require(condition, message):
    if not condition:
        print("FAIL: " + message, file=sys.stderr)
        raise SystemExit(1)
    print("PASS: " + message)


def load_strings(path):
    values = {}
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        values[key] = value
    return values


def main():
    info = json.loads((MOD_ROOT / "assets" / "info.json").read_text(encoding="utf-8"))
    require(info["Author"] == "Cosmo", "metadata credits Cosmo as author")
    require(info["Version"] == "1.0.0", "metadata version is 1.0.0")
    require(info["HarmonyID"] == "com.cosmo.staffportraits", "Harmony ID is stable")

    project = (MOD_ROOT / "Staff Portraits.csproj").read_text(encoding="utf-8")
    require("<Version>1.0.0</Version>" in project, "project version matches metadata")
    require("<Authors>Cosmo</Authors>" in project, "project credits Cosmo as author")
    require("<AssemblyName>com.cosmo.staffportraits</AssemblyName>" in project, "assembly identity matches Mod Buttons action")

    require(
        (MOD_ROOT / "LICENSE.md").read_bytes() == (REPO_ROOT / "LICENSE.md").read_bytes(),
        "mod license is a byte-for-byte root license copy",
    )

    english = load_strings(MOD_ROOT / "assets" / "Localization" / "en" / "strings.txt")
    english_keys = set(english)
    for language in LANGUAGES:
        localized = load_strings(MOD_ROOT / "assets" / "Localization" / language / "strings.txt")
        require(set(localized) == english_keys, f"localization keys match for {language}")
        require(all(value.strip() for value in localized.values()), f"localization values are non-empty for {language}")
        require("{0}" in localized["notification.applied"], f"notification placeholder is preserved for {language}")

    source = "\n".join(path.read_text(encoding="utf-8") for path in (MOD_ROOT / "src").glob("*.cs"))
    require("!staffer.IsIdol()" in source, "former-idol staff are excluded")
    require("staffer.type != staff._type.player" in source, "male producer staff record is excluded")
    require("staffer.type != staff._type.player_female" in source, "female producer staff record is excluded")
    require("staff._staff._unique_type.NONE" in source, "unique staff are excluded")
    require("data_girls_textures.textureAssets.Add" not in source, "staff portrait assets are not registered into the idol texture list")
    require("StaffPortraitsConstants.AssetIdPrefix" in source, "staff portrait save IDs are namespaced")
    require("PortraitCanvasWidthPixels = 1024" in source, "portrait pack canvas width is fixed to 1024 pixels")
    require("PortraitCanvasHeightPixels = 1500" in source, "portrait pack canvas height is fixed to 1500 pixels")
    require("value.Length > StaffPortraitsConstants.MaximumIdentifierCharacters" in source, "identifier length limit uses a named constant")
    require("StaffPortraitActions" in source and "OpenStaffStylist" in source, "Action Hub entry point exists")

    buttons = json.loads((MOD_ROOT / "assets" / "ModButtons" / "buttons.json").read_text(encoding="utf-8"))
    require(len(buttons) == 1, "exactly one Mod Buttons action is declared")
    require(buttons[0]["assembly"] == "com.cosmo.staffportraits", "Mod Buttons assembly matches project")
    require(buttons[0]["class"] == "StaffPortraits.StaffPortraitActions", "Mod Buttons class matches public action class")
    require(buttons[0]["method"] == "OpenStaffStylist", "Mod Buttons method matches public action")

    print("\nStaff Portraits source contract checks passed.")


if __name__ == "__main__":
    main()
