# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-alpha.0.0.0] - 2026-10-11

### Added

- `44View` command: restores the four default viewports, fits Top, Front and
  Right at one shared scale centered on the model so the grids line up across
  views, fits Perspective in its default direction, and saves the four cameras
  as the named views Perspective, Top, Front and Right.
- `-44View` options: `Source` (Auto, Visible or Selected), `Padding`,
  `SaveViews` and `Layout`. The choice becomes the default for the plain
  command. The default padding follows Rhino's Zoom Extents border.
- Every camera targets the model and gets its view's standard construction
  plane, and each viewport shows its own named view.
- Each view keeps its display mode, such as Wireframe, Shaded or Rendered,
  when the four default viewports are restored.
- Flat parts, curves and single points fit; a point is framed as a one unit
  cube.
- Scripting object with `Fit`, `FitWith` and a JSON `LastReport`, reachable as
  `rs.GetPlugInObject("FourFourView")`.
- Unit tests for the fit math, and smoke and regression suites that run inside
  Rhino and measure the fitted viewports.
- Plugin icon, logo, banner, demo animation, and a documentation site with a
  matching favicon.
- Build workflow that builds and unit-tests every push and packages version
  tags with yak, and a workflow that publishes the documentation site.

[1.0.0-alpha.0.0.0]: https://github.com/jhauga/rhino3d-44-view/releases/tag/v1.0.0-alpha.0.0.0
