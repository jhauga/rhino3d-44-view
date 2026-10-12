# Usage

## The command

Type `44View`. In one step it:

1. Measures the selected objects, or every visible object when nothing is
   selected.
2. Restores the four default viewports: Top at the upper left, Perspective at
   the upper right, Front at the lower left and Right at the lower right. A
   closed or maximized view comes back as well. Each view keeps the display
   mode it had, such as Wireframe, Shaded or Rendered; a view that was closed
   comes back in Wireframe.
3. Fits Top, Front and Right at one shared scale, each centered on the model,
   with the margin Rhino's Zoom Extents leaves.
4. Fits the Perspective view in its default direction so the whole model is in
   view.
5. Saves the four cameras as the named views Perspective, Top, Front and Right,
   and shows each named view in its own viewport.

The plain command never asks a question. It uses the options last chosen in
`-44View`, or the defaults below.

## Options

Type `-44View` to see the options before the fit:

```text
Press Enter to fit the four views ( Source=Auto  Padding=10  SaveViews=Yes  Layout=Yes )
```

| Option | Values | Default | What it does |
| --- | --- | --- | --- |
| `Source` | `Auto`, `Visible`, `Selected` | `Auto` | Which objects are measured. `Auto` measures the selection when there is one and every visible object otherwise. |
| `Padding` | 0 to 100 | Rhino's Zoom Extents border, normally `10` | Space left around the model, as a percent of its size, in the view that needs the most room. |
| `SaveViews` | `Yes`, `No` | `Yes` | Saves the four cameras as named views. |
| `Layout` | `Yes`, `No` | `Yes` | Restores the four default viewports first. With `No`, the Top, Front, Right and Perspective views are fitted where they are. |

Press Enter to fit with the options shown. The values chosen here become the
defaults for the plain `44View`.

### Macros and toolbar buttons

Every option can be set from a macro. Each of these fits and saves its choices:

```text
! _-44View _Source=_Selected _Padding=5 _Enter
! _-44View _SaveViews=_No _Layout=_Yes _Enter
```

## What gets measured

- Visible objects in model space count, including locked objects and block
  instances.
- Hidden objects, objects on hidden layers, lights, clipping planes and objects
  on layout pages are left out.
- A flat part fits like any other; a single point is framed as a one unit cube
  around it.

When there is nothing to measure, nothing changes and the command line says
why:

```text
44View: nothing to fit; no objects are selected.
```

## Named views

The four cameras are saved as Perspective, Top, Front and Right. Named views
that already have those names are replaced, so running the command again keeps
the list at four. Other named views are left alone.

Each viewport shows its own named view afterwards, so its title reads `Top`
rather than `Top*` until the view is changed again.

## Getting earlier cameras back

`44View` replaces the cameras of the four views, as `4View` does. Rhino's Undo
does not change views. To keep a camera, save it as a named view under another
name, such as `Top before`, before running the command.

## The summary line

Every run prints one line:

```text
44View: fitted 12 visible objects at 0.1375 mm per pixel, limited by Top; named views saved.
```

- **12 visible objects**: what was measured, and whether it was the selection
  or the visible objects.
- **0.1375 mm per pixel**: the scale Top, Front and Right share.
- **limited by Top**: the view that needed the most room. It is filled to the
  padding on one side; the others have more room to spare.

## Scripting

Scripts get the same fit through the plugin object:

```python
import json
import rhinoscriptsyntax as rs

views = rs.GetPlugInObject("FourFourView")
if views.FitWith("Selected", 5.0, True, True):
    report = json.loads(views.LastReport)
    print(report["unitsPerPixel"], report["limitingView"])
```

| Member | What it does |
| --- | --- |
| `Fit()` | Fits with the saved options. Returns true when the views were fitted. |
| `FitWith(source, padding, saveViews, layout)` | Fits with the given options without saving them. `source` is `"Auto"`, `"Visible"` or `"Selected"`; `padding` is a percent from 0 to 100. |
| `LastReport` | JSON describing the most recent run, from the command or a script. |
| `Version` | The plugin version, such as `1.0.0-alpha.0.0.0`. |

The fields of `LastReport`:

| Field | Meaning |
| --- | --- |
| `fitted` | True when the views were fitted |
| `result` | `Success`, `Nothing` or `Failure` |
| `source` | `visible` or `selected` |
| `objectCount` | Objects measured |
| `box` | Their bounding box: min X, Y, Z, then max X, Y, Z |
| `units` | The model's unit abbreviation, such as `mm` |
| `padding` | The padding used, in percent |
| `unitsPerPixel` | The scale Top, Front and Right share |
| `limitingView` | The view that needed the most room |
| `usedMinimumExtent` | True when the objects had no size and a one unit cube was framed |
| `frames` | Per parallel view: `view`, frame `width` and `height` in model units, the `requiredUnitsPerPixel` that view alone needed, and whether it was `limiting` |
| `layoutApplied` | True when the four default viewports were restored first |
| `namedViewsSaved` | True when all four named views were saved |
| `warnings` | Problems that did not stop the fit |
| `message` | The summary line |

## Limitations

- The layout is always Rhino's default four-view arrangement, with Top above
  Front and Right beside Front. First-angle arrangements are not supported.
- `Padding` applies to Top, Front and Right. Perspective uses Rhino's own
  perspective zoom border.
- With `Layout=No` and viewports of different sizes, the views still share one
  scale, but their edges line up only where the viewports are the same size.
