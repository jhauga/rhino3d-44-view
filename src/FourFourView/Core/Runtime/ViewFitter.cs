using System;
using System.Collections.Generic;
using System.Diagnostics;
using FourFourView.Core.Fit;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace FourFourView.Core.Runtime
{
  /// <summary>Points the four cameras at the model with the frames a fit plan gives.</summary>
  /// <remarks>
  /// Each camera is built on a <see cref="ViewportInfo"/> and applied with SetViewProjection,
  /// then its target is moved onto the model so rotating and zooming pivot there. Rhino's
  /// SetProjection and zoom calls would each add another entry to the view's history, so
  /// they only run on a scratch viewport, which supplies the standard orientation, lens and
  /// construction plane of each view.
  /// </remarks>
  public static class ViewFitter
  {
    /// <summary>
    /// Fits Top, Front and Right at the plan's shared scale, centered on the box, and fits
    /// Perspective in its default direction so the box's bounding sphere fills the lens.
    /// </summary>
    public static void Apply(FourViews views, BoundingBox box, FitPlan plan, ICollection<string> warnings)
    {
      FitParallel(views.Top, DefinedViewportProjection.Top, "Top", box, plan.FrameFor(OrthoView.Top), warnings);
      FitParallel(views.Front, DefinedViewportProjection.Front, "Front", box, plan.FrameFor(OrthoView.Front), warnings);
      FitParallel(views.Right, DefinedViewportProjection.Right, "Right", box, plan.FrameFor(OrthoView.Right), warnings);
      FitPerspective(views.Perspective, box, warnings);
    }

    static void FitParallel(RhinoView view, DefinedViewportProjection projection, string name, BoundingBox box, ViewFrame frame, ICollection<string> warnings)
    {
      var viewport = view.MainViewport;
      var info = StandardCamera(viewport, projection, name, out var plane);
      var center = box.Center;
      double radius = box.Diagonal.Length / 2.0;

      // A parallel camera's distance does not change the picture; it only has to sit
      // outside the model with the model between the clipping planes.
      var direction = info.CameraDirection;
      direction.Unitize();
      double distance = 3.0 * radius;
      bool applied = info.SetCameraLocation(center - direction * distance)
        && info.SetFrustum(-frame.Width / 2.0, frame.Width / 2.0, -frame.Height / 2.0, frame.Height / 2.0,
                           distance - 2.0 * radius, distance + 2.0 * radius);
      if (!applied || !viewport.SetViewProjection(info, true))
      {
        warnings.Add("the " + name + " view could not be fitted");
        return;
      }

      // SetViewProjection keeps the old distance from camera to target, so the target has
      // to be put back on the model. The center is on the camera line, so the aim is kept.
      viewport.SetCameraTarget(center, false);
      Finish(viewport, name, plane);
      CheckFit(viewport, center, frame);
    }

    static void FitPerspective(RhinoView view, BoundingBox box, ICollection<string> warnings)
    {
      var viewport = view.MainViewport;
      var info = StandardCamera(viewport, DefinedViewportProjection.Perspective, "Perspective", out var plane);
      var center = box.Center;
      double border = Math.Max(1.0, Rhino.ApplicationSettings.ViewSettings.ZoomExtentsPerspectiveViewBorder);
      double radius = box.Diagonal.Length / 2.0 * border;

      // Back the camera away along its default direction until the box's bounding sphere
      // fits the narrower of the lens's two half angles.
      info.GetFrustum(out double left, out double right, out double bottom, out double top, out double near, out _);
      double halfAngle = Math.Min(Math.Atan(Math.Min(-left, right) / near), Math.Atan(Math.Min(-bottom, top) / near));
      if (!(halfAngle > 0.0 && halfAngle < Math.PI / 2.0))
      {
        warnings.Add("the Perspective view could not be fitted");
        return;
      }

      var direction = info.CameraDirection;
      direction.Unitize();
      double distance = radius / Math.Sin(halfAngle);
      bool applied = info.SetCameraLocation(center - direction * distance);
      info.SetFrustumNearFar(box);
      if (!applied || !viewport.SetViewProjection(info, true))
      {
        warnings.Add("the Perspective view could not be fitted");
        return;
      }

      viewport.SetCameraTarget(center, false);
      Finish(viewport, "Perspective", plane);
    }

    // The camera direction, up vector, lens and construction plane Rhino uses for a standard
    // view, read from a scratch viewport the same size as the real one.
    static ViewportInfo StandardCamera(RhinoViewport viewport, DefinedViewportProjection projection, string name, out Plane plane)
    {
      using (var scratch = new RhinoViewport())
      {
        scratch.Size = viewport.Size;
        scratch.SetProjection(projection, name, true);
        plane = scratch.GetConstructionPlane().Plane;
        return new ViewportInfo(scratch);
      }
    }

    // Names the view and sets its standard construction plane, keeping the grid settings.
    // The plane goes on the construction plane history, so UndoCPlane brings back the old one.
    static void Finish(RhinoViewport viewport, string name, Plane plane)
    {
      viewport.Name = name;
      var cplane = viewport.GetConstructionPlane();
      if (SamePlane(cplane.Plane, plane))
        return;

      cplane.Plane = plane;
      viewport.PushConstructionPlane(cplane);
    }

    static bool SamePlane(Plane a, Plane b)
    {
      const double tolerance = 1e-9;
      return a.Origin.DistanceTo(b.Origin) <= tolerance
        && (a.XAxis - b.XAxis).Length <= tolerance
        && (a.YAxis - b.YAxis).Length <= tolerance;
    }

    // Debug builds report a view whose scale or center drifted from the plan.
    [Conditional("DEBUG")]
    static void CheckFit(RhinoViewport viewport, Point3d center, ViewFrame frame)
    {
      var size = viewport.Size;
      if (size.Width <= 0 || size.Height <= 0)
        return;

      viewport.GetFrustum(out double left, out double right, out _, out _, out _, out _);
      double scale = (right - left) / size.Width;
      double wanted = frame.Width / size.Width;
      if (Math.Abs(scale - wanted) > 1e-9 * wanted)
        RhinoApp.WriteLine("44View debug: {0} scale is {1}, expected {2}", viewport.Name, scale, wanted);

      var pixel = viewport.WorldToClient(center);
      double offX = pixel.X - size.Width / 2.0;
      double offY = pixel.Y - size.Height / 2.0;
      if (Math.Abs(offX) > 0.5 || Math.Abs(offY) > 0.5)
        RhinoApp.WriteLine("44View debug: {0} is off center by {1}, {2} pixels", viewport.Name, offX, offY);
    }
  }
}
