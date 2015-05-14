# Open source VetCompass web services client library
This project is a client side API to facilitate consumption of the VetCompass clinical coding web services.  We currently provide a client library for :

* .net 4.5
* .net 3.5

If you are using a different technology on your client you will still be able to consume the web services via http calls using a 3rd library suitable for your platform.

It is made available under the permissive [MIT licence](LICENSE)
## A brief note on the web services
The VetCompass web services support the process of [clinical coding](http://en.wikipedia.org/wiki/Clinical_coder) in the context of veterinary clinics. The web services uses the veterinary-specific [VeNom codes](http://www.venomcoding.org/).  The API works as a web service which can be consumed by 3rd parties who want to enable clinical coding in their applications. Implementors wire up a text box in their system which takes a user’s search input as a string, and send the string to the web service.  The web service sends back an ranked list of codes which are most likely to be the codes that they are looking for.  The user chooses from the initial list, or amends their query to find a different list of codes to choose from.  

The service runs as a set of RESTful web services with JSON as the format of the messages.  Any client technology that can use an HTTP stack can consume the web service (for instance winforms, WPF, client-side javascript, pretty much any technology stack).  

The API has only 3 methods:

* Create a query session
* Execute a query
* Register a clinical code selection

# How to use the VetCompass client library

* Add a reference via nuget.  The package is called VetCompassClient
* TODO 

# A note on developing new versions of the VetCompass client library
*This section is mainly for developers wishing to make changes/upgrades to the library*

The VetCompassClient targets two versions of .net: 3.5 & 4.5.  There are two projects, one for each version of the framework.  The code base is the same and compiler directives are used where necessary to differentiate different frameworks.  The methodololgy was based on [this accepted answer](http://stackoverflow.com/questions/2923210/conditional-compilation-and-framework-targets) on stackoverflow.

VetCompassClient makes use of MS's task library.  This is part of the base classes in .net 4.5 but not in 3.5.  Someone has [back ported](https://www.nuget.org/packages/TaskParallelLibrary/) most of the tasks library into 3.5.  This is a dependency in the 3.5 version but not in 4.5.

Both versions have a dependency is [Json.net](http://www.newtonsoft.com/json) which is fully internalised using the /internalize switch.  

Both dependencies are IlMerged into a single dll, one for 3.5 and 1 for 4.5

## IlMerge

The build process ilmerges merges in some dependencies.  The JSON serialisor (NewtonSoft.JSON) is additionally internalised (/internalize).  This is to avoid version clashes if a 3rd party has a dependency on a different version of the dll. In the .net 3.5 version, the dependency on the tasks library is merged but not internalised.  This is because the types in the library are part of the API.

You need to install [IlMerge](http://www.microsoft.com/en-gb/download/details.aspx?id=17630) to compile the sln.

The msbuild files were altered using [Hanselman's method](http://www.hanselman.com/blog/MixingLanguagesInASingleAssemblyInVisualStudioSeamlesslyWithILMergeAndMSBuild.aspx)

## How to publish the nuget package 

* There is a nuget nuspec at clr/. Check the version & any new dependencies etc
* Set sln to Release
* Compile (this produces the dlls in the place that the nuspec expects)
* Create the .nupkg file. I used [the nuget gui](https://docs.nuget.org/create/using-a-gui-to-build-packages#nuget-package-explorer---gui-tool-for-building-packages)
* Publish to nuget (File..Publish)

http://stackoverflow.com/questions/2923210/conditional-compilation-and-framework-targets