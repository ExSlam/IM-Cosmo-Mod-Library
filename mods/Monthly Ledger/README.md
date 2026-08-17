# Monthly Ledger

Monthly Ledger adds an Action Hub button that opens a queued, game-style popup containing exact cash records for completed calendar months.

## Dependencies

- Mod Buttons
- IM Data Core 2.0.0 or newer
- IM UI Framework 2.0.3 or newer


## 0.2.0 UI overhaul

- Uses IM UI Framework 2.0.2's vanilla asset layer for the real `Resources/scrollbar/Scrollbar` control and the real `Resources/button/basic/Pink` previous/next month buttons.
- Stops shrinking the popup close control to 32 px; it now uses the framework/game-standard 36 px height.
- Applies Idol Manager's currently selected game font to ledger TMP text. Bundled fonts are matched to loaded TMP assets when possible; custom/OS fonts use IM UI Framework's runtime TMP bridge.
- Income and expense categories can be collapsed independently. Their totals remain visible while details are hidden, and the expanded/collapsed state is retained while navigating months.
- Category headers reuse the vanilla idol-group list's exported Open/Closed collapse control when the game's `data_girls.prefab_group` reference is available, with a font-based fallback if it is not.
- Scroll behavior follows the base game's `ScrollRectDefault`: clamped movement, sensitivity 25 on Windows/Linux, and the game's macOS sensitivity/deceleration values.

The ledger records every company-money mutation. Business-contract entries retain the contract type, contractor, product, selected idol, payment, stamina, liability, multiplier, and negotiation count. Singles retain their title, group, participating idols, creative parameters, marketing, gross revenue, and production cost. Shows retain their title, medium, genre, host, cast, episode, audience, revenue, and weekly budget; when Fans Watch is active they also retain fan audience and fatigue.

Theater records retain a chronological daily attendance breakdown with theater name, performed schedule, audience type, attendance percentage, ticket price, and income. First-of-month streaming records retain subscription price, subscriber change, subscriber total, and monthly streaming income. Cafe records retain the dish of the day, working idols or unstaffed state, profit or loss, new fans, and appeal. Concert cost and revenue records retain the concert title, venue, ticket price, projected hype and attendance, final hype, revenue and profit, accident outcomes, and the ordered song/talk setlist with centers and talk participants.

The base game's combined weekly deduction is split into one snapshot per idol salary, one snapshot per staff salary, rent, and loan payments. Idol entries retain fame and scandal points; staff entries retain their localized job role and every skill's localized name, level, and progress. Financing, story adjustments, cheats, and money changes from other mods remain visible so each month's net value reconciles with the company balance.

When Assistant Manager is active, its custom staff types use the role title returned by that mod's Harmony-patched job-title lookup and retain both Production and Influence skill snapshots.

Historical values are not estimated. The first selectable month is the first complete calendar month after exact capture began, and the current unfinished month is never shown.

Month and record dates, the close control, and matching finance/category/detail labels are resolved from Idol Manager's active `Language.Data` table. Monthly Ledger's embedded localization provides mod-specific text and fallbacks where the base game has no semantically equivalent label.

## Build

`dotnet build "mods/Monthly Ledger/Monthly Ledger.csproj" -c Release`


## 0.2.1 UI pass
Monthly Ledger now uses the game's MUIP search input and scrollbar resources, a visible per-instance scrollbar color treatment, vanilla-style rounded panel sprites (preferring the producer Salaries popup), centered month navigation, and fixed idol-group collapse indicators. Search filters the displayed transaction records while leaving the monthly summary totals unchanged.


## 0.2.3 UI correction pass

The ledger now uses small 4-6 unit rounded radii through Idol Manager's own rounded-corner shader rather than reusing a rounded button sprite. The permanent scrollbar follows the Salaries popup's visual treatment with a pale right gutter and `mainScript.blue32` handle, the search caret is blue instead of white, and all dynamic empty/search-result text is forced through IM UI Framework's selected-game-font bridge.
