# -*- coding: utf-8 -*-
"""
Test framework for the suites that run inside Rhino.

RhinoCommon only exists inside a running Rhino, so these suites cannot use a
normal .NET test runner. This module supplies what a runner needs: a way to
register cases, assertions that fail with a readable message, measurements
taken from the live viewports, a guard that keeps the plugin's saved options
as they were, and captures of the views.

Written for both IronPython 2.7 (what -_RunPythonScript hosts) and CPython 3,
so it stays ASCII only and avoids syntax exclusive to either version.
"""

import json
import os
import traceback

import Rhino

try:
    import clr
    clr.AddReference("System.Drawing")
except Exception:
    pass

PLUGIN_ID = "6a5e29e1-b3a2-4b20-8f9a-d2489db3ed76"
SETTING_KEYS = ("Source", "Padding", "SaveViews", "Layout")
VIEW_NAMES = ("Top", "Front", "Right", "Perspective")
PARALLEL_NAMES = ("Top", "Front", "Right")


# ---- Assertions -------------------------------------------------------------


class Failure(AssertionError):
    """Raised by the assertions below. Carries the message shown in the report."""
    pass


def ok(condition, message):
    if not condition:
        raise Failure(message)


def equal(actual, expected, message):
    if actual != expected:
        raise Failure("%s: expected %r, got %r" % (message, expected, actual))


def close(actual, expected, message, tolerance=1e-6):
    if abs(float(actual) - float(expected)) > tolerance:
        raise Failure("%s: expected %r, got %r (tolerance %s)" % (message, expected, actual, tolerance))


def close_relative(actual, expected, message, relative=1e-9):
    allowed = relative * abs(float(expected))
    if abs(float(actual) - float(expected)) > allowed:
        raise Failure("%s: expected %r, got %r (relative tolerance %s)" % (message, expected, actual, relative))


def bbox_equal(actual, expected, message, tolerance=1e-6):
    """Compares two [minX, minY, minZ, maxX, maxY, maxZ] boxes."""
    equal(len(actual), 6, message + ": bounding box shape")
    for index in range(6):
        if abs(float(actual[index]) - float(expected[index])) > tolerance:
            raise Failure("%s: expected box %s, got %s (tolerance %s)" % (message, expected, actual, tolerance))


def not_none(value, message):
    if value is None:
        raise Failure(message + ": got None")
    return value


# ---- Suites and the runner --------------------------------------------------


class Suite(object):
    """A named list of cases that run in order. Each case takes the shared context."""

    def __init__(self, name, description=""):
        self.name = name
        self.description = description
        self.cases = []

    def case(self, title):
        def register(function):
            self.cases.append((title, function))
            return function
        return register


class Runner(object):
    """Runs suites, records one result per case, and builds the JSON report."""

    def __init__(self, context):
        self.context = context
        self.results = []

    def run(self, suite):
        self.context.log("")
        self.context.log("== suite: %s ==" % suite.name)
        for title, function in suite.cases:
            self._run_case(suite.name, title, function)

    def _run_case(self, suite_name, title, function):
        record = {"suite": suite_name, "case": title, "status": "passed", "message": ""}
        try:
            function(self.context)
        except Failure:
            record["status"] = "failed"
            record["message"] = str(_current_exception())
        except Exception:
            record["status"] = "error"
            record["message"] = traceback.format_exc()
        self.results.append(record)

        marker = {"passed": "PASS", "failed": "FAIL", "error": "ERROR"}[record["status"]]
        self.context.log("%-5s %s / %s" % (marker, suite_name, title))
        if record["message"]:
            for line in record["message"].strip().splitlines():
                self.context.log("        " + line)

    def counts(self):
        summary = {"passed": 0, "failed": 0, "error": 0}
        for record in self.results:
            summary[record["status"]] = summary[record["status"]] + 1
        return summary

    def succeeded(self):
        counts = self.counts()
        return counts["failed"] == 0 and counts["error"] == 0

    def report(self):
        return {
            "ok": self.succeeded(),
            "counts": self.counts(),
            "total": len(self.results),
            "results": self.results,
            "artifacts": self.context.artifacts,
        }


def _current_exception():
    import sys
    return sys.exc_info()[1]


# ---- Shared context ---------------------------------------------------------


class Context(object):
    """Everything the cases share: the document, the plugin, paths, logging and state."""

    def __init__(self, doc, plugin_object, tests_dir, log_path):
        self.doc = doc
        self.ffv = plugin_object
        self.tests_dir = tests_dir
        self.root = os.path.dirname(tests_dir)
        self.artifacts_dir = os.path.join(tests_dir, "artifacts")
        self.log_path = log_path
        self.artifacts = []
        self.state = {}
        if not os.path.isdir(self.artifacts_dir):
            os.makedirs(self.artifacts_dir)

    def log(self, message):
        text = "%s" % message
        handle = open(self.log_path, "a")
        try:
            handle.write(text + "\n")
        finally:
            handle.close()
        Rhino.RhinoApp.WriteLine(text)

    def artifact(self, name, description):
        self.artifacts.append({"path": name, "description": description})

    def artifact_path(self, name):
        return os.path.join(self.artifacts_dir, name)

    def pump(self):
        """Redraws and lets Rhino process pending window messages."""
        self.doc.Views.Redraw()
        Rhino.RhinoApp.Wait()

    def run(self, macro):
        """Runs a command macro; returns whether it ran, the command result, and what it printed."""
        before = len(history())
        ran = Rhino.RhinoApp.RunScript(macro, False)
        self.pump()
        return {"ran": bool(ran),
                "result": str(Rhino.Commands.Command.LastCommandResult),
                "printed": history()[before:]}

    def report(self):
        """The plugin's last report as a dictionary, or None before the first run."""
        text = str(self.ffv.LastReport)
        return json.loads(text) if text else None


def history():
    text = Rhino.RhinoApp.CommandHistoryWindowText
    return str(text) if text is not None else ""


def read_text(path):
    handle = open(path, "r")
    try:
        return handle.read()
    finally:
        handle.close()


# ---- Viewport measurements --------------------------------------------------


def view(doc, name):
    """The standard model view whose main viewport has the given name."""
    for candidate in doc.Views.GetStandardRhinoViews():
        if str(candidate.MainViewport.Name) == name:
            return candidate
    return None


def frustum(viewport):
    """Left, right, bottom, top, near and far of a viewport's frustum."""
    try:
        result = viewport.GetFrustum()
    except TypeError:
        result = viewport.GetFrustum(0.0, 0.0, 0.0, 0.0, 0.0, 0.0)
    return [float(value) for value in list(result)[1:7]]


def units_per_pixel(viewport):
    """Model units per pixel across and up the viewport."""
    f = frustum(viewport)
    size = viewport.Size
    return (f[1] - f[0]) / size.Width, (f[3] - f[2]) / size.Height


def client(viewport, point):
    """A world point in the viewport's pixel coordinates, from its top left corner."""
    p = viewport.WorldToClient(point)
    return float(p.X), float(p.Y)


def screen(rhino_view, point):
    """A world point in screen pixels, so positions compare across views."""
    x, y = client(rhino_view.MainViewport, point)
    rect = rhino_view.ScreenRectangle
    return rect.Left + x, rect.Top + y


def corners(box):
    a, b = box.Min, box.Max
    return [Rhino.Geometry.Point3d(x, y, z) for x in (a.X, b.X) for y in (a.Y, b.Y) for z in (a.Z, b.Z)]


def box_from(values):
    return Rhino.Geometry.BoundingBox(Rhino.Geometry.Point3d(values[0], values[1], values[2]),
                                      Rhino.Geometry.Point3d(values[3], values[4], values[5]))


def box_values(box):
    return [box.Min.X, box.Min.Y, box.Min.Z, box.Max.X, box.Max.Y, box.Max.Z]


def union(boxes):
    """The box around a list of boxes; invalid when the list is empty."""
    result = None
    for box in boxes:
        if not box.IsValid:
            continue
        result = box if result is None else Rhino.Geometry.BoundingBox.Union(result, box)
    return result if result is not None else Rhino.Geometry.BoundingBox.Empty


def named_views(doc):
    return [str(doc.NamedViews[i].Name) for i in range(doc.NamedViews.Count)]


def camera_state(viewport):
    """
    Frustum sides, camera location and target, for comparing two moments. A
    perspective frustum's sides are measured at its near plane, which Rhino's
    display moves on every redraw, so they are compared as angles: each side
    divided by the near distance.
    """
    f = frustum(viewport)
    sides = f[:4]
    if viewport.IsPerspectiveProjection:
        sides = [side / f[4] for side in sides]
    loc = viewport.CameraLocation
    tgt = viewport.CameraTarget
    return sides + [loc.X, loc.Y, loc.Z, tgt.X, tgt.Y, tgt.Z]


# ---- Saved options ----------------------------------------------------------


def _try_get(method, key, placeholder):
    try:
        result = method(key)
    except TypeError:
        result = method(key, placeholder)
    return result[1] if result[0] else None


class SettingsGuard(object):
    """
    Keeps the plugin's saved options as they were before the run. Reads only
    through Keys and the TryGet methods, because a PersistentSettings Get call
    with a default value stores that default.
    """

    def __init__(self, settings):
        self.settings = settings
        self.saved = self.current()

    def current(self):
        return {
            "Source": _try_get(self.settings.TryGetString, "Source", ""),
            "Padding": _try_get(self.settings.TryGetDouble, "Padding", 0.0),
            "SaveViews": _try_get(self.settings.TryGetBool, "SaveViews", False),
            "Layout": _try_get(self.settings.TryGetBool, "Layout", False),
            "keys": sorted([str(key) for key in self.settings.Keys]),
        }

    def clear(self):
        for key in SETTING_KEYS:
            self.settings.DeleteItem(key)

    def restore(self):
        self.clear()
        if self.saved["Source"] is not None:
            self.settings.SetString("Source", self.saved["Source"])
        if self.saved["Padding"] is not None:
            self.settings.SetDouble("Padding", self.saved["Padding"])
        if self.saved["SaveViews"] is not None:
            self.settings.SetBool("SaveViews", self.saved["SaveViews"])
        if self.saved["Layout"] is not None:
            self.settings.SetBool("Layout", self.saved["Layout"])


# ---- Captures ---------------------------------------------------------------


def capture_views(context, prefix):
    """Renders each standard view at its own size with grid and axes: one PNG per view."""
    import System.Drawing.Imaging
    saved = []
    for name in VIEW_NAMES:
        rhino_view = view(context.doc, name)
        if rhino_view is None:
            context.log("        no %s view to capture" % name)
            continue
        bitmap = rhino_view.CaptureToBitmap(True, True, True)
        if bitmap is None:
            context.log("        capturing the %s view returned nothing" % name)
            continue
        file_name = "%s-%s.png" % (prefix, name)
        bitmap.Save(context.artifact_path(file_name), System.Drawing.Imaging.ImageFormat.Png)
        bitmap.Dispose()
        context.artifact(file_name, "%s view, %s" % (name, prefix))
        saved.append(file_name)
    return saved


def capture_screen(context, prefix):
    """
    Copies the four viewports from the screen, borders and titles included.
    Best effort: it needs a visible Rhino window, so it logs and moves on
    when the copy fails.
    """
    try:
        import System.Drawing
        import System.Drawing.Imaging
        rects = [v.ScreenRectangle for v in context.doc.Views.GetStandardRhinoViews()]
        left = min([r.Left for r in rects])
        top = min([r.Top for r in rects])
        width = max([r.Right for r in rects]) - left
        height = max([r.Bottom for r in rects]) - top
        bitmap = System.Drawing.Bitmap(width, height)
        graphics = System.Drawing.Graphics.FromImage(bitmap)
        graphics.CopyFromScreen(left, top, 0, 0, System.Drawing.Size(width, height))
        graphics.Dispose()
        file_name = "%s-screen.png" % prefix
        bitmap.Save(context.artifact_path(file_name), System.Drawing.Imaging.ImageFormat.Png)
        bitmap.Dispose()
        context.artifact(file_name, "the four viewports copied from the screen, " + prefix)
        return file_name
    except Exception:
        context.log("        screen copy skipped: " + traceback.format_exc().strip().splitlines()[-1])
        return None
