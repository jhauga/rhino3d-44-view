using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using FourFourView.Core;
using Rhino;

namespace FourFourView
{
  /// <summary>
  /// The scripting surface returned by PlugIn.GetPlugInObject, reachable from Python as
  /// rs.GetPlugInObject("FourFourView"). Every method works on the active document, prints
  /// the 44View summary line, and never shows a dialog.
  /// </summary>
  [ComVisible(true)]
  public class FourFourViewScriptObject
  {
    /// <summary>Plugin version, for callers that want to check compatibility.</summary>
    public string Version => typeof(FourFourViewScriptObject).Assembly
      .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
      .OfType<AssemblyInformationalVersionAttribute>()
      .Select(a => a.InformationalVersion)
      .FirstOrDefault() ?? "0.0.0";

    /// <summary>JSON describing the most recent run, from the command or a script; empty before the first run.</summary>
    public string LastReport => FourFourViewPlugin.Instance?.LastReport?.ToJson() ?? string.Empty;

    /// <summary>Fits the four views with the saved options. Returns true when the views were fitted.</summary>
    public bool Fit() => Run(FitOptions.Load(FourFourViewPlugin.Instance.Settings));

    /// <summary>
    /// Fits the four views with the given options without saving them. Source is Auto,
    /// Visible or Selected; padding is a percent from 0 to 100. Returns true when the views
    /// were fitted.
    /// </summary>
    public bool FitWith(string source, double padding, bool saveViews, bool layout)
    {
      if (!FitOptions.TryParseSource(source, out var parsed))
        return Reject("44View: source must be Auto, Visible or Selected.");
      if (double.IsNaN(padding) || padding < FitOptions.MinPadding || padding > FitOptions.MaxPadding)
        return Reject("44View: padding must be from 0 to 100.");

      return Run(new FitOptions { Source = parsed, Padding = padding, SaveViews = saveViews, Layout = layout });
    }

    static bool Run(FitOptions options)
    {
      var doc = RhinoDoc.ActiveDoc;
      if (doc == null)
        return Reject("44View: no document is open.");

      var report = FourFourViewRunner.Run(doc, options);
      FourFourViewPlugin.Instance.LastReport = report;
      RhinoApp.WriteLine(report.Message);
      return report.Fitted;
    }

    static bool Reject(string message)
    {
      FourFourViewPlugin.Instance.LastReport = FitReport.Rejected(message);
      RhinoApp.WriteLine(message);
      return false;
    }
  }
}
