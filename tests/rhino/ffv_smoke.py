# -*- coding: utf-8 -*-
"""
Smoke suite: the plugin is installed, wired up, and does the obvious thing.

These cases answer "is this build usable at all". They assert coarse facts
(the plugin loads, the command exists, a box gets fitted) so that a failure
here points at the plugin rather than at the fitting math. The suite leaves
the document empty for the regression suite.
"""

import os
import re

import Rhino
import System

from Rhino.Geometry import Box, Interval, Plane

import ffv_harness as h

SUITE = h.Suite("smoke", "Plugin loads, registers and fits a model.")


def _guid():
    return System.Guid(h.PLUGIN_ID)


@SUITE.case("plugin is loaded from the build output")
def plugin_loaded(context):
    plugin = Rhino.PlugIns.PlugIn.Find(_guid())
    h.not_none(plugin, "PlugIn.Find returned nothing for the FourFourView id")
    path = str(Rhino.PlugIns.PlugIn.PathFromId(_guid()))
    h.ok(path.lower().endswith("fourfourview.rhp"),
         "plugin path should end with FourFourView.rhp, got %s" % path)


@SUITE.case("44View is a registered command")
def command_registered(context):
    h.ok(Rhino.Commands.Command.IsCommand("44View"), "44View is not a registered command")


@SUITE.case("scripting object exposes Version, LastReport, Fit and FitWith")
def scripting_surface(context):
    for member in ("Version", "LastReport", "Fit", "FitWith"):
        h.ok(hasattr(context.ffv, member), "plugin object is missing %s" % member)


@SUITE.case("plugin version matches the source and the package manifest")
def version_matches(context):
    info = h.read_text(os.path.join(context.root, "src", "FourFourView", "Properties", "AssemblyInfo.cs"))
    match = re.search(r'AssemblyInformationalVersion\("([^"]+)"\)', info)
    h.not_none(match, "AssemblyInfo.cs has no AssemblyInformationalVersion")
    h.equal(str(context.ffv.Version), match.group(1), "plugin version and AssemblyInfo.cs disagree")

    manifest_path = os.path.join(context.root, "manifest.yml")
    if not os.path.exists(manifest_path):
        context.log("        manifest.yml does not exist yet; compared with AssemblyInfo.cs only")
        return
    found = re.search(r"^version:\s*(\S+)\s*$", h.read_text(manifest_path), re.MULTILINE)
    h.not_none(found, "manifest.yml has no version line")
    h.equal(str(context.ffv.Version), found.group(1), "plugin version and manifest.yml disagree")


@SUITE.case("44View fits a box and prints its summary line")
def fits_a_box(context):
    doc = context.doc
    box_id = doc.Objects.AddBrep(Box(Plane.WorldXY, Interval(0, 10), Interval(0, 5), Interval(0, 2)).ToBrep())
    try:
        outcome = context.run("_44View")
        h.equal(outcome["result"], "Success", "44View command result")
        h.ok("44View: fitted 1 visible object" in outcome["printed"],
             "summary line missing from the command history: %r" % outcome["printed"][-300:])
        report = h.not_none(context.report(), "LastReport after 44View")
        h.ok(report["fitted"], "LastReport says the views were not fitted: %s" % report["message"])
        h.bbox_equal(report["box"], [0, 0, 0, 10, 5, 2], "measured box")
    finally:
        doc.Objects.Delete(box_id, True)
        context.pump()


@SUITE.case("44View on an empty model reports that there is nothing to fit")
def nothing_to_fit(context):
    outcome = context.run("_44View")
    h.equal(outcome["result"], "Nothing", "44View command result on an empty model")
    report = h.not_none(context.report(), "LastReport")
    h.ok(not report["fitted"], "LastReport claims a fit on an empty model")
    h.ok("nothing to fit" in report["message"],
         "message should say there is nothing to fit, got %r" % report["message"])
