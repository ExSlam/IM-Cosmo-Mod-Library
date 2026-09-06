# Monthly Ledger

Monthly Ledger adds an Action Hub button that opens a queued, game-style popup containing exact cash records for completed calendar months.

## Dependencies

- Mod Buttons
- IM Data Core 3.4.33 or newer with the current sidecar-v6/journal-v3 money-history contract
- IM UI Framework 2.1.0 or newer

## Current behavior

Month navigation uses the same scene-derived previous/next controls as the Singles chart/Graduation Calendar, with a red per-instance theme. The ledger uses the scene-native Producer Contracts/Salaries/Loans list-scroll pattern: a separate vertical `Slider` with a fixed circular handle and thin track, not `ScrollRect.verticalScrollbar`.

Monthly Ledger exhaustively pages IM Data Core's money history by EventId until the selected month is complete, including dense months with more than 10,000 transactions. It independently asks IM Data Core for the uncapped aggregate and refuses to present the month if paged row count/income/expense disagree with that aggregate. Structured v6 coverage is shown explicitly: fully covered empty months are known-empty, while partial/gapped months are labeled incomplete instead of being mistaken for complete history. Search filters only the displayed transaction records and does not change summary totals. Coverage uncertainty alone is quiet once exhaustive pages agree with IM Data Core aggregates; prominent warnings are reserved for known partial/gapped/pre-coverage months. External adjustments are attributed to their recorded source assembly/call when available instead of being presented as one opaque bucket.

The UI uses Idol Manager's selected game font, the game's small rounded-corner shader treatment, a MUIP search input with game-font text, and vanilla-style collapse indicators for income/expense categories. The current unfinished month is never shown, and the first selectable month is the first complete calendar month after exact capture began.

Months with no positive transactions render a localized **No income** row with ¥0.

## Captured detail

The ledger records company-money mutations and keeps source-specific detail when IM Data Core captured it:

- business contracts: contract type, contractor, product, selected idol, payment, stamina, liability, multiplier, and negotiations;
- singles: title, group, participating idols, creative parameters, marketing, gross revenue, and production cost;
- shows: title, medium, genre, host, cast, episode, audience, revenue, weekly budget, and compatible fan/fatigue details;
- theaters: chronological attendance detail, schedule, audience type, ticket price, and income;
- streaming: subscription price, subscriber change/total, and monthly streaming income;
- cafes: dish of the day, staffing, profit/loss, fans, and appeal;
- concerts: title, venue, ticket price, projected/final hype and attendance, revenue/profit, accident outcomes, and ordered song/talk setlist details;
- weekly deductions: separate idol salary, staff salary, rent, and loan-payment rows;
- staff severance: staff member, localized role, payment, and skill snapshot.

Weekly idol salary rows report **Paid this week** and retain the exact salary included in vanilla's weekly deduction after automatic policy-driven salary adjustments. Idol entries also retain fame/scandal context; staff entries retain localized role and skill information.

Financing, story adjustments, cheats, and money changes from other mods remain visible so each month's net value reconciles with the company balance.

When Assistant Manager is active, its custom staff types use the role title returned by that mod's Harmony-patched job-title lookup and retain both Production and Influence skill snapshots.

## Localization

Month and record dates, the close control, and matching finance/category/detail labels are resolved from Idol Manager's active `Language.Data` table. Monthly Ledger's embedded localization supplies mod-specific strings and English fallback when the base game has no semantically equivalent label.

## Build

```powershell
dotnet build "mods/Monthly Ledger/Monthly Ledger.csproj" -c Release
```

Release history belongs in [`CHANGELOG.md`](CHANGELOG.md).
