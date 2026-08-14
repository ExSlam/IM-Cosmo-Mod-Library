# IM Data Core 3.1 migration

## From IMDC 3.0 / sidecar v3

No migration step is required.

IMDC 3.1 still writes `IMDataCore.LightweightSidecar` format version 3. The new `IdempotencyKey` event member is optional and appears only on custom events written through `TryAppendCustomEventOnce`.

Existing v3 sidecars load normally. Existing custom events have no idempotency key and remain ordinary append-only history.

## From lightweight sidecar v1/v2

No separate tool is required.

IMDC 3.1 reads format versions 1 and 2, validates them, rebuilds runtime state, and continues normally. The next successful sidecar persistence writes format version 3.

Before first v3 write, the original sidecar remains the source file. On healthy replacement, the atomic writer retains the previous generation as `<sidecar>.imdc.bak`.

## Checkpoint behavior change

An existing sidecar must contain an exact checkpoint for the vanilla save being loaded. IMDC 3.1 no longer activates unmatched sidecars by an in-game-date cutoff.

This is intentionally conservative. If a vanilla save was externally restored or copied without its matching IMDC checkpoint, supplemental history is detached rather than guessed. The existing sidecar is protected from overwrite for that path.

## Backup recovery

If the primary sidecar is unreadable or invalid, IMDC 3.1 validates `<sidecar>.imdc.bak`. A valid backup may become the recovery source for the session, but it still must contain an exact checkpoint for the loaded vanilla save.

Backup recovery does not modify the damaged primary immediately. A later successful save can repair the primary while preserving the known-good backup.

## From pre-2.0 IM Data Core

IMDC 3.1 does not perform runtime migration from the historical database-backed persistence system.

It does not probe historical database filenames, old Workshop installation paths, old `IMDataCore/saves` trees, or flat fallback JSON candidates.

A future database migration utility should be separate from the runtime mod so it can explicitly identify the old schema/version, associate old data with a vanilla save safely, back up the source, translate verified semantics, and leave the source database untouched unless the user explicitly chooses otherwise.

## Newer sidecar versions

IMDC 3.1 refuses to overwrite a sidecar whose format version is newer than it understands. This makes accidental downgrade safer.
