# -*- coding: utf-8 -*-
"""
Entry point Rhino runs for the in-Rhino test suites.

Launched by tests/run-tests.ps1 as:

    Rhino.exe "<copy of a template>.3dm" /nosplash /netcore /runscript="-_RunPythonScript <this file>"

It loads the plugin, sets the plugin's saved options aside, maximizes Rhino's
window, runs the requested suites, writes tests/artifacts/rhino-report.json and
a readable log beside it, puts the window and the saved options back, and
closes Rhino. Rhino's exit code says nothing about the results, so the launcher
reads the report.

Environment variables:
    FFV_TESTS_DIR   the tests folder (falls back to this file's location)
    FFV_RHP         the .rhp to load when the plugin is not registered yet
    FFV_SUITES      comma separated: smoke, regression (default both)
"""

import json
import os
import sys
import traceback

import Rhino
import System
import scriptcontext


def tests_dir():
    from_env = os.environ.get("FFV_TESTS_DIR")
    if from_env and os.path.isdir(from_env):
        return from_env
    return os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


TESTS = tests_dir()
ARTIFACTS = os.path.join(TESTS, "artifacts")
REPORT_PATH = os.path.join(ARTIFACTS, "rhino-report.json")
LOG_PATH = os.path.join(ARTIFACTS, "rhino-tests.log")

sys.path.insert(0, os.path.join(TESTS, "rhino"))


def fresh(path):
    handle = open(path, "w")
    handle.close()


def write_report(report):
    handle = open(REPORT_PATH, "w")
    try:
        handle.write(json.dumps(report, indent=1, sort_keys=True))
    finally:
        handle.close()


def append_log(text):
    try:
        handle = open(LOG_PATH, "a")
        handle.write(text + "\n")
        handle.close()
    except Exception:
        pass


def load_plugin(plugin_id):
    """
    Returns the plugin and its scripting object. The plugin loads on demand, so
    asking for the scripting object is what loads it; the .rhp named by
    FFV_RHP is the fallback when Rhino has not registered the plugin yet.
    """
    guid = System.Guid(plugin_id)
    plugin_object = Rhino.RhinoApp.GetPlugInObject("FourFourView")
    if Rhino.PlugIns.PlugIn.Find(guid) is None:
        rhp = os.environ.get("FFV_RHP")
        if rhp and os.path.exists(rhp):
            Rhino.PlugIns.PlugIn.LoadPlugIn(rhp)
        plugin_object = Rhino.RhinoApp.GetPlugInObject("FourFourView")

    plugin = Rhino.PlugIns.PlugIn.Find(guid)
    if plugin is None:
        raise RuntimeError("the FourFourView plugin could not be loaded")
    if plugin_object is None:
        raise RuntimeError("the FourFourView plugin loaded but exposed no scripting object")
    return plugin, plugin_object


def selected_suites():
    requested = os.environ.get("FFV_SUITES", "smoke,regression")
    return [name.strip() for name in requested.split(",") if name.strip()]


def main():
    if not os.path.isdir(ARTIFACTS):
        os.makedirs(ARTIFACTS)
    fresh(LOG_PATH)
    if os.path.exists(REPORT_PATH):
        os.remove(REPORT_PATH)

    report = {"ok": False, "counts": {"passed": 0, "failed": 0, "error": 0},
              "total": 0, "results": [], "artifacts": []}
    guard = None
    window = None

    try:
        import ffv_harness as h

        doc = scriptcontext.doc
        plugin, plugin_object = load_plugin(h.PLUGIN_ID)
        guard = h.SettingsGuard(plugin.Settings)
        guard.clear()

        context = h.Context(doc, plugin_object, TESTS, LOG_PATH)
        context.state["guard"] = guard
        window = h.WindowGuard(context)
        window.maximize()
        context.log("44 View in-Rhino tests")
        context.log("rhino          : %s" % Rhino.RhinoApp.Version)
        context.log("plugin version : %s" % plugin_object.Version)
        context.log("plugin path    : %s" % Rhino.PlugIns.PlugIn.PathFromId(System.Guid(h.PLUGIN_ID)))
        context.log("document units : %s" % doc.ModelUnitSystem)
        context.log("suites         : %s" % ", ".join(selected_suites()))

        runner = h.Runner(context)
        import ffv_smoke
        import ffv_regression
        modules = {"smoke": ffv_smoke.SUITE, "regression": ffv_regression.SUITE}
        for name in selected_suites():
            if name not in modules:
                context.log("unknown suite %r, skipping" % name)
                continue
            runner.run(modules[name])

        counts = runner.counts()
        context.log("")
        context.log("%d passed, %d failed, %d errored out of %d"
                    % (counts["passed"], counts["failed"], counts["error"], len(runner.results)))
        report = runner.report()

    except Exception:
        report["results"].append({
            "suite": "harness", "case": "start up",
            "status": "error", "message": traceback.format_exc()})
        report["counts"]["error"] = report["counts"]["error"] + 1
        report["total"] = len(report["results"])
        report["ok"] = False
        append_log(traceback.format_exc())

    finally:
        try:
            if window is not None:
                window.restore()
        except Exception:
            append_log("could not put the window back:\n" + traceback.format_exc())
        try:
            if guard is not None:
                guard.restore()
        except Exception:
            append_log("could not put the saved options back:\n" + traceback.format_exc())
        write_report(report)
        try:
            scriptcontext.doc.Modified = False
        except Exception:
            pass
        Rhino.RhinoApp.Exit(False)


main()
