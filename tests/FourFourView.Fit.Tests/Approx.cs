using System;
using System.Globalization;
using Xunit;

namespace FourFourView.Fit.Tests
{
  /// <summary>Floating-point comparison for values computed with a handful of operations.</summary>
  internal static class Approx
  {
    /// <summary>Allowed difference, relative to the expected value.</summary>
    public const double Relative = 1e-12;

    public static void Equal(double expected, double actual, string what)
    {
      bool close = expected == actual || Math.Abs(expected - actual) <= Relative * Math.Abs(expected);
      Assert.True(close, what + ": expected " + Text(expected) + ", got " + Text(actual));
    }

    static string Text(double value) => value.ToString("R", CultureInfo.InvariantCulture);
  }
}
