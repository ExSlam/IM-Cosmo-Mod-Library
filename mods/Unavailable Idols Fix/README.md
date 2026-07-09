# Unavailable Idols Fix

Planned lifecycle and assignment fix for idols who announce graduation, become temporarily unavailable, or permanently leave the agency.

## Status

Implementation in progress. The current code covers lifecycle-safe cast handling, strict launch validation, localized notifications, announced-graduation contract eligibility, transactional cast editors, and the optional IM Data Core pre-cleanup snapshot bridge. Repository-wide Debug and Release builds pass with zero warnings.

In-game lifecycle testing is still required before release.

## Intended scope

- Keep an idol fully active after she announces graduation and until `Graduate` actually finalizes her departure, including when the date is delayed.
- Keep the room cancel control available when graduation is announced during an active room task, without replacing the graduation status.
- Treat injury, depression, and hiatus as temporary unavailability rather than permanent cast cleanup.
- Preserve durable pre-launch casts while blocking launch until every required participant is available or explicitly removed.
- Remove a temporarily unavailable idol only from permanent casts of shows that had already launched.
- Break contracts and clear ordinary room assignments during temporary absence, while preserving an active clinic assignment for an injured or depressed idol.
- Stop concert and show slots from receiving unrelated random replacement idols.
- Pause mentorship, push, and other durable assignment effects without deleting their state.
- Capture final pre-graduation details before permanent cleanup, including fan buckets and relationship data.
- Integrate softly with IM Data Core and Graduation Details when their Harmony owners are present.
- Keep historical IM Data Core events and Graduation Details snapshots after the live idol is cleaned up.
- Add strict launch validation and transactional editing safeguards for concerts and analogous cast editors.
- Emit localized notifications when a project becomes blocked and whenever graduation cleanup removes an idol from a project, mentorship, or push slot.
- Keep announced-graduation idols eligible for one-time photoshoots and variety appearances, but exclude them from new advertisement and TV-drama contracts.
- Leave world tours and election participant selection unchanged; elections inherit validation from their linked single and concert.
- Cooperate with all other Cosmo fixes and data/UI mods through narrowly scoped patches and explicit Harmony ordering.
- Correct adjacent immutable `DateTime` graduation-date no-ops as a compatibility phase without replacing Graduation Rebalances or Tel patch logic.
- Respect Tel's Never Graduate by disabling graduation-date correction writes while `com.tel.nevergraduate` owns the scheduled graduation-date updater.

See [DESIGN.md](DESIGN.md) for the removal-path audit and candidate implementation designs.

## Build

```powershell
dotnet build "mods/Unavailable Idols Fix/Unavailable Idols Fix.csproj" -c Release
```
