#!/usr/bin/env python3
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parent.parent
source = (ROOT / 'src' / 'IdolCareerDiary.cs').read_text(encoding='utf-8-sig')

for token in (
    'EventConcertCardUsed = "concert_card_used"',
    'EventConcertCrisisDecision = "concert_crisis_decision"',
    'EventConcertCrisisApplied = "concert_crisis_applied"',
    'EventConcertFinalResolved = "concert_final_resolved"',
    'RenderConcertCardIncident(',
    'RenderConcertCrisisIncident(',
    'FindMatchingConcertCrisisAppliedIndex(',
    'RenderConcertOutcomeSummary(',
    'C.KeyAccidentSuccessChance',
    'C.KeyNoCriticalFailure',
    'C.KeyExpectedHypeDelta',
    'C.KeyHypeBefore',
    'C.KeyHypeAfter',
    'C.KeyHypeDeltaApplied',
    'C.KeyCardConsumed',
    'C.KeyCardAccidentHappeningBefore',
    'C.KeyCardAccidentHappeningAfter',
    'C.KeyCardAccidentSuccessBefore',
    'C.KeyCardAccidentSuccessAfter',
    'C.KeyCardNoCriticalFailureBefore',
    'C.KeyCardNoCriticalFailureAfter',
    'C.KeyNoAccidentCounter',
    'C.KeyUsedAccidentCount',
):
    assert token in source, token

method_start = source.index('private void RenderConcertDetailContext(')
method_end = source.index('private static string BuildConcertSetlistStatLine', method_start)
method = source[method_start:method_end]
assert 'C.TitleConcertEvents' in method
assert 'C.TitleConcertCardsUsed' not in method
assert 'C.TitleConcertDisasters' not in method
assert 'consumedAppliedEventIndexes' in method
assert 'RenderConcertOutcomeSummary(concertEvents);' in method

keys = (
    'TitleConcertEvents',
    'TitleConcertOutcome',
    'TextConcertCardUsed',
    'TextConcertCrisisEvent',
    'TextNoConcertEvents',
    'TextCardConsumed',
    'TextAccidentPreventionPercent',
    'TextCrisisSuccessBonusPercent',
    'TextCriticalFailureProtection',
    'TextHype',
    'TextSongsWithoutAccidents',
)
for language in ('en', 'cn', 'fr', 'jp', 'kr', 'ptbr', 'ru'):
    text = (ROOT / 'assets' / 'Localization' / language / 'strings.txt').read_text(encoding='utf-8-sig')
    values = {}
    for line in text.splitlines():
        if '=' in line and not line.lstrip().startswith('#'):
            key, value = line.split('=', 1)
            values[key] = value
    for key in keys:
        assert key in values and values[key].strip(), (language, key)

info = json.loads((ROOT / 'assets' / 'info.json').read_text(encoding='utf-8-sig'))
assert info['Version'] == '1.3.2', info['Version']
csproj = (ROOT / 'Idol Career Diary.csproj').read_text(encoding='utf-8-sig')
assert '<Version>1.3.2</Version>' in csproj

print('PASS: Idol Career Diary chronological concert incident presentation contract')
