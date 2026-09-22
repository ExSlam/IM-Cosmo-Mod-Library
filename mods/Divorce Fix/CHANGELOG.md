# Changelog

## 2.0.1

- Move legacy divorce repair after all save-load callbacks. The old dating-loader patch could miss a divorce, inspect the previous career's roster, or fail before player staff finished loading.
- Match partners against the adopted save's idol records. Preserve happy marriages and unrecognized graduation outcomes.
- Skip legacy repair safely when the player or divorce translation is unavailable.
- Clear marriage after a new bad outcome only when the idol actually graduated.
- Retain standalone operation and compatibility with SNLF; no additional persistence format or dependency.
