# Tests

Three suites, ordered by how much they need to run and by how early they
should fail.

| Suite | Runs in | Needs Rhino | What it proves |
| --- | --- | --- | --- |
| `unit` | `dotnet test` | no | The fit math: one shared scale, each view's frame, flat shapes, points and bad input |
| `smoke` | Rhino | yes | The plugin loads, registers `44View`, shows its icon and fits a model |
| `regression` | Rhino | yes | The fitted views, measured from the live viewports, for every option and edge case |

## Running them

```powershell
powershell -File tests\run-tests.ps1                              # everything
powershell -File tests\run-tests.ps1 -Suite unit                  # fast, no Rhino
powershell -File tests\run-tests.ps1 -Suite rhino                 # smoke and regression
powershell -File tests\run-tests.ps1 -Suite regression -SkipBuild
```

The unit suite also runs on its own through the normal .NET tooling:

```bash
dotnet test tests/FourFourView.Fit.Tests/FourFourView.Fit.Tests.csproj
```

For the in-Rhino suites, the runner builds the plugin in Release, copies a
template into `tests/artifacts/`, starts Rhino 8 on the copy with
`RHINO_PACKAGE_DIRS` pointed at the build output, and waits for
`tests/artifacts/rhino-report.json`. Rhino's exit code says nothing about the
results, so the report decides pass or fail. Everything a run writes lands in
`tests/artifacts/`, which is not committed.

Rhino's window is maximized while the suites run and put back when they end.
The viewports then have the same size on every run, whatever size Rhino was
last left at, and the screen copies come out at full size.

The default template is Rhino's small-objects millimeter template; pass
`-Template` to start from another. Keep the repository in a folder whose path
has no spaces: the runner passes the test script's path to Rhino's
`/runscript` option without quotes.

Continuous integration runs the unit suite only. A hosted build machine has no
Rhino, so the smoke and regression suites run on a developer machine.

## Layout

```text
tests/
  run-tests.ps1                one entry point for all three suites
  FourFourView.Fit.Tests/      xUnit project, compiles the fit math directly
  rhino/
    run_in_rhino.py            what Rhino runs: loads the plugin, runs the suites, writes the report
    ffv_harness.py             cases, assertions, viewport measurements, captures
    ffv_model.py               the demo model
    ffv_smoke.py               the smoke suite
    ffv_regression.py          the regression suite
  artifacts/                   reports, logs, captures and pictures (ignored)
```

## The demo model

The regression suite builds a plate with two holes and two posts, 60 x 30 x 14
model units with its corner at the origin. The plate is 2 thick, each post has
radius 5 and stands 12 on the plate, and each hole has radius 1.5. The parts
sit on the layers Board, Posts and Holes.

## What the regression suite checks

After every fit, the suite measures the viewports themselves and compares the
result with the plugin's report:

- Top, Front and Right share one scale, the report's units per pixel, to
  within one part in a billion.
- Each view is centered on the model to within half a pixel, and its camera
  target sits on the model.
- Each frame holds the model with the padding around it, and the view that
  needs the most room is filled on one axis.
- A model corner falls on the same screen column in Top and Front, and on the
  same screen row in Front and Right. This is what lines the grids up.
- Every camera looks in its standard direction and has its standard
  construction plane.
- The Perspective view shows every corner of the model.
- Exactly four named views exist, and each viewport already shows its own.

Then one case each covers an empty model, a fit that does not save named
views, a repeated fit, options set from a `-44View` macro, a selection with
and without the Auto source, an empty selection, zero padding, hidden and
locked objects and block instances, a flat curve, a single point, a fit that
keeps the current viewport arrangement, and a different display mode in each
view, kept through the layout even when a view was maximized or closed.

## Pictures

| File | Shows |
| --- | --- |
| `before.png` | The demo model after the built-in `4View`: the four view captures arranged like the viewports, labelled, under a title strip |
| `after.png` | The same after `44View` |
| `before-screen.png`, `after-screen.png` | The four viewports copied from the screen, with Rhino's own titles and borders |
| `display-modes-screen.png` | The four viewports copied from the screen after `44View`, each in a different display mode |

The before and after pictures show the Perspective view in Shaded. The runner
assembles `before.png` and `after.png` with ImageMagick when `magick` is on the
PATH. The screen copies need a visible Rhino window and are skipped
otherwise. Pictures are for looking at and for the documentation; the
assertions decide whether a run passes.

## Why the in-Rhino suites are written this way

RhinoCommon only exists inside a running Rhino, so anything touching the
command, the viewports or the named views cannot use a normal .NET test
runner. Those suites are Python modules that Rhino executes, with
`tests/rhino/ffv_harness.py` supplying the parts a runner needs: case
registration, assertions that fail with a readable message, measurements taken
from the viewports, and captures.

The fit math has no RhinoCommon dependency, which is why the unit project can
compile it straight into the test assembly and run in milliseconds without
Rhino installed.

The Python is written for both IronPython 2.7, which `-_RunPythonScript`
hosts, and CPython 3. It stays ASCII only and avoids syntax exclusive to
either version.

## Saved options

`44View` remembers the options chosen in `-44View`. The in-Rhino suites set any
saved options aside when they start and put them back when they finish. They
read the options only through the settings' TryGet methods, because a Get call
with a default value stores that default.

## Manual checks

Things the suites cannot reach. Check them by hand before a release.

- **Undo.** Rhino ignores undo records created while a script is still
  running, so the harness cannot test Undo. Rhino's Undo does not change
  views. UndoView in a viewport steps back through the camera changes one at a
  time; the first presses after a fit change nothing visible.
- **First-angle layouts.** `44View` always restores Rhino's default
  four-view layout, with Top above Front and Right beside Front.
- **Non-English Rhino.** When the views cannot be found by their English
  names after the layout, they are identified by position. An English Rhino
  never takes that path.
- **Rhino for Mac.** Not tested.
