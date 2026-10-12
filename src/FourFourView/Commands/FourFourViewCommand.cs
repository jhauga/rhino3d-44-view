using System;
using FourFourView.Core;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace FourFourView.Commands
{
  /// <summary>
  /// Restores the four default viewports, fits the model in each at one shared scale and
  /// saves the four cameras as named views.
  /// </summary>
  public class FourFourViewCommand : Command
  {
    public FourFourViewCommand() { Instance = this; }

    /// <summary>The only instance of this command.</summary>
    public static FourFourViewCommand Instance { get; private set; }

    /// <summary>The command name as typed on the Rhino command line.</summary>
    public override string EnglishName => "44View";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      var settings = FourFourViewPlugin.Instance.Settings;
      var options = FitOptions.Load(settings);

      // The plain command runs at once with the saved options. Typed as -44View, or run
      // from a macro, it offers the options first and saves what was chosen.
      if (mode == RunMode.Scripted)
      {
        var answer = AskForOptions(options);
        if (answer != Result.Success)
          return answer;
        options.Save(settings);
      }

      var report = FourFourViewRunner.Run(doc, options);
      FourFourViewPlugin.Instance.LastReport = report;
      RhinoApp.WriteLine(report.Message);
      return report.Result;
    }

    static Result AskForOptions(FitOptions options)
    {
      var get = new GetOption();
      get.SetCommandPrompt("Press Enter to fit the four views");
      get.AcceptNothing(true);

      string[] sources = Enum.GetNames(typeof(FitSource));
      var padding = new OptionDouble(options.Padding, FitOptions.MinPadding, FitOptions.MaxPadding);
      var saveViews = new OptionToggle(options.SaveViews, "No", "Yes");
      var layout = new OptionToggle(options.Layout, "No", "Yes");

      while (true)
      {
        get.ClearCommandOptions();
        int sourceIndex = get.AddOptionList("Source", sources, (int)options.Source);
        get.AddOptionDouble("Padding", ref padding);
        get.AddOptionToggle("SaveViews", ref saveViews);
        get.AddOptionToggle("Layout", ref layout);

        var result = get.Get();
        if (result == GetResult.Option)
        {
          if (get.OptionIndex() == sourceIndex)
            options.Source = (FitSource)get.Option().CurrentListOptionIndex;
          continue;
        }
        if (result != GetResult.Nothing)
          return Result.Cancel;
        break;
      }

      options.Padding = padding.CurrentValue;
      options.SaveViews = saveViews.CurrentValue;
      options.Layout = layout.CurrentValue;
      return Result.Success;
    }
  }
}
