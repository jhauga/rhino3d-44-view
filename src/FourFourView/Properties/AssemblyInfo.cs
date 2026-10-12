using Rhino.PlugIns;
using System.Reflection;
using System.Runtime.InteropServices;

// Shown in Rhino under Options > Plug-ins > FourFourView > Details.
[assembly: PlugInDescription(DescriptionType.Organization, "John Haugabook")]
[assembly: PlugInDescription(DescriptionType.WebSite, "https://github.com/jhauga/rhino3d-44-view")]

[assembly: AssemblyTitle("FourFourView")]
[assembly: AssemblyDescription("Restores the four default viewports, fits the model in each at one shared scale so the grids line up, and saves the four cameras as named views.")]
[assembly: AssemblyCompany("John Haugabook")]
[assembly: AssemblyProduct("44 View")]
[assembly: AssemblyCopyright("Copyright 2026 John Haugabook")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

// ===========================================================================
// THIS GUID IS THE PLUG-IN ID. Minted once for this plugin; never change it
// after release and never copy it into another project.
// ===========================================================================
[assembly: Guid("6a5e29e1-b3a2-4b20-8f9a-d2489db3ed76")]

// yak spec reads AssemblyInformationalVersion first, then AssemblyVersion.
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0-alpha.0.0.0")]
