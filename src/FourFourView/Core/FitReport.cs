using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using FourFourView.Core.Fit;
using Rhino.Commands;
using Rhino.Geometry;

namespace FourFourView.Core
{
  /// <summary>What one 44View run did, for the command line and for scripts.</summary>
  public sealed class FitReport
  {
    /// <summary>True when the four views were fitted.</summary>
    public bool Fitted { get; internal set; }

    /// <summary>The command result the run maps to.</summary>
    public Result Result { get; internal set; } = Result.Nothing;

    /// <summary>The objects that were measured: Visible or Selected, never Auto.</summary>
    public FitSource Source { get; internal set; } = FitSource.Visible;

    /// <summary>How many objects the box was measured from.</summary>
    public int ObjectCount { get; internal set; }

    /// <summary>The world bounding box of the measured objects.</summary>
    public BoundingBox Box { get; internal set; } = BoundingBox.Empty;

    /// <summary>Abbreviation of the model's unit system, such as mm.</summary>
    public string Units { get; internal set; } = string.Empty;

    /// <summary>The padding the run used, in percent.</summary>
    public double Padding { get; internal set; }

    /// <summary>The shared scale and frames; null when nothing was planned.</summary>
    public FitPlan Plan { get; internal set; }

    /// <summary>True when the four default viewports were restored first.</summary>
    public bool LayoutApplied { get; internal set; }

    /// <summary>True when all four named views were saved.</summary>
    public bool NamedViewsSaved { get; internal set; }

    /// <summary>Things that went wrong without stopping the fit.</summary>
    public List<string> Warnings { get; } = new List<string>();

    /// <summary>The line printed to the command line.</summary>
    public string Message { get; internal set; } = string.Empty;

    /// <summary>A report for a request refused before anything was measured.</summary>
    public static FitReport Rejected(string message)
      => new FitReport { Result = Result.Failure, Message = message };

    /// <summary>The report as one line of JSON with invariant number formatting.</summary>
    public string ToJson()
    {
      using var stream = new MemoryStream();
      using (var json = new Utf8JsonWriter(stream))
      {
        json.WriteStartObject();
        json.WriteBoolean("fitted", Fitted);
        json.WriteString("result", Result.ToString());
        json.WriteString("source", Source.ToString().ToLowerInvariant());
        json.WriteNumber("objectCount", ObjectCount);

        json.WritePropertyName("box");
        if (Box.IsValid)
        {
          json.WriteStartArray();
          foreach (double value in new[] { Box.Min.X, Box.Min.Y, Box.Min.Z, Box.Max.X, Box.Max.Y, Box.Max.Z })
            json.WriteNumberValue(value);
          json.WriteEndArray();
        }
        else
        {
          json.WriteNullValue();
        }

        json.WriteString("units", Units);
        json.WriteNumber("padding", Padding);

        if (Plan != null)
        {
          json.WriteNumber("unitsPerPixel", Plan.UnitsPerPixel);
          json.WriteString("limitingView", Plan.LimitingView.ToString());
          json.WriteBoolean("usedMinimumExtent", Plan.UsedMinimumExtent);
        }
        else
        {
          json.WriteNull("unitsPerPixel");
          json.WriteNull("limitingView");
          json.WriteBoolean("usedMinimumExtent", false);
        }

        json.WriteStartArray("frames");
        if (Plan != null)
        {
          foreach (var frame in Plan.Frames)
          {
            json.WriteStartObject();
            json.WriteString("view", frame.View.ToString());
            json.WriteNumber("width", frame.Width);
            json.WriteNumber("height", frame.Height);
            json.WriteNumber("requiredUnitsPerPixel", frame.RequiredUnitsPerPixel);
            json.WriteBoolean("limiting", frame.Limiting);
            json.WriteEndObject();
          }
        }
        json.WriteEndArray();

        json.WriteBoolean("layoutApplied", LayoutApplied);
        json.WriteBoolean("namedViewsSaved", NamedViewsSaved);
        json.WriteStartArray("warnings");
        foreach (var warning in Warnings)
          json.WriteStringValue(warning);
        json.WriteEndArray();
        json.WriteString("message", Message);
        json.WriteEndObject();
      }
      return Encoding.UTF8.GetString(stream.ToArray());
    }
  }
}
