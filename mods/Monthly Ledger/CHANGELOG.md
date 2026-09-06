## 0.3.2

- Reserved prominent coverage warnings for positively incomplete months (gap, partial, or pre-coverage). Unknown/NotApplicable coverage metadata no longer produces a red warning after exhaustive pages match IM Data Core aggregate totals.
- Replaced the vague external-adjustments presentation with source-aware attribution. Third-party adjustments are grouped as Mod adjustments and split by source assembly; vanilla Controls-origin adjustments are labeled Manual / system adjustments; unattributed records remain explicitly marked as such.
- External adjustment rows now show the recorded source assembly and source type/method when IM Data Core captured them. Search also matches those source fields.

## 0.3.1

- Fixed the v6 coverage gate incorrectly hiding otherwise readable money transactions when coverage completeness was Unknown, Partial, pre-coverage, or otherwise not provably complete.
- Structured coverage now controls completeness warnings only; exact recorded rows are still read and displayed.
- Exhaustive page/aggregate parity remains mandatory before any month is shown.
- Added fallback navigation bounds from backend/career start when structured money coverage has no active interval.

## 0.3.0

- Updated Monthly Ledger for IM Data Core 3.4.33 sidecar-v6/journal-v3 structured money-history coverage.
- Replaced the legacy coverage-start marker with structured coverage plus exact range assessment.
- Replaced the 10,000-row capped read with exhaustive EventId-cursor paging.
- Cross-checks exhaustive page count/income/expense against IM Data Core's uncapped aggregate and fails closed on mismatch.
- Distinguishes complete/known-empty months from partial, gapped, pre-coverage, unknown, and unsupported coverage states.
- Uses an internal exact Int64 whole-yen formatter for every Monthly Ledger money label, including Int64.MinValue, without depending on SNLF formatting patches.
- Raised the IM Data Core requirement to 3.4.33.

# Changelog

## 0.2.6

- Replaced the red text `<` / `>` month controls with the same Singles-chart arrow-button templates used by Graduation Calendar, themed red per instance.
- Added dedicated staff-severance capture/detail output so severance payments no longer fall through as generic staff cost.
- Clarified idol weekly salary details as the amount actually paid that week. IM Data Core snapshots the final salary used by vanilla's weekly expense deduction after any automatic policy-driven salary adjustments.
- Raised the IM Data Core requirement to 3.4.5 for the new transaction attribution.

## 0.2.5

- Kept the visible transaction list capped at 10,000 rows while calculating income, expense, and net totals from IM Data Core's complete uncapped month aggregate.
- Updated the truncation notice to clarify that only the visible list is truncated and raised the IM Data Core requirement to 3.4.3 for the aggregate API.

## 0.2.4

- Switched the ledger from Unity `Scrollbar` sizing to IM UI Framework 2.1's exact scene-derived Producer Contracts/Salaries/Loans `Slider` pattern. The thumb is now the same fixed circular scene sprite instead of a proportional purple pill.
- Removed the ledger-specific fake scrollbar sizing/binding code and the duplicate custom gutter. The framework now owns the native list-scroll behavior.
- Added an explicit localized `No income` row with ¥0 for completed months containing no positive transactions.
- Retained the 0.2.3 game-font, search-caret, rounded-corner, and search-empty-state fixes.

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
