# -*- coding: utf-8 -*-
"""
Regression suite: the four fitted views, measured from the live viewports.

The cases share one document and run in order: an empty model first, then the
demo model from ffv_model.py, the built-in 4View as the before picture, 44View
with the default options as the after picture, and then one case per option
and edge case. Every check reads the viewports themselves (frustum, camera,
target, construction plane, screen position) and compares them with the
plugin's report, rather than trusting the report.
"""

import Rhino

from Rhino.Geometry import BoundingBox, Box, Interval, Plane, Point3d, Rectangle3d, Transform, Vector3d

import ffv_harness as h
import ffv_model

SUITE = h.Suite("regression", "Scale, centering, alignment, named views, options and edge cases.")

DEMO_BOX = list(ffv_model.BOX)
MINIMUM_EXTENT = 1.0
PIXEL_SLACK = 0.5
DIRECTIONS = {"Top": (0.0, 0.0, -1.0), "Front": (0.0, 1.0, 0.0), "Right": (-1.0, 0.0, 0.0)}
NORMALS = {"Top": (0.0, 0.0, 1.0), "Front": (0.0, -1.0, 0.0), "Right": (1.0, 0.0, 0.0), "Perspective": (0.0, 0.0, 1.0)}


# ---- Checks shared by the cases ---------------------------------------------


def default_padding():
    """The padding a plain 44View uses: Rhino's own zoom extents border for parallel views."""
    border = Rhino.ApplicationSettings.ViewSettings.ZoomExtentsParallelViewBorder
    if 1.0 <= border <= 2.0:
        return round((border - 1.0) * 100.0, 1)
    return 10.0


def projected(name, box):
    dx = box.Max.X - box.Min.X
    dy = box.Max.Y - box.Min.Y
    dz = box.Max.Z - box.Min.Z
    return {"Top": (dx, dy), "Front": (dx, dz), "Right": (dy, dz)}[name]


def unit_close(vector, expected, message, tolerance=1e-9):
    length = vector.Length
    got = (vector.X / length, vector.Y / length, vector.Z / length)
    for value, want in zip(got, expected):
        if abs(value - want) > tolerance:
            raise h.Failure("%s: expected %s, got (%.9f, %.9f, %.9f)" % (message, expected, got[0], got[1], got[2]))


def views(context):
    return dict((name, h.not_none(h.view(context.doc, name), "the %s view" % name)) for name in h.VIEW_NAMES)


def check_layout(context):
    """The four-view layout: Top over Front sharing a width, Front beside Right sharing a height."""
    found = views(context)
    top, front, right = found["Top"].ScreenRectangle, found["Front"].ScreenRectangle, found["Right"].ScreenRectangle
    h.equal(top.Left, front.Left, "Top and Front start at the same screen column")
    h.equal(top.Width, front.Width, "Top and Front have the same width")
    h.equal(front.Top, right.Top, "Front and Right start at the same screen row")
    h.equal(front.Height, right.Height, "Front and Right have the same height")
    sizes = [found[name].MainViewport.Size for name in h.PARALLEL_NAMES]
    for size in sizes:
        h.ok(abs(size.Width - sizes[0].Width) <= 1 and abs(size.Height - sizes[0].Height) <= 1,
             "parallel viewports should be the same size within a pixel, got %s" %
             ", ".join(["%dx%d" % (s.Width, s.Height) for s in sizes]))


def check_fitted(context, report):
    """Everything a fit promises, measured from the viewports."""
    found = views(context)
    box = h.box_from(report["box"])
    center = box.Center
    border = 1.0 + float(report["padding"]) / 100.0
    scale = float(report["unitsPerPixel"])
    if report["usedMinimumExtent"]:
        half = MINIMUM_EXTENT / 2.0
        fit_box = BoundingBox(center - Vector3d(half, half, half), center + Vector3d(half, half, half))
    else:
        fit_box = box
    tolerance = 1e-9 * (1.0 + fit_box.Diagonal.Length + Vector3d(center).Length)

    for name in h.PARALLEL_NAMES:
        viewport = found[name].MainViewport
        size = viewport.Size
        h.ok(viewport.IsParallelProjection, "%s should be a parallel view" % name)
        across_scale, up_scale = h.units_per_pixel(viewport)
        h.close_relative(across_scale, scale, "%s units per pixel across" % name)
        h.close_relative(up_scale, scale, "%s units per pixel up" % name)

        x, y = h.client(viewport, center)
        h.close(x, size.Width / 2.0, "%s model center across, in pixels" % name, PIXEL_SLACK)
        h.close(y, size.Height / 2.0, "%s model center up, in pixels" % name, PIXEL_SLACK)
        h.close(viewport.CameraTarget.DistanceTo(center), 0.0, "%s camera target distance from the model center" % name, tolerance)
        unit_close(viewport.CameraDirection, DIRECTIONS[name], "%s camera direction" % name)
        unit_close(viewport.GetConstructionPlane().Plane.Normal, NORMALS[name], "%s construction plane normal" % name)

        across, up = projected(name, fit_box)
        f = h.frustum(viewport)
        h.ok(f[1] - f[0] >= across * border * (1.0 - 1e-9), "%s frame is narrower than the model with its border" % name)
        h.ok(f[3] - f[2] >= up * border * (1.0 - 1e-9), "%s frame is shorter than the model with its border" % name)
        margin_x = across * (border - 1.0) / 2.0 / scale - 1.0
        margin_y = up * (border - 1.0) / 2.0 / scale - 1.0
        for corner in h.corners(fit_box):
            cx, cy = h.client(viewport, corner)
            h.ok(margin_x - PIXEL_SLACK <= cx <= size.Width - margin_x + PIXEL_SLACK,
                 "%s: a model corner sits at x=%.2f, outside the %.1f pixel margin" % (name, cx, margin_x))
            h.ok(margin_y - PIXEL_SLACK <= cy <= size.Height - margin_y + PIXEL_SLACK,
                 "%s: a model corner sits at y=%.2f, outside the %.1f pixel margin" % (name, cy, margin_y))

    limiting = str(report["limitingView"])
    across, up = projected(limiting, fit_box)
    f = h.frustum(found[limiting].MainViewport)
    errors = []
    if across > 0:
        errors.append(abs((f[1] - f[0]) - across * border) / (across * border))
    if up > 0:
        errors.append(abs((f[3] - f[2]) - up * border) / (up * border))
    h.ok(errors and min(errors) <= 1e-9, "the limiting %s view should be filled on one axis, relative errors %s" % (limiting, errors))

    a = Point3d(fit_box.Max.X, fit_box.Min.Y, fit_box.Max.Z)
    b = Point3d(fit_box.Min.X, fit_box.Max.Y, fit_box.Min.Z)
    for point in (a, b):
        h.close(h.screen(found["Top"], point)[0], h.screen(found["Front"], point)[0],
                "Top and Front screen column of a model corner", PIXEL_SLACK)
        h.close(h.screen(found["Front"], point)[1], h.screen(found["Right"], point)[1],
                "Front and Right screen row of a model corner", PIXEL_SLACK)

    perspective = found["Perspective"].MainViewport
    h.ok(not perspective.IsParallelProjection, "Perspective should be a perspective view")
    h.close(perspective.CameraTarget.DistanceTo(center), 0.0, "Perspective camera target distance from the model center", tolerance)
    unit_close(perspective.GetConstructionPlane().Plane.Normal, NORMALS["Perspective"], "Perspective construction plane normal")
    size = perspective.Size
    for corner in h.corners(box):
        cx, cy = h.client(perspective, corner)
        h.ok(0 <= cx <= size.Width and 0 <= cy <= size.Height,
             "Perspective: a model corner sits outside the view at %.1f, %.1f" % (cx, cy))


def check_named_views(context):
    """Exactly the four named views exist, and each viewport already shows its own."""
    doc = context.doc
    names = h.named_views(doc)
    h.equal(sorted(names), sorted(h.VIEW_NAMES), "named views")
    for name in h.VIEW_NAMES:
        viewport = h.view(doc, name).MainViewport
        before = h.camera_state(viewport)
        index = doc.NamedViews.FindByName(name)
        h.ok(index >= 0, "named view %s not found" % name)
        doc.NamedViews.Restore(index, viewport)
        after = h.camera_state(viewport)
        for got, want in zip(after, before):
            h.close(got, want, "%s viewport compared with its named view" % name, 1e-9 * (1.0 + abs(want)))


def fit_with(context, source, padding, save_views, layout):
    fitted = context.ffv.FitWith(source, padding, save_views, layout)
    report = context.report()
    h.ok(fitted, "FitWith(%s, %s, %s, %s) did not fit: %s" % (source, padding, save_views, layout, report["message"]))
    return report


def select_only(context, object_ids):
    doc = context.doc
    doc.Objects.UnselectAll()
    for object_id in object_ids:
        h.ok(doc.Objects.Select(object_id), "could not select %s" % object_id)
    context.pump()


def all_states(context):
    return dict((name, h.camera_state(h.view(context.doc, name).MainViewport)) for name in h.VIEW_NAMES)


def display_modes():
    """Rhino's display modes by English name."""
    return dict((str(mode.EnglishName), mode) for mode in Rhino.Display.DisplayModeDescription.GetDisplayModes())


def display_mode_name(context, name):
    return str(h.view(context.doc, name).MainViewport.DisplayMode.EnglishName)


def shade_perspective(context):
    """The pictures show the Perspective view in Shaded."""
    mode = display_modes().get("Shaded")
    h.not_none(mode, "the Shaded display mode is not available")
    h.view(context.doc, "Perspective").MainViewport.DisplayMode = mode
    context.pump()


# ---- Cases, in the order they run -------------------------------------------


@SUITE.case("an empty model is not fitted and no named view changes")
def empty_model(context):
    named_before = h.named_views(context.doc)
    h.equal(bool(context.ffv.Fit()), False, "Fit() on an empty model")
    report = context.report()
    h.equal(report["result"], "Nothing", "result")
    h.ok("no visible objects" in report["message"], "message should name the empty model, got %r" % report["message"])
    h.equal(h.named_views(context.doc), named_before, "named views after an empty fit")


@SUITE.case("the demo model builds")
def demo_model(context):
    doc = context.doc
    ids = ffv_model.build(doc, context.log)
    context.state["ids"] = ids
    boxes = []
    for name, object_id in ids.items():
        found = doc.Objects.FindId(object_id)
        h.not_none(found, "%s was not added" % name)
        boxes.append(found.Geometry.GetBoundingBox(True))
    h.bbox_equal(h.box_values(h.union(boxes)), DEMO_BOX, "demo model bounding box")


@SUITE.case("before picture: the built-in 4View")
def builtin_4view(context):
    shade_perspective(context)
    for attempt in (1, 2):
        outcome = context.run("_-4View _Enter")
        h.ok(outcome["ran"], "the built-in 4View did not run")
    h.capture_views(context, "before")
    h.capture_screen(context, "before")
    scales = [h.units_per_pixel(h.view(context.doc, name).MainViewport)[0] for name in h.PARALLEL_NAMES]
    context.log("        built-in 4View units per pixel, Top, Front, Right: %s" % ", ".join(["%.6f" % s for s in scales]))


@SUITE.case("SaveViews off fits without touching the named views")
def save_views_off(context):
    named_before = h.named_views(context.doc)
    report = fit_with(context, "Visible", 10.0, False, True)
    h.equal(report["namedViewsSaved"], False, "namedViewsSaved")
    h.equal(h.named_views(context.doc), named_before, "named views after a fit with SaveViews off")


@SUITE.case("after picture: 44View with the default options fits, centers and aligns the four views")
def default_fit(context):
    shade_perspective(context)
    outcome = context.run("_44View")
    h.capture_views(context, "after")
    h.capture_screen(context, "after")
    h.equal(outcome["result"], "Success", "44View command result")
    report = context.report()
    h.ok(report["fitted"], report["message"])
    h.equal(report["source"], "visible", "source")
    h.equal(report["objectCount"], len(context.state["ids"]), "objects measured")
    h.bbox_equal(report["box"], DEMO_BOX, "measured box")
    h.close(report["padding"], default_padding(), "default padding")
    h.equal(report["warnings"], [], "warnings")
    h.ok(report["message"] in outcome["printed"], "summary line missing from the command history")
    check_layout(context)
    check_fitted(context, report)
    check_named_views(context)


@SUITE.case("a repeated 44View keeps exactly four named views")
def repeated_fit(context):
    outcome = context.run("_44View")
    h.equal(outcome["result"], "Success", "44View command result")
    check_named_views(context)


@SUITE.case("-44View sets every option from a macro and saves them")
def scripted_options(context):
    guard = context.state["guard"]
    try:
        outcome = context.run("_-44View _Source=_Visible _Padding=5 _SaveViews=_No _Layout=_Yes _Enter")
        h.equal(outcome["result"], "Success", "-44View command result")
        report = context.report()
        h.close(report["padding"], 5.0, "padding used")
        h.equal(report["namedViewsSaved"], False, "named views saved")
        saved = guard.current()
        h.equal(saved["Source"], "Visible", "saved Source")
        h.close(saved["Padding"], 5.0, "saved Padding")
        h.equal(saved["SaveViews"], False, "saved SaveViews")
        h.equal(saved["Layout"], True, "saved Layout")
        check_fitted(context, report)
    finally:
        guard.clear()


@SUITE.case("a selection is fitted on its own, and Auto picks it")
def selection(context):
    post = context.state["ids"]["post 1"]
    select_only(context, [post])
    try:
        report = fit_with(context, "Selected", 10.0, True, True)
        h.equal(report["source"], "selected", "source")
        h.equal(report["objectCount"], 1, "objects measured")
        h.bbox_equal(report["box"], list(ffv_model.POST_BOXES[0]), "post 1 box")
        check_fitted(context, report)
        h.ok(context.ffv.Fit(), "Fit() with a selection did not fit: " + context.report()["message"])
        h.equal(context.report()["source"], "selected", "source picked by Auto")
    finally:
        context.doc.Objects.UnselectAll()
        context.pump()


@SUITE.case("Selected with nothing selected changes nothing")
def empty_selection(context):
    context.doc.Objects.UnselectAll()
    context.pump()
    before = all_states(context)
    h.equal(bool(context.ffv.FitWith("Selected", 10.0, True, True)), False, "FitWith with nothing selected")
    report = context.report()
    h.equal(report["result"], "Nothing", "result")
    h.ok("no objects are selected" in report["message"], "message should name the empty selection, got %r" % report["message"])
    after = all_states(context)
    for name in h.VIEW_NAMES:
        for got, want in zip(after[name], before[name]):
            h.close(got, want, "%s view after a refused fit" % name, 1e-12 * (1.0 + abs(want)))


@SUITE.case("zero padding fills the limiting view exactly")
def zero_padding(context):
    report = fit_with(context, "Visible", 0.0, False, True)
    h.close(report["padding"], 0.0, "padding")
    check_fitted(context, report)


@SUITE.case("hidden objects are left out; locked objects and block instances count")
def object_kinds(context):
    doc = context.doc
    extras = []
    block = None
    try:
        hidden = doc.Objects.AddBrep(Box(Plane.WorldXY, Interval(200, 210), Interval(200, 210), Interval(0, 10)).ToBrep())
        extras.append(hidden)
        h.ok(doc.Objects.Hide(hidden, True), "could not hide the far box")
        report = fit_with(context, "Visible", 10.0, False, True)
        h.bbox_equal(report["box"], DEMO_BOX, "box with a hidden object far away")

        locked = doc.Objects.AddBrep(Box(Plane.WorldXY, Interval(-20, -10), Interval(0, 10), Interval(0, 10)).ToBrep())
        extras.append(locked)
        h.ok(doc.Objects.Lock(locked, True), "could not lock the side box")
        report = fit_with(context, "Visible", 10.0, False, True)
        h.bbox_equal(report["box"], [-20, 0, 0, 60, 30, 14], "box with a locked object")
        doc.Objects.Unlock(locked, True)
        doc.Objects.Delete(locked, True)
        extras.remove(locked)

        unit = Box(Plane.WorldXY, Interval(0, 1), Interval(0, 1), Interval(0, 1)).ToBrep()
        block = doc.InstanceDefinitions.Add("ffv-test-block", "", Point3d.Origin, unit, Rhino.DocObjects.ObjectAttributes())
        h.ok(block >= 0, "the block definition was not added")
        placement = Transform.Translation(0, 50, 0) * Transform.Scale(Point3d.Origin, 2.0)
        instance = doc.Objects.AddInstanceObject(block, placement)
        extras.append(instance)
        report = fit_with(context, "Visible", 10.0, False, True)
        h.bbox_equal(report["box"], [0, 0, 0, 60, 52, 14], "box with a block instance")
        check_fitted(context, report)
    finally:
        for object_id in extras:
            doc.Objects.Show(object_id, True)
            doc.Objects.Unlock(object_id, True)
            doc.Objects.Delete(object_id, True)
        if block is not None and block >= 0:
            doc.InstanceDefinitions.Delete(block, True, True)
        context.pump()


@SUITE.case("a flat curve fits with finite frames in every view")
def flat_curve(context):
    doc = context.doc
    curve = doc.Objects.AddCurve(Rectangle3d(Plane.WorldXY, 20.0, 10.0).ToNurbsCurve())
    try:
        select_only(context, [curve])
        report = fit_with(context, "Selected", 10.0, False, True)
        h.bbox_equal(report["box"], [0, 0, 0, 20, 10, 0], "rectangle box")
        h.equal(report["usedMinimumExtent"], False, "usedMinimumExtent")
        for frame in report["frames"]:
            h.ok(0 < frame["width"] < 1e9 and 0 < frame["height"] < 1e9, "%s frame is not a finite size" % frame["view"])
        check_fitted(context, report)
    finally:
        doc.Objects.UnselectAll()
        doc.Objects.Delete(curve, True)
        context.pump()


@SUITE.case("a single point fits a cube of the minimum extent")
def single_point(context):
    doc = context.doc
    point = doc.Objects.AddPoint(Point3d(5, 5, 5))
    try:
        select_only(context, [point])
        report = fit_with(context, "Selected", 10.0, False, True)
        h.bbox_equal(report["box"], [5, 5, 5, 5, 5, 5], "point box")
        h.equal(report["usedMinimumExtent"], True, "usedMinimumExtent")
        check_fitted(context, report)
    finally:
        doc.Objects.UnselectAll()
        doc.Objects.Delete(point, True)
        context.pump()


@SUITE.case("Layout off fits the existing four views without rearranging them")
def layout_off(context):
    before = sorted([str(v.MainViewport.Id) for v in context.doc.Views.GetStandardRhinoViews()])
    report = fit_with(context, "Visible", 10.0, False, False)
    h.equal(report["layoutApplied"], False, "layoutApplied")
    after = sorted([str(v.MainViewport.Id) for v in context.doc.Views.GetStandardRhinoViews()])
    h.equal(after, before, "standard views after a fit with Layout off")
    check_fitted(context, report)


def check_display_modes(context, wanted, when):
    for name, mode in sorted(wanted.items()):
        h.equal(display_mode_name(context, name), mode, "%s display mode %s" % (name, when))


@SUITE.case("each view keeps its display mode, also when the layout is rebuilt")
def keeps_display_modes(context):
    doc = context.doc
    available = display_modes()
    wanted = {"Top": "Shaded", "Front": "Ghosted", "Right": "X-Ray", "Perspective": "Rendered"}
    for mode in wanted.values():
        h.ok(mode in available, "the %s display mode is not available" % mode)

    before = dict((name, h.view(doc, name).MainViewport.DisplayMode) for name in h.VIEW_NAMES)
    try:
        for name, mode in wanted.items():
            h.view(doc, name).MainViewport.DisplayMode = available[mode]
        context.pump()
        outcome = context.run("_44View")
        h.capture_screen(context, "display-modes")
        h.equal(outcome["result"], "Success", "44View command result")
        check_display_modes(context, wanted, "after 44View")

        h.view(doc, "Perspective").Maximized = True
        context.pump()
        outcome = context.run("_44View")
        h.equal(outcome["result"], "Success", "44View command result from a maximized view")
        h.ok(not h.view(doc, "Perspective").Maximized, "Perspective should no longer be maximized")
        check_display_modes(context, wanted, "after 44View from a maximized view")

        fit_with(context, "Visible", 10.0, False, False)
        check_display_modes(context, wanted, "after a fit with Layout off")

        h.ok(h.view(doc, "Right").Close(), "the Right view should close")
        context.pump()
        outcome = context.run("_44View")
        h.equal(outcome["result"], "Success", "44View command result with the Right view closed")
        reopened = dict(wanted)
        reopened["Right"] = "Wireframe"
        check_display_modes(context, reopened, "after 44View with the Right view closed")
    finally:
        if [name for name in h.VIEW_NAMES if h.view(doc, name) is None]:
            doc.Views.FourViewLayout(False)
            context.pump()
        for name, mode in before.items():
            rhino_view = h.view(doc, name)
            if rhino_view is not None and mode is not None:
                rhino_view.MainViewport.DisplayMode = mode
        context.pump()
