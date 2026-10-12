# How it works

## Why the grids stop lining up

`4View` brings back the default cameras, which know nothing about the model.
Zoom Extents then fits each view on its own: a model 60 mm long and 14 mm tall
fills Front at one scale, and Right, where it is only 30 mm wide, at another.
The grid squares come out a different size in each view, and a line along the
top of the model in Front does not continue into Right.

## One scale for three views

Each parallel view shows two sides of the model's bounding box:

| View | Looks along | Across the screen | Up the screen |
| --- | --- | --- | --- |
| Top | down the Z axis | X | Y |
| Front | the Y axis | X | Z |
| Right | against the X axis | Y | Z |

To fit its two sides with the padding around them, a view needs a scale of at
least:

```text
required = larger of (across x border / viewport width, up x border / viewport height)
```

The scale is in model units per pixel, and the border is 1 plus the padding as
a fraction: 1.1 for 10 percent. `44View` takes the largest of the three
requirements and uses it in all three views, so every view fits and the view
that needed the most room is filled to the padding on one side. Each view's
frame is that scale times the viewport's size in pixels, centered on the model.

## Why the grids line up now

Top sits above Front, and the two viewports have the same width. At one scale
and one center, they show exactly the same span of X, so each vertical grid
line and each vertical edge of the model meets its continuation across the
border. Front and Right sit side by side with the same height, so they show
the same span of Z, and the horizontal lines carry across that border in the
same way.

## A worked example

A plate with two posts measures 60 x 30 x 14 mm, with one corner at the
origin. The padding is 10 percent, so the border is 1.1, and each viewport is
800 x 400 pixels.

| View | Sides of the model | Required scale, mm per pixel |
| --- | --- | --- |
| Top | 60 x 30 | larger of (60 x 1.1 / 800, 30 x 1.1 / 400) = larger of (0.0825, 0.0825) = 0.0825 |
| Front | 60 x 14 | larger of (0.0825, 14 x 1.1 / 400 = 0.0385) = 0.0825 |
| Right | 30 x 14 | larger of (30 x 1.1 / 800 = 0.04125, 0.0385) = 0.04125 |

The shared scale is 0.0825 mm per pixel, and Top and Front tie for the most
room. Every parallel view shows a frame of 66 x 33 mm around the model's center
at (30, 15, 7):

- Top and Front both show X from -3 to 63 mm.
- Front and Right both show Z from -9.5 to 23.5 mm.
- Right shows Y from -18 to 48 mm, so the plate fills 45 percent of its width.

If the Right viewport were narrower, say 400 x 200 pixels, and the model a
10 x 10 x 50 mm column, Right would need 50 x 1.1 / 200 = 0.275 mm per pixel
and would set the scale for all three views.

## Perspective

The Perspective view keeps Rhino's default viewing direction and lens. Its
camera backs away along that direction until the sphere around the model's
bounding box fits the narrower of the lens's two angles, which leaves the
model whole and centered. The padding does not apply here; Rhino's own
perspective zoom border does.

## Cameras, targets and planes

Every camera aims at the center of the model, and that center becomes the
camera target, so rotating or zooming a view pivots on the model. Each view
gets its standard construction plane: world XY for Top and Perspective, XZ for
Front and YZ for Right. The four cameras are then saved as named views and
each viewport shows its own.

## Flat parts, curves and points

A side of zero length needs no room, so the other side decides. A curve drawn
on the XY plane fits Top as usual and appears as a line in Front and Right. A
single point has no size at all; it is framed as a cube one model unit across.
