from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[1]
SOURCE = (ROOT / "src" / "BirthdayTextFix.cs").read_text(encoding="utf-8")
INFO = json.loads((ROOT / "assets" / "info.json").read_text(encoding="utf-8"))
PROJECT = (ROOT / "Birthday Text Fix.csproj").read_text(encoding="utf-8")


def ordinal_suffix(number: int) -> str:
    number = abs(number)
    last_two = number % 100
    if 11 <= last_two <= 13:
        return "th"
    return {1: "st", 2: "nd", 3: "rd"}.get(number % 10, "th")


def correct(rendered: str, age: int) -> str:
    age_text = str(age)
    i = rendered.find(age_text)
    if i < 0:
        return rendered
    suffix_i = i + len(age_text)
    current = rendered[suffix_i:suffix_i + 2]
    if current not in {"st", "nd", "rd", "th"}:
        return rendered
    return rendered[:suffix_i] + ordinal_suffix(age) + rendered[suffix_i + 2:]


expected = {
    1: "1st birthday",
    2: "2nd birthday",
    3: "3rd birthday",
    4: "4th birthday",
    11: "11th birthday",
    12: "12th birthday",
    13: "13th birthday",
    21: "21st birthday",
    22: "22nd birthday",
    23: "23rd birthday",
    31: "31st birthday",
    32: "32nd birthday",
    33: "33rd birthday",
    41: "41st birthday",
    42: "42nd birthday",
    43: "43rd birthday",
}

for age, wanted in expected.items():
    # Exercise every possible vanilla/static suffix so the repair is not tied
    # to one particular English localization typo.
    for wrong_suffix in ("st", "nd", "rd", "th"):
        rendered = f"{age}{wrong_suffix} birthday"
        assert correct(rendered, age) == wanted, (rendered, correct(rendered, age), wanted)

assert '[HarmonyPatch(typeof(Birthday_Popup), "RenderTitle")]' in SOURCE
assert 'staticVars.Settings.Language != "en"' in SOURCE
assert 'ExtensionMethods.GetOrdinal(age)' in SOURCE
assert 'Language.Insert("BD__TITLE_2", ageText)' in SOURCE
assert INFO["Author"] == "Cosmo"
assert INFO["Version"] == "1.0.0"
assert '<Version>1.0.0</Version>' in PROJECT
assert INFO["HarmonyID"] == "com.cosmo.birthdaytextfix"

print("Birthday Text Fix source and ordinal examples: PASS")
