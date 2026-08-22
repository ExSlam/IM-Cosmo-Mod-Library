# Changelog

## 1.2.4

- Career-window timeline filtering now excludes events before an idol's hiring date and after a completed graduation while retaining the graduation day itself.
- Added **After Graduation** rendering from IM Data Core's `idol_graduation_outcome` milestone and vanilla-resolved `Graduation_Trivia_Text`.
- Election ranking details use compact `rank -> portrait -> idol name -> votes -> points` rows; related idols remain profile-linkable, including the Graduation Details bridge for graduated idols.
- Concert details retain the ordered setlist snapshot, centers, talk breaks/MCs, cards, and chronological disaster outcomes without populating the unrelated generic **With whom** field.

## 1.2.2

- Election labels use IM Data Core's persisted `election_number`, never event `EntityId` or vanilla `_SSK.ID`.
- Removed partial-cache ordinal behavior that could renumber a later election as `Election #1` before older diary pages were loaded.
- Rows missing `election_number` fall back to another row for the same election or vanilla election-count semantics rather than loaded-page count.

## 1.2.1

- Timeline toolbar buttons and the manually-created TMP search field now apply Idol Manager's currently selected game font through IM UI Framework.

## 1.0.0

- Established the profile-integrated career diary backed by IM Data Core timeline/supplemental state and IM UI Framework rendering.
- Established IM Data Core and IM UI Framework as hard runtime dependencies.
