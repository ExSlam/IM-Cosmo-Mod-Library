# Changelog

- Fixed graduated-idol profile rendering so stored birthdate, age, bonds, and portrait data do not depend on a temporary popup flag or a particular profile entry point. Staff profile context is established after opening the popup and reset when switching saves or starting a new game.
- Select the archived portrait before the native portrait renderer starts. When a graduated idol has changed appearance, the live and archived images no longer launch competing requests that can overwrite one another. Existing IM Data Core graduation records are read without rewriting the save.
- Added hire date, tenure, and Total Paid by Agency to the idol profile's Jobs tab, using the game's fonts and existing scrolling layout. Vanilla Total Earnings remains unchanged.
- Reads actual recorded salary payments from IM Data Core. Partial payment history is labelled clearly; unavailable history is not estimated.
- Graduation now freezes hire date, graduation date, final tenure, vanilla Total Earnings, and recorded agency pay (including completeness) in the permanent graduation snapshot. Historical displays use only the snapshot, including after an idol becomes staff.
- Earlier graduation records show unrecorded career values as unavailable rather than recalculating from live idol data. Existing records remain compatible.
- Added translations for all new profile text in English, Simplified Chinese, French, Japanese, Korean, Brazilian Portuguese, and Russian.
