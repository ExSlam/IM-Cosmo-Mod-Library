# Staff Portrait Pack Format

Staff Portraits loads portrait packs from enabled Idol Manager mods. Each pack has one manifest at:

```text
StaffPortraits/pack.json
```

Image paths in the manifest are relative to the `StaffPortraits` directory that contains `pack.json`.

## Example manifest

```json
{
  "formatVersion": 1,
  "packId": "example.office.staff",
  "displayName": "Office Staff",
  "displayNameKey": "staffpack.office.name",
  "author": "Example Artist",
  "assets": [
    {
      "id": "body.office.01",
      "displayName": "Office Outfit 01",
      "displayNameKey": "staffpack.office.body.01",
      "type": "body",
      "file": "Textures/body/office_01.png"
    },
    {
      "id": "hair.bob.01",
      "displayName": "Bob 01",
      "type": "hair",
      "file": "Textures/hair/bob_01.png"
    },
    {
      "id": "face.01",
      "displayName": "Face 01",
      "type": "face",
      "file": "Textures/face/face_01.png"
    },
    {
      "id": "acc.glasses.01",
      "displayName": "Glasses 01",
      "type": "acc",
      "file": "Textures/acc/glasses_01.png"
    }
  ]
}
```

## Required fields

`formatVersion` must currently be `1`.

`packId` is the stable machine identifier for the pack. Use only letters, numbers, `.`, `_`, and `-`. Do not change this value after publishing a pack that players may already use in saves.

`displayName` is the English/fallback name shown in Staff Stylist. `displayNameKey` is optional and lets the portrait-pack mod localize that name through its own `assets/Localization/<language>/strings.txt`. Staff Portraits resolves the key from the mod that owns the pack.

`author` identifies the portrait-pack creator. This is separate from the Staff Portraits mod authorship credit.

`assets` contains the available portrait parts. Every asset requires a stable `id`, a supported `type`, and a PNG `file` path. `displayName` is the English/fallback label and `displayNameKey` can point to the portrait-pack mod's own localization strings. When both are omitted, Staff Stylist uses the stable asset ID as a last-resort label.

Supported `type` values are:

- `body`
- `hair`
- `face`
- `acc` or `accessory`

A usable pack must contain at least one body, one hair, and one face. Accessories are optional, and Staff Stylist always permits no accessory.

## Image rules

Every portrait-part image must be a **1024 x 1500 pixel PNG**. Staff Portraits uses the same composite portrait canvas dimensions as Idol Manager's idol-style layered portraits so body, face, hair, and accessory layers align predictably. The loader rejects any other dimensions and rejects individual PNG files larger than 32 MiB. Transparent backgrounds are expected for layered parts.

A manifest may contain up to 512 portrait assets and must remain at or below 1 MiB.

Staff Portraits validates that image paths stay inside the pack's `StaffPortraits` directory. Absolute paths and directory traversal outside the pack are rejected.

## Save compatibility

A selected part is stored through vanilla staff portrait saving with a stable ID shaped like:

```text
staffportraits:1:<packId>:<type>:<assetId>
```

For save compatibility, keep `packId` and every published asset `id` stable. Filenames may be reorganized in a later pack update as long as the manifest retains the same stable IDs and points them to the new files.

Do not remove or disable a portrait pack while a save still uses its assets. The artwork is external content, so Staff Portraits cannot reconstruct a missing image file. If a save is loaded while a required pack is unavailable, restore the pack and reload before saving again. Staff Portraits suppresses vanilla's same-type fallback for its own namespaced IDs, but the vanilla staff save format cannot retain a missing runtime asset by itself.
