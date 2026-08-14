# IM Data Core 3 migration

## From lightweight sidecar v1/v2

No separate tool is required.

IMDC 3 reads `IMDataCore.LightweightSidecar` format versions 1 and 2, validates them, rebuilds runtime state, and continues normally. The next successful sidecar persistence writes format version 3.

Before first v3 write, the original sidecar remains the source file. On replacement, the atomic writer retains the previous generation as `<sidecar>.imdc.bak`.

## From pre-2.0 IM Data Core

IMDC 3 does not perform runtime migration from the historical database-backed persistence system.

It does not probe historical database filenames, old Workshop installation paths, old `IMDataCore/saves` trees, or flat fallback JSON candidates.

A future database migration utility should be separate from the runtime mod so it can explicitly:

1. identify the old schema/version;
2. associate old data with a vanilla save safely;
3. back up the source database;
4. translate only verified semantics;
5. produce a v3 sidecar;
6. leave the source database untouched unless the user explicitly chooses otherwise.

## Newer sidecar versions

IMDC 3 refuses to overwrite a sidecar whose format version is newer than it understands. This makes accidental downgrade safer.
