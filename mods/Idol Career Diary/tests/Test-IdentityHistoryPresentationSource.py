#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parent.parent
source = (ROOT / 'src' / 'IdolCareerDiary.cs').read_text(encoding='utf-8-sig')

for token in (
    'EventIdolNameChanged = "idol_name_changed"',
    'EventIdolNationalityChanged = "idol_nationality_changed"',
    'BuildIdolIdentityEditPresentation(',
    'C.TextNameTransitionFormat',
    'C.TextNationalityTransitionFormat',
    'C.TextDisplayedNameTransitionFormat',
    'C.JsonIdolOldDisplayName',
    'C.JsonIdolNewDisplayName',
    'C.JsonIdolOldNationalityName',
    'C.JsonIdolNewNationalityName',
):
    assert token in source, token

keys = (
    'TextIdolNameChanged',
    'TextIdolNationalityChanged',
    'TextNameTransitionFormat',
    'TextNationalityTransitionFormat',
    'TextDisplayedNameTransitionFormat',
)
for language in ('en', 'cn', 'fr', 'jp', 'kr', 'ptbr', 'ru'):
    text = (ROOT / 'assets' / 'Localization' / language / 'strings.txt').read_text(encoding='utf-8-sig')
    values = {}
    for line in text.splitlines():
        if '=' in line and not line.lstrip().startswith('#'):
            key, value = line.split('=', 1)
            values[key] = value
    for key in keys:
        assert key in values, (language, key)
    for key in ('TextNameTransitionFormat', 'TextNationalityTransitionFormat', 'TextDisplayedNameTransitionFormat'):
        value = values[key]
        assert value.count('{0}') == 1, (language, key, value)
        assert value.count('{1}') == 1, (language, key, value)

print('PASS: Idol Career Diary identity history presentation and localization contract')
