# Unavailable Idols Fix design specification

## Lifecycle definitions

The mod must distinguish three states that vanilla currently conflates:

1. **Graduation announced:** the idol remains a normal active participant until `data_girls.girls.Graduate` actually executes. Moving the graduation date must extend this active period automatically.
2. **Temporary absence:** injury, depression, or hiatus makes the idol unavailable without destroying durable plans.
3. **Permanent departure:** finalized graduation or firing snapshots the idol and then removes current assignments and relationships.

`announced_graduation` is not a departure boundary. No cleanup may run merely because the announcement occurred or the originally scheduled date arrived and was delayed.

## Required behavior matrix

| System | Graduation announced | Temporary absence | Permanent departure |
| --- | --- | --- | --- |
| Unreleased single cast | Fully usable | Preserve slots; block release | Clear the idol's slots |
| Unreleased permanent-cast show | Fully usable | Preserve slots; block launch/relaunch | Clear the idol's slots |
| Running permanent-cast show | Fully usable | Remove affected slots only; never random-replace | Remove affected slots |
| Concert setlist | Fully usable | Preserve song centers and talk hosts; block launch | Clear affected slots/items without random replacement |
| Contracts | Unchanged | Break using vanilla rules | Break using vanilla rules |
| Ordinary room work | Unchanged | Cancel and clear | Cancel and clear |
| Clinic work | Unchanged | Preserve only while injured/depressed | Cancel and clear |
| Mentorship and pushes | Continue | Preserve state and pause effects/timers | Remove |
| Relationships and tasks | Continue | Preserve | Snapshot, then run permanent cleanup |
| New business proposals | Photoshoot/variety only; exclude advertisements and TV dramas | Unavailable | Unavailable |

No path may silently select a replacement idol.

## Graduation-announcement availability

Vanilla `IsActive()` returns false for `announced_graduation`, which excludes the idol from world tours, stamina distribution, automatic assignment pools, and other global systems. The mod must make an announced idol active everywhere that uses `IsActive` or `GetActiveGirls` while keeping `graduated` inactive.

Vanilla also refuses every status transition away from `announced_graduation` except `graduated`. Making announced idols active would otherwise allow injury/depression handlers to execute their side effects while the status change is rejected. Initial implementation should preserve vanilla's effective medical immunity during the notice period by suppressing new injury/depression rolls for announced idols. Supporting simultaneous announced-graduation and medical states would require a separately persisted compound-state model.

The same status lock means room work cannot replace `announced_graduation` with `practice`. The room pointer remains the authoritative assignment state. When an announced idol has a non-null room, the roster card must show its existing cancel control instead of the drag control; cancellation continues through the room's vanilla method and clears the pointer without erasing the graduation announcement.

Announced idols remain eligible for one-time `photoshoot` and `variety` proposals. Exclude them from the candidate list for new `ad` and `tv_drama` proposals because those create continuing contracts that would immediately be broken at graduation. Existing contracts remain unchanged until a temporary absence or finalized graduation invokes vanilla contract cleanup.

## Temporary absence

### Durable pre-launch projects

Do not call the destructive cast-removal behavior for projects that have not launched:

- Keep every selected idol in an unfinished single.
- Keep every selected idol in a show with `episodeCount == 0`, including a show currently under production or relaunch preparation.
- Keep concert setlist item order, song centers, and all talk-host positions.

Launch buttons and launch entry points must independently reject the project while any required participant has status `injured`, `depressed`, or `hiatus`. UI gating alone is insufficient; direct method calls must be guarded as well.

### Running shows

A show is considered already launched when `episodeCount > 0` and it is not canceled. For permanent casts, clear every matching slot when the idol becomes injured, depressed, or enters hiatus. Do not choose a random replacement and do not restore the slot automatically on recovery.

Entire-group and rotating casts derive participants per episode. Their episode-time cast resolution must omit unavailable idols without mutating a durable cast.

### Mentorships and pushes

Keep mentor pairs, push slots, and accumulated push days. Suppress weekly/daily effects and timer advancement while either required idol is temporarily unavailable. Resume from the same state after recovery.

### Contracts and rooms

Temporary absence continues to break contracts.

Ordinary room jobs are canceled and the idol's room reference is cleared. A doctors-office assignment is retained when injury or depression occurs so treatment is not destroyed. Sending the idol onto general hiatus still clears the clinic assignment.

## World tours and elections

These systems are intentionally outside this mod's participant-management scope:

- World tours operate on the active-idol pool rather than a durable selected roster. Temporary absence already excludes an idol, and the announced-graduation `IsActive` correction makes an announced idol participate until graduation finalizes.
- Elections are linked to a single and optionally a concert. Their launch flow already delegates readiness to those projects, so the stricter single/concert validation supplies the required blocking behavior.
- If an automatically due linked concert is blocked, retain one next-day retry entry; vanilla otherwise removes the queue entry even when the guarded `StartConcert` call does not execute. This is queue preservation only and does not alter election participants, eligibility, or results.

Do not add custom tour/election rosters, participant editors, removal behavior, or persistence. Preserve vanilla election eligibility and result generation.

## Strict launch validation

Create one shared availability predicate for required participants:

- Available: `normal`, `practice`, `scene`, and `announced_graduation` where the specific system normally permits them.
- Temporarily unavailable: `injured`, `depressed`, and `hiatus`.
- Permanently unavailable: `graduated` or an unresolved/missing idol ID.

Apply validation to:

- `singles._single.CanLaunch` and the actual release entry point.
- `Shows._show.CanLaunch`, relaunch validation, and the actual launch entry point.
- `SEvent_Concerts._concert.CanLaunch` and `SEvent_Concerts.StartConcert`.
- Election launch tooltip/button logic only as needed to surface linked single/concert validation failures; do not add participant validation.

Tooltips should identify each unavailable idol and the blocking reason.

## Localized lifecycle notifications

Embed the repository's Mod Localization string runtime in this assembly by shipping an `assets/Localization/en/strings.txt` pack. The mod must not require the standalone Mod Localization System assembly.

When an injury, depression, or hiatus first makes a durable project unavailable, emit one notification per affected unreleased single, unreleased permanent-cast show, and production concert. Each message includes the project title and the complete distinct list of blocking idol names. Join multiple names with a comma and use localizable singular/plural templates so English renders `is` or `are` correctly. Repeated `CanLaunch` polling must never create notification spam; an explicit blocked launch attempt may repeat the relevant notification.

When lifecycle cleanup actually removes an idol, emit a separate notification for every affected object:

- each unreleased single;
- each unreleased show;
- each running show;
- each production concert;
- each mentorship pair;
- each push slot.

Temporary absence does not remove durable single/show/concert casts, mentorships, or pushes, so it emits removal notifications only for running-show slots. Permanent graduation emits every applicable removal notification after deterministic cleanup. Notifications use `idol_status_change`, are null-safe during load/teardown, and do not replace the game's existing injury, depression, hiatus, or graduation messages.

## Permanent graduation cleanup

Patch `data_girls.girls.Graduate` with a highest-priority prefix. Capture final state before dialogue, room cancellation, contract breaking, `RemoveFromEverything(false)`, relationship cleanup, status change, or fan oshihen.

After successful capture:

- Clear the idol from unfinished singles, all unfinished concerts, pre-launch shows, running shows, mentorships, pushes, rooms, and contracts.
- Run the vanilla permanent task and relationship cleanup.
- Never random-replace a song center, talk host, or show-cast member.
- Preserve released singles, finished concerts/tours/elections, show history, IM Data Core events, and Graduation Details snapshots.

The minimum final snapshot contains identity, group, age, birthday, hiring/graduation dates, status, salary, fame, stats, traits, fan buckets and total fans, relationships/bonds, dating state, career participation, current roles, and the assignment IDs being removed.

## Optional-mod integration

### Graduation Details

Graduation Details already owns a prefix on `Graduate` that captures fan buckets, appeal/opinion, bonds, birthdate, age, and portrait before vanilla cleanup. Detect Harmony owner `com.cosmo.graduationdetails` on `Graduate`.

- If its prefix is present, do not duplicate or overwrite its snapshot.
- Prefer a future public `GraduationDetailsApi.CaptureBeforeGraduation` contract over reflection into its internal store.
- Its saved snapshot remains authoritative after live relationships and fans are cleared.

### IM Data Core

IM Data Core currently captures `idol_graduated` in a postfix and its lifecycle payload does not include fan buckets. Detect Harmony owner `com.cosmo.imdatacore` and its public API at runtime without a hard assembly reference.

- Register namespace `com.cosmo.unavailableidolsfix` through `IMDataCoreApi`.
- Append a pre-cleanup custom event such as `graduating_idol_final_snapshot` from the `Graduate` prefix.
- Store the complete primitive snapshot JSON, including pre-oshihen fan totals and assignment IDs.
- Flush before destructive cleanup when possible.
- Leave existing IM Data Core events untouched; the normal postfix `idol_graduated` event should still run.

Failure of an optional integration must be logged once and must not prevent vanilla graduation or the core cleanup fix.

## Transactional cast editing

### Concerts

Vanilla edits the live concert object and Cancel does not restore it. Edit a deep working copy and commit only on Continue. The copy must include setlist order, item type, single reference/ID, song center, talk-host slots, venue, title, ticket price, and fan-appeal/projected-value inputs. Clear `Cast_Changed` only after a successful commit.

### Shows

Show cast selection already uses a local array and commits on Continue. Keep that transaction, but stop clearing `Cast_Changed` merely by opening the editor. Coordinate with Show Cast Assignment Fix so duplicate normalization remains a postcondition rather than a conflicting replacement policy.

### Singles

Single cast selection already commits its local senbatsu on Confirm, but opening the editor clears `Cast_Changed`, and preview recalculation mutates live fan appeal. Snapshot/restore preview-mutated fields on Cancel and acknowledge the warning only on Confirm.

World-tour setup creates a new object and does not currently expose an existing-project cast editor.

## Cosmo mod compatibility

The implementation must remain compatible when any combination of Cosmo mods is enabled. Detect optional integrations by Harmony owner or public API, never by assuming assembly load order.

### Show Cast Assignment Fix (`com.cosmo.showcastassignmentfix`)

- It patches `Show_Popup.SetGirl`, `Show_Popup.Reset`, show creation/save, `Shows._show.NewEpisode`, and `Shows._show.RemoveGirl`.
- Unavailable Idols Fix owns departure semantics: preserve pre-launch casts during temporary absence and clear running/permanent-departure slots without random replacement.
- Show Cast Assignment Fix remains the duplicate-normalization safety net after those mutations.
- Do not replace its duplicate guards or return duplicate casts from a custom prefix.
- If both patch `RemoveGirl`, perform deterministic slot clearing before its postfix normalization.

### IM Data Core (`com.cosmo.imdatacore`)

- IM Data Core observes medical transitions, room cancellation, single/show/concert cast changes, editor commits, episodes, and graduation.
- Prefer invoking vanilla mutation methods under a narrow context flag instead of mutating fields behind their backs, so IM Data Core prefix/postfix snapshots still record real changes.
- For intentionally preserved pre-launch casts, emit no cast-change event because no cast changed.
- Keep its `Priority.Last` capture patches intact and never suppress their postfixes/finalizers.
- Use only its public API for the additional pre-graduation snapshot.

### Graduation Details (`com.cosmo.graduationdetails`)

- Its existing `Graduate` prefix must see the untouched idol before cleanup.
- Unavailable Idols Fix's highest-priority prefix may create its own immutable snapshot/context, but must not clear or mutate idol state before Graduation Details captures it.
- Do not access or rewrite Graduation Details' private JSON store.

### Graduation Rebalances (`com.cosmo.graduationrebalances`)

- It owns `Graduation_Set_Default_Date` and replaces `Graduation_Date_Update`.
- Do not replace either method or assume the announced date is fixed.
- The immutable-`DateTime` compatibility phase may observe `Graduation_Date_Update` at low priority, but must skip vanilla date-delta correction while Graduation Rebalances owns the model.
- When Graduation Rebalances and Tel's Worker Rights are both present, the compatibility phase may apply only the Worker Rights delta and only if the date is unchanged after Worker Rights' postfix ran.
- Base activity solely on current status and let any delayed date continue naturally.
- Its show-episode hosting postfix must receive the final cast after unavailable running-show members have been removed.

### Tel Never Graduate (`com.tel.nevergraduate`)

- It prefixes `data_girls.UpdateGraduationDates` and returns `false`, suppressing scheduled default-date assignment, non-announced date updates, and automatic announcement checks.
- Do not re-enable scheduled graduation-date maintenance or infer that missing default dates are corrupt while this owner is present.
- The immutable-`DateTime` compatibility phase must detect this owner and avoid writing corrected graduation-date deltas, because those dates are intentionally inert under Never Graduate.
- Existing idols already in `announced_graduation` can still be processed by vanilla daily `CheckGraduations()` because Never Graduate does not patch that method; Unavailable Idols Fix should not broaden Tel's patch scope unless explicitly requested.

### Clinic Recovery Priority Fix (`com.cosmo.clinicrecoverypriorityfix`)

- It controls clinic auto-recovery ordering and eligibility, not medical lifecycle cleanup.
- Preserve the existing doctors-office room/job object for an injured or depressed assigned idol; do not cancel and recreate or manually reassign it.
- Do not patch clinic `OnTimeTick`, `CanAutoTrain`, or the agency tick scheduler.

### Room Assignment Fix (`com.cosmo.roomassignmentfix`)

- It owns assignment exclusivity and clinic drag/drop fallback.
- Its availability guard accepts an announced idol only when the effective `IsActive()` result is true, allowing soft interoperability without making Unavailable Idols Fix a hard dependency.
- Unavailable Idols Fix may prevent a clinic cancellation or allow vanilla cancellation elsewhere, but must not directly assign an idol to a room.
- Any later assignment continues through `agency._room.assign` so its ownership guard remains authoritative.

### Assistant Manager (`com.cosmo.assistantmanager`)

- It has state-cleanup patches on `agency._room.CancelJob` and custom concert production handling.
- Do not replace `CancelJob`; use a tightly scoped prefix guard only for the preserved clinic case.
- When cancellation is allowed, the original method and all Assistant Manager patches must run normally.

### Idol Career Diary and historical-data mods

- Never remove an idol from released singles, finished concerts, completed tours/elections, prior show episodes, or historical event records.
- Transactional editor changes must commit through the existing methods observed by diary/data patches.

### Cheats Mod and forced recovery

- Cheats Mod can call `Heal` or `FinishHiatus` outside the normal clinic/calendar flow.
- Availability and launch UI must derive from current idol status rather than a cached recovery callback, so forced recovery immediately unblocks preserved projects.
- Do not patch or depend on Cheats Mod methods.

### Graduation Calendar and UI/localization frameworks

- Graduation Calendar and Graduation Details must continue reading the authoritative, possibly rebalanced `Graduation_Date`.
- Do not maintain a shadow graduation date or cache the original three-month date.
- Any new tooltip/status text should use the repository's localization conventions and remain compatible with IM UI Framework and UI Recovery Tools; gameplay correctness must not depend on those UI mods being installed.

### Divorce Fix (`com.cosmo.divorcefix`)

- Divorce Fix patches only `Dating.AfterMarriage` and `Dating.LoadFunction`; it has no shared Harmony targets with the planned graduation, availability, cast, or launch patches.
- Preserve vanilla `Dating.Partners` history, the final graduated status, and graduation trivia used by its legacy-save repair. Permanent graduation cleanup must not erase or replace those records beyond vanilla behavior.
- Do not patch or depend on Divorce Fix methods.

### Monthly Ledger (`com.cosmo.monthlyledger`)

- Monthly Ledger patches only `PopupManager.Start` and reads financial events from IM Data Core; it does not mutate graduation, medical, cast, or launch state.
- Preserve vanilla financial calls and IM Data Core transaction capture. A blocked launch creates no transaction, while a successful launch continues through the normal money path.
- The optional pre-cleanup graduation snapshot is a non-financial IM Data Core event and must not use a ledger transaction category or amount.
- Do not patch or depend on Monthly Ledger methods.

### Other Cosmo fixes

- Staff Firing Freeze Fix, UI Recovery Tools, and unrelated economic/UI mods have no direct target overlap and must not be hard dependencies.
- Use patch-local state, `ThreadStatic` context only for a single synchronous call, and finalizers that always clear context so exceptions cannot leak behavior into other mods.
- Avoid broad transpilers where a prefix/postfix context or targeted helper patch can express the behavior.
- If a transpiler is unavoidable, validate its match count, fail open to vanilla behavior, and log once.

### Harmony ordering policy

- Snapshot-only prefixes run at `Priority.First` but do not mutate live state.
- IM Data Core/Graduation Details capture prefixes must observe intact pre-mutation state.
- Availability postfixes change only the returned result and do not skip unrelated prefixes/postfixes.
- Mutation policies should allow the original method whenever safe, using scoped context to alter only the random-replacement or clinic-cancellation fragment.
- Validation is duplicated at UI and authoritative launch entry points without suppressing other mods' validation postfixes.
- Every skipped original must be narrowly justified and preserve expected update callbacks, event capture, and UI refresh behavior.

### Compatibility verification matrix

Before release, run the lifecycle and editor tests in each configuration:

1. Unavailable Idols Fix alone.
2. With Show Cast Assignment Fix.
3. With Clinic Recovery Priority Fix and Room Assignment Fix.
4. With Assistant Manager.
5. With Graduation Rebalances, Graduation Calendar, and Graduation Details.
6. With IM Data Core, verifying medical, cast-change, room-cancel, and final-snapshot events.
7. With Idol Career Diary, verifying released/finished historical participation remains visible.
8. With Cheats Mod, using forced heal and forced hiatus completion.
9. With Divorce Fix, verifying normal and legacy bad-divorce partner records survive graduation cleanup and load repair.
10. With Monthly Ledger and IM Data Core, verifying blocked launches create no transaction and successful launches retain their normal ledger entry.
11. With every Cosmo mod enabled together.

For each configuration, inspect Harmony patch ownership/order at runtime and test save/reload between status transition, blocked launch, recovery, delayed graduation, final snapshot, and cleanup.

## Current implementation state

Implemented in the initial code phase:

- announced-graduation `IsActive` correction and medical-side-effect suppression;
- announced-graduation room cancellation UI derived from the authoritative room pointer;
- temporary preservation and deterministic permanent removal policies;
- running permanent-show removal without random replacement;
- mentorship/push preservation and temporary effect pausing;
- localized blocked-project and per-object removal notifications;
- single/show/concert `CanLaunch` and authoritative entry guards;
- advertisement/TV-drama candidate filtering for announced idols while retaining photoshoot/variety eligibility;
- transactional concert editing plus show/single warning acknowledgement and cancel rollback;
- pre-cleanup JSON snapshot delivery through the optional IM Data Core public API;
- non-mutating coexistence with Graduation Details' `Graduate` prefix;
- immutable `DateTime` correction phase for vanilla medical/graduation side effects, vanilla `Graduation_Date_Update`, and Tel Worker Rights' salary penalty no-op, with owner detection and unchanged-date guards to avoid double application.

Still requires live-game verification: Harmony runtime ordering, save/reload behavior, UI tooltip composition, automatic election-concert retry timing, optional API availability during early load, and combined-mod lifecycle scenarios from the matrix above.

## Adjacent date defects

Several vanilla graduation-date changes call immutable `DateTime.AddDays` or `AddMonths` without assigning the return value. These corrections now live in a separate compatibility phase (`DateCompatibilityPatches.cs`) because Graduation Rebalances and Tel's Worker Rights patch the same calculations.

Compatibility rules:

- when Tel Never Graduate owns `data_girls.UpdateGraduationDates`, all compatibility-phase writes to `Graduation_Date` are skipped;
- medical side effects (`Set_Injured`, `Set_Depressed`) apply their missing month deltas only when the status transition completed and the date is still unchanged;
- `Graduate` applies best-friend and clique date deltas only to related idols whose date was not already changed by another patch;
- vanilla `Graduation_Date_Update` day-delta correction is skipped when Graduation Rebalances is present;
- Tel Worker Rights' salary penalty is applied only when the `com.tel.workerrights` owner is detected, after its postfix, and only if the date did not change during that postfix window;
- if another owner has already produced the expected vanilla, Tel, or combined target date, the compatibility phase does nothing.

## Implementation order

1. Announced-graduation activity semantics and medical-side-effect guard.
2. Shared availability predicate and strict single/show/concert validation.
3. Temporary-removal split, running-show removal, clinic exception, mentorship/push pausing.
4. Highest-priority final snapshot plus optional-mod integrations.
5. Deterministic permanent cleanup with no random replacements.
6. Transactional concert, show-warning, and single-preview editing.
7. Compatibility tests with every overlapping Cosmo mod enabled together.
8. Immutable-`DateTime` compatibility corrections with runtime owner/order checks.
