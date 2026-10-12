using System;
using FourFourView.Core.Fit;
using Xunit;

namespace FourFourView.Fit.Tests
{
  /// <summary>Inputs the planner refuses, so a bad viewport or setting never reaches Rhino.</summary>
  public class ArgumentTests
  {
    static readonly Extents3 Plate = new Extents3(60, 30, 14);

    [Theory]
    [InlineData(0, 400)]
    [InlineData(800, 0)]
    [InlineData(-1, 400)]
    [InlineData(800, -400)]
    public void ViewportUnderOnePixelIsRejected(int width, int height)
    {
      var layout = FitPlannerTests.Layout(800, 400, width, height, 800, 400);

      var error = Assert.Throws<ArgumentOutOfRangeException>(() => FitPlanner.Plan(Plate, layout, 1.1, 1.0));
      Assert.Equal("viewports", error.ParamName);
    }

    [Theory]
    [InlineData(0.99)]
    [InlineData(0.0)]
    [InlineData(-1.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void BorderBelowOneOrNotFiniteIsRejected(double border)
    {
      var error = Assert.Throws<ArgumentOutOfRangeException>(() => FitPlanner.Plan(Plate, FitPlannerTests.EqualLayout(), border, 1.0));
      Assert.Equal("border", error.ParamName);
    }

    [Theory]
    [InlineData(-1.0, 30.0, 14.0)]
    [InlineData(60.0, -0.001, 14.0)]
    [InlineData(60.0, 30.0, double.NaN)]
    [InlineData(double.PositiveInfinity, 30.0, 14.0)]
    public void NegativeOrNonFiniteExtentIsRejected(double x, double y, double z)
    {
      var error = Assert.Throws<ArgumentOutOfRangeException>(() => FitPlanner.Plan(new Extents3(x, y, z), FitPlannerTests.EqualLayout(), 1.1, 1.0));
      Assert.Equal("extents", error.ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void MinimumExtentMustBePositiveAndFinite(double minimum)
    {
      var error = Assert.Throws<ArgumentOutOfRangeException>(() => FitPlanner.Plan(Plate, FitPlannerTests.EqualLayout(), 1.1, minimum));
      Assert.Equal("minimumExtent", error.ParamName);
    }

    [Fact]
    public void MissingViewportIsNamed()
    {
      var layout = FitPlannerTests.EqualLayout();
      layout.Remove(OrthoView.Right);

      var error = Assert.Throws<ArgumentException>(() => FitPlanner.Plan(Plate, layout, 1.1, 1.0));
      Assert.Equal("viewports", error.ParamName);
      Assert.Contains("Right", error.Message);
    }

    [Fact]
    public void NullViewportsAreRejected()
    {
      var error = Assert.Throws<ArgumentNullException>(() => FitPlanner.Plan(Plate, null, 1.1, 1.0));
      Assert.Equal("viewports", error.ParamName);
    }

    [Fact]
    public void ExtentsTooLargeToFitAreRejected()
    {
      var error = Assert.Throws<ArgumentOutOfRangeException>(() => FitPlanner.Plan(new Extents3(double.MaxValue, 0, 0), FitPlannerTests.EqualLayout(), 2.0, 1.0));
      Assert.Equal("extents", error.ParamName);
    }

    [Fact]
    public void MinimumExtentTooSmallToFitIsRejected()
    {
      var error = Assert.Throws<ArgumentOutOfRangeException>(() => FitPlanner.Plan(new Extents3(0, 0, 0), FitPlannerTests.EqualLayout(), 1.0, double.Epsilon));
      Assert.Equal("minimumExtent", error.ParamName);
    }
  }
}
