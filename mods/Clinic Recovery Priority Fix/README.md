# Clinic Recovery Priority Fix

`Clinic Recovery Priority Fix` makes automated doctor clinics claim eligible low-stamina idols
before dance, vocal, styling, or cafe auto-practice can claim them.

## Player-facing behavior

- Processes every clinic before other rooms during each agency time tick, regardless of floor or
  build order.
- Allows idle idols to use automated clinic recovery regardless of whether their training
  preference is disabled, vocal, dance, or styling. Vanilla incorrectly applies those training
  specialization restrictions to medical recovery.
- Uses live room ownership for clinic availability instead of treating `girl.status` as the whole
  truth. Active training/treatment/date jobs, paused-training queues, cafe assignments, and active
  scene membership remain protected, while orphan `scene`/`practice` markers no longer strand an
  otherwise idle low-stamina idol.
- After a save is loaded and the asynchronous agency room graph has finished reconstructing,
  normalizes an orphan `scene` status back to `normal` only when no reconstructed room, scene, cafe
  assignment, or paused-training queue actually owns that idol. Current-load generations are
  tracked so a stale loader completing late cannot mutate a newer loaded save.
- Immediately checks clinic auto-recovery again after a clinic finishes its current task, avoiding
  a scheduling gap in which another auto-practice room could take the next recovery candidate.
- Performs one final refill sweep after the agency room tick has finished. Any doctor clinic that is
  still idle runs vanilla `AutoActivitiesCheck()` once more, so an idol released later in the room
  loop or whose stamina changed after the clinics ran early can still queue recovery in the same
  agency tick. Busy clinics are never progressed or re-ticked by this sweep.
- Preserves vanilla clinic eligibility, recovery toggles, stamina thresholds, and lowest-stamina
  selection.
- Does not alter break rooms. Break-room recovery is a passive daily bonus to the lowest-physical-
  stamina active idols; idols are not assigned to or scheduled into break rooms.

## Build

Project file:
- `mods/Clinic Recovery Priority Fix/Clinic Recovery Priority Fix.csproj`

Example command:
- `dotnet build "mods/Clinic Recovery Priority Fix/Clinic Recovery Priority Fix.csproj" -c Release`
