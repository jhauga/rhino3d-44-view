using System.Collections.Generic;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace FourFourView.Core.Runtime
{
  /// <summary>The bounding box of the objects 44View fits, and which objects they were.</summary>
  public readonly struct MeasuredExtents
  {
    public MeasuredExtents(BoundingBox box, int count, FitSource source)
    {
      Box = box;
      Count = count;
      Source = source;
    }

    /// <summary>World bounding box of the measured objects; invalid when there were none.</summary>
    public BoundingBox Box { get; }

    /// <summary>How many objects contributed to the box.</summary>
    public int Count { get; }

    /// <summary>The source actually used: Visible or Selected, never Auto.</summary>
    public FitSource Source { get; }

    /// <summary>True when there is nothing to fit.</summary>
    public bool IsEmpty => Count == 0 || !Box.IsValid;
  }

  /// <summary>Measures the model the way the four views should frame it.</summary>
  public static class ModelExtents
  {
    // Blocks nested deeper than this are skipped rather than followed indefinitely.
    const int MaxBlockDepth = 32;

    /// <summary>
    /// Measures the selected objects or every visible model-space object. Auto picks the
    /// selection when there is one. Lights, clipping planes, grips and layout objects
    /// never count.
    /// </summary>
    public static MeasuredExtents Measure(RhinoDoc doc, FitSource source)
    {
      var selected = new List<RhinoObject>(doc.Objects.GetSelectedObjects(false, false));
      if (source == FitSource.Auto)
        source = selected.Count > 0 ? FitSource.Selected : FitSource.Visible;

      IEnumerable<RhinoObject> objects = source == FitSource.Selected
        ? selected
        : doc.Objects.GetObjectList(VisibleObjects());

      var box = BoundingBox.Empty;
      int count = 0;
      foreach (var obj in objects)
      {
        if (obj == null || IsIgnored(obj))
          continue;

        var objectBox = BoxOf(obj);
        if (!objectBox.IsValid)
          continue;

        box = box.IsValid ? BoundingBox.Union(box, objectBox) : objectBox;
        count++;
      }
      return new MeasuredExtents(box, count, source);
    }

    static ObjectEnumeratorSettings VisibleObjects() => new ObjectEnumeratorSettings
    {
      ActiveObjects = true,
      ReferenceObjects = true,
      NormalObjects = true,
      LockedObjects = true,
      HiddenObjects = false,
      VisibleFilter = true,
      IncludeLights = false,
      IncludeGrips = false,
      IncludePhantoms = false,
      DeletedObjects = false,
    };

    static bool IsIgnored(RhinoObject obj)
    {
      switch (obj.ObjectType)
      {
        case ObjectType.ClipPlane:
        case ObjectType.Light:
        case ObjectType.Grip:
        case ObjectType.Phantom:
        case ObjectType.Detail:
          return true;
      }
      return obj.Attributes.Space != ActiveSpace.ModelSpace;
    }

    static BoundingBox BoxOf(RhinoObject obj)
    {
      var box = obj.Geometry?.GetBoundingBox(true) ?? BoundingBox.Empty;
      if (!box.IsValid && obj is InstanceObject instance)
        box = InstanceBox(instance, Transform.Identity, 0);
      return box;
    }

    static BoundingBox InstanceBox(InstanceObject instance, Transform parent, int depth)
    {
      var box = BoundingBox.Empty;
      var definition = instance.InstanceDefinition;
      if (definition == null || depth > MaxBlockDepth)
        return box;

      var xform = parent * instance.InstanceXform;
      foreach (var member in definition.GetObjects())
      {
        var memberBox = member is InstanceObject nested
          ? InstanceBox(nested, xform, depth + 1)
          : member.Geometry?.GetBoundingBox(xform) ?? BoundingBox.Empty;
        if (memberBox.IsValid)
          box = box.IsValid ? BoundingBox.Union(box, memberBox) : memberBox;
      }
      return box;
    }
  }
}
