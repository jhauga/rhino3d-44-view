using System;
using System.Collections.Generic;
using System.Linq;
using FourFourView.Core.Fit;
using Rhino;
using Rhino.Display;

namespace FourFourView.Core.Runtime
{
  /// <summary>The four standard model views 44View works on.</summary>
  public sealed class FourViews
  {
    public RhinoView Top { get; internal set; }
    public RhinoView Front { get; internal set; }
    public RhinoView Right { get; internal set; }
    public RhinoView Perspective { get; internal set; }
  }

  /// <summary>Restores and finds the four default viewports.</summary>
  public static class ViewLayout
  {
    // False rebuilds the layout from scratch, which also brings back a closed view and
    // restores a maximized one. True leaves a maximized view maximized.
    const bool UseMatchingViews = false;

    // Rows of the four-view layout can differ by a pixel or two where the splitter falls.
    const int RowSlackPixels = 8;

    /// <summary>
    /// Restores the four default viewports when <paramref name="layout"/> is true, then
    /// finds Top, Front, Right and Perspective. Returns null and names the first missing
    /// view when one cannot be found.
    /// </summary>
    public static FourViews EnsureFourViews(RhinoDoc doc, bool layout, out string missing)
    {
      if (layout)
      {
        doc.Views.FourViewLayout(UseMatchingViews);
        doc.Views.Redraw();
        RhinoApp.Wait();
      }

      var standard = doc.Views.GetStandardRhinoViews() ?? Array.Empty<RhinoView>();
      var views = new FourViews
      {
        Top = ByName(standard, "Top"),
        Front = ByName(standard, "Front"),
        Right = ByName(standard, "Right"),
        Perspective = ByName(standard, "Perspective"),
      };

      // A localized Rhino names the views in its own language. A fresh four-view layout
      // always puts Top at the upper left, Perspective at the upper right, Front at the
      // lower left and Right at the lower right, so position identifies them instead.
      if (layout && FirstMissing(views) != null)
        views = ByPosition(standard) ?? views;

      missing = FirstMissing(views);
      return missing == null ? views : null;
    }

    /// <summary>The pixel size of the three parallel viewports.</summary>
    public static Dictionary<OrthoView, PixelSize> PixelSizes(FourViews views) => new Dictionary<OrthoView, PixelSize>
    {
      [OrthoView.Top] = SizeOf(views.Top),
      [OrthoView.Front] = SizeOf(views.Front),
      [OrthoView.Right] = SizeOf(views.Right),
    };

    static PixelSize SizeOf(RhinoView view)
    {
      var size = view.MainViewport.Size;
      return new PixelSize(size.Width, size.Height);
    }

    static RhinoView ByName(IEnumerable<RhinoView> views, string name)
      => views.FirstOrDefault(view => string.Equals(view.MainViewport.Name, name, StringComparison.OrdinalIgnoreCase));

    static FourViews ByPosition(RhinoView[] standard)
    {
      if (standard.Length != 4)
        return null;

      var byRow = standard.OrderBy(view => view.ScreenRectangle.Top).ToArray();
      var upper = byRow.Take(2).OrderBy(view => view.ScreenRectangle.Left).ToArray();
      var lower = byRow.Skip(2).OrderBy(view => view.ScreenRectangle.Left).ToArray();

      bool isGrid = Math.Abs(upper[0].ScreenRectangle.Top - upper[1].ScreenRectangle.Top) <= RowSlackPixels
        && Math.Abs(lower[0].ScreenRectangle.Top - lower[1].ScreenRectangle.Top) <= RowSlackPixels
        && lower[0].ScreenRectangle.Top > upper[0].ScreenRectangle.Top + RowSlackPixels;
      if (!isGrid || !upper[1].MainViewport.IsPerspectiveProjection)
        return null;

      return new FourViews { Top = upper[0], Perspective = upper[1], Front = lower[0], Right = lower[1] };
    }

    static string FirstMissing(FourViews views)
    {
      if (views.Top == null)
        return "Top";
      if (views.Front == null)
        return "Front";
      if (views.Right == null)
        return "Right";
      if (views.Perspective == null)
        return "Perspective";
      return null;
    }
  }
}
