using Rhino.PlugIns;

namespace FourFourView
{
  /// <summary>
  /// Hosts the 44View command. Rhino creates the single instance and loads the plug-in
  /// the first time the command runs, so there is nothing to register at startup.
  /// </summary>
  public class FourFourViewPlugin : PlugIn
  {
    public FourFourViewPlugin() { Instance = this; }

    /// <summary>The only instance of the plug-in.</summary>
    public static FourFourViewPlugin Instance { get; private set; }
  }
}
