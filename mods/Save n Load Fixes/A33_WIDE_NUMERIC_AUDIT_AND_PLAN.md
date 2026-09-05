# SNLF-A33 — Wide numeric continuity and overflow repair

Status: A33.1-A33.6 implemented in v0.54.0; live Unity checkpoint matrix pending
Audit date: 2026-09-01  
SNLF audit baseline: 0.52.0; current implementation: 0.54.0
Game source baseline: supplied decompiled Idol Manager source plus the installed English JSON constants

## 1. Verdict

The suspected defect is real, but it is not one global `money is Int32` mistake.
Vanilla's authoritative resource table, `resources.Money()`, most money histories,
and many project totals already use signed `Int64`. The failures occur where vanilla:

1. calculates a scalable result in `Int32` and widens it only after overflow;
2. routes an `Int64` value through `Single` and an `Int32` rounding API;
3. stores a scalable fan, subscriber, project, or story value in an `Int32` DTO; or
4. sums or multiplies `Int64` values without checked overflow handling.

This audit establishes a new SNLF repair family:

> **SNLF-A33 — Wide numeric continuity and overflow repair**

A33 is required for SNLF's stated goal that loading resumes the exact saved state.
It must cover both stock-reachable overflow and calculations whose current stock cap is
only a UI/config accident. In particular, `Theaters._theater.GetTicketSales()` must
widen before multiplication. At a modded ticket price of ¥99,000,000, its exact result is:

```text
250 visitors × ¥99,000,000 = ¥24,750,000,000
```

Vanilla currently evaluates the two `Int32` factors first and casts the already-wrapped
result to `Int64`. A33 must compute `checked((long)visitors * ticketPrice)`. The existing
theater stat and save DTO already store `Revenue` as `Int64`, so this particular fix needs
no supplemental shadow field.

## 2. Ledger status

This document is an explicit post-freeze audit-ledger reopen, not a silent alteration of
the frozen 32-family/47-regression baseline in `POST_AUDIT_IMPLEMENTATION_PLAN.md`.
Accepting A33 makes it the 33rd additional SNLF repair family. Its provisional regression
suite uses `A33-WN-*` identifiers until the tests exist and are deliberately merged into
the global count.

At audit freeze, this document changed no code or release metadata. The first A33.1 code
update advanced SNLF from 0.52.0 to 0.53.0; the cumulative A33.2-A33.6 implementation is
now 0.54.0 as described in section 14.

### 2.1 Implemented cumulatively through v0.54.0

The A33.1 foundation and cumulative A33.2-A33.6 implementation provide:

- pure checked signed-`Int64` arithmetic and exact rational/decimal rounding helpers;
- an idempotent 17-target Harmony health manifest verified against the installed game
  and HarmonyX assemblies, including actual isolated-process patch application;
- preflight guards for `resources._Add` and `resources._fan.AddPeople` while leaving
  vanilla clamps, fan distribution, statistics, and observer callbacks intact;
- checked core fan totals plus show/loan aggregates;
- all nine mandatory section-6 late-widen repairs;
- exact theater oracles through `250 × Int32.MaxValue`, including
  `250 × ¥99,000,000 = ¥24,750,000,000`;
- exhaustive bit-identical concert results across every stock venue, difficulty,
  discount, and the complete legal 0-10-song domain, with exact wide behavior outside
  that domain; and
- named failure latching that blocks checkpoint writes but preserves in-game load as a
  recovery action;
- transactionally preflighted money/accounting, rent, business, loan, salary, tour,
  theater, café, project, resource, and fan mutations, including mod-expanded inputs;
- checked fan allocation/conservation, sales, show, single, demographic, subscriber,
  history, and story calculations without `Int64 -> Single -> Int32` authority loss;
- canonical decimal-string wide shadows with strict identity/count/mirror witnesses for
  every audited narrow persistent endpoint and version-1 backward compatibility;
- exact gameplay, comparison, script, tooltip, animation, and popup consumers, with
  compatibility clamping only where an unchanged vanilla ABI is inherently `Int32`;
- guarded persistent lifetime counters and identity allocators that refuse exhaustion;
  and
- complete recomposition-idempotent target health integrated with the checkpoint gate.

All six implementation segments are present. Compilation, decompiled-source contracts,
implementation contracts, the pure numeric harness, and the isolated envelope/Harmony
runtime harness pass. Live Unity overwrite-save, Save As, autosave, F9, restart,
repeated-load, and mod-combination behavior remains the final release qualification gate.

## 3. Audit boundary and method

The audit covered every supplied `*.cs` file and traced the following from producer to
consumer and, where applicable, into `SaveManager.SavedData` DTOs:

- money, cost, price, payment, profit, revenue, rent, salary, liability, and debt;
- fans, sales, audience, subscribers, attendance, and their histories;
- all `Mathf.RoundToInt`, `Mathf.CeilToInt`, and `Mathf.FloorToInt` sites;
- casts from `Int64` to `Int32` or `Single`;
- casts to `Int64` that occur after an `Int32` expression;
- `Int32` accumulators fed by scalable values;
- unchecked `Int64` add, subtract, multiply, and aggregate paths;
- persistent `Int32` counters and identity allocators; and
- UI, VN/script, eligibility, comparison, and save/load consumers of the affected values.

Every candidate was classified using its complete source path and installed constants,
not by its variable name alone. Layout coordinates, enum ordinals, bounded percentages,
stamina/skill values, authored dialogue integers, and similar non-scalable domains are
not wide-numeric state merely because they use `Int32`.

The supported mod-aware rule is:

> Any scalar input that fits its vanilla field type may be supplied by a mod. Every
> aggregate, product, delta, and authoritative result must be evaluated in checked
> `Int64` before narrowing or display conversion.

This does not promise support for an input that cannot be represented by the vanilla
field or for more than `Int32.MaxValue` collection entries. Those are ABI/cardinality
horizons and must fail closed before identity collision or collection corruption.

## 4. Proven stock-reachable failures

### 4.1 Rent and agency occupancy

`resources.GetRentPerFloor(int)` returns `Int32` and calls recursive
`MathFunctions.Fibonacci(int, int)`. `GetRoomRent`, `Money_Rent`, and the weekly expense
path retain `Int32` arithmetic until after the result can wrap. Floors are created as
rooms fill and no source-level maximum floor count was found.

Using the installed `rentPerBlock = 10,000` and difficulty multipliers:

| Difficulty | First demonstrated per-floor overflow | Exact value |
| --- | ---: | ---: |
| Easy | floor 28 | `10,000 × F(28) = 3,178,110,000` |
| Normal | floor 25 | `30,000 × F(25) = 2,250,750,000` |
| Hard | floor 24 | `60,000 × F(24) = 2,782,080,000` |

Room-space multiplication and the agency-wide sum can cross the boundary earlier.
The 20% rent discount also currently passes the accumulated value through an `Int32`
rounder.

### 4.2 Tours

`SEvent_Tour.tour` and its save DTO store `ProductionCost`, `ExpectedRevenue`, `Saving`,
`Revenue`, `NewFans`, and country results as `Int32`. `RecalcProductionCost()` sums in
`Int32`, and `GetProfit()` evaluates `Revenue + Saving - ProductionCost` in `Int32`.

The installed game has 18 countries. Selecting level 5 for every country gives:

| Quantity | Exact value |
| --- | ---: |
| Gross production cost | ¥2,252,316,000 |
| Saving | ¥387,586,800 |
| Net cost | ¥1,864,729,200 |
| Maximum revenue | ¥1,943,280,000 |
| `Revenue + Saving` | ¥2,330,866,800 |
| Exact profit | ¥78,550,800 |

Both gross cost and the intermediate revenue-plus-saving sum overflow `Int32` in a
completely legal stock tour.

### 4.3 Long-running shows

Show audience and revenue series are `Int64`, but episode fan gains are `Int32`.
`GetTotalProfit()` also computes `GetBudget() * revenue.Count` in `Int32` before widening.

The installed maximum per-episode cost is ¥11,000,000. The aggregate remains safe at
195 episodes (`¥2,145,000,000`) and overflows at episode 196 (`¥2,156,000,000`), which is
reachable after roughly 3.77 years of weekly episodes.

### 4.4 Scalable fan and subscriber state

Individual fan buckets are `Int64`, but group fan helpers cast each bucket to `Int32`
and sum into `Int32`. Those helpers feed singles, shows, theaters, café worker ordering,
appeal calculations, story targets, and UI. Theater subscriber buckets, per-day
subscriber deltas, show per-episode fans, single release bonus fans, and Stats fan
histories are also stored as `Int32` even though their source populations are unbounded.

These paths either wrap, saturate, or lose the exact value before it reaches an existing
`Int64` consumer.

## 5. Complete affected subsystem matrix

`Persist` means that an exact value cannot be represented by the vanilla DTO and needs
an A33 repair-envelope shadow. `Derived` means the result must be recomputed widely but
does not need duplicate storage.

| Subsystem | Source-proven defect | Required A33 treatment | Persist | Priority |
| --- | --- | --- | --- | --- |
| Core resources | `resources._Add` performs unchecked `current + val`; fan `AddPeople` can wrap and then clamp the wrapped negative value to zero | Checked resource and fan mutations; calculate before commit; checked aggregates | No | P0 |
| Rent/agency | recursive `Int32` Fibonacci, per-room multiplication, aggregate, discount, and weekly debit | Iterative checked `Int64` rent helper; patch all gameplay and UI consumers | Derived | P0 |
| Tours | aggregate fields and profit intermediates are `Int32`; `Single`/`RoundToInt` conversions | Wide authoritative tour state, calculations, affordability, finish mutation, UI, and DTO mirror | Yes | P0 |
| Shows | cost-times-episode late widening; `Int64` totals averaged through `Single`/`RoundToInt`; per-episode fans and their sum are `Int32` | Checked totals and exact averages; wide episode-fan series and consumers | Yes for fan series | P0 |
| Groups | idol `Int64` fan buckets are cast and summed as `Int32` | SNLF `Int64` group fan helpers; patch every consumer seam | Derived | P0 |
| Singles | base sales and marketing transforms use `RoundToInt`; proportional redistribution uses lossy `Single`; release bonus fans are `Int32`; totals/products are unchecked | Wide sales/fan calculation, exact deterministic rounding, checked totals/products, wide release bonus records | Yes for three release bonus totals | P0 |
| Theaters | subscriber target/delta/buckets are `Int32`; subscription aggregate can overflow; ticket multiply widens too late | Wide subscribers and stats; checked ticket/subscription revenue and daily/weekly sums | Yes for subscribers/deltas | P0 |
| Stats | weekly fan total/delta is saturated to `Int32`; money deltas and income sums are unchecked | Wide fan histories; checked money histories | Yes for fan histories | P0 |
| Story tasks | chapter 3/4 fan targets narrow scalable fan totals through `Int32` and `Single` | Wide target creation, comparisons, UI/VN variables, and exact shadow records | Yes | P0 |
| Cafés | build threshold `(count-4)*5,000,000`, occupancy math, daily income, and weekly earnings use `Int32`; popular-worker selectors cast fans to `Int32` | Wide derived thresholds/aggregates and fan ranking; wide café stat shadows if modded per-café outputs exceed `Int32` | Conditional | P1 |
| Business | weekly profit is `Int64` but daily profit is `Int32`; fame/buzz sums are `Int32`; history/liability/earned-money products widen after evaluation | Checked wide aggregates and early widening; patch daily mutations and consumers | No for stock rows | P0 |
| Fan distribution | daily decay, equal distribution, demographic distribution, award multipliers, and oshihen paths route `Int64` totals through `Single`; residue loop is bounded to 1,000 rounds | Exact deterministic wide allocation with conservation and the same safe-range ordering/RNG behavior | No | P0 |
| Rivals | monthly fan and sales growth uses `RoundToInt((float)long × coeff)` | Wide coefficient rounding and checked assignment | No; fields already `Int64` | P1 |
| SSK | 10% of single production cost uses `RoundToInt`; votes use `Single` fan arithmetic | Wide production-cost and vote calculations | No; totals already `Int64` | P1 |
| Concerts | revenue and sold-ticket calculations are `Single`; production cost widens after an `Int32` sum/product | Checked wide capacity/price/cost math and exact coefficient rounding | No; persisted result fields are `Int64` | P1 |
| Research | `Buying_Cost` is `Int64` but 20% growth is performed through `Single` | Checked exact 20% rounding | No | P1 |
| Loans | total weekly payment is `Int32`; availability contains a late cast; payment calculation uses `Single` and stores `Int32` | Wide aggregate/availability/debt arithmetic; wide per-loan payment shadow when outside `Int32` | Yes when needed | P1 |
| Salaries/severance | idol salaries are `Int64` but raises/satisfaction use `Single`; staff severance compares `num*2` before widening and casts negative/out-of-range money to `Int32` | Wide salary arithmetic; early-widen severance; nonpositive available money yields zero severance | No for idol salary; conditional for derived staff values | P1 |
| Event/VN scripts | event money requirement converts money to `Single`; `last_single_production` returns `Int32`; chapter-2 fan action narrows before applying its cap | Use the existing `Int64` requirement evaluator; patch named action branches to wide calculation | No | P0 |
| UI/release screens | several long sales/audience/money values are converted through `Single` and `FloorToInt` for counters/animations | Keep authoritative/display target as `Int64`; bound animation duration/progress separately | No | P1 |
| Lifetime counters | action, injury, relationship, award, concert, and similar counters increment unchecked `Int32` | Checked fail-closed increment at minimum; optional wide shadow for unlimited-time support | Optional policy | P2 |
| Persistent IDs | girl/staff/project/loan/theater/café/floor and related allocators use `Int32` and `max + 1` | Guard before `Int32.MaxValue`; refuse allocation instead of wrap/collision | No viable wide ABI shadow | Horizon |

## 6. Mandatory early-widen sites

The following source sites explicitly cast only after an `Int32` expression. Current
stock bounds do not exempt them. A33 must replace each relevant expression with checked
wide arithmetic before the first operator:

| Source seam | Vanilla shape | A33 shape |
| --- | --- | --- |
| `business.Accept` history money | `(long)(payment * duration * 4)` | checked `Int64` multiply chain |
| `business._proposal.GetLiability` | `(long)(payment * 2 * duration * 4)` | checked `Int64` multiply chain before decimal coefficient |
| `business.active_proposal.GetMoneyEarned` | `(long)((12-weeks) * Payment_per_week)` | checked `Int64` operands before multiply |
| `data_girls` group buzz award | `(long)(count * value)` | checked `Int64` multiply |
| `loans.GetTotalAvailableAmount` | `(long)(1,000,000 * fame * fame)` | checked `Int64` multiply chain |
| `SEvent_Concerts._concert._projectedValues.GetProductionCost` | cast after base-cost plus song-count product | checked `Int64` sum/product before coefficient |
| `Shows._show.GetTotalProfit` | `(long)(budget * episodeCount)` | checked `Int64` multiply |
| `Staff_Fire.GetSeverance` | `(long)(severance * 2)` | checked `Int64` doubling and comparison |
| `Theaters._theater.GetTicketSales` | `(long)(visitors * Ticket_Price)` | `checked((long)visitors * Ticket_Price)` |

The theater patch must include at least these regression oracles:

```text
250 × 40,000      = 10,000,000
250 × 99,000,000  = 24,750,000,000
250 × 2,147,483,647 = 536,870,911,750
```

`Theaters._theater._stat.Revenue` and `Theaters.TheaterData._stat.Revenue` are already
`Int64`; A33 must let the exact wide result flow into those fields unchanged.

## 7. Bounded and negative controls

The audit also closed false positives. They remain useful regression controls, but a
bounded result is not permission to evaluate an aggregate in `Int32`.

- Activity level tables, audition base prices, policy prices, spa prices, influence
  costs, summer-game research costs, and authored event-template integers are scalar
  `Int32` inputs. They may remain ABI-compatible inputs; any later sum/product or resource
  mutation must be wide and checked.
- Theater attendance remains bounded by capacity, but revenue is not bounded by that
  fact because ticket price is independently modifiable.
- Concert's stock setlist and venue costs happen to keep base cost below `Int32`, but the
  cost expression still requires early widening for mod-expanded values.
- Stock loans cap the legal weekly aggregate near ¥400,000,000. The aggregate and
  availability expressions are still widened, and the lossy payment formula is repaired.
- Scandal points normally reach game-over near 20 and buzz is capped, but malformed or
  mod-expanded inputs must not wrap before the cap/observer notification.
- `Date_Influence` methods named `*_Cost` spend influence points, not money. They stay in
  their bounded gameplay domain unless a separate audit proves that domain scalable.
- Layout, pixel, percentage, stamina, skill, enum, and audio counters are not A33 state.

## 8. Numeric architecture

### 8.1 Do not rewrite Assembly-CSharp ABI

Changing vanilla fields or method return types from `Int32` to `Int64` would break
compiled call sites, Harmony targets, reflection consumers, Unity serialization shape,
and other mods. A33 instead adds SNLF-owned helpers, shadows, and narrow Harmony patches.

Vanilla methods that must keep returning `Int32` return a deterministic compatibility
mirror:

- exact value when the authoritative value is in range;
- `Int32.MinValue`/`Int32.MaxValue` clamp when it is not; and
- no SNLF gameplay consumer may read the clamped mirror as authoritative.

### 8.2 Authoritative domain

Use signed `Int64`, not `BigInteger`, for gameplay quantities. Signed values are required
because money and deltas may be negative. `Int64` also matches the game's resource and
existing large-value DTO contracts.

Create one internal numeric substrate, tentatively `WideNumericMath`, with:

- checked add, subtract, negate, multiply, and aggregate helpers;
- checked multiply-add helpers that never overflow in an intermediate;
- exact quotient/remainder allocation helpers;
- deterministic clamp helpers only for compatibility mirrors;
- exact rounding helpers for compiled `Single` coefficients; and
- a named fault result carrying subsystem, method, operands, and operation.

For a coefficient stored as `Single`, derive its exact rational value from its IEEE-754
bits. In the source-proven safe domain, retain vanilla behavior bit-for-bit. Outside that
domain, apply the exact compiled coefficient with explicitly tested floor/ceil/nearest
semantics. Do not route the wide operand through `Single`.

### 8.3 Failure semantics

All inputs and all deltas for a mutation must be calculated and checked before any live
state changes. If the exact result exceeds signed `Int64`:

1. abort the whole gameplay mutation;
2. retain the prior live state;
3. latch a named A33 health failure and log the operands;
4. block repair-dependent checkpoint writes; and
5. never wrap, guess, or silently saturate authoritative state.

Pure queries using only `Int32` factors, including theater tickets, are mathematically
safe in `Int64`; their checked helper exists to freeze the contract and catch later type
changes.

### 8.4 Shadow ownership

Use load-epoch-bound SNLF shadow registries keyed by stable vanilla identity. Clear them
before target reconstruction and populate them only from a validated target envelope or
from a source-proven legacy witness. Do not use IMDataCore history as restore authority.

## 9. A33 envelope section

Keep repair-envelope root format version 1 and add an optional, section-marked record:

```text
records.wide_numeric_state_version = 2
records.wide_numeric_state = { typed sparse sections }
```

Every A33 `Int64` shadow is serialized as a deliberate canonical decimal string. The
accepted grammar is exactly:

```text
0
-?[1-9][0-9]*
```

Parsing must reject plus signs, leading zeroes, surrounding whitespace, exponent form,
decimal points, grouping separators, non-ASCII digits, and values outside signed
`Int64`. Serialize, parse, and compare with `InvariantCulture`.

Typed sparse sections are required for:

1. tour aggregate and per-country wide values keyed by tour ID and country identity;
2. single release `NewFans`, `NewHardcoreFans`, and `NewCasualFans` keyed by single ID;
3. show per-episode fan values keyed by show ID and episode ordinal;
4. theater subscriber buckets keyed by theater ID and demographic tuple;
5. theater stat subscriber deltas keyed by theater ID and stat date/ordinal;
6. Stats `total_fans_per_week` and `fans_change_per_week` series;
7. story `ch3_aya_fans`, `ch4_fans_needed`, and the chapter-four scandal baseline with
   explicit presence flags;
8. loan weekly payments when the exact value is outside the vanilla field; and
9. café daily profit/stat values only when mod-expanded inputs make the exact value
   exceed the vanilla `Int32` field.

The section must include count/identity witnesses sufficient to prove that each shadow
matches the exact vanilla DTO occurrence. Present malformed, duplicated, orphaned, or
partially matching records invalidate the A33 section and prevent an exact-load claim.
An authoritative empty version-2 section is distinct from an older envelope that lacks
the marker. Version 1 remains readable; because it predates the chapter-four scandal
baseline, that one value is legacy-seeded and is never falsely reported as exact.

## 10. Legacy and recovery rules

A v0.52 or older envelope without `wide_numeric_state_version` is a legacy A33 absence,
not an invalid envelope. Exact recovery is allowed only from an independent witness:

- group totals can be recomputed from saved idol `Int64` fan buckets;
- current money is already saved as `Int64`;
- a tour may be recomputed only when its saved selections and a trusted exact config
  fingerprint uniquely determine the value; and
- derived aggregates may be recomputed from complete exact component rows.

Exact history cannot be recovered from a wrapped/saturated `Int32` alone. In particular,
old show fan histories, single release bonus fan totals, Stats fan histories, theater
subscriber values, and story targets may have many possible preimages. For those cases,
SNLF may provide a deterministic compatibility fallback, but it must log that the
original state was not recovered and must not label the load exact.

## 11. FixSaveFile and JSON changes

The current raw-text `val` to `_val` migration remains the correct design. A33 must not
make `FixSaveFile` parse and reserialize the whole save, and it must not patch SimpleJSON
globally.

Required codec work is limited to the new schema:

1. teach the finite envelope serializer/materializer the typed A33 sections;
2. validate every deliberate wide decimal string canonically;
3. distinguish those schema-declared strings from accidental quoted numeric/Boolean
   scalars when detecting the known SimpleJSON rewrite signature;
4. retain the rule that compatibility recovery is allowed only for a uniformly rewritten
   envelope with a unique typed interpretation;
5. retain forward-image/preimage checks for old unwitnessed `Int64` numerics;
6. leave existing vanilla `Int64` fields as bare JSON numbers; and
7. prove that raw migration preserves every pre-existing byte except inserted underscores
   in the audited legacy girl-parameter key.

Because A33 wide shadows are deliberate strings, a future SimpleJSON pass cannot destroy
their integer precision. Their section marker and schema type make them ordinary valid
strings, not evidence that the whole envelope was rewritten.

No additional `FixSaveFile` rewrite is needed for theater revenue: its vanilla DTO member
is already `Int64`, and the existing raw migration preserves its numeric token unchanged.

## 12. Implementation segments

### A33.1 — Numeric substrate and health

- add `WideNumericMath` and `WideNumericPatchHealth`;
- add exact safe-domain differential tests;
- patch checked core resource/fan mutations and aggregates;
- patch all mandatory late-widen sites in section 6, including theater ticket sales; and
- latch/fail closed on signed `Int64` overflow before mutation.

### A33.2 — Money and accounting

- rent/agency;
- tours, shows, singles, SSK, and concerts;
- business, cafés, theaters, research, and loans;
- salaries/severance and event/VN money branches; and
- all money tooltips, affordability checks, and finish/credit/debit sinks.

### A33.3 — Fans and simulation

- wide group fan queries;
- deterministic fan distribution/decay/oshihen;
- singles, shows, theaters, rivals, SSK votes, café ranking, and story actions; and
- conservation and safe-range RNG-order checks.

### A33.4 — Exact persistence

- add the section-marked envelope DTOs;
- freeze shadows on the save caller thread after vanilla DTO population;
- perform raw-presence and complete round-trip validation before queue admission;
- clear/rebind by load epoch and stable identity; and
- implement honest legacy-absence handling.

### A33.5 — Consumers and compatibility

- patch UI counters, animation targets, tooltips, requirements, comparisons, and VN
  formatting to consume authoritative wide values;
- keep bounded visual progress/duration independent of the value displayed; and
- validate SNLF-absent loading of clamped vanilla mirrors.

### A33.6 — Long-horizon closure

- guard persistent `Int32` lifetime counters before wrap;
- decide, document, and test which counters receive wide shadows;
- guard every persistent allocator before `Int32.MaxValue`; and
- reject identity exhaustion rather than wrap or reuse an ID.

Each segment is independently patch-health-gated. A partial or recomposed Harmony shape
must disable only the affected A33 authority and must never claim exact protection.

## 13. Candidate Harmony target manifest

These are the source-level targets/consumers found by the audit. Before each implementation
segment, freeze the exact overload and IL-site counts in its source and contract tests.
Do not treat this list as permission for an unbounded namespace-wide patch.

### Core resources and rent

- `resources.Set`, `resources._Add`, `resources.GetFansTotal`,
  `resources.Money_BusinessContracts`, `resources.GetRentPerFloor`,
  `resources.GetRoomRent`, `resources.Money_Rent`, `resources.Money_StaffSalary`,
  `resources.Money_GirlsSalary`, `resources.Money_WeeklyExpenses`,
  `resources.Money_WeeklyProfit`, `resources.Money_DailyProfit`,
  `resources.DailyFansChange`, `resources.OnNewWeek`, `resources.OnNewDay`, and
  `resources._fan.AddPeople`;
- `agency.GetRoomRent`, the room construction debit, and every rent/room tooltip consumer;
- fame/scandal observer conversions reached from `Set/_Add`, with bounded compatibility
  notifications after the authoritative wide mutation.

### Projects

- tour: `SEvent_Tour.FinishTour`, tour save/load copy helpers, and
  `tour.GetProfit`, `AddRevenue`, `AddFans`, `GetAttendance`,
  `GetNewFansByAttendance`, `GetTotalAudience`, `GetNewFans`, `GetSaving`,
  `GetProductionCost`, `RecalcProductionCost`, and `RecalcExpectedRevenue`;
- show: show save/load copy helpers and `_show.GetAverageParam(List<long>)`, both
  `GetTotalParam` overloads, `GetTotalProfit`, `GetAllNewFans`, `SetSales`,
  `SetNewFans`, `SetRevenue`, and affected getters;
- single: single save/load copy helpers and `GenerateSales`, `ValueAfterMarketing`,
  `AddBonusFans`, `AddNewFans`, both fan-allocation helpers, `_single.GetTotalSales`,
  `_single.GetTotalNewFans`, `_single.GetProductionCost`, `_single.GetMoney`, and
  `_single.SetReleaseData`;
- concert: concert save/load copy helpers and `_projectedValues.GetNumberOfSoldTickets`,
  `GetRevenue`, `GetActualProfit`, `SetAttendance`, and `GetProductionCost`;
- SSK: `_SSK.TotalProductionCost`, `_SSK.GetProductionCost`, `_SSK.GenerateResults`,
  and result/vote consumers;
- rivals: `Rivals.OnNewMonth` fan and sales assignments.

### Fans, theaters, Stats, and story

- `Groups._group.GetFansOfType` overloads and every affected appeal/single/show/theater/
  café consumer;
- `data_girls.AddFans_Equally` overloads, `data_girls.AddFans`,
  `data_girls.AddFans_Oshihen`, `girls.AddFans` overloads, and `girls.GetFansToAdd`;
- `Theaters.CompleteDay`, `Theaters.GetLastWeekEarning`, and theater
  `GetAvgRevenue`, `GetSubscribers`, `GetSubRevenue`, `GetNewSubscribers`,
  `GetNumberOfVisitors`, `GetTicketSales`, plus theater save/load copy helpers;
- `Stats.OnNewWeek`, `Stats.money.GetTotalIncome`, and fan/money history consumers;
- `tasks.AddTask("ch4_4")`, `Story_Data.Set_Ch3_Aya_Fans`, chapter target comparisons,
  task description/tooltip formatting, and the corresponding save/load state;
- café `RenderCafe` fan-order selectors, `RenderRooms`, `GetFansNeededToBuild`,
  `GetMoneyPerDay`, `GetLastWeekEarning`, `GetTooltip`, `GetMoneyToAdd`, and café
  save/load stat copy helpers.

### Accounting, scripts, and consumers

- business `Accept`, `GetTotalWeeklyProfit`, `AddWeeklyEarnings`,
  `GetTotalWeeklyBuzz`, `GetTotalWeeklyFame`, `OnNewDay`, proposal `GetLiability`,
  and active-proposal `GetMoneyEarned`;
- `Research.category.Buy_Points`, `loans.GetTotalPaymentPerWeek`,
  `loans.GetTotalAvailableAmount`, `loans.GetAmountAvailableForLoan`,
  `loans.GetTotalDebt`, loan `GetInterest`, `GetDebt`, and `RecalcPaymentPerWeek`;
- idol salary expectation/satisfaction/raise paths and `Staff_Fire.GetSeverance`;
- `Event_Requirements.Check` money branch, `vn_actions.ParseResource` named
  `last_single_production` branch, and named wide-fan custom actions;
- `Show_Release`, `Single_Release`, tour and concert popups, `ResourceDisplay`,
  money/fan tooltips, and any other consumer proven by call search to narrow an A33 value.

### Long-horizon guards

- all `GetNew*ID`/`GetNext*ID` and `max + 1` persistent allocators for idols, staff,
  groups, singles, shows, tours, concerts, SSKs, loans, theaters, cafés, dishes, and floors;
- each persistent lifetime-counter increment selected by A33.6, with an explicit source
  manifest rather than a broad `int++` transformation.

## 14. Version policy

The audit document itself did not bump the mod because it initially changed no executable
code. The first A33.1 code update was **0.53.0**. The cumulative A33.2-A33.6 implementation
and the final direct-consumer closure are **0.54.0**. Each later executable update must
receive the next unused version; versions are never reused.

Every code update from this point must atomically synchronize the same version in:

1. `src/SaveNLoadFixesConstants.cs`;
2. `Save n Load Fixes.csproj`;
3. `assets/info.json`;
4. the README version heading/current-development text; and
5. a new top-level `CHANGELOG.md` entry and every version reference introduced by that
   entry.

Add a static test that fails on any mismatch or on a code-changing update without a new
changelog version heading.

## 15. Provisional regression gates

The A33 suite must, at minimum, cover these named families before its count is merged
into the frozen global regression ledger:

- `A33-WN-CORE`: safe-domain parity, checked resource/fan arithmetic, signed `Int64`
  boundary failures, no partial mutation, and health/checkpoint behavior;
- `A33-WN-RENT`: all three demonstrated floor thresholds, room occupancy, aggregate,
  discount, UI, and save/load;
- `A33-WN-TOUR`: the exact all-level-5 figures above, affordability, profit, finish,
  envelope round trip, legacy witnesses, and mod-off mirror;
- `A33-WN-SHOW`: episode 195/196 boundary, exact long averages, fan shadows, and release UI;
- `A33-WN-SINGLE`: fan totals over `Int32`, marketing transforms, physical redistribution,
  bonus-fan shadows, checked sales/profit, and VN production-cost action;
- `A33-WN-THEATER`: ¥40,000, ¥99,000,000, and `Int32.MaxValue` ticket prices; subscription
  revenue; subscriber/delta shadows; daily/weekly checked sums; and restart/F9;
- `A33-WN-STATS-STORY`: exact fan histories, chapter 3/4 targets, formatting, comparison,
  legacy absence, and impossible-preimage diagnostics;
- `A33-WN-BUSINESS-CAFE`: daily business profit beyond `Int32`, every early-widen product,
  café threshold at count 434, wide aggregation, and popular-worker ordering;
- `A33-WN-FAN-ENGINE`: allocation conservation, demographic totals, residue, decay,
  oshihen, negative guard, and unchanged safe-range RNG consumption;
- `A33-WN-PROJECTS`: rivals, SSK, concert, research, loan, salary, severance, event
  requirement, and named VN action boundaries;
- `A33-WN-JSON`: deliberate decimal strings, malformed/noncanonical/out-of-range rejection,
  known uniform SimpleJSON recovery, mixed-shape rejection, and raw byte preservation;
- `A33-WN-HORIZON`: counter guard and every persistent allocator exhaustion path;
- `A33-WN-CONSUMERS`: UI, tooltip, animation, comparison, and script consumers; and
- `A33-WN-VERSION`: exact version synchronization across constants, csproj, info.json,
  README, and changelog.

All tests require four install modes where relevant: SNLF absent, SNLF only, standalone
SWOF only, and both. Safe-range output must remain vanilla-identical. Wide-range output
must be exact with SNLF and remain loadable, though necessarily clamped where vanilla's
unchanged DTO cannot represent the shadow, when SNLF is absent.

## 16. Release gate

A33 is not complete when the project merely compiles. Release requires:

1. every mandatory target patched with an exact manifest count;
2. live Mono/Harmony qualification with no self-health ambiguity;
3. exact overwrite-save, Save As, autosave, F9, restart, and repeated-load results;
4. an envelope containing the complete A33 section whenever wide shadows exist;
5. byte-preserving `FixSaveFile` behavior;
6. no regression in the existing SNLF 1–47 suite; and
7. no log claim of exact restoration for a legacy value whose original preimage is not
   uniquely recoverable.

The 0.54.0 code satisfies the frozen target-resolution, complete-envelope,
byte-preservation, and isolated regression gates. Items 2, 3, and the live portion of 6
remain pending until the installed Unity game completes the stated checkpoint matrix.
