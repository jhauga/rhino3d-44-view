# -*- coding: utf-8 -*-
"""
The demo model the regression suite builds: a plate with two holes and two
posts, 60 x 30 x 14 model units with its corner at the origin. The plate is
2 thick, each post has radius 5 and stands 12 on the plate, and each hole has
radius 1.5. The same model appears in the before and after pictures.
"""

import Rhino
import System.Drawing

from Rhino.Geometry import Box, Brep, Circle, Cylinder, Interval, Plane, Point3d, Vector3d

BOX = (0.0, 0.0, 0.0, 60.0, 30.0, 14.0)
POSTS = ((12.0, 15.0), (48.0, 15.0))
HOLES = ((6.0, 4.0), (54.0, 26.0))
POST_BOXES = ((7.0, 10.0, 2.0, 17.0, 20.0, 14.0), (43.0, 10.0, 2.0, 53.0, 20.0, 14.0))
LAYERS = (("Board", (108, 143, 191)), ("Posts", (61, 79, 102)), ("Holes", (232, 131, 58)))


def _layer(doc, name, rgb):
    index = doc.Layers.FindByFullPath(name, -1)
    if index < 0:
        layer = Rhino.DocObjects.Layer()
        layer.Name = name
        layer.Color = System.Drawing.Color.FromArgb(rgb[0], rgb[1], rgb[2])
        index = doc.Layers.Add(layer)
    return index


def _attributes(layer_index, name):
    attributes = Rhino.DocObjects.ObjectAttributes()
    attributes.LayerIndex = layer_index
    attributes.Name = name
    return attributes


def build(doc, log):
    """Adds the model to the document and returns the ids of its parts by name."""
    tolerance = doc.ModelAbsoluteTolerance
    layers = dict((name, _layer(doc, name, rgb)) for name, rgb in LAYERS)
    ids = {}

    plate = Box(Plane.WorldXY, Interval(0, 60), Interval(0, 30), Interval(0, 2)).ToBrep()
    for x, y in HOLES:
        cutter = Cylinder(Circle(Plane(Point3d(x, y, -1), Vector3d.ZAxis), 1.5), 4).ToBrep(True, True)
        result = Brep.CreateBooleanDifference(plate, cutter, tolerance)
        if result is not None and len(result) == 1:
            plate = result[0]
        else:
            log("        the hole at %s, %s could not be cut; the plate stays whole there" % (x, y))
    ids["plate"] = doc.Objects.AddBrep(plate, _attributes(layers["Board"], "plate"))

    for number, (x, y) in enumerate(POSTS):
        name = "post %d" % (number + 1)
        post = Cylinder(Circle(Plane(Point3d(x, y, 2), Vector3d.ZAxis), 5), 12).ToBrep(True, True)
        ids[name] = doc.Objects.AddBrep(post, _attributes(layers["Posts"], name))

    for number, (x, y) in enumerate(HOLES):
        name = "hole %d" % (number + 1)
        ring = Circle(Plane(Point3d(x, y, 2), Vector3d.ZAxis), 1.5)
        ids[name] = doc.Objects.AddCircle(ring, _attributes(layers["Holes"], name))

    doc.Views.Redraw()
    return ids
