# VetCompassClient
Public client side code for VetCompass

# A note on building

The VetCompassClient targets two versions of .net: 3.5 & 4.5.  There are two projects, one for each version of the framework.  The code base is the same and compiler directives are used where necessary to differentiate different frameworks.  VetCompassClient makes use of MS's task library.  This is part of the base classes in .net 4.5 but not in 3.5.  Someone has [back ported](https://www.nuget.org/packages/TaskParallelLibrary/) most of the tasks library into 3.5.  This is a dependency in the 3.5 version but not in 4.5.

An additional dependency is on [Json.net](http://www.newtonsoft.com/json).  

Both dependencies are IlMerged into a single dll, one for 3.5 and 1 for 4.5

## IlMerge

The build process ilmerges merges in some dependencies.  The JSON serialisor (NewtonSoft.JSON) is additionally internalised (/internalize).  This is to avoid version clashes if a 3rd party has a dependency on a different version of the dll. In the .net 3.5 version, the dependency on the tasks library is merged but not internalised.  This is because the types in the library are part of the API.

You need to install [IlMerge](http://www.microsoft.com/en-gb/download/details.aspx?id=17630) to compile the sln.

TODO : how to install via Nuget 