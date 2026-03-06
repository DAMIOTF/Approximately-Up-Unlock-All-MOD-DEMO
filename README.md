# ApproximatelyUpMOD

MelonLoader mod for Approximately Up Demo.

Source repository target: `https://github.com/DAMIOTF/Approximately-Up-MOD`

## What is inside

- Harmony patches for gameplay tweaks.
- UniverseLib panel with item tools, resource toggles, and teleport helpers.
- Automatic GUI startup activation with fallback retries and diagnostics logs.

## Build requirements

- Visual Studio with .NET Framework 4.7.2 support.
- MelonLoader references from local game install.
- Game-managed assemblies (`Assembly-CSharp.dll`, Unity modules) from local install.
- `UniverseLib.Mono.dll` available on local machine (path passed via build property).

> Build paths are configurable with MSBuild properties:
> - `GameRootDir` (root game directory, contains `ApproximatelyUp_Data` and `MelonLoader`)
> - `UniverseLibPath` (absolute path to `UniverseLib.Mono.dll`)

## Build

1. Restore NuGet packages.
2. Build `ApproximatelyUpMOD.csproj` in Release mode.
3. Copy generated DLL from `bin/Release` to your MelonLoader mods folder.

## Automatic releases on each push

Workflow file: `.github/workflows/build-and-release.yml`

The workflow is configured for a `self-hosted` Windows runner because game DLL dependencies are local and cannot be downloaded on standard GitHub runners.

Required repository variables:

- `APPROX_UP_GAME_DIR` example: `D:\SteamLibrary\steamapps\common\Approximately Up Demo`
- `APPROX_UP_UNIVERSELIB_DLL` example: `D:\Mods\Dependencies\UniverseLib.Mono.dll`

On each push to `main` the workflow:

1. Builds `ApproximatelyUpMOD.csproj` in Release mode.
2. Packs outputs into `ApproximatelyUpMOD-build.zip`.
3. Updates tag `auto-build`.
4. Publishes/updates Release `Auto Build (latest push)` with the newest binaries.

## Controls

- `F10`: Toggle mod panel visibility.

The mod now tries to open the panel automatically after scene load and logs each important initialization step in MelonLoader logs.
