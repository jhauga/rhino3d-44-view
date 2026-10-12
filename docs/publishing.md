# Publishing

## Versions

Versions follow [Semantic Versioning](https://semver.org/). During development
the version carries an alpha suffix of three numbers,
`1.0.0-alpha.<milestone>.<feature>.<fix>`, starting at `1.0.0-alpha.0.0.0`.
The first public release drops the suffix and ships as `1.0.0`.

The version is written in five places. Change all five together:

| File | What holds the version |
| --- | --- |
| `src/FourFourView/Properties/AssemblyInfo.cs` | `AssemblyInformationalVersion` |
| `manifest.yml` | `version` |
| `README.md` | the "Current version" line |
| `CHANGELOG.md` | a new section heading |
| `docs/roadmap.md` | the "Current state" heading |

`AssemblyVersion` and `AssemblyFileVersion` stay numeric (`1.0.0.0`), so
Rhino's plug-in list shows `1.0.0.0`. The smoke suite fails when the plugin
version and `manifest.yml` disagree.

## Build the package

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
```

The package name comes from `manifest.yml`, and yak reads the distribution
tag from the RhinoCommon version the plugin was built against: `rh8_0` means
Rhino 8 and later service releases. The result is
`44-view-<version>-rh8_0-any.yak`.

## Try it on the test server

The test server resets every day, so a version number can be pushed there and
pushed again the next day.

```bash
YAK="/c/Program Files/Rhino 8/System/Yak.exe"
"$YAK" login
"$YAK" push --source https://test.yak.rhino3d.com 44-view-1.0.0-alpha.0.0.0-rh8_0-any.yak
"$YAK" search --source https://test.yak.rhino3d.com --all --prerelease 44-view
```

## Publish

```bash
"$YAK" push 44-view-1.0.0-alpha.0.0.0-rh8_0-any.yak
"$YAK" search --all --prerelease 44-view
```

A published version cannot be replaced. A bad one is hidden with
`yak yank 44-view <version>` and its number is not used again. Prerelease
versions install only when someone asks for them by version.

## GitHub release

Pushing a tag that starts with `v`, such as `v1.0.0-alpha.0.0.0`, runs the
Build workflow's package job: it builds the plugin, runs `yak build`, and
attaches the package to the tag's GitHub release. A release already posted for
the tag, for example with the changelog section as its notes, keeps its notes
and gets the package added; otherwise the job creates the release with
generated notes. Versions with a suffix are marked as prereleases. The
workflow never pushes to the package server.

## Food4Rhino

A Food4Rhino app page needs these fields:

- Title: `44 View`
- Short description: one sentence on what the command does.
- Body: the overview from the [documentation site](index.md), with the demo
  animation.
- Category: the Rhino category that covers display and viewports.
- License agreement: MIT.
- Support email: an address that is read, such as a project inbox.
- Project icon: `docs/assets/icon-256.png`.

Media is linked rather than uploaded: Food4Rhino accepts links to YouTube,
Vimeo, p3d.in, mcneel.github.io and ShapeDiver.

There are two ways to connect the page to the package server, and neither runs
on its own:

- Push the package with yak first, then choose **Create App from Yak** on
  Food4Rhino.
- Upload `FourFourView.rhp` to the app page, then choose **Push to Yak**.
