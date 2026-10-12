using Rhino;
using Rhino.Commands;

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
      RhinoApp.WriteLine("44View: not implemented yet");
      return Result.Success;
    }
  }
}
