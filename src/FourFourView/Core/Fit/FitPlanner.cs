using System;
using System.Collections.Generic;

namespace FourFourView.Core.Fit
{
  /// <summary>
  /// Works out one scale for the Top, Front and Right views so the model fits all three with
  /// a border around it. Sharing the scale is what lines the grids up: views with the same
  /// pixel width show the same span of world X, and views with the same pixel height show
  /// the same span of world Z.
  /// </summary>
  public static class FitPlanner
  {
    static readonly OrthoView[] Views = { OrthoView.Top, OrthoView.Front, OrthoView.Right };

    // A view counts as limiting when its requirement is within this relative distance of
    // the shared scale, so a tie on paper is still a tie after rounding.
    const double TieTolerance = 1e-12;

    /// <summary>
    /// The model's size as one view sees it: the extent across the screen and the extent up
    /// the screen, in model units.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The view is not Top, Front or Right.</exception>
    public static (double Width, double Height) Projected(OrthoView view, Extents3 extents)
    {
      switch (view)
      {
        case OrthoView.Top:
          return (extents.X, extents.Y);
        case OrthoView.Front:
          return (extents.X, extents.Z);
        case OrthoView.Right:
          return (extents.Y, extents.Z);
        default:
          throw new ArgumentOutOfRangeException(nameof(view), view, "Only Top, Front and Right are parallel views.");
      }
    }

    /// <summary>
    /// Plans the fit. Each view needs the scale, in model units per pixel, at which its side
    /// of the model fits its viewport with the border. The shared scale is the largest of the
    /// three, so every view fits and the one that needs the most room is filled. Each frame
    /// is the shared scale times that viewport's pixel size, so a frame always has its
    /// viewport's aspect ratio.
    /// </summary>
    /// <param name="extents">The model's bounding box size along world X, Y and Z.</param>
    /// <param name="viewports">The pixel size of the Top, Front and Right viewports.</param>
    /// <param name="border">How much larger than the model a frame is: 1.0 for no margin, 1.1 for ten percent.</param>
    /// <param name="minimumExtent">The size of the cube fitted instead when the extents give no scale, as for a single point.</param>
    /// <exception cref="ArgumentNullException"><paramref name="viewports"/> is null.</exception>
    /// <exception cref="ArgumentException">A viewport size is missing.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// An extent is negative or not finite, a viewport is less than one pixel either way, the
    /// border is below 1.0 or not finite, the minimum extent is not a positive finite number,
    /// or the frames would not be finite positive numbers.
    /// </exception>
    public static FitPlan Plan(Extents3 extents, IReadOnlyDictionary<OrthoView, PixelSize> viewports, double border, double minimumExtent)
    {
      if (!IsExtent(extents.X) || !IsExtent(extents.Y) || !IsExtent(extents.Z))
        throw new ArgumentOutOfRangeException(nameof(extents), extents, "Extents must be finite and not negative.");
      if (viewports == null)
        throw new ArgumentNullException(nameof(viewports));
      if (!double.IsFinite(border) || border < 1.0)
        throw new ArgumentOutOfRangeException(nameof(border), border, "The border must be finite and at least 1.0.");
      if (!double.IsFinite(minimumExtent) || minimumExtent <= 0.0)
        throw new ArgumentOutOfRangeException(nameof(minimumExtent), minimumExtent, "The minimum extent must be finite and greater than zero.");

      var pixels = new PixelSize[Views.Length];
      for (int i = 0; i < Views.Length; i++)
      {
        if (!viewports.TryGetValue(Views[i], out var size))
          throw new ArgumentException("No pixel size was given for the " + Views[i] + " viewport.", nameof(viewports));
        if (size.Width <= 0 || size.Height <= 0)
          throw new ArgumentOutOfRangeException(nameof(viewports), size, "The " + Views[i] + " viewport must be at least one pixel wide and high.");
        pixels[i] = size;
      }

      bool usedMinimumExtent = false;
      double[] required = Requirements(extents, pixels, border);
      double scale = Largest(required);
      if (!(scale > 0.0))
      {
        usedMinimumExtent = true;
        required = Requirements(new Extents3(minimumExtent, minimumExtent, minimumExtent), pixels, border);
        scale = Largest(required);
        if (!(scale > 0.0))
          throw new ArgumentOutOfRangeException(nameof(minimumExtent), minimumExtent, "The minimum extent is too small to fit.");
      }

      var frames = new ViewFrame[Views.Length];
      int limiting = -1;
      for (int i = 0; i < Views.Length; i++)
      {
        bool isLimiting = required[i] >= scale * (1.0 - TieTolerance);
        if (isLimiting && limiting < 0)
          limiting = i;

        frames[i] = new ViewFrame(Views[i], scale * pixels[i].Width, scale * pixels[i].Height, required[i], isLimiting);
        if (!double.IsFinite(frames[i].Width) || !double.IsFinite(frames[i].Height))
          throw new ArgumentOutOfRangeException(nameof(extents), extents, "The extents are too large to fit with this border.");
      }

      return new FitPlan(scale, Views[limiting], frames, usedMinimumExtent);
    }

    static double[] Requirements(Extents3 extents, PixelSize[] pixels, double border)
    {
      var required = new double[Views.Length];
      for (int i = 0; i < Views.Length; i++)
      {
        var (width, height) = Projected(Views[i], extents);
        required[i] = Math.Max(width * border / pixels[i].Width, height * border / pixels[i].Height);
      }
      return required;
    }

    static double Largest(double[] values)
    {
      double largest = values[0];
      for (int i = 1; i < values.Length; i++)
        largest = Math.Max(largest, values[i]);
      return largest;
    }

    static bool IsExtent(double value) => double.IsFinite(value) && value >= 0.0;
  }
}
