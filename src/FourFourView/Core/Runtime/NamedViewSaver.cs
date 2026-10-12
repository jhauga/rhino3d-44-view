using System.Collections.Generic;
using Rhino;
using Rhino.Display;

namespace FourFourView.Core.Runtime
{
  /// <summary>Saves the four fitted cameras as named views.</summary>
  public static class NamedViewSaver
  {
    /// <summary>
    /// Saves Perspective, Top, Front and Right as named views of the same names, then shows
    /// each named view in its own viewport, which links the two so the viewport title reads
    /// the plain name. A named view that already has one of those names is replaced, so
    /// repeated runs keep the list at four entries. Returns true when all four were saved.
    /// </summary>
    public static bool Save(RhinoDoc doc, FourViews views, ICollection<string> warnings)
    {
      bool saved = SaveOne(doc, views.Perspective, "Perspective", warnings);
      saved &= SaveOne(doc, views.Top, "Top", warnings);
      saved &= SaveOne(doc, views.Front, "Front", warnings);
      saved &= SaveOne(doc, views.Right, "Right", warnings);
      return saved;
    }

    static bool SaveOne(RhinoDoc doc, RhinoView view, string name, ICollection<string> warnings)
    {
      int index = doc.NamedViews.Add(name, view.MainViewport.Id);
      if (index >= 0)
      {
        // The camera is the one just saved, so restoring it changes nothing on screen.
        doc.NamedViews.Restore(index, view.MainViewport);
        return true;
      }

      warnings.Add("the " + name + " named view could not be saved");
      return false;
    }
  }
}
