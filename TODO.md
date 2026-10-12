# TODO

Task-level tracking for 44 View. Higher-level direction lives in
[docs/roadmap.md](docs/roadmap.md).

## Current

- [x] Fit Top, Front and Right at one shared scale, centered on the model
- [x] Restore the four default viewports, including a closed or maximized view
- [x] Save and show the named views Perspective, Top, Front and Right
- [x] Options in `-44View`, saved as the defaults for `44View`
- [x] Unit, smoke and regression suites
- [x] Build a yak package and install it from a local folder
- [ ] Check by hand on large models (hundreds of objects, big meshes)
- [ ] Check by hand on inch and meter models
- [ ] Check by hand in a Rhino window resized to unusual proportions

## Found Issues

- [ ] UndoView takes two or three presses to return a view to its previous
      camera, and the first presses change nothing visible: applying the
      camera, moving its target and showing the named view each add a history
      entry
- [ ] Views of a non-English Rhino are found by position after the layout;
      that path has not run in a non-English Rhino yet
- [ ] With `Layout=No` and viewports of different sizes, the views share one
      scale but their edges line up only where the viewports are the same size
- [ ] `Padding` does not apply to Perspective, which uses Rhino's own
      perspective zoom border
- [ ] Exact bounding boxes are computed for every object; on very large models
      this may be slow
- [ ] RhinoCommon 8.0.23304.9001 restores through the net48 fallback (NU1701
      suppressed); evaluate a newer package with a net8.0 library
- [ ] The test runner passes the test script's path to Rhino unquoted, so the
      repository path must not contain spaces

## Improvements

- [ ] First-angle layouts (Top below Front, Right beside it on the left)
- [ ] A three-view variant to pair with Rhino's `3View`
- [ ] Padding per view, and an option to pad Perspective as well
- [ ] A toolbar button with the plugin icon
- [ ] Verify and polish behavior on Rhino for Mac

## New Ideas

- [ ] Animate the change with Rhino's animated named view restore
- [ ] Fit to a layer or a group instead of the selection
- [ ] Fit the detail views of a layout page
- [ ] Keep the current cameras as named views before replacing them

## Documentation

- [ ] Add a picture of the `-44View` option prompt to docs/usage.md
- [ ] Record a short screen video for the Food4Rhino page

## Testing

- [ ] Run the in-Rhino suites in CI on a self-hosted runner with Rhino
- [ ] Move the workflows to action versions that run on Node.js 24; GitHub
      warns that the v4 actions in use target Node.js 20
- [ ] Add a run with a non-English Rhino
- [ ] Measure the time on a model with hundreds of objects

## Packaging and Release

- [ ] Push a package to the yak test server and install it from there
- [ ] Confirm that the package server accepts a name starting with a digit
- [ ] Create the Food4Rhino app page from the published package
- [ ] Tag the first release and check the package attached by the Build workflow
