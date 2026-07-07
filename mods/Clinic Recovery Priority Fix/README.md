# Clinic Recovery Priority Fix

`Clinic Recovery Priority Fix` makes automated doctor clinics claim eligible low-stamina idols
before dance, vocal, styling, or cafe auto-practice can claim them.

## Player-facing behavior

- Processes every clinic before other rooms during each agency time tick, regardless of floor or
  build order.
- Allows idle idols to use automated clinic recovery regardless of whether their training
  preference is disabled, vocal, dance, or styling. Vanilla incorrectly applies those training
  specialization restrictions to medical recovery.
- Immediately checks clinic auto-recovery again after a clinic finishes its current task, avoiding
  a scheduling gap in which another auto-practice room could take the next recovery candidate.
- Preserves vanilla clinic eligibility, recovery toggles, stamina thresholds, and lowest-stamina
  selection.
- Does not alter break rooms. Break-room recovery is a passive daily bonus to the lowest-physical-
  stamina active idols; idols are not assigned to or scheduled into break rooms.

## Build

Project file:
- `mods/Clinic Recovery Priority Fix/Clinic Recovery Priority Fix.csproj`

Example command:
- `dotnet build "mods/Clinic Recovery Priority Fix/Clinic Recovery Priority Fix.csproj" -c Release`
