# reWASD Profile Switcher — Playnite Extension

A [Playnite](https://playnite.link/) extension that automatically switches
your [reWASD](https://www.rewasd.com) gamepad profile(s) when a game starts
and stops, by shelling out to `reWASDCommandLine.exe`.

Supports multiple devices at once, each with its own **default profile**
(applied whenever a game closes, and as the fallback when a running game's
library has no override) plus optional **per-library overrides** — e.g. one
profile for your Steam library, a different one for another library,
applied automatically based on which library the game that just started
belongs to. A library override can also be set to **Passthrough**, which
turns reWASD's remap off instead of applying a profile — releasing the
virtual controller so the real physical device is visible as-is (e.g. so
Steam Input sees an actual Steam Controller instead of reWASD's virtual
Xbox 360 pad).

Off by default — irrelevant if you play with keyboard/mouse.

## Architecture

Single Playnite `GenericPlugin`, two projects:

| Project | What it is |
|---|---|
| `RewasdProfileSwitcher.Core` | Pure logic (CLI invocation, profile resolution, profile-file naming), no PlayniteSDK dependency, unit tested. |
| `RewasdProfileSwitcher.Playnite` | The actual Playnite extension: settings UI, `OnGameStarting`/`OnGameStopped` hooks. |

Unlike a multi-`Type` project (GameLibrary + MetadataProvider + GenericPlugin
in one repo, which Playnite requires as separate `.pext` packages — see
[MrScion/trcustom-playnite-ext](https://github.com/MrScion/trcustom-playnite-ext)
for why), this extension is a single `GenericPlugin`, so it ships as one
`.pext`.

## How it works

- **Master switch** (off by default) in Settings enables/disables the whole
  integration.
- **Devices**: add one entry per gamepad, each with its own reWASD Device ID
  (from "Copy device ID" in reWASD), a default profile + slot, and any
  number of per-library profile overrides.
  - **Connected-gamepad picker**: clicking "+ Add device" enumerates HID
    gamepads/joysticks currently plugged in (Xbox, PS5/DualSense, Steam
    Controller, and anything else exposing the standard HID gamepad/
    joystick usage — not a hardcoded vendor list) and offers them in a
    picker to prefill the device's name. This is Windows-side device
    detection only — reWASD's own Device ID still has to be pasted in by
    hand, since reWASD doesn't expose it anywhere outside its own GUI
    ("Copy device ID"); its command-line tool has no device-listing
    command (confirmed against reWASD's official docs).
- **On game start**: for each configured device, applies that device's
  profile for the started game's library if one is configured (or turns
  remap off, if that library is set to Passthrough), otherwise the
  device's default profile.
- **On game stop**: for each configured device, applies its default
  profile (back to "desktop") — turning remap back on first, in case the
  game that just closed belonged to a Passthrough library.
- Libraries are picked from ones already represented in your Playnite game
  database (grouped by `Game.PluginId`, labeled by `Game.Source.Name`) —
  there's no other way to enumerate installed library plugins from a
  `GenericPlugin`, so a library needs at least one game in your database
  before it can be picked.

## Settings

Three tabs:

- **General**
  - The master on/off switch.
  - **reWASD installation folder** — defaults to reWASD's standard install
    location (`C:\Program Files\reWASD`); change it if you installed
    reWASD elsewhere. Used to auto-detect `reWASDCommandLine.exe` below.
  - **reWASD command-line tool** — auto-detected from the folder above;
    only browse for it manually if auto-detection didn't find it.
  - **reWASD profiles folder** (optional) — point this at the folder
    containing reWASD's own `Profiles` subfolder (each profile has its own
    folder there, with a `Controller` subfolder holding its `.rewasd`
    file(s)). Once set, the Profiles tab offers a picker of the `.rewasd`
    files found there instead of browsing the file system by hand.
- **Devices** — add/remove devices; the selected device's name and reWASD
  Device ID.
- **Profiles** — the selected device's default profile + Slot (a
  Slot 1–4 dropdown, with a "Test" button), and its per-library profile
  overrides (add/remove/test each one individually, and mark any of them
  as Passthrough instead of a profile file).

## Installation

1. Download the `.pext` from this repo's `extras/Versiones/<version>/`
   folder (or a GitHub Release, if published).
2. Drag it into Playnite, or **Settings → Extensions → Install add-on...**
3. Restart Playnite.
4. Go to **Settings → Extensions → reWASD Profile Switcher**, enable the
   integration on the **General** tab, point it at reWASD's install and
   profiles folders, then add your device(s) on the **Devices** tab and
   their profiles on the **Profiles** tab.

### Developer / unpacked install

Build the solution, then in **Settings → For Developers → External
extensions**, add `src/RewasdProfileSwitcher.Playnite/bin/...`.

## Repository layout

```
src/
  RewasdProfileSwitcher.Core/          Pure logic, no PlayniteSDK dependency
  RewasdProfileSwitcher.Core.Tests/    xUnit tests for Core
  RewasdProfileSwitcher.Playnite/      The GenericPlugin extension
addon-database/                        Add-on/installer manifests, in the shape expected by
                                        Playnite's official extensions database, for a future
                                        submission there (not submitted yet)
extras/Versiones/                      Packaged .pext releases (not tracked in git)
```

## Versioning

Releases are `0.1.x`, bumped by one on every new package produced.

---

*Built with AI-assisted ("vibe coding") development.*
