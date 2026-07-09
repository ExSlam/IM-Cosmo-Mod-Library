# Monthly Ledger

Monthly Ledger adds an Action Hub button that opens a queued, game-style popup containing exact cash records for completed calendar months.

## Dependencies

- Mod Buttons
- IM Data Core 1.2.0 or newer
- IM UI Framework 1.3.0 or newer

The ledger records every company-money mutation. Business-contract entries retain the contract type, contractor, product, selected idol, payment, stamina, liability, multiplier, and negotiation count. Singles retain their title, group, participating idols, creative parameters, marketing, gross revenue, and production cost. Shows retain their title, medium, genre, host, cast, episode, audience, revenue, and weekly budget; when Fans Watch is active they also retain fan audience and fatigue.

Theater records retain a chronological daily attendance breakdown with theater name, performed schedule, audience type, attendance percentage, ticket price, and income. First-of-month streaming records retain subscription price, subscriber change, subscriber total, and monthly streaming income. Cafe records retain the dish of the day, working idols or unstaffed state, profit or loss, new fans, and appeal. Concert cost and revenue records retain the concert title, venue, ticket price, projected hype and attendance, final hype, revenue and profit, accident outcomes, and the ordered song/talk setlist with centers and talk participants.

The base game's combined weekly deduction is split into one snapshot per idol salary, one snapshot per staff salary, rent, and loan payments. Idol entries retain fame and scandal points; staff entries retain their localized job role and every skill's localized name, level, and progress. Financing, story adjustments, cheats, and money changes from other mods remain visible so each month's net value reconciles with the company balance.

When Assistant Manager is active, its custom staff types use the role title returned by that mod's Harmony-patched job-title lookup and retain both Production and Influence skill snapshots.

Historical values are not estimated. The first selectable month is the first complete calendar month after exact capture began, and the current unfinished month is never shown.

Month and record dates, the close control, and matching finance/category/detail labels are resolved from Idol Manager's active `Language.Data` table. Monthly Ledger's embedded localization provides mod-specific text and fallbacks where the base game has no semantically equivalent label.

## Build

`dotnet build "mods/Monthly Ledger/Monthly Ledger.csproj" -c Release`
