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
It's easy to use:
```csharp
ICodingSessionFactory client = new CodingSessionFactory(clientId, sharedSecret, new Uri("https://vetcompass.herokuapp.com/api/1.0/session/"));
ICodingSession session = client.StartCodingSession(new CodingSubject { CaseNumber = "fluffy01",IsFemale = true,VeNomSpeciesCode = 1232}, timeoutMilliseconds:700);
Task<VeNomQueryResponse> futureResults = session.QueryAsync(new VeNomQuery("hit by car"));
```
* You need to arrange a shared secret & clientId Guid with the VetCompass developers
* Contact RVC to generate these details.  This is necessary in order to authenticate your client (which is done via [HMAC](http://www.thebuzzmedia.com/designing-a-secure-rest-api-without-oauth-authentication) )
* Add a reference to the client library via nuget.  The package is called VetCompassClient
* When your end-user has selected a code, remember to call RegisterSelection. The web service relies on these calls to learn the terms that your users use
* The asynchronous call returns a Task&lt;VeNomQueryResponse&gt;
    * The task will fault on error and this means the CodingSession should be abandoned
    * If the task is cancelled, then a timeout occurred.  You can adjust the timeout via the Timeout property on the CodingSession
    * If your Start call timed out, you will need to create a new CodingSession
* Prefer the asynchronous method in winforms/WPF application which will keep your UI responsive  
* There is an example winforms project which shows how to synchronise back to the UI thread

# A note on developing new versions of the VetCompass client library
*This section is mainly for developers wishing to make changes/upgrades to the library*

The VetCompassClient targets two versions of .net: 3.5 & 4.5.  There are two projects, one for each version of the framework.  The code base is the same and compiler directives are used where necessary to differentiate different frameworks.  The methodology was based on [this accepted answer](http://stackoverflow.com/questions/2923210/conditional-compilation-and-framework-targets) on StackOverflow.

VetCompassClient makes use of MS's task library.  This is part of the base classes in .net 4.5 but not in 3.5.  Someone has [back ported](https://www.nuget.org/packages/TaskParallelLibrary/) most of the tasks library into 3.5.  This is a dependency in the 3.5 version but not in 4.5.

Both versions have a dependency on [Json.net](http://www.newtonsoft.com/json) which is fully internalised using the /internalize switch.  

All dependencies are IlMerged into a single dll as part of the compilation process. The produces 2 dlls; one for .net 3.5 and one for .net 4.5

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