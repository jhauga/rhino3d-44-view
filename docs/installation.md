# Installation

## Requirements

- Rhino 8 for Windows, service release 8.20 or later. From 8.20 on, Rhino runs
  plugins on .NET 8, the runtime this plugin targets.
- To build from source: the .NET 8 SDK or newer.

Rhino for Mac has not been tested.

## Build

```bash
dotnet build src/FourFourView/FourFourView.csproj -c Release
```

The plugin builds to `src/FourFourView/bin/Release/net8.0/FourFourView.rhp`,
next to `FourFourView.deps.json`.

A running Rhino keeps a loaded plugin file locked. The build moves a locked
`FourFourView.rhp` aside as `FourFourView.rhp.locked-*` and writes the new one
next to it, so building while Rhino is open works; the new build loads the next
time Rhino starts.

## Install

### Drag and drop

Drag `FourFourView.rhp` from the build output onto an open Rhino window. Rhino
loads the plugin and remembers where it came from, so later builds in the same
folder load on the next start.

### Manual copy

Copy `FourFourView.rhp` and `FourFourView.deps.json` to a folder of your
choice, such as `C:\Users\<user>\AppData\Roaming\FourFourView\`, then drag the
`.rhp` onto Rhino once to register it from there.

### Package

A package installs the plugin the way Rhino's Package Manager does, into
Rhino's package folder. Build one, then install it from the folder it was
built in:

```bash
dotnet build src/FourFourView/FourFourView.csproj -c Release
mkdir -p dist/misc
cp src/FourFourView/bin/Release/net8.0/FourFourView.rhp dist/
cp src/FourFourView/bin/Release/net8.0/FourFourView.deps.json dist/
cp manifest.yml docs/assets/icon.png dist/
cp README.md dist/misc/README.md
cp LICENSE dist/misc/LICENSE.txt
cd dist
"/c/Program Files/Rhino 8/System/Yak.exe" build
"/c/Program Files/Rhino 8/System/Yak.exe" install --source "$(pwd -W)" 44-view 1.0.0-alpha.0.0.0
```

The commands are for Git Bash. `yak build` writes
`44-view-1.0.0-alpha.0.0.0-rh8_0-any.yak`, and `yak install` places the
package in `C:\Users\<user>\AppData\Roaming\McNeel\Rhinoceros\packages\8.0\44-view\`.
Restart Rhino afterwards; Rhino looks for new packages only at startup.

Prerelease versions are installed only when the version is named, as above.

## Check the installation

Open a model and type `44View`. The command line prints one line, such as:

```text
44View: fitted 12 visible objects at 0.1375 mm per pixel, limited by Top; named views saved.
```

`Options > Plug-ins` lists the plugin as FourFourView, with its icon and the
file it was loaded from.

## Uninstall

- **Drag and drop or manual copy:** open `Options > Plug-ins`, select
  FourFourView, clear its check box so it no longer loads, and restart Rhino.
- **Package:** run `"/c/Program Files/Rhino 8/System/Yak.exe" uninstall 44-view`,
  then restart Rhino.
