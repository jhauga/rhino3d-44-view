using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Display;

namespace FourFourView.Core.Runtime
{
  /// <summary>
  /// The display mode of each of the four views, such as Wireframe, Shaded or Rendered,
  /// recorded before the layout is restored and set again afterwards. Restoring the
  /// four-view layout returns every view to its default display mode.
  /// </summary>
  public sealed class ViewDisplayModes
  {
    readonly Dictionary<string, Guid> modes = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Records the display mode of whichever of the four views exist now.</summary>
    public static ViewDisplayModes Capture(RhinoDoc doc)
    {
      var captured = new ViewDisplayModes();
      var views = ViewLayout.FindFourViews(doc, true);
      captured.Remember("Top", views.Top);
      captured.Remember("Front", views.Front);
      captured.Remember("Right", views.Right);
      captured.Remember("Perspective", views.Perspective);
      return captured;
    }

    /// <summary>
    /// Gives each view the display mode recorded for it. A view that did not exist when
    /// the modes were recorded keeps the mode it has.
    /// </summary>
    public void Apply(FourViews views)
    {
      Restore("Top", views.Top);
      Restore("Front", views.Front);
      Restore("Right", views.Right);
      Restore("Perspective", views.Perspective);
    }

    void Remember(string name, RhinoView view)
    {
      var mode = view?.MainViewport.DisplayMode;
      if (mode != null)
        modes[name] = mode.Id;
    }

    void Restore(string name, RhinoView view)
    {
      if (view == null || !modes.TryGetValue(name, out var id))
        return;

      var viewport = view.MainViewport;
      if (viewport.DisplayMode?.Id == id)
        return;

      var mode = DisplayModeDescription.GetDisplayMode(id);
      if (mode != null)
        viewport.DisplayMode = mode;
    }
  }
}
