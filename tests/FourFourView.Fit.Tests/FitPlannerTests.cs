using System;
using System.Collections.Generic;
using FourFourView.Core.Fit;
using Xunit;

namespace FourFourView.Fit.Tests
{
  /// <summary>
  /// The shared scale and each view's frame, checked against numbers worked out by hand.
  /// The plate is 60 x 30 x 14 model units; the default four-view layout gives every
  /// viewport 800 x 400 pixels.
  /// </summary>
  public class FitPlannerTests
  {
    static readonly Extents3 Plate = new Extents3(60, 30, 14);

    internal static Dictionary<OrthoView, PixelSize> Layout(int topWidth, int topHeight, int frontWidth, int frontHeight, int rightWidth, int rightHeight)
      => new Dictionary<OrthoView, PixelSize>
      {
        [OrthoView.Top] = new PixelSize(topWidth, topHeight),
        [OrthoView.Front] = new PixelSize(frontWidth, frontHeight),
        [OrthoView.Right] = new PixelSize(rightWidth, rightHeight),
      };

    internal static Dictionary<OrthoView, PixelSize> EqualLayout() => Layout(800, 400, 800, 400, 800, 400);

    static void Frame(FitPlan plan, OrthoView view, double width, double height, double required, bool limiting)
    {
      var frame = plan.FrameFor(view);
      Approx.Equal(width, frame.Width, view + " frame width");
      Approx.Equal(height, frame.Height, view + " frame height");
      Approx.Equal(required, frame.RequiredUnitsPerPixel, view + " required units per pixel");
      Assert.Equal(limiting, frame.Limiting);
    }

    [Fact]
    public void WorkedExampleSharesTheLoosestScale()
    {
      var plan = FitPlanner.Plan(Plate, EqualLayout(), 1.1, 1.0);

      Approx.Equal(0.0825, plan.UnitsPerPixel, "shared scale");
      Assert.Equal(OrthoView.Top, plan.LimitingView);
      Assert.False(plan.UsedMinimumExtent);
      Frame(plan, OrthoView.Top, 66, 33, 0.0825, true);
      Frame(plan, OrthoView.Front, 66, 33, 0.0825, true);
      Frame(plan, OrthoView.Right, 66, 33, 0.04125, false);
    }

    [Fact]
    public void FramesComeInTopFrontRightOrder()
    {
      var plan = FitPlanner.Plan(Plate, EqualLayout(), 1.1, 1.0);

      Assert.Collection(plan.Frames,
        frame => Assert.Equal(OrthoView.Top, frame.View),
        frame => Assert.Equal(OrthoView.Front, frame.View),
        frame => Assert.Equal(OrthoView.Right, frame.View));
    }

    [Fact]
    public void GridsLineUpWhereViewportsShareAnEdge()
    {
      // Splitters rarely divide the window evenly; here Right is a pixel narrower.
      var plan = FitPlanner.Plan(Plate, Layout(812, 377, 812, 377, 811, 377), 1.1, 1.0);

      Assert.Equal(plan.FrameFor(OrthoView.Top).Width, plan.FrameFor(OrthoView.Front).Width);
      Assert.Equal(plan.FrameFor(OrthoView.Front).Height, plan.FrameFor(OrthoView.Right).Height);
    }

    [Fact]
    public void UnequalViewportsKeepOneScale()
    {
      var plan = FitPlanner.Plan(Plate, Layout(800, 400, 800, 300, 600, 300), 1.1, 1.0);

      Approx.Equal(0.0825, plan.UnitsPerPixel, "shared scale");
      Assert.Equal(OrthoView.Top, plan.LimitingView);
      Frame(plan, OrthoView.Top, 66, 33, 0.0825, true);
      Frame(plan, OrthoView.Front, 66, 24.75, 0.0825, true);
      Frame(plan, OrthoView.Right, 49.5, 24.75, 0.055, false);
    }

    [Fact]
    public void SmallestViewportCanSetTheScale()
    {
      var plan = FitPlanner.Plan(new Extents3(10, 10, 50), Layout(800, 400, 800, 400, 400, 200), 1.1, 1.0);

      Approx.Equal(0.275, plan.UnitsPerPixel, "shared scale");
      Assert.Equal(OrthoView.Right, plan.LimitingView);
      Frame(plan, OrthoView.Top, 220, 110, 0.0275, false);
      Frame(plan, OrthoView.Front, 220, 110, 0.1375, false);
      Frame(plan, OrthoView.Right, 110, 55, 0.275, true);
    }

    [Fact]
    public void ZeroExtentLeavesTheOtherAxisToDecide()
    {
      var plan = FitPlanner.Plan(new Extents3(60, 30, 0), EqualLayout(), 1.1, 1.0);

      Approx.Equal(0.0825, plan.UnitsPerPixel, "shared scale");
      Assert.False(plan.UsedMinimumExtent);
      Frame(plan, OrthoView.Top, 66, 33, 0.0825, true);
      Frame(plan, OrthoView.Front, 66, 33, 0.0825, true);
      Frame(plan, OrthoView.Right, 66, 33, 0.04125, false);
    }

    [Fact]
    public void LineIsFittedByTheViewsThatSeeItsLength()
    {
      var plan = FitPlanner.Plan(new Extents3(60, 0, 0), EqualLayout(), 1.1, 1.0);

      Approx.Equal(0.0825, plan.UnitsPerPixel, "shared scale");
      Assert.False(plan.UsedMinimumExtent);
      Frame(plan, OrthoView.Top, 66, 33, 0.0825, true);
      Frame(plan, OrthoView.Front, 66, 33, 0.0825, true);
      Frame(plan, OrthoView.Right, 66, 33, 0.0, false);
    }

    [Fact]
    public void SinglePointFitsACubeOfTheMinimumExtent()
    {
      var plan = FitPlanner.Plan(new Extents3(0, 0, 0), EqualLayout(), 1.1, 1.0);

      Assert.True(plan.UsedMinimumExtent);
      Approx.Equal(0.00275, plan.UnitsPerPixel, "shared scale");
      Assert.Equal(OrthoView.Top, plan.LimitingView);
      Frame(plan, OrthoView.Top, 2.2, 1.1, 0.00275, true);
      Frame(plan, OrthoView.Front, 2.2, 1.1, 0.00275, true);
      Frame(plan, OrthoView.Right, 2.2, 1.1, 0.00275, true);
    }

    [Fact]
    public void MinimumExtentIsUnusedWhenTheModelHasSize()
    {
      var plan = FitPlanner.Plan(Plate, EqualLayout(), 1.1, 1000.0);

      Assert.False(plan.UsedMinimumExtent);
      Approx.Equal(0.0825, plan.UnitsPerPixel, "shared scale");
    }

    [Fact]
    public void BorderOfOneFillsTheLimitingView()
    {
      var plan = FitPlanner.Plan(Plate, EqualLayout(), 1.0, 1.0);

      Approx.Equal(0.075, plan.UnitsPerPixel, "shared scale");
      Frame(plan, OrthoView.Top, 60, 30, 0.075, true);
      Frame(plan, OrthoView.Front, 60, 30, 0.075, true);
      Frame(plan, OrthoView.Right, 60, 30, 0.0375, false);
    }

    [Fact]
    public void QuarterPaddingWidensEveryFrame()
    {
      var plan = FitPlanner.Plan(Plate, EqualLayout(), 1.25, 1.0);

      Approx.Equal(0.09375, plan.UnitsPerPixel, "shared scale");
      Frame(plan, OrthoView.Top, 75, 37.5, 0.09375, true);
      Frame(plan, OrthoView.Front, 75, 37.5, 0.09375, true);
      Frame(plan, OrthoView.Right, 75, 37.5, 0.046875, false);
    }

    [Fact]
    public void InvariantsHoldForManyShapesAndLayouts()
    {
      var random = new Random(4404);
      for (int run = 0; run < 2000; run++)
      {
        var extents = new Extents3(Size(random), Size(random), Size(random));
        int sharedWidth = random.Next(1, 4000);
        int sharedHeight = random.Next(1, 4000);
        var layout = Layout(
          sharedWidth, random.Next(1, 4000),
          sharedWidth, sharedHeight,
          random.Next(1, 4000), sharedHeight);
        double border = 1.0 + random.NextDouble();
        string label = "run " + run;

        var plan = FitPlanner.Plan(extents, layout, border, 1.0);

        Assert.True(plan.UnitsPerPixel > 0.0 && double.IsFinite(plan.UnitsPerPixel), label + ": shared scale");
        Assert.Equal(plan.FrameFor(OrthoView.Top).Width, plan.FrameFor(OrthoView.Front).Width);
        Assert.Equal(plan.FrameFor(OrthoView.Front).Height, plan.FrameFor(OrthoView.Right).Height);

        var fitted = plan.UsedMinimumExtent ? new Extents3(1, 1, 1) : extents;
        OrthoView? firstLimiting = null;
        foreach (var frame in plan.Frames)
        {
          var pixels = layout[frame.View];
          Assert.Equal(plan.UnitsPerPixel * pixels.Width, frame.Width);
          Assert.Equal(plan.UnitsPerPixel * pixels.Height, frame.Height);
          Assert.True(frame.RequiredUnitsPerPixel <= plan.UnitsPerPixel, label + ": " + frame.View + " needs more than the shared scale");

          var (across, up) = FitPlanner.Projected(frame.View, fitted);
          Assert.True(frame.Width >= across * border * (1.0 - Approx.Relative), label + ": " + frame.View + " frame is narrower than the model");
          Assert.True(frame.Height >= up * border * (1.0 - Approx.Relative), label + ": " + frame.View + " frame is shorter than the model");

          if (frame.Limiting && firstLimiting == null)
            firstLimiting = frame.View;
        }
        Assert.Equal(firstLimiting, plan.LimitingView);
      }
    }

    // A third of the sizes are zero, so flat parts, lines and points come up too.
    static double Size(Random random) => random.Next(3) == 0 ? 0.0 : random.NextDouble() * 1000.0;
  }
}
