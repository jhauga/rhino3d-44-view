namespace FourFourView.Core.Fit
{
  /// <summary>
  /// What one parallel view shows after the fit: a frame of <see cref="Width"/> by
  /// <see cref="Height"/> model units, centered on the model.
  /// </summary>
  /// <param name="View">The view the frame belongs to.</param>
  /// <param name="Width">Frame width in model units: the shared scale times the viewport width in pixels.</param>
  /// <param name="Height">Frame height in model units: the shared scale times the viewport height in pixels.</param>
  /// <param name="RequiredUnitsPerPixel">The scale this view alone needs to fit its side of the model with the border.</param>
  /// <param name="Limiting">True when this view's requirement is the shared scale.</param>
  public readonly record struct ViewFrame(
    OrthoView View, double Width, double Height, double RequiredUnitsPerPixel, bool Limiting);
}
