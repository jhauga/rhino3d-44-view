using System;
using FourFourView.Core.Fit;
using Xunit;

namespace FourFourView.Fit.Tests
{
  /// <summary>Which two world axes each parallel view puts across and up the screen.</summary>
  public class ProjectedTests
  {
    static readonly Extents3 Box = new Extents3(1, 2, 3);

    [Theory]
    [InlineData(OrthoView.Top, 1.0, 2.0)]
    [InlineData(OrthoView.Front, 1.0, 3.0)]
    [InlineData(OrthoView.Right, 2.0, 3.0)]
    public void EachViewShowsTwoWorldAxes(OrthoView view, double across, double up)
    {
      var (width, height) = FitPlanner.Projected(view, Box);

      Assert.Equal(across, width);
      Assert.Equal(up, height);
    }

    [Fact]
    public void UnknownViewIsRejected()
    {
      Assert.Throws<ArgumentOutOfRangeException>(() => FitPlanner.Projected((OrthoView)7, Box));
    }
  }
}
