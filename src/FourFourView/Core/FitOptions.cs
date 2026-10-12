using System;
using Rhino;

namespace FourFourView.Core
{
  /// <summary>Which objects 44View measures.</summary>
  public enum FitSource
  {
    /// <summary>The selected objects when there are any, otherwise every visible object.</summary>
    Auto,

    /// <summary>Every visible object in model space.</summary>
    Visible,

    /// <summary>Only the selected objects.</summary>
    Selected,
  }

  /// <summary>The choices one 44View run is made with.</summary>
  public sealed class FitOptions
  {
    /// <summary>Smallest padding accepted, in percent.</summary>
    public const double MinPadding = 0.0;

    /// <summary>Largest padding accepted, in percent.</summary>
    public const double MaxPadding = 100.0;

    /// <summary>Padding used when Rhino's own zoom border cannot be read, in percent.</summary>
    public const double FallbackPadding = 10.0;

    const string SourceKey = "Source";
    const string PaddingKey = "Padding";
    const string SaveViewsKey = "SaveViews";
    const string LayoutKey = "Layout";

    double padding = FallbackPadding;

    /// <summary>Which objects are measured.</summary>
    public FitSource Source { get; set; } = FitSource.Auto;

    /// <summary>
    /// Space left around the model in the view that needs the most room, as a percent of
    /// the model's size there. Values outside 0 to 100 are clamped.
    /// </summary>
    public double Padding
    {
      get => padding;
      set => padding = double.IsNaN(value) ? FallbackPadding : Math.Min(MaxPadding, Math.Max(MinPadding, value));
    }

    /// <summary>Whether the four cameras are saved as the named views Perspective, Top, Front and Right.</summary>
    public bool SaveViews { get; set; } = true;

    /// <summary>Whether the four default viewports are restored before fitting.</summary>
    public bool Layout { get; set; } = true;

    /// <summary>The border factor the fit planner takes: 1.0 plus the padding as a fraction.</summary>
    public double Border => 1.0 + Padding / 100.0;

    /// <summary>
    /// The padding Rhino's own Zoom Extents leaves in parallel views, so a plain 44View
    /// frames the model the way Zoom Extents does.
    /// </summary>
    public static double DefaultPadding()
    {
      double border = Rhino.ApplicationSettings.ViewSettings.ZoomExtentsParallelViewBorder;
      if (double.IsFinite(border) && border >= 1.0 && border <= 2.0)
        return Math.Round((border - 1.0) * 100.0, 1);
      return FallbackPadding;
    }

    /// <summary>
    /// The options saved by the last -44View, or the defaults when none were saved. A saved
    /// value that cannot be read, or a padding outside 0 to 100, counts as not saved.
    /// </summary>
    /// <remarks>
    /// Reads with the TryGet methods: PersistentSettings.GetDouble with a default value
    /// stores that default, which would freeze the padding at whatever Rhino's zoom border
    /// was the first time the command ran.
    /// </remarks>
    public static FitOptions Load(PersistentSettings settings)
    {
      var options = new FitOptions { Padding = DefaultPadding() };
      if (settings == null)
        return options;

      if (settings.TryGetString(SourceKey, out string sourceText) && TryParseSource(sourceText, out var source))
        options.Source = source;
      if (settings.TryGetDouble(PaddingKey, out double savedPadding) && savedPadding >= MinPadding && savedPadding <= MaxPadding)
        options.Padding = savedPadding;
      if (settings.TryGetBool(SaveViewsKey, out bool saveViews))
        options.SaveViews = saveViews;
      if (settings.TryGetBool(LayoutKey, out bool layout))
        options.Layout = layout;
      return options;
    }

    /// <summary>Saves these options as the ones a plain 44View uses from now on.</summary>
    public void Save(PersistentSettings settings)
    {
      if (settings == null)
        return;

      settings.SetString(SourceKey, Source.ToString());
      settings.SetDouble(PaddingKey, Padding);
      settings.SetBool(SaveViewsKey, SaveViews);
      settings.SetBool(LayoutKey, Layout);
    }

    /// <summary>Reads Auto, Visible or Selected, ignoring case.</summary>
    public static bool TryParseSource(string text, out FitSource source)
    {
      source = FitSource.Auto;
      if (string.IsNullOrWhiteSpace(text))
        return false;

      foreach (FitSource value in Enum.GetValues(typeof(FitSource)))
      {
        if (string.Equals(value.ToString(), text.Trim(), StringComparison.OrdinalIgnoreCase))
        {
          source = value;
          return true;
        }
      }
      return false;
    }
  }
}
