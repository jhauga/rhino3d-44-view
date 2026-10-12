namespace FourFourView.Core.Fit
{
  /// <summary>The three parallel views that share one scale.</summary>
  public enum OrthoView
  {
    /// <summary>Looks down the world Z axis. Across the screen is world X, up is world Y.</summary>
    Top,

    /// <summary>Looks along the world Y axis. Across the screen is world X, up is world Z.</summary>
    Front,

    /// <summary>Looks against the world X axis. Across the screen is world Y, up is world Z.</summary>
    Right,
  }
}
