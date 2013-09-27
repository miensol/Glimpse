﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Glimpse.Core.Extensibility;

[assembly: ComVisible(false)]
[assembly: Guid("63826849-ecbe-4abd-afa1-16b6ce461795")]


[assembly: AssemblyTitle("Glimpse for ASP.NET 3.5 Assembly")]
[assembly: AssemblyDescription("Main extensibility implementations for running Glimpse with ASP.NET 3.5.")]
[assembly: AssemblyProduct("Glimpse")]
[assembly: AssemblyCopyright("© 2012 Nik Molnar & Anthony van der Hoorn")]
[assembly: AssemblyTrademark("Glimpse™")]


// Version is in major.minor.build format to support http://semver.org/
// Keep these three attributes in sync
[assembly: AssemblyVersion("1.4.1")]
[assembly: AssemblyFileVersion("1.4.1")]
[assembly: AssemblyInformationalVersion("1.4.1")]

[assembly: InternalsVisibleTo("Glimpse.Test.AspNet")]
[assembly: NuGetPackage("Glimpse.AspNet")]