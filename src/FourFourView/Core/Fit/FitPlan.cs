using System;
using System.Collections.Generic;

namespace FourFourView.Core.Fit
{
  /// <summary>
  /// The result of <see cref="FitPlanner.Plan"/>: one scale shared by the Top, Front and
  /// Right views, and the frame each view gets at that scale.
  /// </summary>
  public sealed class FitPlan
  {
    internal FitPlan(double unitsPerPixel, OrthoView limitingView, ViewFrame[] frames, bool usedMinimumExtent)
    {
      UnitsPerPixel = unitsPerPixel;
      LimitingView = limitingView;
      Frames = Array.AsReadOnly(frames);
      UsedMinimumExtent = usedMinimumExtent;
    }

    /// <summary>Model units per pixel in every parallel view: the largest requirement of the three.</summary>
    public double UnitsPerPixel { get; }

    /// <summary>The view whose requirement is the shared scale; on a tie, the first of Top, Front, Right.</summary>
    public OrthoView LimitingView { get; }

    /// <summary>One frame per view, in the order Top, Front, Right.</summary>
    public IReadOnlyList<ViewFrame> Frames { get; }

    /// <summary>
    /// True when the extents gave no scale, as for a single point, and a cube of the minimum
    /// extent was fitted instead.
    /// </summary>
    public bool UsedMinimumExtent { get; }

    /// <summary>The frame of one view.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The view is not Top, Front or Right.</exception>
    public ViewFrame FrameFor(OrthoView view)
    {
      foreach (var frame in Frames)
      {
        if (frame.View == view)
          return frame;
      }
      throw new ArgumentOutOfRangeException(nameof(view), view, "Only Top, Front and Right have frames.");
    }
  }
}
