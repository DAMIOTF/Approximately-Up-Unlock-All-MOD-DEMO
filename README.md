# Approximately Up MOD

> **Current version: v2.3.0** — tested against game build [34252973](https://steamdb.info/app/4396220/history/?changeid=34252973)

A MelonLoader mod for **Approximately Up (Demo)** that unlocks hidden game content and adds quality-of-life overrides through an in-game GUI panel (toggle with **F10**).

---

## Features

### Item Unlocker
Reveals all items present in the game files that are normally locked or inaccessible.  
Use the **Items** section of the panel to browse and assign them to your hotbar with one click.

### Resources
- Set a custom **materials amount** override (default 999, max 99999) applied when spawning items.
- Toggle **Enforce Materials Amount** to keep the override active at all times.

### Building Grid
- Enable/disable **grid snapping** for precise placement.
- Toggle **placement collision** checking on or off.

### Thruster Power Override
- Override the thrust output of every thruster on your ship per type (e.g. Ion Thruster, Plasma Thruster).
- Values are applied in real-time to all placed thrusters; new ones are picked up within ~0.4 s.
- **Reset** button restores original game values per type.

### Teleport
- **Teleport to station** — jump instantly to any docked station.
- **Teleport to planet** — jump instantly to any planet in the current system.

### Electrics
#### Wireless Transmitter Channels
- Change the **maximum number of channels** available on wireless transmitters (default: 99, max: 9999).
- Applied to all placed transmitters instantly; new ones picked up within ~0.4 s.

#### Cable Max Power (P/s)
- Override the **maximum power capacity** for each cable type: Power Cable, Power Nano Cable, Plasma Cable.
- Default values are auto-discovered the first time you place a cable of that type.
- Applied to all placed cables instantly; new ones are picked up within ~0.4 s.

### Ship Safety
- Toggle **ship tearing** (structural integrity damage) on or off.

---

## Screenshots

![In-game screenshot 1](./Images/1.png)
![In-game screenshot 2](./Images/2.png)
![In-game screenshot 3](./Images/3.png)

---

## Requirements

| Requirement | Version |
|---|---|
| [MelonLoader](https://github.com/LavaGang/MelonLoader.Installer/blob/master/README.md#how-to-install-re-install-or-update-melonloader) | v0.7.1 Open-Beta |
| Approximately Up (Demo) | build 34252973 |

---

## Installation

1. Install **MelonLoader v0.7.1 Open-Beta** using the [official installer guide](https://github.com/LavaGang/MelonLoader.Installer/blob/master/README.md#how-to-install-re-install-or-update-melonloader).
2. Download all three files from the latest [Release](https://github.com/DAMIOTF/Approximately-Up-Unlock-All-MOD-DEMO/releases/latest):
   - `ApproximatelyUpMOD.dll`
   - `UniverseLib.Mono.dll`
   - `0Harmony.dll`
3. Open your game folder and go to the `Mods` subfolder (create it if it does not exist).
4. Copy all three `.dll` files into `Mods/`.
5. Launch the game.
6. Press **F10** to open or close the mod panel.

### Expected folder structure

```
Approximately Up Demo/
└── Mods/
    ├── ApproximatelyUpMOD.dll
    ├── UniverseLib.Mono.dll
    └── 0Harmony.dll
```

![File structure example 1](./Images/4.png)
![File structure example 2](./Images/5.png)

---

## Troubleshooting

| Problem | Fix |
|---|---|
| GUI does not appear | Copy all DLL files from `Mods/` into `UserLibs/` as well |
| Mod does not load at all | Verify MelonLoader is installed correctly and is v0.7.1 Open-Beta |
| Game crashes on startup | Check that the game version matches build 34252973 |
| Can't type in game (chat, search, blueprints) | Make sure you are using v2.3.0 or later — this was fixed in that release |

For anything else, contact me on Discord: **dmtftf**

---

## Building from Source

### Requirements

- Windows
- .NET Framework 4.7.2 targeting pack
- Visual Studio 2022 or MSBuild 2022 with C# support
- NuGet package restore
- Local installation of Approximately Up Demo

### Dependencies

**From NuGet** (automatically restored):
- `0Harmony` 2.4.2 — [nuget.org](https://www.nuget.org/packages/Lib.Harmony)

**From the game folder** (`ApproximatelyUp_Data/Managed`):
- `Assembly-CSharp.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.InputLegacyModule.dll`
- `UnityEngine.IMGUIModule.dll`
- `UnityEngine.TextRenderingModule.dll`
- `UnityEngine.UI.dll`
- `UnityEngine.UIModule.dll`
- `Unity.Collections.dll`
- `Unity.Entities.dll`
- `Unity.Mathematics.dll`
- `Unity.Physics.dll`

**From the game's `Mods/` folder**:
- `UniverseLib.Mono.dll` — [github.com/sinai-dev/UniverseLib](https://github.com/sinai-dev/UniverseLib/releases)

**From the game's `MelonLoader/net6/` folder**:
- `MelonLoader.dll`

### Build

```powershell
dotnet msbuild .\ApproximatelyUpMOD.csproj /t:Build /p:Configuration=Release `
  /p:GameRootDir="D:\SteamLibrary\steamapps\common\Approximately Up Demo" `
  /p:UniverseLibPath="D:\SteamLibrary\steamapps\common\Approximately Up Demo\Mods\UniverseLib.Mono.dll"
```

Output DLL: `bin/Release/ApproximatelyUpMOD.dll`

