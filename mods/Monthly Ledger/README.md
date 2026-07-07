# Monthly Ledger

Monthly Ledger adds an Action Hub button that opens a queued, game-style popup containing exact cash records for completed calendar months.

## Dependencies

- Mod Buttons
- IM Data Core 1.2.0 or newer
- IM UI Framework 1.3.0 or newer

The ledger records every company-money mutation. Business-contract entries retain the contract type, contractor, product, selected idol, payment, stamina, liability, multiplier, and negotiation count. Singles retain their title, group, participating idols, creative parameters, marketing, gross revenue, and production cost. Shows retain their title, medium, genre, host, cast, episode, audience, revenue, and weekly budget; when Fans Watch is active they also retain fan audience and fatigue.

The base game's combined weekly deduction is split into one snapshot per idol salary, one snapshot per staff salary, rent, and loan payments. Idol entries retain fame and scandal points; staff entries retain every skill's level and progress. Financing, story adjustments, cheats, and money changes from other mods remain visible so each month's net value reconciles with the company balance.

Historical values are not estimated. The first selectable month is the first complete calendar month after exact capture began, and the current unfinished month is never shown.

Month and record dates, the close control, and matching finance/category/detail labels are resolved from Idol Manager's active `Language.Data` table. Monthly Ledger's embedded localization provides mod-specific text and fallbacks where the base game has no semantically equivalent label.

## Build

`dotnet build "mods/Monthly Ledger/Monthly Ledger.csproj" -c Release`
