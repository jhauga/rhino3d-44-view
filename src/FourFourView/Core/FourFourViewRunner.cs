using System;
using System.Globalization;
using System.Text;
using FourFourView.Core.Fit;
using FourFourView.Core.Runtime;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;

namespace FourFourView.Core
{
  /// <summary>One 44View run, shared by the command and the scripting object.</summary>
  public static class FourFourViewRunner
  {
    // Size, in model units, of the cube fitted around objects that have no size.
    const double MinimumExtent = 1.0;

    /// <summary>
    /// Measures the model, restores the four viewports, fits them at one shared scale and
    /// saves the named views. Nothing in the document changes when there is nothing to fit
    /// or a view is missing.
    /// </summary>
    public static FitReport Run(RhinoDoc doc, FitOptions options)
    {
      var report = new FitReport { Padding = options.Padding, Units = UnitAbbreviation(doc) };

      var measured = ModelExtents.Measure(doc, options.Source);
      report.Source = measured.Source;
      report.ObjectCount = measured.Count;
      report.Box = measured.Box;
      if (measured.IsEmpty)
      {
        report.Result = Result.Nothing;
        report.Message = measured.Source == FitSource.Selected
          ? "44View: nothing to fit; no objects are selected."
          : "44View: nothing to fit; the model has no visible objects.";
        return report;
      }

      // Restoring the layout resets every view's display mode, so record them first.
      var displayModes = ViewDisplayModes.Capture(doc);
      var views = ViewLayout.EnsureFourViews(doc, options.Layout, out string missing);
      report.LayoutApplied = options.Layout;
      if (views == null)
      {
        report.Result = Result.Failure;
        report.Message = "44View: the " + missing + " view is missing. Run -44View with Layout=Yes to restore the four views.";
        return report;
      }
      displayModes.Apply(views);

      var box = measured.Box;
      var extents = new Extents3(box.Max.X - box.Min.X, box.Max.Y - box.Min.Y, box.Max.Z - box.Min.Z);
      try
      {
        report.Plan = FitPlanner.Plan(extents, ViewLayout.PixelSizes(views), options.Border, MinimumExtent);
      }
      catch (ArgumentException ex)
      {
        report.Result = Result.Failure;
        report.Message = "44View: the views could not be fitted. " + ex.Message;
        return report;
      }

      ViewFitter.Apply(views, ZoomBox(box, doc.ModelAbsoluteTolerance), report.Plan, report.Warnings);

      if (options.SaveViews)
        report.NamedViewsSaved = NamedViewSaver.Save(doc, views, report.Warnings);

      if (doc.Views.ActiveView is RhinoPageView)
        doc.Views.ActiveView = views.Perspective;
      doc.Views.Redraw();

      report.Fitted = true;
      report.Result = Result.Success;
      report.Message = Summary(report, options);
      return report;
    }

    // The box Rhino zooms to: any side thinner than the tolerance is widened to the minimum
    // extent, so a flat part, a line or a point still gets a sensible camera depth.
    static BoundingBox ZoomBox(BoundingBox box, double tolerance)
    {
      var min = box.Min;
      var max = box.Max;
      double half = MinimumExtent / 2.0;
      if (max.X - min.X < tolerance) { double c = (min.X + max.X) / 2.0; min.X = c - half; max.X = c + half; }
      if (max.Y - min.Y < tolerance) { double c = (min.Y + max.Y) / 2.0; min.Y = c - half; max.Y = c + half; }
      if (max.Z - min.Z < tolerance) { double c = (min.Z + max.Z) / 2.0; min.Z = c - half; max.Z = c + half; }
      return new BoundingBox(min, max);
    }

    static string Summary(FitReport report, FitOptions options)
    {
      var plan = report.Plan;
      var text = new StringBuilder("44View: fitted ");
      text.Append(report.ObjectCount.ToString(CultureInfo.InvariantCulture));
      text.Append(report.Source == FitSource.Selected ? " selected object" : " visible object");
      if (report.ObjectCount != 1)
        text.Append('s');
      text.Append(" at ").Append(Number(plan.UnitsPerPixel)).Append(' ').Append(report.Units);
      text.Append(" per pixel, limited by ").Append(plan.LimitingView);
      if (plan.UsedMinimumExtent)
        text.Append(", around a ").Append(Number(MinimumExtent)).Append(' ').Append(report.Units).Append(" cube because the objects have no size");

      if (!options.SaveViews)
        text.Append("; named views not saved");
      else if (report.NamedViewsSaved)
        text.Append("; named views saved");
      else
        text.Append("; not every named view could be saved");

      foreach (var warning in report.Warnings)
        text.Append("; ").Append(warning);
      return text.Append('.').ToString();
    }

    static string Number(double value)
      => value >= 100.0
        ? value.ToString("0.#", CultureInfo.InvariantCulture)
        : value.ToString("G4", CultureInfo.InvariantCulture);

    static string UnitAbbreviation(RhinoDoc doc)
    {
      switch (doc.ModelUnitSystem)
      {
        case UnitSystem.Microns: return "um";
        case UnitSystem.Millimeters: return "mm";
        case UnitSystem.Centimeters: return "cm";
        case UnitSystem.Meters: return "m";
        case UnitSystem.Kilometers: return "km";
        case UnitSystem.Inches: return "in";
        case UnitSystem.Feet: return "ft";
        case UnitSystem.Yards: return "yd";
        case UnitSystem.Miles: return "mi";
        case UnitSystem.None: return "units";
        default: return doc.ModelUnitSystem.ToString().ToLowerInvariant();
      }
    }
  }
}
