# VetCompass web services client library
This project is a client side API to facilitate consumption of the VetCompass clinical coding web services.  We currently provide a client library for :

* .net 4.5
* .net 3.5

If you are using a different technology you will still be able to consume the web services.  They are exposed via HTTP so you can simply use any library which can make HTTP calls.  This page contains documentation on the underlying protocol for the web services which is relevent for all clients.

This client library is made available under the permissive [MIT licence](LICENSE)
## A brief note on the web services
The VetCompass web services support the process of [clinical coding](http://en.wikipedia.org/wiki/Clinical_coder) in the context of veterinary clinics. The web services make it easier for clinicians to find clinical codes because it has learned how vets refer to diseases.  Veterinary clinical concepts are described by different phrases which mean the same thing.  Also, vets use acronyms and frequently miss-type words. The web service knows how to map these references to their underlying meaning in the codes.  88% of queries result in a code being selection.  A typical search duration is 3 seconds.


The web services uses the veterinary-specific [VeNom coding set](http://www.venomcoding.org/).  The web services are consumed by 3rd parties who want to enable clinical coding in their applications. Implementors take a user’s search input as a string, and send the string to the web service.  The web service sends back a list of codes which are most likely to be the codes that they are looking for.  The user chooses from this list, or amends their query to find a new list of codes to choose from.  The web service is fast; it is designed to be queried on each key-press.

The service runs in a RESTful manner with JSON as the format of the messages.  Any client technology that can use an HTTP stack can consume the web service (for instance winforms, client-side javascript, PHP, Java ... pretty much every technology stack).  

## Webservices documentation
This section is aimed at non .Net users. There is a .Net client library (see below)

The API has only 3 methods:

* Create a query session
* Execute a query
* Register a clinical code selection

### Create a session
```
POST https://vetcompass.herokuapp.com/api/1.0/session/{Session UUID}
```
{Session UUID} is a client generated [unique identifier](https://en.wikipedia.org/wiki/Universally_unique_identifier) (AKA guid) for your session.  

The session UUID groups API calls into a session or unit of work.  A session scope begins when the user starts to query for VeNom codes in your app.  A session scope ends when a user selects a code in your application and you call to register that code selection.  If the user abandons the session in your application, the session is ended implicitly with no such call.  If the user wants to code multiple codes, create multiple sessions. Sessions are cheap and fast, and a typical session lasts 3 seconds with a user selecting a Venom code.


The body of the request is a JSON object describing the signalment of the patient which is being coded.  This signalment is needed to better predict codes based on species, age, etc.  The CaseNumber is needed for your possible future usage. If the application context of the coding session is not a particular patient (ie looking up a breed name for a query), simply leave the signalment values blank (they are all optional).

Example usage:

```
POST https://vetcompass.herokuapp.com/api/1.0/session/7e79ada4-c2ff-4e77-a1c0-f6bb3f96a005
```
```json
BODY :
{
	"CaseNumber":"498796",
	"VeNomBreedCode":null,
	"BreedName":"Doberman",
	"VeNomSpeciesCode"15461,
	"SpeciesName":"Canine",
	"IsFemale":false,
	"IsNeutered":false,
	"ApproximateDateOfBirth":"2013-01-05T00:00:00",
	"PartialPostCode":"AB12 3"
}
```

### Execute a query

```
GET https://vetcompass.herokuapp.com/api/1.0/session/{Session UUID}/search/{URL escaped query}
```

{URL escaped query} is the string that your user typed into a text box in your application.  The string must be [URL encoded](http://www.w3schools.com/tags/ref_urlencode.asp).



# How to use the VetCompass client library
It's easy to use:
```csharp
ICodingSessionFactory client = new CodingSessionFactory(clientId, sharedSecret, new Uri("https://vetcompass.herokuapp.com/api/1.0/session/"));
ICodingSession session = client.StartCodingSession(new CodingSubject { CaseNumber = "fluffy01",IsFemale = true,VeNomSpeciesCode = 1232}, timeoutMilliseconds:700);
Task<VeNomQueryResponse> futureResults = session.QueryAsync(new VeNomQuery("hit by car"));
```

* Add a reference to the client library via nuget.  The package is called VetCompassClient
* The asynchronous call returns a Task&lt;VeNomQueryResponse&gt;
    * The task will fault on error and this means the CodingSession should be abandoned
    * If the task is cancelled, then a timeout occurred.  You can adjust the timeout via the Timeout property on the CodingSession
    * If your Start call timed out, you will need to create a new CodingSession
* When your end-user has selected a code, remember to call RegisterSelection. The web service relies on these calls to learn the terms that your users use
* In order to authenticate with the web services you will need to arrange a shared secret & clientId Guid with the VetCompass developers
* Contact RVC to generate these details.  This is necessary in order to authenticate your client (which is done via [HMAC](http://www.thebuzzmedia.com/designing-a-secure-rest-api-without-oauth-authentication) )  It is possible to use the webservice without authenticating, but future functionality will only be available to authenticated users.
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