# ClientMods — SPT 4.1 compatibility port

Port of [IgorEisberg/SPT-ClientMods](https://github.com/IgorEisberg/SPT-ClientMods) at `2b9ed9f`, built against SPT **4.1.6** and EFT **0.16.9.5.40743**. This is an unofficial compatibility port. The original license is in `LICENSE`.

## Included mods

| Mod | Version |
| --- | --- |
| CustomInteractions and its prepatcher | 1.8.1 |
| ItemContextMenuExt | 1.8.1 |
| ItemAttributeFix | 1.7.1 |
| ItemSellPrice | 1.7.1 |
| MagazineInspector | 1.7.1 |
| MunitionsExpert | 1.7.1 |

## Compatibility changes

- Updated game types, namespaces, session interfaces, reflection field names and visibility, and context-menu patch targets for SPT 4.1.
- CustomInteractions uses the standard game assembly and an explicit callback constructor argument. Its prepatcher preserves public callback access and can be applied repeatedly.
- MunitionsExpert embeds its PNG icons directly. Additional ammo attributes are added only when the game's cache is first created, without skipping the original method.
- Shared project references use a configurable `GameRoot`, replacing the manually populated `Shared` directory and modified reference assembly.

## Build

Supply your own SPT 4.1.6 installation. Game DLLs are not included in this repository. `GameRoot` defaults to the parent of this repository; pass it explicitly when the checkout is elsewhere.

### PowerShell build

The build script uses the Roslyn assemblies included with PowerShell 7.6 and does not require a .NET SDK download:

```powershell
pwsh -File .\tools\Build.ps1 -GameRoot 'D:\SPT4'
```

The seven mod DLLs are written to `artifacts`. This is the build path used for validation of this port.

### SDK build

With a recent .NET SDK and .NET Framework 4.7.2 targeting references installed:

```powershell
dotnet build ClientMods.sln -c Release /p:GameRoot='D:\SPT4'
```

This alternative build path has not been validated on the development machine because it has no .NET SDK installed.

## Verification

After the PowerShell build, compile and run the verification tool with the Windows .NET Framework compiler:

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /out:artifacts\Verify.exe tools\Verify.cs
& .\artifacts\Verify.exe 'D:\SPT4' "$PWD\artifacts"
```

It checks all 13 patch targets and parameter signatures, 11 reflection bindings, and all three embedded PNGs against their source files. During the initial port, all 514 compiled method/field references were also resolved against the installed assemblies, and the prepatcher was run twice against an in-memory game assembly copy.

An SPT 4.1.6 game startup subsequently confirmed loading of all six plugins, application of the prepatcher, and activation of all 13 patches without errors from this set. This is startup validation, not a complete raid/UI regression test or a guarantee of compatibility with every mod pack.

`tools/Inspect.ps1` is an optional metadata inspection helper; it accepts `-GameRoot`, `-Names`, and `-Method` and does not modify game assemblies.

## Installation

Close the game before installing. Back up older versions, and ensure that each plugin DLL is installed only once.

- Copy `IcyClawz.CustomInteractions.Prepatch.dll` to `BepInEx/patchers/`.
- Copy the other six `IcyClawz.*.dll` files to `BepInEx/plugins/IcyClawz.ClientMods/`.
- Do not copy `Verify.exe`, source files, or game reference DLLs into BepInEx.

For rollback, remove these seven DLLs and restore any previous versions you backed up.

### MunitionsExpert and other color mods

MunitionsExpert's ammo recoloring is enabled by default, as in the original mod. To let Item Valuation or another mod supply the ammo colors, disable the Boolean toggle under **Colorize Icon Backgrounds** in the F12 settings. Additional ammo attributes remain enabled. This is a user configuration choice; personal BepInEx configuration files are not included in the repository.
