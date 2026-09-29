# Changelog

## Unreleased

- Numeric entry dialogs now use a fully opaque background, including graduation date, fame, and stat editors.
- Updated the EroEvents cheats menu with the shared native buttons, game fonts, search field, and scrolling controls. Both cheat search fields display “Search” in English and its translation in the other supported languages.
- Unique-idol recruitment cards now have native pencil buttons for names, ages, and all eight stats. Changes apply to the recruitment preview and carry into the hired idol; scrolling retains edits. Ages are restricted to the lower of the original age or 16 through 120, and stats to 0–100. Already-recruited idols cannot be edited from the recruitment screen.
- Added opaque name-entry dialogs and translated all new editor labels and validation messages into the seven supported languages.

- The bully picker now lists every selected idol by name below the selection count, with a scrollable list for large selections and a translated empty-selection message.
- Replaced the selector and numeric dialog action buttons with the same native Settings button templates used by Mod Buttons, preserving their gradient, shadow, hover behavior, and readable white labels. Button language bindings retain the cheat captions when the popup opens.

- Reworked the graduation, selected-idol, and unique-idol recruitment selectors with embedded IM UI Framework controls, rounded game surfaces, native scroll indicators, game fonts, and canvas-aware sizing. The framework does not require a separate Workshop installation.
- Replaced the selected-idol "stats 100" action with eight independent 0–100 controls. Each has MIN/MAX, native salary minus/plus/pencil buttons, and bounded keyboard entry. Edits are staged until Apply; switching idols or closing discards unapplied changes.
- Fame now uses the same controls with whole-number limits of 1–10. Graduation date controls support native minus/plus buttons and keyboard entry for day, month, and year, while retaining the minimum three-month delay.
- Cancelling an announced graduation now applies the replacement date chosen in the same screen and clears the pending retirement dialogue.
- Preserved targeted scandal, non-producer breakup, and producer-relationship actions, and recruitment search, portrait choices, lazy portrait loading, and duplicate-recruitment prevention.
- Localized the changed labels, instructions, numeric input validation, and notifications in all seven supported languages; dates and numbers follow the selected game language.

- Added **Complete all room activities**, which finishes each currently running finite room timer once through the game's normal completion handler. Training, project production, auditions, proposals, loans, dates, treatment, and timed room scenes retain their normal outcomes and result prompts.
- Added **Add bullies to idol** using the existing idol picker: choose a victim and press **Next**, select multiple bullies with green selection highlights, then press **OK** to apply. Existing bullies remain unchanged; the victim and already-active bullies are excluded from the second picker.
- Bullying uses the game's saved clique records and per-member exclusions, preserving existing clique memberships and avoiding accidental recruitment of unselected members.
- Added both cheats' button text, tooltips, picker labels, instructions, and notifications in English, Simplified Chinese, French, Japanese, Korean, Brazilian Portuguese, and Russian.
