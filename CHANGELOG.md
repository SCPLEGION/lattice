# Changelog

Versions are `RELEASE.MINOR.PATCH`, kept in `About/About.xml` (`<modVersion>`) and shown in the mod settings and on the main menu.

- **RELEASE** – a big rework that changes how things look or behave.
- **MINOR** – new features and options; settings carry over.
- **PATCH** – fixes only. The "shame" number: it goes up when something shipped broken.

Bump with `scripts/bump.sh release|minor|patch`, fill in the new section, commit, then tag `vX.Y.Z`.

<!-- versions -->

## 1.0.0 - 2026-10-29

First versioned release.

- Cinematic main menu: colony backdrop with a slow drift, game title, a Continue card for the latest save (thumbnail, colony, play time, how long ago; one click loads it), large options that slide in, and a bottom bar with DLC icons, web links, language and version.
- Settings window: section sidebar (Appearance, Windows, Architect, Main menu, Mods, About), toggle switches, labelled sliders, live accent preview and a preview of the menu background.
- Mod manager: blue "Older" state (badge, edge, own category) for mods made only for the last 1-2 game versions, which 1.7 usually still loads. They are no longer counted as issues and pass "Compatible only". How many versions back count is set in Settings → Mods (0 turns it off).
- Fixed: a backdrop that failed to load was read from disk every frame.
- Fixed: the main menu ignored "Restyle vanilla widgets".
- Settings defaults now live in one place; Reset uses them.
