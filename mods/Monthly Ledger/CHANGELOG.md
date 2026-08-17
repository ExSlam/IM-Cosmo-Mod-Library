# Changelog

## 0.2.3
- Replaced the oversized rounded-button-sprite treatment with IM UI Framework 2.0.3's vanilla rounded-corner shader path and small Salaries-style radii (4-6 UI units).
- Reworked the ledger scrollbar to match the Salaries popup presentation: permanent right-side gutter, blue game-color handle, visible light track, explicit viewport gutter, and final sibling ordering above the mask.
- Changed the search caret from the MUIP prefab's white default to the game's blue color and added a matching translucent selection color.
- Fixed search empty/no-match states so their TMP text always receives the active game font even on early-return dynamic refresh paths.
- Reduced the prior scrollbar compensation padding while retaining enough right-side clearance for the permanent gutter.

## 0.2.2
- Fixed the MUIP search field build error caused by assigning `TMP_InputField.textComponent` (`TMP_Text`) to `TextMeshProUGUI`.
- Made both search text and placeholder handling use the base `TMP_Text` contract and assign the resolved game TMP font directly, avoiding subtype assumptions in MUIP prefabs.

## 0.2.1
- Restored a live transaction search field using Idol Manager's MUIP input-field prefab.
- Made the vanilla scrollbar visible on the ledger's white surface with an instance-local theme override.
- Fixed overlapping expanded/collapsed group arrows (`Open` vs `Opened`).
- Centered and tightened month navigation, reduced UI sizing slightly, and added compensating left content padding for the scrollbar.
- Added rounded corners to the popup, scroll surface, search field, summary cards, and category headers using the Salaries popup panel sprite when available, with the shipped MUIP rounded sprite as fallback.

## 0.2.0

- Overhauled Monthly Ledger UI for IM UI Framework 2.0.2.
- Uses the real vanilla scrollbar resource and pink basic button prefabs for month navigation.
- Uses the game-selected font through IMUI's bundled/external font bridge.
- Restored the close button to the standard 36 px scaffold height.
- Added collapsible income/expense categories modeled on the vanilla idol-group list and reusing its Open/Closed collapse visuals when available.
